namespace Misaki.HighPerformance.Jobs;

internal sealed class WaitItem : IThreadPoolWorkItem
{
    private readonly IJobScheduler _scheduler;
    private readonly JobHandle _jobHandle;

    private readonly TaskCompletionSource _completionSource;

    public Task Task => _completionSource.Task;

    public WaitItem(IJobScheduler scheduler, JobHandle jobHandle, CancellationToken cancellationToken)
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
    private readonly IJobScheduler _scheduler;
    private readonly Memory<JobHandle> _jobHandles;

    private readonly TaskCompletionSource _completionSource;

    public Task Task => _completionSource.Task;

    public WaitAllItem(IJobScheduler scheduler, Memory<JobHandle> jobHandles, CancellationToken cancellationToken)
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
    private readonly IJobScheduler _scheduler;
    private readonly ReadOnlyMemory<JobHandle> _jobHandles;

    private readonly TaskCompletionSource<JobHandle> _completionSource;
    
    public Task<JobHandle> Task => _completionSource.Task;
    
    public WaitAnyItem(IJobScheduler scheduler, ReadOnlyMemory<JobHandle> jobHandles, CancellationToken cancellationToken)
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
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, int threadIndex, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a single job for execution on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, int threadIndex, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a single job for execution on any thread, with an optional dependency on another job.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJob;

    /// <summary>
    /// Schedules a single job for execution on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJob"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle Schedule<T>(ref readonly T job, JobPriority priority = JobPriority.Normal)
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
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread, with an optional dependency on another job..
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="dependency">The job that this job depends on.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallelFor;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallelFor<T>(ref readonly T job, int totalIteration, int batchSize, JobPriority priority = JobPriority.Normal)
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
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on a specified thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="threadIndex">The index of the thread that is preferred to execute the job. This is used to assign thread-specific data. Use -1 to allow any thread to execute the job.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, int threadIndex, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread, with an optional dependency on another job..
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="dependency">The job that this job depends on.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobHandle dependency, JobPriority priority = JobPriority.Normal)
        where T : unmanaged, IJobParallel;

    /// <summary>
    /// Schedules a parallel job for execution, dividing the workload into batches and distributing it across threads on any thread without dependency.
    /// </summary>
    /// <typeparam name="T">The type of the job to execute. Must implement <see cref="IJobParallelFor"/> and be unmanaged.</typeparam>
    /// <param name="job">The job instance to be executed. The job data will be copied internally.</param>
    /// <param name="totalIteration">The total number of iterations to be processed by the job.</param>
    /// <param name="batchSize">The number of iterations to include in each batch.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <returns>A <see cref="JobHandle"/> that can be used to track the completion of the scheduled job.
    ///     Returns <see cref="JobHandle.Invalid"/> if the job data allocation fails.</returns>
    JobHandle ScheduleParallel<T>(ref readonly T job, int totalIteration, int batchSize, JobPriority priority = JobPriority.Normal)
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

    /// <summary>
    /// Waits asynchronously until the specified job is completed, allowing the calling thread to perform other work while waiting.
    /// </summary>
    /// <param name="handle">The handle of the job to wait for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    Task WaitAsync(JobHandle handle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits asynchronously until all specified job handles have completed, allowing the calling thread to perform other work while waiting.
    /// </summary>
    /// <remarks>
    /// The collection handles will be reordered in-place to move completed handles to the front.
    /// </remarks>
    /// <param name="handles">A read-only memory containing the job handles to monitor for completion.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    Task WaitAllAsync(Memory<JobHandle> handles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits asynchronously until any of the specified job handles has completed, allowing the calling thread to perform other work while waiting, and returns the first completed handle.
    /// </summary>
    /// <param name="handles">A read-only memory containing the job handles to monitor for completion.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    Task<JobHandle> WaitAnyAsync(ReadOnlyMemory<JobHandle> handles, CancellationToken cancellationToken = default);
}
