namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// The state of a job in its lifecycle.
/// </summary>
public enum JobState
{
    /// <summary>
    /// The job is in an invalid state, indicating an error or uninitialized state. Or already finished the execution and cleaned up by the system.
    /// </summary>
    Invalid = -1,
    /// <summary>
    /// The job has been created but not yet scheduled for execution.
    /// </summary>
    Created = 0,
    /// <summary>
    /// The job is scheduled and waiting to be executed.
    /// </summary>
    Scheduled = 1,
    /// <summary>
    /// The job is currently being executed.
    /// </summary>
    Running = 2,
    /// <summary>
    /// The job has completed execution.
    /// </summary>
    Completed = 3
}

internal unsafe struct JobInfo
{
    public const int MAX_DEPENDENTS = 8;

    // The list of jobs that are waiting for THIS job to complete.
    public fixed int dependentsID[MAX_DEPENDENTS]; // The actual list of IDs
    public fixed int dependentsGeneration[MAX_DEPENDENTS]; // The actual list of generations
    public int dependentCount;

    public JobRanges jobRanges;

    public void* pJobData;
    public JobExecutionFunc pExecutionFunc;

    public JobState state;
    public int remainingBatches;

    public int threadIndex; // The preferred thread index to run this job on, -1 means any thread
    public int dependencyCount; // Numbers of jobs that this job depends on, when it reaches 0, the job can be executed
}

internal struct JobRanges
{
    public int batchSize;
    public int totalIteration;
    public int currentIndex;

    public static JobRanges Single => new()
    {
        batchSize = 1,
        totalIteration = 1,
        currentIndex = 0,
    };
}