using BenchmarkDotNet.Running;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.Jobs;

//BenchmarkRunner.Run<SPMDBenchmark>();
var hashMap = new UnsafeHashMap<int, int>(10, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
hashMap[0] = 5;
hashMap[1] = 6;

Console.WriteLine(hashMap[1]);

ref var v = ref hashMap.GetValueRefOrAddDefault(1, out var exists);

Console.WriteLine(exists);

v = 10;
Console.WriteLine(hashMap[1]);

hashMap.Dispose();