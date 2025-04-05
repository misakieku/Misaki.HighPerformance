using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Unsafe.Buffer;
using Misaki.HighPerformance.Unsafe.Collections;

namespace Misaki.HighPerformance.Test;

[MemoryDiagnoser]
public class CollectionBenchmark
{
    [Params(10, 100, 1000)]
    public int count = 100;

    [GlobalSetup]
    public void Setup()
    {
        AllocationManager.Initialize(512_000);
    }

    [Benchmark]
    public void Array()
    {
        var array = new int[count];
    }

    [Benchmark]
    public void UnsafeArray()
    {
        var array = new UnsafeArray<int>(count, Allocator.Temp);
        AllocationManager.Reset();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        AllocationManager.Dispose();
    }
}