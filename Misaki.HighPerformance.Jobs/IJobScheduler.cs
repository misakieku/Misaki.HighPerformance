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
