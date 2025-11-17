using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Test.Benchmark;

public class CollectionBenchmark
{
    private UnsafeArray<Vector256<int>> _array;

    [Params(10, 100, 1000)]
    public int count;

    [GlobalSetup]
    public void Setup()
    {
        _array = new UnsafeArray<Vector256<int>>(count, Allocator.Persistent);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _array.Dispose();
    }

    [Benchmark]
    public void WithCapacityChecks()
    {
        for (var i = 0; i < _array.Count; i++)
        {
            if (i < 0 || i >= _array.Count)
            {
                throw new IndexOutOfRangeException();
            }

            _array[i] = default;
        }
    }

    [Benchmark]
    public void WithoutCapacityChecks()
    {
        for (var i = 0; i < _array.Count; i++)
        {
            _array[i] = default;
        }
    }
}