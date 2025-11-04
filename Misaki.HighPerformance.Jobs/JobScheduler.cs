using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Provides a mechanism for scheduling and executing jobs across multiple worker threads.
/// </summary>
/// <remarks>The <see cref="JobScheduler"/> class is designed to manage the execution of jobs, including support
/// for dependencies, parallel execution, and thread-specific job assignment. It allows developers to schedule jobs that
/// implement the <see cref="IJob"/> or <see cref="IJobParallelFor"/> interfaces, and it ensures efficient utilization
/// of worker threads through job batching and work-stealing mechanisms.  This class is thread-safe and can be used in
/// multi-threaded environments. However, it must be disposed when no longer needed to release resources and terminate
/// worker threads.</remarks>
public sealed unsafe class JobScheduler : IDisposable
{
    private const int _SLEEP_THRESHOLD = 100;

    private FreeList _jobDataAllocator;
    private readonly ConcurrentSlotMap<JobInfo> _jobInfoPool;
    private readonly ConcurrentQueue<JobHandle> _jobQueue;
    private readonly WorkerThread[] _workerThreads;

    private readonly Lock _lock;
    private readonly SemaphoreSlim _workSignal;
    private readonly CancellationTokenSource _cts;

    private bool _disposed = false;

    public int WorkerCount => _workerThreads.Length;

    internal bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class with the specified number of worker threads.
    /// </summary>
    /// <param name="threadCount">The number of worker threads to create. If less than 1, at least one thread will be created.</param>
    public JobScheduler(int threadCount)
    {
        _jobDataAllocator = new(8);
        _jobInfoPool = new();
        _jobQueue = new();

        _lock = new();
        _workSignal = new(0);
        _cts = new();

        var workerCount = Math.Max(1, threadCount);
        _workerThreads = new WorkerThread[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            _workerThreads[i] = new WorkerThread(i, this);
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

    private void EnqueueJobIfReady(JobHandle handle)
    {
        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle._id, handle._generation, out var exist);

        if (exist && Volatile.Read(ref jobInfo.dependencyCount) == 0)
        {
            if (Interlocked.CompareExchange(ref jobInfo.state, JobState.Scheduled, JobState.Created) != JobState.Created)
            {
                return;
            }

            ConcurrentQueue<JobHandle> jobQueue;
            if (jobInfo.threadIndex >= 0 && jobInfo.threadIndex < _workerThreads.Length)
            {
                jobQueue = _workerThreads[jobInfo.threadIndex].LocalQueue;
            }
            else
            {
                jobQueue = _jobQueue;
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

    private JobHandle CreateJobHandle(ref JobInfo jobInfo, params ReadOnlySpan<JobHandle> dependencies)
    {
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

            lock (_lock)
            {
                ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(dependency._id, dependency._generation, out var exist);
                if (!exist || Volatile.Read(ref Unsafe.As<JobState, int>(ref depJobInfo.state)) == (int)JobState.Completed)
                {
                    continue;
                }

                if (depJobInfo.dependentCount >= JobInfo.MAX_DEPENDENTS)
                {
                    // Too many dependents
                    // TODO: Handle this case properly
                    _jobDataAllocator.Free(jobInfo.pJobData);
                    return JobHandle.Invalid;
                }

                depJobInfo.dependentsID[depJobInfo.dependentCount] = id;
                depJobInfo.dependentsGeneration[depJobInfo.dependentCount] = generation;
                depJobInfo.dependentCount++;
            }

            Interlocked.Increment(ref infoInPool.dependencyCount);
        }

        EnqueueJobIfReady(handle);

        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasWork()
    {
        return !_jobQueue.IsEmpty || _workerThreads.Any(w => !w.LocalQueue.IsEmpty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WaitForWork()
    {
        _workSignal.Wait(_cts.Token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryStealJob(int threadIndex, out JobHandle outHandle)
    {
        if (threadIndex >= 0 && threadIndex < _workerThreads.Length
            && _workerThreads[threadIndex].LocalQueue.TryDequeue(out outHandle))
        {
            return true;
        }
        else if (_jobQueue.TryDequeue(out outHandle))
        {
            return true;
        }

        outHandle = JobHandle.Invalid;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref JobInfo GetJobInfoReference(JobHandle handle, out bool exist)
    {
        if (!handle.IsValid)
        {
            exist = false;
            return ref Unsafe.NullRef<JobInfo>();
        }

        return ref _jobInfoPool.GetElementReferenceAt(handle._id, handle._generation, out exist);
    }

    internal void MarkJobComplete(JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        ref var info = ref _jobInfoPool.GetElementReferenceAt(handle._id, handle._generation, out var exist);
        if (!exist)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref info.state, JobState.Completed, JobState.Running) != JobState.Running)
        {
            return;
        }

        var dependentsToNotify = stackalloc JobHandle[JobInfo.MAX_DEPENDENTS];
        var dependentCount = 0;

        lock (_lock)
        {
            dependentCount = info.dependentCount;
            for (var i = 0; i < dependentCount; i++)
            {
                dependentsToNotify[i] = new JobHandle(info.dependentsID[i], info.dependentsGeneration[i]);
            }
        }

        _jobDataAllocator.Free(info.pJobData);
        _jobInfoPool.Remove(handle._id, handle._generation);

        for (var i = 0; i < dependentCount; i++)
        {
            var depHandle = dependentsToNotify[i];

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(depHandle._id, depHandle._generation, out var depExist);
            if (depExist && Interlocked.Decrement(ref depJobInfo.dependencyCount) == 0)
            {
                EnqueueJobIfReady(depHandle);
            }
        }
    }

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="dependency">A <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.
    ///     Use <see cref="JobHandle.Invalid"/> if there are no dependencies.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle Schedule<T>(ref T job, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJob
    {
        var jobData = _jobDataAllocator.Allocate(MemoryUtility.SizeOf<T>(), MemoryUtility.AlignOf<T>());
        if (jobData == null)
        {
            return JobHandle.Invalid;
        }

        fixed (T* pJob = &job)
        {
            MemoryUtility.MemCpy(pJob, jobData, MemoryUtility.SizeOf<T>());
        }

        var jobInfo = new JobInfo
        {
            pJobData = jobData,
            pExecutionFunc = &JobExecutor.Execute<T>,

            remainingBatches = 1,
            threadIndex = threadIndex,

            jobRanges = JobRanges.Single,
        };

        return CreateJobHandle(ref jobInfo, dependency);
    }

    /// <summary>
    /// Schedules a single job for execution on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle Schedule<T>(ref T job, int threadIndex)
        where T : unmanaged, IJob
        => Schedule(ref job, threadIndex, JobHandle.Invalid);

    /// <summary>
    /// Schedules a single job for execution on any thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle Schedule<T>(ref T job, JobHandle dependency)
        where T : unmanaged, IJob
        => Schedule(ref job, -1, dependency);

    /// <summary>
    /// Schedules a single job for execution on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle Schedule<T>(ref T job)
        where T : unmanaged, IJob
        => Schedule(ref job, -1, JobHandle.Invalid);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="dependency">A <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.
    ///     Use <see cref="JobHandle.Invalid"/> if there are no dependencies.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle ScheduleParallel<T>(ref T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJobParallelFor
    {
        var jobData = _jobDataAllocator.Allocate(MemoryUtility.SizeOf<T>(), MemoryUtility.AlignOf<T>());
        if (jobData == null)
        {
            return JobHandle.Invalid;
        }

        fixed (T* pJob = &job)
        {
            MemoryUtility.MemCpy(pJob, jobData, MemoryUtility.SizeOf<T>());
        }

        var optimalBatchSize = Math.Max(1, batchSize);
        var totalBatches = (totalIteration + optimalBatchSize - 1) / optimalBatchSize;

        var jobInfo = new JobInfo
        {
            pJobData = jobData,
            pExecutionFunc = &JobExecutor.ExecuteParallel<T>,

            remainingBatches = totalBatches,
            threadIndex = threadIndex,

            jobRanges = new()
            {
                currentIndex = 0,
                batchSize = optimalBatchSize,
                totalIteration = totalIteration,
            },
        };

        return CreateJobHandle(ref jobInfo, dependency);
    }

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle ScheduleParallel<T>(ref T job, int totalIteration, int batchSize, int threadIndex)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallel(ref job, totalIteration, batchSize, threadIndex, JobHandle.Invalid);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread, with an optional dependency on another job..
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle ScheduleParallel<T>(ref T job, int totalIteration, int batchSize, JobHandle dependency)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallel(ref job, totalIteration, batchSize, -1, dependency);

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that will execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    public JobHandle ScheduleParallel<T>(ref T job, int totalIteration, int batchSize)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallel(ref job, totalIteration, batchSize, -1, JobHandle.Invalid);

    /// <summary>
    /// Combines multiple job dependencies into a single <see cref="JobHandle"/>.
    /// </summary>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> instances representing the dependencies to combine.</param>
    /// <returns>A <see cref="JobHandle"/> that represents the combined dependencies. The returned handle can be used to ensure
    ///     that all specified dependencies are completed before proceeding.</returns>
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

        return CreateJobHandle(ref jobInfo, dependencies);
    }

    /// <summary>
    /// Retrieves the current status of a job identified by the specified handle.
    /// </summary>
    /// <param name="handle">The handle representing the job whose status is to be retrieved. The handle must be valid.</param>
    /// <returns>The current status of the job as a <see cref="JobState"/> value.
    ///     Returns <see cref="JobState.Invalid"/> if the handle is invalid or the job does not exist.</returns>
    public JobState GetJobStatus(JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return JobState.Invalid;
        }

        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle._id, handle._generation, out var exist);
        if (!exist)
        {
            return JobState.Completed; // We assume completed if not found. Invalid state is reserved for error.
        }

        return (JobState)Volatile.Read(ref Unsafe.As<JobState, int>(ref jobInfo.state));
    }

    /// <summary>
    /// Blocks the calling thread until the specified job is completed.
    /// </summary>
    /// <param name="handle">The handle of the job to wait for.</param>
    public void WaitComplete(JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        var spin = new SpinWait();
        while (_jobInfoPool.TryGetElement(handle._id, handle._generation, out var jobInfo))
        {
            if (jobInfo.state == JobState.Completed)
            {
                return;
            }

            spin.SpinOnce(_SLEEP_THRESHOLD);
        }
    }

    /// <summary>
    /// Blocks the calling thread until all specified job handles have completed.
    /// </summary>
    /// <remarks>This method waits for all jobs referenced by the provided handles to complete before
    /// returning. The calling thread will be blocked until every job has finished. If any handle is invalid or does not
    /// correspond to an active job, it is considered completed. This method is not thread-safe and should not be called
    /// concurrently from multiple threads.</remarks>
    /// <param name="handles">A collection of job handles to wait for. Each handle represents an asynchronous job whose completion is awaited.
    /// The collection must not be empty.</param>
    public void WaitAll(params ReadOnlySpan<JobHandle> handles)
    {
        var sleepThreshold = _SLEEP_THRESHOLD * handles.Length;
        var spin = new SpinWait();

        while (true)
        {
            var completedCount = 0;
            foreach (var handle in handles)
            {
                if (!_jobInfoPool.Contain(handle._id, handle._generation))
                {
                    completedCount++;
                }
            }

            if (completedCount == handles.Length)
            {
                return;
            }

            spin.SpinOnce(sleepThreshold);
        }
    }

    /// <summary>
    /// Waits until any of the specified job handles has completed and returns the first completed handle.
    /// </summary>
    /// <remarks>This method blocks the calling thread until at least one of the specified jobs has finished.
    /// The returned handle corresponds to the job that completed first among those provided. The order of handles in
    /// the span may affect which handle is returned if multiple jobs complete simultaneously.</remarks>
    /// <param name="handles">A read-only span containing the job handles to monitor for completion. Each handle represents a job whose
    /// completion status will be checked.</param>
    /// <returns>The first job handle from the provided collection that has completed.</returns>
    public JobHandle WaitAny(params ReadOnlySpan<JobHandle> handles)
    {
        var sleepThreshold = _SLEEP_THRESHOLD * handles.Length;
        var spin = new SpinWait();

        while (true)
        {
            foreach (var handle in handles)
            {
                if (!_jobInfoPool.Contain(handle._id, handle._generation))
                {
                    return handle;
                }
            }

            spin.SpinOnce(sleepThreshold);
        }
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

        _jobInfoPool.Clear();
        _jobQueue.Clear();
        _jobDataAllocator.Dispose();

        _cts.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}