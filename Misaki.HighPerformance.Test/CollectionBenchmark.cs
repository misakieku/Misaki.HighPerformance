using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Collections.Services;

namespace Misaki.HighPerformance.Test;

[MemoryDiagnoser]
public class CollectionBenchmark
{
    [Params(10, 100, 1000)]
    public int count = 100;

    [Benchmark]
    public void Array()
    {
        for (var i = 0; i < count; i++)
        {
            var array = new int[count];
        }
    }

    [Benchmark]
    public void UnsafeArray()
    {
        for (var i = 0; i < count; i++)
        {
            var array = new UnsafeArray<int>(count, Allocator.Temp);
            for (var j = 0; j < count; j++)
            {
                array[j] = j;
            }

            foreach (var item in array)
            {
                Console.WriteLine(item);
            }

            AllocationManager.Reset();
        }
    }
}