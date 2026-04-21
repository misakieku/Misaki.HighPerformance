using Misaki.HighPerformance.Jobs;
using System.Numerics;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public interface IJobSPMD<TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    void Execute<TLane>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane : ISPMD<TLane, TNumber>;
}

internal struct SPMDJobWrapper<T, TNumber> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    public T innerJob;
    public int totalCount;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber>.LaneWidth;
        var remaining = totalCount - baseIndex;

        if (remaining >= WideLane<TNumber>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    public T innerJob;
    public int totalCount;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber>>(loopIndex, in ctx);
    }
}

public static class IJobParallelForSPMDExtensions
{
    public static void Run<T, TNumber>(this ref T job, int totalCount, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalCount + WideLane<TNumber>.LaneWidth - 1) / WideLane<TNumber>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber>.LaneWidth;
                var remaining = totalCount - baseIndex;

                if (remaining >= WideLane<TNumber>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalCount; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber>>(loopIndex, in ctx);
            }
        }
    }

    public static JobHandle ScheduleParallelSPDM<T, TNumber>(this JobScheduler jobScheduler, ref T job, int totalCount, int batchSize, int threadIndex, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber>
            {
                innerJob = job,
                totalCount = totalCount,
            };

            var iterations = (totalCount + WideLane<TNumber>.LaneWidth - 1) / WideLane<TNumber>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, threadIndex, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber>
            {
                innerJob = job,
                totalCount = totalCount,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalCount, batchSize, threadIndex, dependencies);
        }
    }
}
