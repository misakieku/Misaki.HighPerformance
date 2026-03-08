namespace Misaki.HighPerformance.Jobs;

public readonly ref struct JobExecutionContext
{
    public int ThreadIndex
    {
        get;
    }

    public IJobScheduler JobScheduler
    {
        get;
    }

    public JobExecutionContext(int threadIndex, IJobScheduler jobScheduler)
    {
        ThreadIndex = threadIndex;
        JobScheduler = jobScheduler;
    }
}