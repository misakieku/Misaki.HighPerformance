using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.Jobs;

internal struct JobDispatchingJob : IJobParallelFor
{
    public UnsafeArray<UnsafeArray<int>> data;
    public UnsafeList<JobHandle>.ParallelWriter handles;

    private struct InnerJob : IJobParallelFor
    {
        public UnsafeArray<int> data;

        public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
        {
            ref var value = ref data[loopIndex];
            value *= 2;
        }
    }

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        if (loopIndex % 2 == 0)
        {
            var innerJob = new InnerJob
            {
                data = data[loopIndex]
            };

            var handle = ctx.JobScheduler.ScheduleParallelFor(in innerJob, data[loopIndex].Length, 64, ctx.ThreadIndex);
            handles.AddNoResize(handle);
        }
    }
}