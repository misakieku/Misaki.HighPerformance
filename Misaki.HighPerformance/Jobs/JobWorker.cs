namespace Misaki.HighPerformance.Jobs;

internal readonly struct JobWorker<T>(T job, JobHandle handle) : IThreadPoolWorkItem where T : struct, IJob
{
    public void Execute()
    {
        job.Execute();
        handle.CompleteOne();
    }
}

internal readonly struct ParallelJobWorker<T>(T job, JobHandle handle, int start, int end) : IThreadPoolWorkItem where T : struct, IJobParallelFor
{
    public void Execute()
    {
        for (var i = start; i < end; i++)
        {
            job.Execute(i);
        }

        handle.CompleteOne();
    }
}