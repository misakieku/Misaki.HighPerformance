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

//using Misaki.HighPerformance.Test.Benchmark;

//BenchmarkDotNet.Running.BenchmarkRunner.Run<MathematicsBenchmark>();

using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

var scope = AllocationManager.CreateStackScope();
var array = new UnsafeArray<int>(10, Allocator.Stack);
for (var i = 0; i < array.Count; i++)
{
    array[i] = i;
}

foreach (var item in array)
{
    Console.WriteLine(item);
}

scope.Dispose();
