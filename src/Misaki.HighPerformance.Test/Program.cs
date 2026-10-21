using BenchmarkDotNet.Running;
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

//BenchmarkRunner.Run<MathematicsBenchmark>();
var b = new MathematicsBenchmark();
b.f4Add();

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
