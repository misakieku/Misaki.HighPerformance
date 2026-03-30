using Misaki.HighPerformance.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Jobs;

public interface IJobScheduler
{
    /// <summary>
    /// Gets the number of worker threads managed by the job scheduler.
    /// </summary>
    int WorkerCount
    {
        get;
    }

    /// <summary>
    /// Schedules a single job for execution on a specified thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="dependency">A <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.
    ///     Use <see cref="JobHandle.Invalid"/> if there are no dependencies.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a single job for execution on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, int threadIndex)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a single job for execution on any thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, JobHandle dependency)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a single job for execution on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="dependency">A <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.
    ///     Use <see cref="JobHandle.Invalid"/> if there are no dependencies.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread, with an optional dependency on another job..
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="dependency">A <see cref="JobHandle"/> representing the dependencies that must be completed before this job can begin.
    ///     Use <see cref="JobHandle.Invalid"/> if there are no dependencies.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread, with an optional dependency on another job..
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Combines multiple job dependencies into a single <see cref="JobHandle"/>.
    /// </summary>
    /// <param name="dependencies">A collection of <see cref="JobHandle"/> instances representing the dependencies to combine.</param>
    /// <returns>A <see cref="JobHandle"/> that represents the combined dependencies. The returned handle can be used to ensure
    ///     that all specified dependencies are completed before proceeding.</returns>
    JobHandle CombineDependencies(params ReadOnlySpan<JobHandle> dependencies);

    /// <summary>
    /// Retrieves the current status of a job identified by the specified handle.
    /// </summary>
    /// <param name="handle">The handle representing the job whose status is to be retrieved. The handle must be valid.</param>
    /// <returns>The current status of the job as a <see cref="JobState"/> value.
    ///     Returns <see cref="JobState.Invalid"/> if the handle is invalid or the job does not exist.</returns>
    JobState GetJobStatus(JobHandle handle);

    /// <summary>
    /// Blocks the calling thread until the specified job is completed.
    /// </summary>
    /// <param name="handle">The handle of the job to wait for.</param>
    void Wait(JobHandle handle);

    /// <summary>
    /// Blocks the calling thread until all specified job handles have completed.
    /// </summary>
    /// <remarks>
    /// The collection handles will be reordered in-place to move completed handles to the front.
    /// </remarks>
    /// <param name="handles">A collection of job handles to wait for.</param>
    void WaitAll(params Span<JobHandle> handles);

    /// <summary>
    /// Waits until any of the specified job handles has completed and returns the first completed handle.
    /// </summary>
    /// <param name="handles">A read-only span containing the job handles to monitor for completion.</param>
    /// <returns>The first job handle from the provided collection that has completed.</returns>
    JobHandle WaitAny(params ReadOnlySpan<JobHandle> handles);
}

/// <summary>
/// Provides a mechanism for scheduling and executing jobs across multiple worker threads.
/// </summary>
public sealed unsafe partial class JobScheduler : IJobScheduler, IDisposable
{
    // Don't sleep indefinitely because that causes our 1ms job to become 15ms.
    private const int _SLEEP_THRESHOLD = -1;

    // Lock-Free constants: State mask (low 16 bits) and RC unit (1 << 16)
    private const int _STATE_MASK = 0xFFFF;
    private const int _RC_ONE = 0x10000;

    private readonly ConcurrentSlotMap<JobInfo> _jobInfoPool;
    private readonly ConcurrentQueue<JobHandle> _jobQueue;
    private readonly WorkerThread[] _workerThreads;

    private readonly SemaphoreSlim _workSignal;
    private readonly CancellationTokenSource _cts;

    private bool _disposed = false;

    internal volatile int _totalJobCount;

    internal bool IsCancellationRequested => _cts.IsCancellationRequested;

    public int WorkerCount => _workerThreads.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class with the specified number of worker threads.
    /// </summary>
    /// <param name="threadCount">The number of worker threads to create. If less than 1, at least one thread will be created.</param>
    public JobScheduler(int threadCount)
    {
        var workerCount = Math.Max(1, threadCount);

        _jobInfoPool = new();
        _jobQueue = new();

        _workSignal = new(0);
        _cts = new();

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
        ref var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);

        if (exist && Volatile.Read(ref jobInfo.dependencyCount) == 0)
        {
            // Note: JobState.Created is 0, JobState.Scheduled is 1. We assume RC logic doesn't touch initial state (RC=0).
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

            Interlocked.Increment(ref _totalJobCount);
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

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(dependency.ID, dependency.Generation, out var exist);
            if (!exist)
            {
                // Dependency does not exist (likely completed already)
                continue;
            }

            // Lock-free registration: Try to acquire "Reader Lock" by incrementing RC in high bits.
            // If state is already Completed, we skip (dependency met).
            var registered = false;
            var completed = false;
            var spin = new SpinWait();

            while (true)
            {
                var stateVal = Volatile.Read(ref Unsafe.As<JobState, int>(ref depJobInfo.state));
                var state = (JobState)(stateVal & _STATE_MASK);

                if (state == JobState.Completed)
                {
                    completed = true;
                    break;
                }

                // Attempt to increment RC (Reader Count)
                if (Interlocked.CompareExchange(ref Unsafe.As<JobState, int>(ref depJobInfo.state), stateVal + _RC_ONE, stateVal) == stateVal)
                {
                    // RC acquired. We are safe from "Remove" and state change.
                    var count = Interlocked.Increment(ref depJobInfo.dependentCount);
                    if (count <= JobInfo.MAX_DEPENDENTS)
                    {
                        // Safely write to the fixed buffer
                        depJobInfo.dependentsID[count - 1] = id;
                        depJobInfo.dependentsGeneration[count - 1] = generation;
                        registered = true;
                    }

                    // Release RC
                    Interlocked.Add(ref Unsafe.As<JobState, int>(ref depJobInfo.state), -_RC_ONE);

                    if (!registered)
                    {
                        // Failed to register because MAX_DEPENDENTS reached.
                        // Backtrack the counter increment.
                        Interlocked.Decrement(ref depJobInfo.dependentCount);

                        // Cleanup and fail
                        NativeMemory.Free(jobInfo.pJobData);
                        return JobHandle.Invalid;
                    }

                    break;
                }

                spin.SpinOnce(-1);
            }

            if (!registered && !completed)
            {
                // Should not happen if logic is correct, unless loop logic changed
                Interlocked.Increment(ref infoInPool.dependencyCount);
            }
            else if (registered)
            {
                // Successfully added dependency
                Interlocked.Increment(ref infoInPool.dependencyCount);
            }
            // else: completed is true, registered is false -> Dependency is already done, so we don't increment our dependencyCount.
        }

        EnqueueJobIfReady(handle);

        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasWork()
    {
        if (!_jobQueue.IsEmpty)
        {
            return true;
        }

        for (var i = 0; i < _workerThreads.Length; i++)
        {
            if (!_workerThreads[i].LocalQueue.IsEmpty)
            {
                return true;
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
    internal bool TryStealFromMain(int threadIndex, out JobHandle outHandle)
    {
        return _jobQueue.TryDequeue(out outHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryStealFromWorker(int threadIndex, out JobHandle outHandle)
    {
        return _workerThreads[threadIndex].LocalQueue.TryDequeue(out outHandle);
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
        if (!handle.IsValid)
        {
            return;
        }

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
            var stateVal = Volatile.Read(ref Unsafe.As<JobState, int>(ref info.state));
            var state = (JobState)(stateVal & _STATE_MASK);

            if (state == JobState.Completed)
            {
                return;
            }

            //if (state != JobState.Running)
            //{
            //    // If in valid state (e.g. Scheduled?), we still assume we can complete it.
            //    // Usually it should be Running.
            //}

            // Construct new value: State=Completed, preserve RC (temporarily) or strictly replace only low bits?
            // We set low bits to Completed. High bits (RC) remain.
            var newState = (stateVal & ~_STATE_MASK) | (int)JobState.Completed;

            if (Interlocked.CompareExchange(ref Unsafe.As<JobState, int>(ref info.state), newState, stateVal) == stateVal)
            {
                // Successfully set State to Completed. New readers will see Completed and back off.
                // Now we must wait for existing readers to finish (RC to become 0).
                while (true)
                {
                    var current = Volatile.Read(ref Unsafe.As<JobState, int>(ref info.state));
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

        // We now have exclusive access to dependentsID (no new readers, old readers finished).
        var dependentCount = info.dependentCount;
        var dependentsToNotify = stackalloc JobHandle[dependentCount];
        for (var i = 0; i < dependentCount; i++)
        {
            dependentsToNotify[i] = new JobHandle(info.dependentsID[i], info.dependentsGeneration[i]);
        }

        NativeMemory.Free(info.pJobData);
        _jobInfoPool.Remove(handle.ID, handle.Generation);
        Interlocked.Decrement(ref _totalJobCount);

        for (var i = 0; i < dependentCount; i++)
        {
            var depHandle = dependentsToNotify[i];

            ref var depJobInfo = ref _jobInfoPool.GetElementReferenceAt(depHandle.ID, depHandle.Generation, out var depExist);
            if (depExist && Interlocked.Decrement(ref depJobInfo.dependencyCount) == 0)
            {
                EnqueueJobIfReady(depHandle);
            }
        }
    }

    public JobHandle Schedule<T>(ref readonly T job, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJob
    {
        var pJobData = NativeMemory.Alloc((nuint)sizeof(T));
        if (pJobData == null)
        {
            return JobHandle.Invalid;
        }

        Unsafe.Copy(pJobData, in job);

        var jobInfo = new JobInfo
        {
            pJobData = pJobData,
            pExecutionFunc = &JobExecutor.Execute<T>,

            remainingBatches = 1,
            threadIndex = threadIndex,

            jobRanges = JobRanges.Single,
        };

        return CreateJobHandle(ref jobInfo, dependency);
    }

    public JobHandle Schedule<T>(ref readonly T job, int threadIndex)
        where T : unmanaged, IJob
        => Schedule(in job, threadIndex, JobHandle.Invalid);

    public JobHandle Schedule<T>(ref readonly T job, JobHandle dependency)
        where T : unmanaged, IJob
        => Schedule(in job, -1, dependency);

    public JobHandle Schedule<T>(ref readonly T job)
        where T : unmanaged, IJob
        => Schedule(in job, -1, JobHandle.Invalid);

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJobParallelFor
    {
        var pJobData = NativeMemory.Alloc((nuint)sizeof(T));
        if (pJobData == null)
        {
            return JobHandle.Invalid;
        }

        fixed (T* pJob = &job)
        {
            NativeMemory.Copy(pJobData, pJob, (nuint)sizeof(T));
        }

        var optimalBatchSize = Math.Max(1, batchSize);
        var totalBatches = (totalIteration + optimalBatchSize - 1) / optimalBatchSize;

        var jobInfo = new JobInfo
        {
            pJobData = pJobData,
            pExecutionFunc = &JobExecutor.ExecuteParallelFor<T>,

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

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, threadIndex, JobHandle.Invalid);

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, -1, dependency);

    public JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize)
        where T : unmanaged, IJobParallelFor
        => ScheduleParallelFor(in job, totalIteration, batchSize, -1, JobHandle.Invalid);

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency)
        where T : unmanaged, IJobParallel
    {
        var pJobData = NativeMemory.Alloc((nuint)sizeof(T));
        if (pJobData == null)
        {
            return JobHandle.Invalid;
        }

        fixed (T* pJob = &job)
        {
            NativeMemory.Copy(pJobData, pJob, (nuint)sizeof(T));
        }

        var optimalBatchSize = Math.Max(1, batchSize);
        var totalBatches = (totalIteration + optimalBatchSize - 1) / optimalBatchSize;

        var jobInfo = new JobInfo
        {
            pJobData = pJobData,
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

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex)
        where T : unmanaged, IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, threadIndex, JobHandle.Invalid);

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency)
        where T : unmanaged, IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, -1, dependency);

    public JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize)
        where T : unmanaged, IJobParallel
        => ScheduleParallel(in job, totalIteration, batchSize, -1, JobHandle.Invalid);

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
        return (JobState)(Volatile.Read(ref Unsafe.As<JobState, int>(ref jobInfo.state)) & _STATE_MASK);
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
            ref readonly var jobInfo = ref _jobInfoPool.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            // Mask out RC
            if ((jobInfo.state & (JobState)_STATE_MASK) == JobState.Completed)
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

        _workSignal.Dispose();
        _cts.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
