//var threadCount = 8;
//var map = new ConcurrentSlotMap<int>();

//var barrier = new Barrier(threadCount);

//Parallel.For(0, threadCount, threadIndex =>
//{
//    barrier.SignalAndWait();
//    for (var i = 0; i < 1000; i++)
//    {
//        var id = map.Add(i + threadIndex * 1000, out var gen);
//        if (i % 100 == 0)
//        {
//            map.Remove(id, gen);
//        }
//    }
//});

//Console.WriteLine($"Count should be {threadCount * 990}, actual: {map.Count}");

using Misaki.HighPerformance.Test.Benchmark;

//BenchmarkDotNet.Running.BenchmarkRunner.Run<ParallelNoiseBenchmark>();

var benchmark = new ParallelNoiseBenchmark();
var sw = new System.Diagnostics.Stopwatch();

benchmark.Setup();

for (var i = 0; i < 1024; i++)
{
    benchmark.JobSystem();
}

sw.Start();

for (var i = 0; i < 1024; i++)
{
    benchmark.JobSystem();
}

sw.Stop();

benchmark.Cleanup();

Console.WriteLine($"JobSystem: {sw.Elapsed.TotalMilliseconds / 1024.0} ms");
