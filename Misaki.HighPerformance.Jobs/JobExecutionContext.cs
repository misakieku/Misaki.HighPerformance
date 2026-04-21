namespace Misaki.HighPerformance.Jobs;

public readonly ref struct JobExecutionContext
{
    /// <summary>
    /// Gets the index of the current thread executing the job.
    /// </summary>
    public int ThreadIndex
    {
        get; init;
    }

    /// <summary>
    /// Gets the job scheduler that is responsible for managing the execution of jobs.
    /// </summary>
    public JobScheduler JobScheduler
    {
        get; init;
    }

    /// <summary>
    /// Gets the state object for the job scheduler.
    /// </summary>
    public object? State
    {
        get; init;
    }

    /// <summary>
    /// Gets the handle for the currently executing job.
    /// </summary>
    public JobHandle SelfHandle
    {
        get; init;
    }
}