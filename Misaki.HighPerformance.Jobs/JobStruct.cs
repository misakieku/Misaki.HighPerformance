namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Internal job struct for IJob execution, similar to Unity's JobStruct pattern.
/// This provides the bridge between the job scheduler and user job implementations.
/// </summary>
/// <typeparam name="T">The job type implementing IJob.</typeparam>
internal struct JobStruct<T> where T : class, IJob
{
    /// <summary>
    /// Cached function delegate for this job type.
    /// This avoids allocations during job scheduling.
    /// </summary>
    internal static readonly ExecuteJobDelegate ExecuteDelegate;

    static JobStruct()
    {
        // Create and cache the function delegate
        ExecuteDelegate = Execute;
    }

    /// <summary>
    /// Executes the job. This method matches the ExecuteJobDelegate signature.
    /// </summary>
    /// <param name="jobData">The job data object.</param>
    public static void Execute(object jobData)
    {
        var typedJobData = (T)jobData;
        typedJobData.Execute();
    }

    /// <summary>
    /// Schedules this job type for execution.
    /// </summary>
    /// <param name="jobData">The job data.</param>
    /// <param name="dependsOn">Job handle this job depends on.</param>
    /// <returns>A job handle for the scheduled job.</returns>
    public static JobHandle Schedule(T jobData, JobHandle dependsOn = default)
    {
        return JobScheduler.ScheduleJob(jobData, ExecuteDelegate, JobType.Job, 0, 0, dependsOn);
    }
}

/// <summary>
/// Internal job struct for IJobParallelFor execution, similar to Unity's ParallelForJobStruct.
/// This provides efficient parallel execution with work stealing.
/// </summary>
/// <typeparam name="T">The job type implementing IJobParallelFor.</typeparam>
internal struct ParallelForJobStruct<T> where T : class, IJobParallelFor
{
    /// <summary>
    /// Cached function delegate for this job type.
    /// </summary>
    internal static readonly ExecuteParallelJobDelegate ExecuteDelegate;

    static ParallelForJobStruct()
    {
        // Create and cache the function delegate
        ExecuteDelegate = Execute;
    }

    /// <summary>
    /// Executes the parallel job using work stealing. This method matches the ExecuteParallelJobDelegate signature.
    /// </summary>
    /// <param name="jobData">The job data object.</param>
    /// <param name="ranges">Job ranges for work distribution.</param>
    /// <param name="jobIndex">Index of the current worker thread.</param>
    public static unsafe void Execute(object jobData, ref JobRanges ranges, int jobIndex)
    {
        var typedJobData = (T)jobData;

        while (true)
        {
            if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                break;

            // Execute the batch
            var endThatCompilerCanSeeWillNeverChange = end;
            for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
            {
                typedJobData.Execute(i);
            }
        }
    }

    /// <summary>
    /// Schedules this parallel job type for execution.
    /// </summary>
    /// <param name="jobData">The job data.</param>
    /// <param name="arrayLength">Total number of iterations.</param>
    /// <param name="innerLoopBatchCount">Batch size for each worker. If <= 0, an optimal batch size will be calculated.</param>
    /// <param name="dependsOn">Job handle this job depends on.</param>
    /// <returns>A job handle for the scheduled job.</returns>
    public static JobHandle ScheduleParallel(T jobData, int arrayLength, int innerLoopBatchCount = 0, JobHandle dependsOn = default)
    {
        if (arrayLength <= 0)
            throw new ArgumentException("Array length must be greater than 0", nameof(arrayLength));

        // Calculate optimal batch size if not specified
        if (innerLoopBatchCount <= 0)
        {
            var workerCount = Environment.ProcessorCount;
            innerLoopBatchCount = Math.Max(1, arrayLength / (workerCount * 4));
        }

        return JobScheduler.ScheduleParallelJob(jobData, ExecuteDelegate, arrayLength, innerLoopBatchCount, dependsOn);
    }
}
