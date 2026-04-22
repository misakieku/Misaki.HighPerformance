using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

internal static class JobExecutor
{
    public static void Execute<T>(int dataID, int dataGeneration, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
        where T : struct, IJob
    {
        ref var job = ref JobDataPool<T>.GetReference(dataID, dataGeneration, out var exists);
        Debug.Assert(exists, "Job data not found in the pool.");

        job.Execute(in ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public static void ExecuteParallelFor<T>(int dataID, int dataGeneration, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
        where T : struct, IJobParallelFor
    {
        ref var job = ref JobDataPool<T>.GetReference(dataID, dataGeneration, out var exists);
        Debug.Assert(exists, "Job data not found in the pool.");

        while (true)
        {
            if (!GetWorkerStealingRange(ref jobRanges, out var start, out var end))
            {
                break;
            }

            for (var i = start; i < end; i++)
            {
                job.Execute(i, in ctx);
            }
        }
    }

    public static void ExecuteParallel<T>(int dataID, int dataGeneration, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
        where T : struct, IJobParallel
    {
        ref var job = ref JobDataPool<T>.GetReference(dataID, dataGeneration, out var exists);
        Debug.Assert(exists, "Job data not found in the pool.");

        while (true)
        {
            if (!GetWorkerStealingRange(ref jobRanges, out var start, out var end))
            {
                break;
            }

            job.Execute(start, end, in ctx);
        }
    }
}