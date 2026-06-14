namespace Misaki.HighPerformance.Jobs;

public enum WorkerThreadState
{
    Idle,       // Blocked on semaphore, no work available
    Spinning,   // SpinWait loop, actively looking for work  
    Executing,  // Executing a job
}

public readonly struct WorkerThreadStateEvent
{
    public int ThreadIndex
    {
        get; init;
    }
    public WorkerThreadState State
    {
        get; init;
    }
    public long Timestamp
    {
        get; init;
    }
    public string? JobTypeName
    {
        get; init;
    }
}