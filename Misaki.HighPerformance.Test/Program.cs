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

//using Misaki.HighPerformance.LowLevel.Buffer;
//using Misaki.HighPerformance.LowLevel.Collections;
//using Misaki.HighPerformance.LowLevel.Utilities;

//using (AllocationManager.CreateStackScope())
//{
//    var array = new UnsafeArray<int>(10, Allocator.Stack);
//    for (var i = 0; i < array.Count; i++)
//    {
//        array[i] = i;
//    }

//    foreach (var item in array.AsSpan())
//    {
//        Console.WriteLine(item);
//    }
//}

var arr1 = new Misaki.HighPerformance.LowLevel.Collections.UnsafeArray<int>(10, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
var arr2 = arr1;

arr1.Dispose();
try
{
    arr2[0] = 42; // This should throw an exception because arr1 has been disposed.
    arr2.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"Caught expected exception: {ex.Message}");
}
