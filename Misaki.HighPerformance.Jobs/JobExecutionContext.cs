namespace Misaki.HighPerformance.Jobs;

public readonly ref struct JobExecutionContext
{
    public int ThreadIndex
    {
        get; init;
    }

    public IJobScheduler JobScheduler
    {
        get; init;
    }

    public object? State
    {
        get; init;
    }

    public JobHandle SelfHandle
    {
        get; init;
    }
}