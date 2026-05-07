using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.UnitTest;
using Misaki.HighPerformance.Test.UnitTest.Jobs;
using System.Buffers;

//BenchmarkRunner.Run<GGXMipGenerationBenchmark>();

//const int count = 16;

//var bench = new GGXMipGenerationBenchmark();
//bench.Setup();

//for (var i = 0; i < count; i++)
//{
//    bench.JobGGX();
//}

//var sw = System.Diagnostics.Stopwatch.StartNew();

//for (var i = 0; i < count; i++)
//{
//    bench.JobGGX();
//}

//sw.Stop();
//var avgTime = sw.Elapsed.TotalMilliseconds / count;
//Console.WriteLine($"GGX Mip Generation (Inline): {avgTime} ms");
//bench.Cleanup();

//GlobalSetup.GlobalInitialize(null!);
//TestJobSystem.Initialize(null!);

AllocationManager.Initialize();

Console.WriteLine(0);
for (var i = 0; i < 64; i++)
{
    var size = Random.Shared.Next(2048, 8192);
    var arr = new UnsafeArray<Guid>(size, AllocationHandle.FreeList); // AllocationHandle.FreeList
    arr.Dispose();
}

Thread.Sleep(1000);

Console.WriteLine(1);
for (var i = 0; i < 64; i++)
{
    var size = Random.Shared.Next(2048, 8192);
    var arr = new UnsafeArray<Guid>(size, AllocationHandle.FreeList); // AllocationHandle.FreeList
    arr.Dispose();
}

Console.Read();
AllocationManager.Dispose();