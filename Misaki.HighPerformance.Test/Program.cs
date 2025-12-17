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

using System.Runtime.Intrinsics;

BenchmarkDotNet.Running.BenchmarkRunner.Run<Misaki.HighPerformance.Test.Benchmark.MathematicsBenchmark>();

//using Misaki.HighPerformance.Collections;
//using Misaki.HighPerformance.LowLevel.Buffer;
//using Misaki.HighPerformance.LowLevel.Collections;

//AllocationManager.EnableDebugLayer();
//using var csm = new UnsafeSlotMap<int>(4, Allocator.Persistent);
//AllocationManager.Dispose();

//using Misaki.HighPerformance.Mathematics;
//using System.Numerics;

//var a = new Misaki.HighPerformance.Mathematics.float4x4(
//    1, 2, 3, 4,
//    5, 6, 7, 8,
//    9, 10, 11, 12,
//    13, 14, 15, 16);
//var b = new Misaki.HighPerformance.Mathematics.float4x4(
//    16, 15, 14, 13,
//    12, 11, 10, 9,
//    8, 7, 6, 5,
//    4, 3, 2, 1);

//Console.WriteLine(math.mul(a, b));

//var ma = new Matrix4x4(
//        1, 2, 3, 4,
//        5, 6, 7, 8,
//        9, 10, 11, 12,
//        13, 14, 15, 16);
//var mb = new Matrix4x4(
//        16, 15, 14, 13,
//        12, 11, 10, 9,
//        8, 7, 6, 5,
//        4, 3, 2, 1);
//Console.WriteLine(Matrix4x4.Multiply(ma, mb));
