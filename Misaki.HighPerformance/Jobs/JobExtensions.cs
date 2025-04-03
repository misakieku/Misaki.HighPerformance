namespace Misaki.HighPerformance.Jobs;

public static class JobExtensions
{
    public static JobHandle Schedule<T>(this T job, bool preferLocal = false)
        where T : struct, IJob
    {
        var handle = new JobHandle(1);
        var worker = new JobWorker<T>(job, handle);
        ThreadPool.UnsafeQueueUserWorkItem(worker, preferLocal);

        return handle;
    }

    public static JobHandle Schedule<T>(this T job, ReadOnlySpan<JobHandle> dependencies, bool preferLocal = false)
        where T : struct, IJob
    {
        foreach (var dependency in dependencies)
        {
            dependency.WaitComplete();
        }

        return job.Schedule(preferLocal);
    }

    public static JobHandle Schedule<T>(this T job, int length, int batchCount, bool preferLocal = false)
        where T : struct, IJobParallelFor
    {
        var batchSize = (length + batchCount - 1) / batchCount;
        var handle = new JobHandle(batchCount);

        for (var i = 0; i < batchCount; i++)
        {
            var start = i * batchSize;
            var end = Math.Min(start + batchSize, length);
            var worker = new ParallelJobWorker<T>(job, handle, start, end);
            ThreadPool.UnsafeQueueUserWorkItem(worker, preferLocal);
        }

        return handle;
    }

    public static JobHandle Schedule<T>(this T job, int length, int batchCount, ReadOnlySpan<JobHandle> dependencies, bool preferLocal = false)
        where T : struct, IJobParallelFor
    {
        foreach (var dependency in dependencies)
        {
            dependency.WaitComplete();
        }

        return job.Schedule(length, batchCount, preferLocal);
    }
}