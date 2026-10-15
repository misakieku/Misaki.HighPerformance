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

//BenchmarkDotNet.Running.BenchmarkRunner.Run<ParallelNoiseBenchmark>();

const int count = 2048;

var bench = new ParallelNoiseBenchmark();
bench.Setup();

for (var i = 0; i < count; i++)
{
    bench.JobSystem();
}

var sw = System.Diagnostics.Stopwatch.StartNew();

for (var i = 0; i < count; i++)
{
    bench.JobSystem();
}

sw.Stop();
var avgTime = sw.Elapsed.TotalMilliseconds / count;
Console.WriteLine($"{avgTime} ms");
bench.Cleanup();

// GlobalSetup.GlobalInitialize(null!);
// TestJobSystem.Initialize(null!);
