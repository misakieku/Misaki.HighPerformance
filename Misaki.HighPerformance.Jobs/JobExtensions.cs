namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Extension methods for scheduling jobs in a more user-friendly way.
/// These methods provide the public API for the job system.
/// </summary>
public static class JobExtensions
{
    /// <summary>
    /// Schedules an IJob for execution.
    /// </summary>
    /// <typeparam name="T">The job type implementing IJob.</typeparam>
    /// <param name="jobData">The job data to execute.</param>
    /// <param name="dependsOn">Optional job handle this job depends on.</param>
    /// <returns>A job handle that can be used to wait for completion or create dependencies.</returns>
    public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = default)
        where T : class, IJob
    {
        return JobStruct<T>.Schedule(jobData, dependsOn);
    }

    /// <summary>
    /// Schedules an IJobParallelFor for parallel execution.
    /// </summary>
    /// <typeparam name="T">The job type implementing IJobParallelFor.</typeparam>
    /// <param name="jobData">The job data to execute.</param>
    /// <param name="arrayLength">The total number of iterations to execute.</param>
    /// <param name="innerLoopBatchCount">The batch size for each worker thread. If 0 or negative, an optimal batch size will be calculated.</param>
    /// <param name="dependsOn">Optional job handle this job depends on.</param>
    /// <returns>A job handle that can be used to wait for completion or create dependencies.</returns>
    public static JobHandle ScheduleParallel<T>(this T jobData, int arrayLength, int innerLoopBatchCount = 0, JobHandle dependsOn = default)
        where T : class, IJobParallelFor
    {
        return ParallelForJobStruct<T>.ScheduleParallel(jobData, arrayLength, innerLoopBatchCount, dependsOn);
    }

    /// <summary>
    /// Schedules an IJobParallelFor for parallel execution with automatic batch size calculation.
    /// </summary>
    /// <typeparam name="T">The job type implementing IJobParallelFor.</typeparam>
    /// <param name="jobData">The job data to execute.</param>
    /// <param name="arrayLength">The total number of iterations to execute.</param>
    /// <param name="dependsOn">Optional job handle this job depends on.</param>
    /// <returns>A job handle that can be used to wait for completion or create dependencies.</returns>
    public static JobHandle ScheduleParallel<T>(this T jobData, int arrayLength, JobHandle dependsOn)
        where T : class, IJobParallelFor
    {
        return ParallelForJobStruct<T>.ScheduleParallel(jobData, arrayLength, 0, dependsOn);
    }
}
