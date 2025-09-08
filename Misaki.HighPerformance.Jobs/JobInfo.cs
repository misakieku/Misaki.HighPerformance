namespace Misaki.HighPerformance.Jobs;

public enum JobStatus
{
    Invalid = -1,
    Created = 0,
    Scheduled = 1,
    Running = 2,
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
    public JobExecuteFunc executeDelegate;

    public JobStatus status;
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