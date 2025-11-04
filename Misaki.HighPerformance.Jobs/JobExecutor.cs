namespace Misaki.HighPerformance.Jobs;

internal static unsafe class JobExecutor
{
    public static bool Execute<T>(void* pJobData, ref JobRanges jobRanges, ref int remainingBatches, int threadIndex)
        where T : unmanaged, IJob
    {
        var pJob = (T*)pJobData;
        pJob->Execute(threadIndex);

        return Interlocked.Decrement(ref remainingBatches) == 0;
    }

    private static bool GetWorkerStealingRange(ref JobRanges jobRanges, out int start, out int end)
    {
        start = Interlocked.Add(ref jobRanges.currentIndex, jobRanges.batchSize) - jobRanges.batchSize;

        if (start >= jobRanges.totalIteration)
        {
            end = start;
            return false;
        }

        end = Math.Min(start + jobRanges.batchSize, jobRanges.totalIteration);
        return true;
    }

    public static bool ExecuteParallel<T>(void* pJobData, ref JobRanges jobRanges, ref int remainingBatches, int threadIndex)
        where T : unmanaged, IJobParallelFor
    {
        var pJob = (T*)pJobData;
        var wasTheLastBatch = false;

        while (true)
        {
            if (!GetWorkerStealingRange(ref jobRanges, out var start, out var end))
            {
                break;
            }

            for (var i = start; i < end; i++)
            {
                pJob->Execute(i, threadIndex);
            }

            if (Interlocked.Decrement(ref remainingBatches) == 0)
            {
                wasTheLastBatch = true;
            }
        }

        return wasTheLastBatch;
    }
}