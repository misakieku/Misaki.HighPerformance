using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.Benchmark;

[MemoryDiagnoser]
public unsafe class CollectionBenchmark
{
    [Params(10, 100, 1000)]
    public int count;

    [GlobalSetup]
    public void Setup()
    {
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

        ((ArenaAllocator*)AllocationManager.TempHandle.Allocator)->Reset();
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