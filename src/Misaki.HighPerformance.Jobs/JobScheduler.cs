using Misaki.HighPerformance.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Jobs;

public struct JobSchedulerDesc()
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
    public ThreadPriority ThreadPriority
    {
        get; set;
    } = ThreadPriority.Normal;

    /// <summary>
    /// Gets or sets the state object for the job scheduler. This can be used to store any user-defined data or context that may be needed by the jobs or worker threads. The job scheduler does not interpret or manage this state in any way; it is simply provided as a convenience for users of the job scheduler. The default value is null.
    /// </summary>
    public object? State
    {
        get; set;
    }

    public int HelperThreadCount
    {
        get; set;
    } = 1;
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
        _scheduler.Wait(_jobHandle, false);
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
    private readonly ConcurrentSlotMap<JobInfo> _jobInfoPool;
    private readonly ConcurrentQueue<JobHandle>[] _jobQueues;
    private readonly WorkerThread[] _workerThreads;

    private readonly JobEdge[] _jobEdges;
    private int _watermark;
    private long _freeListHead;

    private readonly SemaphoreSlim _workSignal;
    private readonly CancellationTokenSource _cts;

    private readonly int _workerThreadCount;
    private readonly int _helperThreadCount;

    private readonly object? _state;

    private bool _disposed = false;

    internal object? State => _state;
    internal bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>
    /// An event that is triggered whenever a worker thread changes its state.
    /// </summary>
    /// <remarks>
    /// This event will execute on worker threads directly, and never been fired when MHP_ENABLE_PROFILING is not defined.
    /// </remarks>
    public event Action<WorkerThreadStateEvent>? OnWorkerThreadStateChanged;

    /// <summary>
    /// Gets the number of worker threads managed by the job scheduler.
    /// </summary>
    public int WorkerCount => _workerThreadCount;

    /// <summary>
    /// Gets the number of external helper threads, which is the number of threads reserved for external use.
    /// </summary>
    public int ExternalHelperThreadCount => _helperThreadCount;

    /// <summary>
    /// Gets the total number of threads that is possible to execute the jobs, including worker threads and external helper threads. You can use this property to allocate thread local storage.
    /// </summary>
    public int ThreadLocalCount => _workerThreads.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class with the specified description.
    /// </summary>
    /// <param name="desc">The description for the job scheduler.</param>
    public JobScheduler(scoped in JobSchedulerDesc desc)
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
        _freeListHead = (long)0xFFFFFFFF;

        _workSignal = new SemaphoreSlim(0);
        _cts = new CancellationTokenSource();

        _workerThreadCount = workerCount;
        _helperThreadCount = desc.HelperThreadCount;
        _state = desc.State;

        _workerThreads = new WorkerThread[workerCount + _helperThreadCount];

        for (var i = _helperThreadCount; i < _workerThreads.Length; i++)
        {
            _workerThreads[i] = new WorkerThread(i, this, desc.ThreadPriority);
        }

        for (var i = _helperThreadCount; i < _workerThreads.Length; i++)
        {
            _workerThreads[i].Start();
        }
    }

    private void EnqueueJobIfReady(JobHandle handle, bool preferLocal)
    {
        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);

        if (exist && Volatile.Read(ref jobInfo.dependencyCount) == 0)
        {
            var spin = new SpinWait();
            while (true)
            {
                var stateVal = Volatile.Read(ref jobInfo.state);
                if (JobUtility.GetState(stateVal) != JobState.Created)
                {
                    return;
                }

                var newState = (stateVal & ~JobUtility.STATE_MASK) | JobUtility.JOBSTATE_SCHEDULED;
                if (Interlocked.CompareExchange(ref jobInfo.state, newState, stateVal) == stateVal)
                {
                    break;
                }

                spin.SpinOnce(-1);
            }

            // Ensure the count of this job handle won't exceed the number of worker threads.
            // Worker threads will steal parallel iteration ranges from each other.
            var handleCount = Math.Min(jobInfo.jobRanges.TotalBatches, _workerThreads.Length);

            var tier = (int)jobInfo.priority;
            var i = 0;

            if (preferLocal && WorkerThread.IsWorkerThread)
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
        while (true)
        {
            var headIndex = (int)(headCounter & 0xFFFFFFFF);
            if (headIndex == -1)
            {
                break;
            }

            var version = (uint)(headCounter >> 32);
            var nextIndex = Volatile.Read(ref _jobEdges[headIndex].nextEdgeIndex);

            var nextCounter = ((long)(version + 1) << 32) | (uint)nextIndex;

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

        var spin = new SpinWait();
        while (true)
        {
            var currentHeadCounter = Volatile.Read(ref _freeListHead);
            var currentHeadIndex = (int)(currentHeadCounter & 0xFFFFFFFF);
            var version = (uint)(currentHeadCounter >> 32);

            _jobEdges[tailEdgeIndex].nextEdgeIndex = currentHeadIndex;

            var newHeadCounter = ((long)(version + 1) << 32) | (uint)firstEdgeIndex;

            if (Interlocked.CompareExchange(ref _freeListHead, newHeadCounter, currentHeadCounter) == currentHeadCounter)
            {
                break;
            }

            spin.SpinOnce(-1);
        }

        firstEdgeIndex = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryAttachParentOnly(JobHandle parentJob)
    {
        if (!parentJob.IsValid)
        {
            return false;
        }

        ref var parentInfo = ref _jobInfoPool.GetElementReferenceAt(parentJob.ID, parentJob.Generation, out var exist);
        if (!exist)
        {
            return false;
        }

        var spin = new SpinWait();
        while (true)
        {
            if (!_jobInfoPool.Contains(parentJob.ID, parentJob.Generation))
            {
                return false;
            }

            var stateVal = Volatile.Read(ref parentInfo.state);
            var state = JobUtility.GetState(stateVal);
            var rc = JobUtility.GetRefCount(stateVal);

            // Cannot attach to completed/invalid parent, or a running parent whose body has already finished (RC == 0)
            if (state == JobState.Completed || state == JobState.Invalid || (state == JobState.Running && rc == 0))
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref parentInfo.state, stateVal + JobUtility.RC_ONE, stateVal) == stateVal)
            {
                // Re-verify that parent wasn't recycled concurrently
                if (!_jobInfoPool.Contains(parentJob.ID, parentJob.Generation))
                {
                    Interlocked.Add(ref parentInfo.state, -JobUtility.RC_ONE);
                    return false;
                }

                return true;
            }

            spin.SpinOnce(-1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DetachFromParent(JobHandle parentJob)
    {
        if (!parentJob.IsValid)
        {
            return;
        }

        ref var parentInfo = ref _jobInfoPool.GetElementReferenceAt(parentJob.ID, parentJob.Generation, out var parentExist);
        if (parentExist)
        {
            var stateAfterRelease = Interlocked.Add(ref parentInfo.state, -JobUtility.RC_ONE);
            if (JobUtility.GetRefCount(stateAfterRelease) == 0 && JobUtility.GetState(stateAfterRelease) == JobState.Running)
            {
                MarkJobComplete(parentJob);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryAttachToParent(JobHandle parentJob, ref JobInfo childInfo)
    {
        if (TryAttachParentOnly(parentJob))
        {
            childInfo.parentJob = parentJob;
            return true;
        }

        childInfo.parentJob = JobHandle.Invalid;
        return false;
    }

    private JobHandle CreateJobHandle(ref JobInfo jobInfo, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
    {
        if (jobInfo.parentJob.IsValid)
        {
            TryAttachToParent(jobInfo.parentJob, ref jobInfo);
        }

        // Advance count to account for all dependencies upfront + 1 guard lock
        Interlocked.Add(ref jobInfo.dependencyCount, dependencies.Length + 1);

        var id = _jobInfoPool.Add(jobInfo, out var generation);
        ref var infoInPool = ref _jobInfoPool.GetElementReferenceAt(id, generation, out _);

        var handle = new JobHandle(id, generation);

        infoInPool.firstDependentEdgeIndex = -1;
        infoInPool.firstUpstreamEdgeIndex = -1;
        for (var i = 0; i < dependencies.Length; i++)
        {
            var dependency = dependencies[i];

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(dependency.ID, dependency.Generation, out var exist);
            if (!exist)
            {
                Interlocked.Decrement(ref infoInPool.dependencyCount);
                continue;
            }

            // Lock-free registration: Try to acquire "Reader Lock" by incrementing RC in high bits.
            // If state is already Completed, we skip (dependency met).
            var registered = false;
            var spin = new SpinWait();

            while (true)
            {
                if (!_jobInfoPool.Contains(dependency.ID, dependency.Generation))
                {
                    break;
                }

                var stateVal = Volatile.Read(ref depJobInfo.state);
                var state = JobUtility.GetState(stateVal);

                if (state == JobState.Completed || state == JobState.Invalid)
                {
                    break;
                }

                // Attempt to increment RC (Reader Count)
                if (Interlocked.CompareExchange(ref depJobInfo.state, stateVal + JobUtility.RC_ONE, stateVal) == stateVal)
                {
                    // Re-verify that the slot wasn't recycled concurrently before or during the CAS
                    if (!_jobInfoPool.Contains(dependency.ID, dependency.Generation))
                    {
                        Interlocked.Add(ref depJobInfo.state, -JobUtility.RC_ONE);
                        break;
                    }

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

                    // Backward edge: link dependency into this job's upstream chain
                    var backwardEdgeIndex = AllocateEdge();
                    ref var bEdge = ref _jobEdges[backwardEdgeIndex];
                    bEdge.dependentJob = dependency;
                    bEdge.nextEdgeIndex = infoInPool.firstUpstreamEdgeIndex;
                    infoInPool.firstUpstreamEdgeIndex = backwardEdgeIndex;

                    // Release RC
                    var stateAfterRelease = Interlocked.Add(ref depJobInfo.state, -JobUtility.RC_ONE);
                    // The Main Thread MUST clean up if it is the last thread holding the lock!
                    if (JobUtility.GetRefCount(stateAfterRelease) == 0 && JobUtility.GetState(stateAfterRelease) == JobState.Running)
                    {
                        MarkJobComplete(dependency);
                    }

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

    [Conditional("MHP_ENABLE_PROFILING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BroadcastStateChange(int threadIndex, WorkerThreadState state, string? jobTypeName = null)
    {
        OnWorkerThreadStateChanged?.Invoke(new WorkerThreadStateEvent
        {
            ThreadIndex = threadIndex,
            State = state,
            Timestamp = Stopwatch.GetTimestamp(),
            JobTypeName = jobTypeName,
        });
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

        // NOTE: Because we call this on the thread that get rc = 0, not the last one to complete. So we can directly set state to Completed without caring about RC. This also means we don't need to preserve upper bits.
        var spin = new SpinWait();
        while (Interlocked.CompareExchange(ref info.state, JobUtility.JOBSTATE_COMPLETED, JobUtility.JOBSTATE_RUNNING) != JobUtility.JOBSTATE_RUNNING)
        {
            if (!_jobInfoPool.Contains(handle.ID, handle.Generation) || JobUtility.ReadState(ref info) == JobState.Completed)
            {
                return;
            }

            spin.SpinOnce(-1);
        }

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
        FreeEdgeChain(ref info.firstUpstreamEdgeIndex);

        var parentJob = info.parentJob;

        if (info.pFreeFunc != null)
        {
            info.pFreeFunc(in info);
        }

        _jobInfoPool.Remove(handle.ID, handle.Generation);

        // Handle attached parent continuation
        if (parentJob.IsValid)
        {
            DetachFromParent(parentJob);
        }
    }

    /// <summary>
    /// Runs a job immediately on the current thread, blocking until it is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed.</param>
    /// <param name="parent">The parent job that this job must be finished before the parent job can be completed. If the parent job is not valid, this job will be treated as a root job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run<T>(ref T job, JobHandle parent = default, params Span<JobHandle> dependencies)
        where T : IJob
    {
        var attached = parent.IsValid && TryAttachParentOnly(parent);
        try
        {
            if (!dependencies.IsEmpty)
            {
                WaitAll(dependencies);
            }

            var ctx = new JobExecutionContext
            {
                ThreadIndex = WorkerThread.ThreadIndex,
                JobScheduler = this,
                State = _state,
                SelfHandle = parent,
            };

            job.Execute(in ctx);
        }
        finally
        {
            if (attached)
            {
                DetachFromParent(parent);
            }
        }
    }

    /// <summary>
    /// Runs a job immediately on the current thread, blocking until it is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run<T>(ref T job, params Span<JobHandle> dependencies)
        where T : IJob
        => Run(ref job, default, dependencies);

    /// <summary>
    /// Runs a parallel for job immediately on the current thread, blocking until it is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed.</param>
    /// <param name="totalIteration">The total number of iterations to run.</param>
    /// <param name="parent">The parent job that this job must be finished before the parent job can be completed. If the parent job is not valid, this job will be treated as a root job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunParallelFor<T>(ref T job, int totalIteration, JobHandle parent = default, params Span<JobHandle> dependencies)
        where T : IJobParallelFor
    {
        var attached = parent.IsValid && TryAttachParentOnly(parent);
        try
        {
            if (!dependencies.IsEmpty)
            {
                WaitAll(dependencies);
            }

            var ctx = new JobExecutionContext
            {
                ThreadIndex = WorkerThread.ThreadIndex,
                JobScheduler = this,
                State = _state,
                SelfHandle = parent,
            };

            for (var i = 0; i < totalIteration; i++)
            {
                job.Execute(i, in ctx);
            }
        }
        finally
        {
            if (attached)
            {
                DetachFromParent(parent);
            }
        }
    }

    /// <summary>
    /// Runs a parallel for job immediately on the current thread, blocking until it is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed.</param>
    /// <param name="totalIteration">The total number of iterations to run.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunParallelFor<T>(ref T job, int totalIteration, params Span<JobHandle> dependencies)
        where T : IJobParallelFor
        => RunParallelFor(ref job, totalIteration, default, dependencies);

    /// <summary>
    /// Runs a parallel job immediately on the current thread, blocking until it is complete. The job is executed in batches, with each batch containing a specified number of iterations.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallel"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed.</param>
    /// <param name="totalIteration">The total number of iterations to run.</param>
    /// <param name="batchSize">The number of iterations to run in each batch.</param>
    /// <param name="parent">The parent job that this job must be finished before the parent job can be completed. If the parent job is not valid, this job will be treated as a root job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunParallel<T>(ref T job, int totalIteration, int batchSize, JobHandle parent = default, params Span<JobHandle> dependencies)
        where T : IJobParallel
    {
        var attached = parent.IsValid && TryAttachParentOnly(parent);
        try
        {
            if (!dependencies.IsEmpty)
            {
                WaitAll(dependencies);
            }

            var ctx = new JobExecutionContext
            {
                ThreadIndex = WorkerThread.ThreadIndex,
                JobScheduler = this,
                State = _state,
                SelfHandle = parent,
            };

            for (var i = 0; i < totalIteration; i += batchSize)
            {
                var end = Math.Min(i + batchSize, totalIteration);
                job.Execute(i, end, in ctx);
            }
        }
        finally
        {
            if (attached)
            {
                DetachFromParent(parent);
            }
        }
    }

    /// <summary>
    /// Runs a parallel job immediately on the current thread, blocking until it is complete. The job is executed in batches, with each batch containing a specified number of iterations.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallel"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed.</param>
    /// <param name="totalIteration">The total number of iterations to run.</param>
    /// <param name="batchSize">The number of iterations to run in each batch.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunParallel<T>(ref T job, int totalIteration, int batchSize, params Span<JobHandle> dependencies)
        where T : IJobParallel
        => RunParallel(ref job, totalIteration, batchSize, default, dependencies);

    public void RunCustom<T>(ref CustomJobDesc<T> jobDesc, params Span<JobHandle> dependencies)
    {
        var attached = jobDesc.parentJob.IsValid && TryAttachParentOnly(jobDesc.parentJob);
        try
        {
            if (!dependencies.IsEmpty)
            {
                WaitAll(dependencies);
            }

            var ctx = new JobExecutionContext
            {
                ThreadIndex = WorkerThread.ThreadIndex,
                JobScheduler = this,
                State = _state,
                SelfHandle = jobDesc.parentJob,
            };

            jobDesc.pExecutionFunc(ref jobDesc.data, ref jobDesc.jobRanges, in ctx);
            jobDesc.pFreeFunc(ref jobDesc.data);
        }
        finally
        {
            if (attached)
            {
                DetachFromParent(jobDesc.parentJob);
            }
        }
    }

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="parent">The parent job that this job must be finished before the parent job can be completed. If the parent job is not valid, this job will be treated as a root job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle Schedule<T>(ref readonly T job, bool preferLocal = false, JobPriority priority = JobPriority.Normal, JobHandle parent = default, params ReadOnlySpan<JobHandle> dependencies)
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
            parentJob = parent,
#if MHP_ENABLE_PROFILING
            jobTypeName = typeof(T).Name,
#endif
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
        => Schedule(in job, false, priority, default, dependencies);

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
        => Schedule(in job, false, JobPriority.Normal, default, dependencies);

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
        => Schedule(in job, preferLocal, JobPriority.Normal, default, dependencies);

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle Schedule<T>(ref readonly T job, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJob
        => Schedule(in job, preferLocal, priority, default, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="parent">The parent job that this job must be finished before the parent job can be completed. If the parent job is not valid, this job will be treated as a root job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.  <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal = false, JobPriority priority = JobPriority.Normal, JobHandle parent = default, params ReadOnlySpan<JobHandle> dependencies)
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
            parentJob = parent,
#if MHP_ENABLE_PROFILING
            jobTypeName = typeof(T).Name,
#endif
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
        => ScheduleParallelFor(in job, totalIteration, batchSize, false, priority, default, dependencies);

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
        => ScheduleParallelFor(in job, totalIteration, batchSize, false, JobPriority.Normal, default, dependencies);

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
        => ScheduleParallelFor(in job, totalIteration, batchSize, preferLocal, JobPriority.Normal, default, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.  <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, preferLocal, priority, default, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="parent">The parent job that this job must be finished before the parent job can be completed. If the parent job is not valid, this job will be treated as a root job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal = false, JobPriority priority = JobPriority.Normal, JobHandle parent = default, params ReadOnlySpan<JobHandle> dependencies)
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
            parentJob = parent,
#if MHP_ENABLE_PROFILING
            jobTypeName = typeof(T).Name,
#endif
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
        => ScheduleParallel(in job, totalIteration, batchSize, false, priority, default, dependencies);

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
        => ScheduleParallel(in job, totalIteration, batchSize, false, JobPriority.Normal, default, dependencies);

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
        => ScheduleParallel(in job, totalIteration, batchSize, preferLocal, JobPriority.Normal, default, dependencies);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be struct.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, preferLocal, priority, default, dependencies);

    /// <summary>
    /// Schedules a custom job for execution with user-defined <see cref="JobInfo"/>.
    /// </summary>
    /// <param name="jobDesc">The description of the custom job to be scheduled, containing all necessary information for execution.</param>
    /// <param name="preferLocal">A value indicating whether the job should be preferred to run on the local thread.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle ScheduleCustom<T>(ref readonly CustomJobDesc<T> jobDesc, bool preferLocal, params ReadOnlySpan<JobHandle> dependencies)
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

            pExecutionFunc = &JobExecutor.ExecuteCustom<T>,
            pFreeFunc = &JobExecutor.FreeCustom<T>,

            pCustomExecutionFunc = jobDesc.pExecutionFunc,
            pCustomFreeFunc = jobDesc.pFreeFunc,

            priority = jobDesc.priority,
            jobRanges = jobDesc.jobRanges,
            parentJob = jobDesc.parentJob,
#if MHP_ENABLE_PROFILING
            jobTypeName = typeof(T).Name,
#endif
        };

        return CreateJobHandle(ref jobInfo, preferLocal, dependencies);
    }

    /// <summary>
    /// Schedules a custom job for execution with user-defined <see cref="JobInfo"/>.
    /// </summary>
    /// <param name="jobDesc">The description of the custom job to be scheduled, containing all necessary information for execution.</param>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job. Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    /// <returns></returns>
    public JobHandle ScheduleCustom<T>(ref readonly CustomJobDesc<T> jobDesc, params ReadOnlySpan<JobHandle> dependencies)
        => ScheduleCustom(in jobDesc, false, dependencies);

    /// <summary>
    /// Combines multiple job dependencies into a single <see cref="JobHandle"/>.
    /// </summary>
    /// <remarks>
    /// Use this for large number of dependencies to avoid deep dependency chains, potential overflow of the dependency chain capacity, and reduce scheduling overhead.
    /// </remarks>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> instances representing the dependencies to combine.</param>
    /// <returns>A <see cref="JobHandle"/> that represents the combined dependencies. The returned handle can be used to ensure that all specified dependencies are completed before proceeding.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JobHandle CombineDependencies(params ReadOnlySpan<JobHandle> dependencies)
    {
        if (dependencies.Length == 0)
        {
            return JobHandle.Invalid;
        }

        var size = (uint)dependencies.Length * (uint)sizeof(JobHandle);
        var pDependencies = (JobHandle*)NativeMemory.Alloc(size);
        var job = new CombinedDependenciesJob
        {
            dependencies = pDependencies,
            dependencyCount = dependencies.Length
        };

        Unsafe.CopyBlock(ref *(byte*)pDependencies, ref MemoryMarshal.GetReference(MemoryMarshal.AsBytes(dependencies)), size);

        var desc = new CustomJobDesc<CombinedDependenciesJob>
        {
            data = ref job,
            pExecutionFunc = &CombinedDependenciesJob.Execute,
            pFreeFunc = &CombinedDependenciesJob.Free,
            jobRanges = JobRanges.Single,
        };

        return ScheduleCustom(in desc);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryExecuteUpstream(ref JobInfo jobInfo, int callerThreadIndex, int depth = 0)
    {
        if (depth >= 4)
        {
            return false;
        }

        var edgeIdx = Volatile.Read(ref jobInfo.firstUpstreamEdgeIndex);
        var edgeCount = 0;
        while (edgeIdx != -1 && edgeCount++ < 128)
        {
            var upstreamHandle = _jobEdges[edgeIdx].dependentJob;
            edgeIdx = _jobEdges[edgeIdx].nextEdgeIndex;

            if (!_jobInfoPool.Contains(upstreamHandle.ID, upstreamHandle.Generation))
            {
                continue;
            }

            ref var upstreamInfo = ref _jobInfoPool.GetElementReferenceAt(upstreamHandle.ID, upstreamHandle.Generation, out var exist);
            if (!exist)
            {
                continue;
            }

            var upstreamState = JobUtility.ReadState(ref upstreamInfo);
            if (upstreamState == JobState.Scheduled)
            {
                if (JobUtility.TryExecuteJob(this, upstreamHandle, callerThreadIndex))
                {
                    return true;
                }
            }
            else if (upstreamState == JobState.Running)
            {
                var ranges = upstreamInfo.jobRanges;
                if (ranges.TotalBatches > 1 && Volatile.Read(ref upstreamInfo.jobRanges.currentIndex) < ranges.totalIteration)
                {
                    if (JobUtility.TryExecuteJob(this, upstreamHandle, callerThreadIndex))
                    {
                        return true;
                    }
                }
            }
            else if (upstreamState == JobState.Created)
            {
                if (TryExecuteUpstream(ref upstreamInfo, callerThreadIndex, depth + 1))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Blocks the calling thread until the specified job is completed.
    /// </summary>
    /// <param name="handle">The handle of the job to wait for.</param>
    /// <param name="inlineExecution">A value indicating whether to help execute the job while waiting. Defaults to true. Only ONE external thread is allowed to help execute jobs if you rely on thread local storage.</param>
    public void Wait(JobHandle handle, bool inlineExecution = true)
    {
        if (!handle.IsValid)
        {
            return;
        }

        var callerThreadIndex = WorkerThread.ThreadIndex;
        var spin = new YieldOnlySpin();
        while (true)
        {
            ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            // Mask out RC
            var state = JobUtility.ReadState(ref jobInfo);
            if (state == JobState.Completed)
            {
                return;
            }

            var madeProgress = false;
            if (inlineExecution)
            {
                // Only try to help execute THIS specific job or its upstream dependencies.
                if (state == JobState.Scheduled)
                {
                    madeProgress = JobUtility.TryExecuteJob(this, handle, callerThreadIndex);
                }
                else if (state == JobState.Running)
                {
                    var ranges = jobInfo.jobRanges;
                    if (ranges.TotalBatches > 1 && Volatile.Read(ref jobInfo.jobRanges.currentIndex) < ranges.totalIteration)
                    {
                        madeProgress = JobUtility.TryExecuteJob(this, handle, callerThreadIndex);
                    }
                }
                else if (state == JobState.Created)
                {
                    madeProgress = TryExecuteUpstream(ref jobInfo, callerThreadIndex);
                }
            }

            if (!madeProgress)
            {
                spin.SpinOnce(500);
            }
        }
    }

    /// <summary>
    /// Blocks the calling thread until all specified job handles have completed.
    /// </summary>
    /// <remarks>
    /// The collection handles will be reordered in-place to move completed handles to the front.
    /// </remarks>
    /// <param name="handles">A collection of job handles to wait for.</param>
    /// <param name="inlineExecution">A value indicating whether to help execute the jobs while waiting. Defaults to true.</param>
    public void WaitAll(params Span<JobHandle> handles)
    {
        WaitAll(true, handles);
    }

    public void WaitAll(bool inlineExecution, params Span<JobHandle> handles)
    {
        if (handles.Length == 0)
        {
            return;
        }

        var callerThreadIndex = WorkerThread.ThreadIndex;
        var spin = new YieldOnlySpin();
        var threshold = handles.Length * 300;
        var completedCount = 0;

        while (true)
        {
            var madeProgress = false;
            for (var i = completedCount; i < handles.Length; i++)
            {
                var handle = handles[i];
                ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
                if (!exist)
                {
                    var temp = handles[completedCount];
                    handles[completedCount] = handle;
                    handles[i] = temp;
                    completedCount++;
                }
                else if (inlineExecution)
                {
                    var state = JobUtility.ReadState(ref jobInfo);
                    if (state == JobState.Scheduled)
                    {
                        madeProgress = JobUtility.TryExecuteJob(this, handle, callerThreadIndex);
                    }
                    else if (state == JobState.Running)
                    {
                        var ranges = jobInfo.jobRanges;
                        if (ranges.TotalBatches > 1 && Volatile.Read(ref jobInfo.jobRanges.currentIndex) < ranges.totalIteration)
                        {
                            madeProgress = JobUtility.TryExecuteJob(this, handle, callerThreadIndex);
                        }
                    }
                    else if (state == JobState.Created)
                    {
                        madeProgress = TryExecuteUpstream(ref jobInfo, callerThreadIndex);
                    }
                }
            }

            if (completedCount == handles.Length)
            {
                return;
            }

            if (!madeProgress)
            {
                spin.SpinOnce(threshold);
            }
        }
    }

    /// <summary>
    /// Waits until any of the specified job handles has completed and returns the first completed handle.
    /// </summary>
    /// <param name="handles">A read-only span containing the job handles to monitor for completion.</param>
    /// <returns>The first job handle from the provided collection that has completed.</returns>
    public JobHandle WaitAny(params ReadOnlySpan<JobHandle> handles)
    {
        var spin = new YieldOnlySpin();
        var threshold = handles.Length * 300;

        while (true)
        {
            foreach (var handle in handles)
            {
                if (!_jobInfoPool.Contains(handle.ID, handle.Generation))
                {
                    return handle;
                }
            }

            spin.SpinOnce(threshold);
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

        for (var i = _helperThreadCount; i < _workerThreads.Length; i++)
        {
            _workerThreads[i].Dispose();
        }


        foreach (var info in _jobInfoPool)
        {
            info.pFreeFunc(in info);
        }

        _workSignal.Dispose();
        _cts.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
