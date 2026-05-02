using Misaki.HighPerformance.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

public struct JobSchedulerDesc
{
    /// <summary>
    /// Gets or sets the number of worker threads to be created and managed by the job scheduler. If set to less than 1, at least one worker thread will be created.
    /// </summary>
    public required int ThreadCount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the maximum number of dependencies in the dependency edge pool. This determines how many job dependencies can be tracked simultaneously.
    /// </summary>
    public required int DependencyChainCapacity
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the priority of the worker threads. This can be used to influence the scheduling of the threads by the operating system. The default value is <see cref="ThreadPriority.Normal"/>.
    /// </summary>
    public required ThreadPriority ThreadPriority
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the state object for the job scheduler. This can be used to store any user-defined data or context that may be needed by the jobs or worker threads. The job scheduler does not interpret or manage this state in any way; it is simply provided as a convenience for users of the job scheduler. The default value is null.
    /// </summary>
    public object? State
    {
        get; set;
    }
}

internal sealed class WaitItem : IThreadPoolWorkItem
{
    private readonly JobScheduler _scheduler;
    private readonly JobHandle _jobHandle;

    private readonly TaskCompletionSource _completionSource;

    public Task Task => _completionSource.Task;

    public WaitItem(JobScheduler scheduler, JobHandle jobHandle, CancellationToken cancellationToken)
    {
        _scheduler = scheduler;
        _jobHandle = jobHandle;
        _completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        cancellationToken.Register((cs, tk) => ((TaskCompletionSource)cs!).TrySetCanceled(tk), _completionSource);
    }

    public void Execute()
    {
        _scheduler.Wait(_jobHandle);
        _completionSource.SetResult();
    }
}

internal sealed class WaitAllItem : IThreadPoolWorkItem
{
    private readonly JobScheduler _scheduler;
    private readonly Memory<JobHandle> _jobHandles;

    private readonly TaskCompletionSource _completionSource;

    public Task Task => _completionSource.Task;

    public WaitAllItem(JobScheduler scheduler, Memory<JobHandle> jobHandles, CancellationToken cancellationToken)
    {
        _scheduler = scheduler;
        _jobHandles = jobHandles;
        _completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        cancellationToken.Register((cs, tk) => ((TaskCompletionSource)cs!).TrySetCanceled(tk), _completionSource);
    }

    public void Execute()
    {
        _scheduler.WaitAll(_jobHandles.Span);
        _completionSource.SetResult();
    }
}

internal sealed class WaitAnyItem : IThreadPoolWorkItem
{
    private readonly JobScheduler _scheduler;
    private readonly ReadOnlyMemory<JobHandle> _jobHandles;

    private readonly TaskCompletionSource<JobHandle> _completionSource;

    public Task<JobHandle> Task => _completionSource.Task;

    public WaitAnyItem(JobScheduler scheduler, ReadOnlyMemory<JobHandle> jobHandles, CancellationToken cancellationToken)
    {
        _scheduler = scheduler;
        _jobHandles = jobHandles;
        _completionSource = new TaskCompletionSource<JobHandle>(TaskCreationOptions.RunContinuationsAsynchronously);

        cancellationToken.Register((cs, tk) => ((TaskCompletionSource)cs!).TrySetCanceled(tk), _completionSource);
    }

    public void Execute()
    {
        var completedHandle = _scheduler.WaitAny(_jobHandles.Span);
        _completionSource.SetResult(completedHandle);
    }
}

/// <summary>
/// Provides a mechanism for scheduling and executing jobs across multiple worker threads.
/// </summary>
public sealed unsafe partial class JobScheduler : IDisposable
{
    // Don't sleep indefinitely because that causes our 1ms job to become 15ms.
    private const int _SLEEP_THRESHOLD = -1;

    private readonly ConcurrentSlotMap<JobInfo> _jobInfoPool;
    private readonly ConcurrentQueue<JobHandle>[] _jobQueues;
    private readonly WorkerThread[] _workerThreads;

    private readonly JobEdge[] _jobEdges;
    private int _watermark;
    private long _freeListHead;

    private readonly SemaphoreSlim _workSignal;
    private readonly CancellationTokenSource _cts;

    private readonly object? _state;

    private bool _disposed = false;

    internal object? State => _state;
    internal bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>
    /// Gets the number of worker threads managed by the job scheduler.
    /// </summary>
    public int WorkerCount => _workerThreads.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class with the specified description.
    /// </summary>
    /// <param name="desc">The description for the job scheduler.</param>
    public JobScheduler(ref readonly JobSchedulerDesc desc)
    {
        var workerCount = Math.Max(1, desc.ThreadCount);

        _jobInfoPool = new ConcurrentSlotMap<JobInfo>(128);
        _jobQueues = new ConcurrentQueue<JobHandle>[3];

        for (var i = 0; i < 3; i++)
        {
            _jobQueues[i] = new ConcurrentQueue<JobHandle>();
        }

        _jobEdges = new JobEdge[desc.DependencyChainCapacity];
        _watermark = 0;
        _freeListHead = -1L;

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
    [Obsolete("Use JobScheduler(JobSchedulerDesc) instead.")]
    public JobScheduler(int threadCount, ThreadPriority priority = ThreadPriority.Normal, object? state = null)
    {
        var workerCount = Math.Max(1, threadCount);

        _jobInfoPool = new ConcurrentSlotMap<JobInfo>(128);
        _jobQueues = new ConcurrentQueue<JobHandle>[3];

        for (var i = 0; i < 3; i++)
        {
            _jobQueues[i] = new ConcurrentQueue<JobHandle>();
        }

        _jobEdges = new JobEdge[4096];
        _watermark = 0;
        _freeListHead = -1L;

        _workSignal = new SemaphoreSlim(0);
        _cts = new CancellationTokenSource();

        _state = state;

        _workerThreads = new WorkerThread[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            _workerThreads[i] = new WorkerThread(i, this, priority);
        }

        for (var i = 0; i < workerCount; i++)
        {
            _workerThreads[i].Start();
        }
    }

    ~JobScheduler()
    {
        Dispose();
    }

    private void EnqueueJobIfReady(JobHandle handle, bool preferLocal)
    {
        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);

        if (exist && Volatile.Read(ref jobInfo.dependencyCount) == 0)
        {
            // Note: JobState.Created is 0, JobState.Scheduled is 1. We assume RC logic doesn't touch initial state (RC=0).
            if (Interlocked.CompareExchange(ref jobInfo.state, JobUtility.JOBSTATE_SCHEDULED, JobUtility.JOBSTATE_CREATED) != JobUtility.JOBSTATE_CREATED)
            {
                return;
            }

            // Ensure the count of this job handle won't exceed the number of worker threads.
            // Worker threads will steal parallel iteration ranges from each other.
            var handleCount = Math.Min(jobInfo.jobRanges.TotalBatches, _workerThreads.Length);

            var tier = (int)jobInfo.priority;
            var i = 0;

            if (preferLocal)
            {
                var index = WorkerThread.ThreadIndex;
                for (; i < handleCount; i++)
                {
                    if (!_workerThreads[index].LocalQueues[tier].TryPush(handle))
                    {
                        break;
                    }
                }
            }

            for (; i < handleCount; i++)
            {
                _jobQueues[tier].Enqueue(handle);
            }

            _workSignal.Release(handleCount);
        }
    }

    private int AllocateEdge()
    {
        var headCounter = Volatile.Read(ref _freeListHead);
        while (headCounter != -1L)
        {
            // Lower 32 bits is the index, upper 32 bits is the version. We need to read both to ensure the consistency of the free list head.
            var headIndex = (int)(headCounter & 0xFFFFFFFF);
            var nextIndex = _jobEdges[headIndex].nextEdgeIndex;

            var nextCounter = nextIndex == -1 ? -1L : (((headCounter >> 32) + 1) << 32) | (uint)nextIndex;

            if (Interlocked.CompareExchange(ref _freeListHead, nextCounter, headCounter) == headCounter)
            {
                return headIndex;
            }

            headCounter = Volatile.Read(ref _freeListHead);
        }

        return Interlocked.Increment(ref _watermark) - 1;
    }

    private void FreeEdgeChain(ref int firstEdgeIndex)
    {
        if (firstEdgeIndex == -1)
        {
            return;
        }

        var tailEdgeIndex = firstEdgeIndex;
        while (_jobEdges[tailEdgeIndex].nextEdgeIndex != -1)
        {
            tailEdgeIndex = _jobEdges[tailEdgeIndex].nextEdgeIndex;
        }

        var currentHeadCounter = Volatile.Read(ref _freeListHead);
        long newHeadCounter;
        do
        {
            var currentHeadIndex = (int)(currentHeadCounter & 0xFFFFFFFF);
            _jobEdges[tailEdgeIndex].nextEdgeIndex = currentHeadIndex;

            newHeadCounter = (((currentHeadCounter >> 32) + 1) << 32) | (uint)firstEdgeIndex;
        }
        while (Interlocked.CompareExchange(ref _freeListHead, newHeadCounter, currentHeadCounter) != currentHeadCounter);

        firstEdgeIndex = -1;
    }

    private JobHandle CreateJobHandle(ref JobInfo jobInfo, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
    {
        // Advance count to account for all dependencies upfront + 1 guard lock
        Interlocked.Add(ref jobInfo.dependencyCount, dependencies.Length + 1);

        var id = _jobInfoPool.Add(jobInfo, out var generation);
        ref var infoInPool = ref _jobInfoPool.GetElementReferenceAt(id, generation, out _);

        var handle = new JobHandle(id, generation);

        infoInPool.firstDependentEdgeIndex = -1;
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

                    // Get an index for the new edge from the edge pool
                    var newEdgeIndex = AllocateEdge();
                    ref var edge = ref _jobEdges[newEdgeIndex];
                    edge.dependentJob = handle;

                    // Because rc is a read lock, there may be multiple concurrent registrations happening, so we need to insert the new edge to the head of the list with a CAS loop.
                    int currentFirst;
                    do
                    {
                        currentFirst = Volatile.Read(ref depJobInfo.firstDependentEdgeIndex);
                        edge.nextEdgeIndex = currentFirst;
                    }
                    while (Interlocked.CompareExchange(ref depJobInfo.firstDependentEdgeIndex, newEdgeIndex, currentFirst) != currentFirst);

                    // Release RC
                    Interlocked.Add(ref depJobInfo.state, -JobUtility.RC_ONE);

                    registered = true;

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
            EnqueueJobIfReady(handle, preferLocal);
        }

        return handle;
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
        return _workerThreads[threadIndex].LocalQueues[tier].TrySteal(out outHandle);
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

    internal void MarkJobComplete(JobHandle handle)
    {
        Debug.Assert(handle.IsValid);

        ref var info = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

#if false
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
#else
        // NOTE: We are the last one to complete. Because we call this on the thread that get rc = 0, not the last one to complete. So we can directly set state to Completed without caring about RC. This also means we don't need to preserve upper bits.
        var spin = new SpinWait();
        while (Interlocked.CompareExchange(ref info.state, JobUtility.JOBSTATE_COMPLETED, JobUtility.JOBSTATE_RUNNING) != JobUtility.JOBSTATE_RUNNING)
        {
            if (JobUtility.ReadState(ref info) == JobState.Completed)
            {
                return;
            }

            spin.SpinOnce(-1);
        }
#endif

        var it = info.GetDependentIterator(_jobEdges);
        while (it.MoveNext())
        {
            var depHandle = it.Current;

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(depHandle.ID, depHandle.Generation, out var depExist);
            if (depExist && Interlocked.Decrement(ref depJobInfo.dependencyCount) == 0)
            {
                EnqueueJobIfReady(depHandle, true);
            }
        }

        FreeEdgeChain(ref info.firstDependentEdgeIndex);

        if (info.pFreeFunc != null)
        {
            info.pFreeFunc(info.dataID, info.dataGeneration);
        }

        _jobInfoPool.Remove(handle.ID, handle.Generation);
    }

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle Schedule<T>(ref readonly T job, bool preferLocal, JobPriority priority = JobPriority.Normal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJob
    {
        var id = JobDataPool<T>.Allocate(in job, out var generation);
        var jobInfo = new JobInfo
        {
            dataID = id,
            dataGeneration = generation,

            pExecutionFunc = &JobExecutor.Execute<T>,
            pFreeFunc = &JobDataPool<T>.Free,

            priority = priority,
            jobRanges = JobRanges.Single,
        };

        return CreateJobHandle(ref jobInfo, preferLocal, dependencies);
    }

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle Schedule<T>(ref readonly T job, JobPriority priority = JobPriority.Normal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJob
        => Schedule(in job, false, priority, dependencies);

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle Schedule<T>(ref readonly T job, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJob
        => Schedule(in job, false, JobPriority.Normal, dependencies);

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle Schedule<T>(ref readonly T job, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJob
        => Schedule(in job, preferLocal, JobPriority.Normal, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.  <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority = JobPriority.Normal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallelFor
    {
        if (totalIteration <= 0)
        {
            return JobHandle.Invalid;
        }

        var id = JobDataPool<T>.Allocate(in job, out var generation);
        var optimalBatchSize = Math.Max(1, batchSize);
        var jobInfo = new JobInfo
        {
            dataID = id,
            dataGeneration = generation,

            pExecutionFunc = &JobExecutor.ExecuteParallelFor<T>,
            pFreeFunc = &JobDataPool<T>.Free,

            priority = priority,
            jobRanges = new JobRanges
            {
                batchSize = optimalBatchSize,
                totalIteration = totalIteration,
            },
        };

        return CreateJobHandle(ref jobInfo, preferLocal, dependencies);
    }

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobPriority priority = JobPriority.Normal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, false, priority, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, false, JobPriority.Normal, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, preferLocal, JobPriority.Normal, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority = JobPriority.Normal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallel
    {
        if (totalIteration <= 0)
        {
            return JobHandle.Invalid;
        }

        var id = JobDataPool<T>.Allocate(in job, out var generation);
        var optimalBatchSize = Math.Max(1, batchSize);
        var jobInfo = new JobInfo
        {
            dataID = id,
            dataGeneration = generation,

            pExecutionFunc = &JobExecutor.ExecuteParallel<T>,
            pFreeFunc = &JobDataPool<T>.Free,

            priority = priority,
            jobRanges = new JobRanges
            {
                batchSize = optimalBatchSize,
                totalIteration = totalIteration,
            },
        };

        return CreateJobHandle(ref jobInfo, preferLocal, dependencies);
    }

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobPriority priority = JobPriority.Normal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, false, priority, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, false, JobPriority.Normal, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, preferLocal, JobPriority.Normal, dependencies);

    /// <summary>
    /// Schedules a custom job for execution with user-defined <see cref="JobInfo"/>.
    /// </summary>
    /// <param name="jobInfo">The information about the job to be scheduled.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleCustom<T>(CustomJobDesc<T> jobDesc, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
        where T : struct
    {
        if (jobDesc.jobRanges.totalIteration == 0 || jobDesc.jobRanges.batchSize == 0 || Unsafe.IsNullRef(in jobDesc.data))
        {
            return JobHandle.Invalid;
        }

        var id = JobDataPool<T>.Allocate(in jobDesc.data, out var generation);
        var jobInfo = new JobInfo
        {
            dataID = id,
            dataGeneration = generation,

            pExecutionFunc = jobDesc.pExecutionFunc,
            pFreeFunc = jobDesc.pFreeFunc,

            priority = jobDesc.priority,
            jobRanges = jobDesc.jobRanges,
        };

        return CreateJobHandle(ref jobInfo, preferLocal, dependencies);
    }

    /// <summary>
    /// Combines multiple job dependencies into a single <see cref="JobHandle"/>.
    /// </summary>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> instances representing the dependencies to combine.</param>
    /// <returns>A <see cref="JobHandle"/> that represents the combined dependencies. The returned handle can be used to ensure that all specified dependencies are completed before proceeding.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle CombineDependencies(params ReadOnlySpan<JobHandle> dependencies)
    {
        var jobInfo = new JobInfo
        {
            pExecutionFunc = null,
            pFreeFunc = null,
            jobRanges = JobRanges.Single,
        };

        return CreateJobHandle(ref jobInfo, false, dependencies);
    }

    /// <summary>
    /// Retrieves the current status of a job identified by the specified handle.
    /// </summary>
    /// <param name="handle">The handle representing the job whose status is to be retrieved. The handle must be valid.</param>
    /// <returns>The current status of the job as a <see cref="JobState"/> value. Returns <see cref="JobState.Invalid"/> if the handle is invalid or the job does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>
    /// Blocks the calling thread until the specified job is completed.
    /// </summary>
    /// <param name="handle">The handle of the job to wait for.</param>
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

    /// <summary>
    /// Blocks the calling thread until all specified job handles have completed.
    /// </summary>
    /// <remarks>
    /// The collection handles will be reordered in-place to move completed handles to the front.
    /// </remarks>
    /// <param name="handles">A collection of job handles to wait for.</param>
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

    /// <summary>
    /// Waits until any of the specified job handles has completed and returns the first completed handle.
    /// </summary>
    /// <param name="handles">A read-only span containing the job handles to monitor for completion.</param>
    /// <returns>The first job handle from the provided collection that has completed.</returns>
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

    /// <summary>
    /// Waits asynchronously until the specified job is completed, allowing the calling thread to perform other work while waiting.
    /// </summary>
    /// <param name="handle">The handle of the job to wait for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
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

    /// <summary>
    /// Waits asynchronously until all specified job handles have completed, allowing the calling thread to perform other work while waiting.
    /// </summary>
    /// <remarks>
    /// The collection handles will be reordered in-place to move completed handles to the front.
    /// </remarks>
    /// <param name="handles">A memory containing the job handles to monitor for completion.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
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

    /// <summary>
    /// Waits asynchronously until any of the specified job handles has completed, allowing the calling thread to perform other work while waiting, and returns the first completed handle.
    /// </summary>
    /// <param name="handles">A read-only memory containing the job handles to monitor for completion.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
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

        _workSignal.Dispose();
        _cts.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
