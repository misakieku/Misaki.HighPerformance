using BenchmarkDotNet.Running;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.Jobs;

BenchmarkRunner.Run<SPMDBenchmark>();
