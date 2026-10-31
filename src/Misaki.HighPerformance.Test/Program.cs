using BenchmarkDotNet.Running;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.UnitTest;
using Misaki.HighPerformance.Test.UnitTest.Jobs;
using System.Buffers;
using System.Numerics;

//BenchmarkRunner.Run<MathematicsBenchmark>();
//var b = new MathematicsBenchmark();
//b.f4Add();

//var test = new JobSchedulerStressTests();
//test.Setup();
//Console.WriteLine("Starting Diamond stress test loop...");
//for (var round = 1; round <= 20; round++)
//{
//    Console.WriteLine($"--- Round {round} ---");
//    test.StressTest_DeepDependencyDiamond_EdgeRecycling();
//}
//Console.WriteLine("All rounds passed!");
//test.Cleanup();
//return;

var f4x4 = new float4x4(
    new float4(1, 2, 3, 4),
    new float4(5, 6, 7, 8),
    new float4(9, 10, 11, 12),
    new float4(13, 14, 15, 16)
);

Console.WriteLine($"f4x4: {f4x4}");
Console.WriteLine($"f4x4.Transpose(): {math.transpose(f4x4)}");
