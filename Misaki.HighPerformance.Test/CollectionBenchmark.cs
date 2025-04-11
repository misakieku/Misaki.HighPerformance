using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Unsafe.Buffer;
using Misaki.HighPerformance.Unsafe.Collections;

namespace Misaki.HighPerformance.Test;

[MemoryDiagnoser]
public unsafe class CollectionBenchmark
{
    [Params(10, 100, 1000)]
    public int count;

    [GlobalSetup]
    public void Setup()
    {
        AllocationManager.Initialize();
    }

    [Benchmark]
    public void Array()
    {
        var array = new int[count];
        for (var i = 0; i < count; i++)
        {
            array[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public void UnsafeArray()
    {
        var array = new UnsafeArray<int>(count, Allocator.Temp);
        for (var i = 0; i < count; i++)
        {
            array[i] = i;
        }
    }

    [Benchmark]
    public void StackArray()
    {
        var array = stackalloc int[count];
        for (var i = 0; i < count; i++)
        {
            array[i] = i;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        AllocationManager.Dispose();
    }
}