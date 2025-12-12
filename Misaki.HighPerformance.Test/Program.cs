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

//using Misaki.HighPerformance.LowLevel;

//BenchmarkDotNet.Running.BenchmarkRunner.Run<Misaki.HighPerformance.Test.Benchmark.CollectionBenchmark>();

using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

var csm = new ConcurrentSlotMap<int>();
Console.WriteLine(csm.Contains(0, 0));
Console.WriteLine(csm.Count == 0);