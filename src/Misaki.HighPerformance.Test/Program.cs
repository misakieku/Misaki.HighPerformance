using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.UnitTest;
using Misaki.HighPerformance.Test.UnitTest.Jobs;
using System.Buffers;
using System.Numerics;

// BenchmarkDotNet.Running.BenchmarkRunner.Run<ObjectPoolBenchmark>();
AllocationManager.Initialize();

try
{
    for (var i = 0; i < 100_000; i++)
    {
        using var jobScheduler = new JobScheduler(new JobSchedulerDesc
        {
            ThreadCount = Environment.ProcessorCount,
            DependencyChainCapacity = 64,
            ThreadPriority = ThreadPriority.Normal
        });

        using var scope = AllocationManager.CreateStackScope();
        using var data = new UnsafeArray<int>(8, scope.AllocationHandle);
        var job = new TestJob { data = data };
        var handle = jobScheduler.ScheduleParallelFor(in job, 8, 64);
        jobScheduler.Wait(handle);

        Console.WriteLine(i);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}

AllocationManager.Dispose();

struct TestJob : IJobParallelFor
{
    public UnsafeArray<int> data;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        data[loopIndex] = loopIndex * 2;
    }
}

// const int count = 16;
//
// var bench = new GGXMipGenerationBenchmark();
// bench.Setup();
//
// for (var i = 0; i < count; i++)
// {
//     bench.JobGGX();
// }
//
// var sw = System.Diagnostics.Stopwatch.StartNew();
//
// for (var i = 0; i < count; i++)
// {
//     bench.JobGGX();
// }
//
// sw.Stop();
// var avgTime = sw.Elapsed.TotalMilliseconds / count;
// Console.WriteLine($"GGX Mip generation (Inline): {avgTime} ms");
// bench.Cleanup();
//
// GlobalSetup.GlobalInitialize(null!);
// TestJobSystem.Initialize(null!);