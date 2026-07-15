using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.UnitTest;
using Misaki.HighPerformance.Test.UnitTest.Jobs;
using System.Numerics;

BenchmarkDotNet.Running.BenchmarkRunner.Run<ObjectPoolBenchmark>();

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
