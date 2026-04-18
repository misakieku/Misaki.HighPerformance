using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

public struct JobSchedulerDesc
{
    public int ThreadCount
    {
        get; set;
    }

    public ThreadPriority ThreadPriority
    {
        get; set;
    }

    public object? State
    {
        get; set;
    }
}

/// <summary>
/// Provides a mechanism for scheduling and executing jobs across multiple worker threads.
/// </summary>
public sealed unsafe partial class JobScheduler : IJobScheduler, IDisposable
{
    // Don't sleep indefinitely because that causes our 1ms job to become 15ms.
    private const int _SLEEP_THRESHOLD = -1;

    private FreeList _freeList;

    private readonly ConcurrentSlotMap<JobInfo> _jobInfoPool;
    private readonly ConcurrentQueue<JobHandle>[] _jobQueues;
    private readonly WorkerThread[] _workerThreads;

    private readonly SemaphoreSlim _workSignal;
    private readonly CancellationTokenSource _cts;

    private readonly object? _state;

    private bool _disposed = false;

    internal object? State => _state;
    internal bool IsCancellationRequested => _cts.IsCancellationRequested;

    public int WorkerCount => _workerThreads.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class with the specified description.
    /// </summary>
    /// <param name="desc">The description for the job scheduler.</param>
    public JobScheduler(ref readonly JobSchedulerDesc desc)
    {
        var workerCount = Math.Max(1, desc.ThreadCount);

        _freeList = new FreeList(MemoryUtility.AlignOf<IntPtr>(), maxConcurrencyLevel: workerCount);

        _jobInfoPool = new ConcurrentSlotMap<JobInfo>(128);
        _jobQueues = new ConcurrentQueue<JobHandle>[3];

        for (var i = 0; i < 3; i++)
        {
            _jobQueues[i] = new ConcurrentQueue<JobHandle>();
        }

        _workSignal = new SemaphoreSlim(0);
        _cts = new CancellationTokenSource();

        _state = desc.State;

        _workerThreads = new WorkerThread[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            _workerThreads[i] = new WorkerThread(i, this, desc.ThreadPriority);
        }

        foreach (var worker in _workerThreads)
        {
            worker.Start();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class with the specified number of worker threads.
    /// </summary>
    /// <param name="threadCount">The number of worker threads to create. If less than 1, at least one thread will be created.</param>
    /// <param name="priority">The priority of the worker threads.</param>
    /// <param name="state">The state object for the job scheduler.</param>
    /// <param name="allowManagedJobs">A value indicating whether managed jobs are allowed.</param>
    [Obsolete("Use JobScheduler(JobSchedulerDesc) instead.")]
    public JobScheduler(int threadCount, ThreadPriority priority = ThreadPriority.Normal, object? state = null, bool allowManagedJobs = false)
    {
        var workerCount = Math.Max(1, threadCount);

        _freeList = new FreeList(MemoryUtility.AlignOf<IntPtr>(), maxConcurrencyLevel: workerCount);

        _jobInfoPool = new ConcurrentSlotMap<JobInfo>(128);
        _jobQueues = new ConcurrentQueue<JobHandle>[3];

        for (var i = 0; i < 3; i++)
        {
            _jobQueues[i] = new ConcurrentQueue<JobHandle>();
        }

        _workSignal = new SemaphoreSlim(0);
        _cts = new CancellationTokenSource();

        _state = state;

        _workerThreads = new WorkerThread[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            _workerThreads[i] = new WorkerThread(i, this, priority);
        }

        foreach (var worker in _workerThreads)
        {
            worker.Start();
        }
    }

    ~JobScheduler()
    {
        Dispose();
    }

    private void EnqueueJobIfReady(JobHandle handle, int threadIndex)
    {
        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);

        if (exist && Volatile.Read(ref jobInfo.dependencyCount) == 0)
        {
            // Note: JobState.Created is 0, JobState.Scheduled is 1. We assume RC logic doesn't touch initial state (RC=0).
            if (Interlocked.CompareExchange(ref jobInfo.state, JobUtility.JOBSTATE_SCHEDULED, JobUtility.JOBSTATE_CREATED) != JobUtility.JOBSTATE_CREATED)
            {
                return;
            }

            var tier = (int)jobInfo.priority;
            ConcurrentQueue<JobHandle> jobQueue;
            if (jobInfo.threadIndex >= 0 && jobInfo.threadIndex < _workerThreads.Length)
            {
                jobQueue = _workerThreads[jobInfo.threadIndex].LocalQueues[tier];
            }
            else if (threadIndex >= 0 && threadIndex < _workerThreads.Length)
            {
                // Put into the local thread queue if the scheduling thread is a worker thread. This can improve cache locality and reduce contention on the main queue.
                jobQueue = _workerThreads[threadIndex].LocalQueues[tier];
            }
            else
            {
                jobQueue = _jobQueues[tier];
            }

            // Ensure the count of this job handle won't exceed the number of worker threads.
            // Worker threads will steal parallel iteration ranges from each other.
            var handleCount = Math.Min(jobInfo.remainingBatches, _workerThreads.Length);

            for (var i = 0; i < handleCount; i++)
            {
                jobQueue.Enqueue(handle);
            }

            _workSignal.Release(handleCount);
        }
    }

    private JobHandle CreateJobHandle(ref JobInfo jobInfo, int threadIndex, params ReadOnlySpan<JobHandle> dependencies)
    {
        var validDepCount = 0;
        for (var i = 0; i < dependencies.Length; i++)
        {
            if (dependencies[i].IsValid)
            {
                validDepCount++;
            }
        }

        // Advance count to account for all dependencies upfront + 1 guard lock
        jobInfo.dependencyCount = validDepCount + 1;

        var id = _jobInfoPool.Add(jobInfo, out var generation);
        ref var infoInPool = ref _jobInfoPool.GetElementReferenceAt(id, generation, out _);

        var handle = new JobHandle(id, generation);

        for (var i = 0; i < dependencies.Length; i++)
        {
            var dependency = dependencies[i];
            if (!dependency.IsValid)
            {
                continue;
            }

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(dependency.ID, dependency.Generation, out var exist);
            if (!exist)
            {
                // Dependency does not exist (likely completed already)
                Interlocked.Decrement(ref infoInPool.dependencyCount);
                continue;
            }

            // Lock-free registration: Try to acquire "Reader Lock" by incrementing RC in high bits.
            // If state is already Completed, we skip (dependency met).
            var registered = false;
            var spin = new SpinWait();

            while (true)
            {
                var stateVal = Volatile.Read(ref depJobInfo.state);
                var state = JobUtility.GetState(stateVal);

                if (state == JobState.Completed)
                {
                    break;
                }

                // Attempt to increment RC (Reader Count)
                if (Interlocked.CompareExchange(ref depJobInfo.state, stateVal + JobUtility.RC_ONE, stateVal) == stateVal)
                {
                    // RC acquired. We are safe from "Remove" and state change.
                    var count = Interlocked.Increment(ref depJobInfo.dependentCount);
                    if (count <= JobInfo.MAX_DEPENDENTS)
                    {
                        // Safely write to the fixed buffer
                        depJobInfo.dependentsID[count - 1] = id;
                        depJobInfo.dependentsGeneration[count - 1] = generation;
                    }
                    else
                    {
                        if (!depJobInfo.additionalDependents.IsCreated)
                        {
                            depJobInfo.additionalDependents = new UnsafeList<JobHandle>(4, AllocationHandle.Persistent);
                        }

                        depJobInfo.additionalDependents.Add(handle);
                    }

                    registered = true;

                    // Release RC
                    Interlocked.Add(ref depJobInfo.state, -JobUtility.RC_ONE);

                    break;
                }

                spin.SpinOnce(-1);
            }

            // If we didn't successfully register (completed fast), drop it from the advanced counter
            if (!registered)
            {
                Interlocked.Decrement(ref infoInPool.dependencyCount);
            }
        }

        // Lower the initial 1 guard lock; Enqueue if met
        if (Interlocked.Decrement(ref infoInPool.dependencyCount) == 0)
        {
            EnqueueJobIfReady(handle, threadIndex);
        }

        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasWork()
    {
        for (var i = 0; i < _jobQueues.Length; i++)
        {
            if (!_jobQueues[i].IsEmpty)
            {
                return true;
            }
        }

        for (var i = 0; i < _workerThreads.Length; i++)
        {
            for (var j = 0; j < _workerThreads[i].LocalQueues.Length; j++)
            {
                if (!_workerThreads[i].LocalQueues[j].IsEmpty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WaitForWork(int timeout)
    {
        _workSignal.Wait(timeout, _cts.Token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryStealFromMain(int tier, out JobHandle outHandle)
    {
        return _jobQueues[tier].TryDequeue(out outHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryStealFromWorker(int threadIndex, int tier, out JobHandle outHandle)
    {
        return _workerThreads[threadIndex].LocalQueues[tier].TryDequeue(out outHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref JobInfo GetJobInfoReference(JobHandle handle, out bool exist)
    {
        if (!handle.IsValid)
        {
            exist = false;
            return ref Unsafe.NullRef<JobInfo>();
        }

        return ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out exist);
    }

    internal void MarkJobComplete(JobHandle handle, int threadIndex)
    {
        Debug.Assert(handle.IsValid);

        ref var info = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

        // Lock-free Completion:
        // 1. Transition State to Completed (preserving or setting upper bits?).
        //    Actually, we want to block new Readers. Setting state to Completed blocks new Readers.
        // 2. Wait for existing Readers (RC == 0).
        var spin = new SpinWait();
        while (true)
        {
            var stateVal = Volatile.Read(ref info.state);
            var state = JobUtility.GetState(stateVal);

            if (state == JobState.Completed)
            {
                return;
            }

            // Preserve upper bits (RC) and set state to Completed. This blocks new Readers.
            var newState = (stateVal & ~JobUtility.STATE_MASK) | (int)JobState.Completed;
            if (Interlocked.CompareExchange(ref info.state, newState, stateVal) == stateVal)
            {
                // Successfully set State to Completed. New readers will see Completed and back off.
                // Now we must wait for existing readers to finish (RC to become 0).
                while (true)
                {
                    var current = Volatile.Read(ref info.state);
                    if (((uint)current >> 16) == 0)
                    {
                        break; // RC is 0. Safe to proceed.
                    }

                    spin.SpinOnce(-1);
                }
                break;
            }

            spin.SpinOnce(-1);
        }

        var it = info.GetDependentIterator();
        while (it.MoveNext())
        {
            var depHandle = it.Current;

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(depHandle.ID, depHandle.Generation, out var depExist);
            if (depExist && Interlocked.Decrement(ref depJobInfo.dependencyCount) == 0)
            {
                EnqueueJobIfReady(depHandle, threadIndex);
            }
        }

        info.additionalDependents.Dispose();

        _freeList.Free(info.pJobData);
        _jobInfoPool.Remove(handle.ID, handle.Generation);
    }

    public JobHandle Schedule<T>(ref readonly T job, int threadIndex, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob
    {
        var pJobData = _freeList.Allocate(MemoryUtility.SizeOf<T>(), MemoryUtility.AlignOf<T>());
        if (pJobData == null)
        {
            return JobHandle.Invalid;
        }

        *(T*)pJobData = job;

        var jobInfo = new JobInfo
        {
            pJobData = pJobData,
            pExecutionFunc = &JobExecutor.Execute<T>,

            remainingBatches = 1,
            threadIndex = threadIndex,

            jobRanges = JobRanges.Single,
        };

        return CreateJobHandle(ref jobInfo, threadIndex, dependency);
    }

    public JobHandle Schedule<T>(ref readonly T job, int threadIndex, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob
        => Schedule(in job, threadIndex, JobHandle.Invalid, priority);

    public JobHandle Schedule<T>(ref readonly T job, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob
        => Schedule(in job, -1, dependency, priority);

    public JobHandle Schedule<T>(ref readonly T job, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob
        => Schedule(in job, -1, JobHandle.Invalid, priority);

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor
    {
        var pJobData = _freeList.Allocate(MemoryUtility.SizeOf<T>(), MemoryUtility.AlignOf<T>());
        if (pJobData == null)
        {
            return JobHandle.Invalid;
        }

        *(T*)pJobData = job;

        var optimalBatchSize = Math.Max(1, batchSize);
        var totalBatches = (totalIteration + optimalBatchSize - 1) / optimalBatchSize;

        var jobInfo = new JobInfo
        {
            pJobData = pJobData,
            pExecutionFunc = &JobExecutor.ExecuteParallelFor<T>,

            remainingBatches = totalBatches,
            threadIndex = threadIndex,

            jobRanges = new JobRanges()
            {
                currentIndex = 0,
                batchSize = optimalBatchSize,
                totalIteration = totalIteration,
            },
        };

        return CreateJobHandle(ref jobInfo, threadIndex, dependency);
    }

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, threadIndex, JobHandle.Invalid, priority);

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, -1, dependency, priority);

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, -1, JobHandle.Invalid, priority);

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel
    {
        var pJobData = _freeList.Allocate(MemoryUtility.SizeOf<T>(), MemoryUtility.AlignOf<T>());
        if (pJobData == null)
        {
            return JobHandle.Invalid;
        }

        *(T*)pJobData = job;

        var optimalBatchSize = Math.Max(1, batchSize);
        var totalBatches = (totalIteration + optimalBatchSize - 1) / optimalBatchSize;

        var jobInfo = new JobInfo
        {
            pJobData = pJobData,
            pExecutionFunc = &JobExecutor.ExecuteParallel<T>,

            remainingBatches = totalBatches,
            threadIndex = threadIndex,

            jobRanges = new JobRanges()
            {
                currentIndex = 0,
                batchSize = optimalBatchSize,
                totalIteration = totalIteration,
            },
        };

        return CreateJobHandle(ref jobInfo, threadIndex, dependency);
    }

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, threadIndex, JobHandle.Invalid, priority);

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, -1, dependency, priority);

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, -1, JobHandle.Invalid, priority);

    public JobHandle CombineDependencies(params ReadOnlySpan<JobHandle> dependencies)
    {
        var jobInfo = new JobInfo
        {
            pJobData = null,
            pExecutionFunc = null,

            remainingBatches = 1,
            threadIndex = -1,

            jobRanges = JobRanges.Single,
        };

        return CreateJobHandle(ref jobInfo, -1, dependencies);
    }

    public JobState GetJobStatus(JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return JobState.Invalid;
        }

        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return JobState.Completed; // We assume completed if not found. Invalid state is reserved for error.
        }

        // Mask out the Reader Count (upper 16 bits) to return the actual State
        return JobUtility.GetState(Volatile.Read(ref jobInfo.state));
    }

    public void Wait(JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        // TODO: Maybe we can steal a up stream or current job to execute while waiting?
        // For example, if we wait on job A which depends on job B, and both are not scheduled yet, we can steal and execute job B to speed up the completion of A.

        var spin = new SpinWait();
        while (true)
        {
            ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            // Mask out RC
            if (JobUtility.ReadState(ref jobInfo) == JobState.Completed)
            {
                return;
            }

            // var sleepThreshold = jobInfo.jobRanges.totalIteration * jobInfo.jobRanges.batchSize * 100;
            spin.SpinOnce(_SLEEP_THRESHOLD);
        }
    }

    public void WaitAll(params Span<JobHandle> handles)
    {
        if (handles.Length == 0)
        {
            return;
        }

        var spin = new SpinWait();
        var completedCount = 0;

        while (true)
        {
            for (var i = completedCount; i < handles.Length; i++)
            {
                var handle = handles[i];
                if (!_jobInfoPool.Contains(handle.ID, handle.Generation))
                {
                    // Move completed handle to the front (completedCount index) to avoid checking it again.
                    var temp = handles[completedCount];
                    handles[completedCount] = handle;
                    handles[i] = temp;

                    completedCount++;
                }
            }

            if (completedCount == handles.Length)
            {
                return;
            }

            spin.SpinOnce(_SLEEP_THRESHOLD);
        }
    }

    public JobHandle WaitAny(params ReadOnlySpan<JobHandle> handles)
    {
        var spin = new SpinWait();

        while (true)
        {
            foreach (var handle in handles)
            {
                if (!_jobInfoPool.Contains(handle.ID, handle.Generation))
                {
                    return handle;
                }
            }

            spin.SpinOnce(_SLEEP_THRESHOLD);
        }
    }

    public Task WaitAsync(JobHandle handle, CancellationToken cancellationToken = default)
    {
        if (!handle.IsValid)
        {
            return Task.CompletedTask;
        }

        var workItem = new WaitItem(this, handle, cancellationToken);
        ThreadPool.UnsafeQueueUserWorkItem(workItem, preferLocal: true);

        return workItem.Task;
    }

    public Task WaitAllAsync(Memory<JobHandle> handles, CancellationToken cancellationToken = default)
    {
        if (handles.Length == 0)
        {
            return Task.CompletedTask;
        }

        var workItem = new WaitAllItem(this, handles, cancellationToken);
        ThreadPool.UnsafeQueueUserWorkItem(workItem, preferLocal: true);

        return workItem.Task;
    }

    public Task<JobHandle> WaitAnyAsync(ReadOnlyMemory<JobHandle> handles, CancellationToken cancellationToken = default)
    {
        if (handles.Length == 0)
        {
            return Task.FromResult(JobHandle.Invalid);
        }

        var workItem = new WaitAnyItem(this, handles, cancellationToken);
        ThreadPool.UnsafeQueueUserWorkItem(workItem, preferLocal: true);

        return workItem.Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cts.Cancel();

        foreach (var worker in _workerThreads)
        {
            worker.Dispose();
        }

        foreach (var info in _jobInfoPool)
        {
            if (info.pJobData != null)
            {
                _freeList.Free(info.pJobData);
            }
        }

        _freeList.Dispose();
        _workSignal.Dispose();
        _cts.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
