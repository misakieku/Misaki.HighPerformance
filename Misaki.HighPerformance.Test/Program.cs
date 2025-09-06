using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.Jobs;

// Test the job system
JobSystemExample.RunExample();

Console.WriteLine("\nPress any key to run benchmarks...");
Console.ReadKey();

BenchmarkDotNet.Running.BenchmarkRunner.Run<MathematicsBenchmark>();
//var b = new MathematicsBenchmark();
//b.Vector2Add();
