using BenchmarkDotNet.Running;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Test.Benchmark;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//BenchmarkRunner.Run<GGXMipGenerationBenchmark>();

const int count = 16;

var bench = new GGXMipGenerationBenchmark();
bench.Setup();

for (int i = 0; i < count; i++)
{
    bench.JobGGX();
}

var sw = System.Diagnostics.Stopwatch.StartNew();

for (int i = 0; i < count; i++)
{
    bench.JobGGX();
}

sw.Stop();
var avgTime = sw.Elapsed.TotalMilliseconds / count;
Console.WriteLine($"GGX Mip Generation (Inline): {avgTime} ms");
bench.Cleanup();