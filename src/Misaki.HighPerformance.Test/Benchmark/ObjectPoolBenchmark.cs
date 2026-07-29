using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Buffer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.Benchmark;

[MemoryDiagnoser]
public class ObjectPoolBenchmark
{
    private ObjectPool<List<int>> _objPool = null!;
    private MemoryPool<Stack, Stack.CreationOptions> _mmePool;

    [GlobalSetup]
    public void Init()
    {
        _objPool = new ObjectPool<List<int>>(() => new List<int>(10), null);
        _mmePool = new MemoryPool<Stack, Stack.CreationOptions>(new Stack.CreationOptions
        {
            size = 1 * 1024 * 1024
        });
    }

    [GlobalCleanup]
    public void Shutdown()
    {
        _objPool.Dispose();
        _mmePool.Dispose();
    }

    [Benchmark]
    public List<int> ObjectPool()
    {
        var list = _objPool.Rent();
        _objPool.Return(list);
        return list;
    }

    [Benchmark]
    public UnsafeList<int> MemoryPool()
    {
        using var scope = _mmePool.Allocator.CreateScope(_mmePool.AllocationHandle);
        using var list = new UnsafeList<int>(10, scope.AllocationHandle);
        return list;
    }

    [Benchmark]
    public List<int> NoPool()
    {
        var list = new List<int>(10);
        return list;
    }
}
