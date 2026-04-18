using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.Benchmark;

public struct BigStruct
{
    public int a, b, c, d, e, f, g, h, i, j;
    public long k, l, m, n, o, p, q, r, s, t;
}

public class Integer
{
    public BigStruct value;
}

public class ConcurrentSlotMapBenchmark
{
    private readonly ConcurrentSlotMap<BigStruct> _concurrentMap = new ConcurrentSlotMap<BigStruct>(1000);
    private readonly SlotMap<BigStruct> _slotMap = new SlotMap<BigStruct>(1000);
    private readonly BigStruct[] _slots = new BigStruct[1000];
    private readonly Integer[] _objects = new Integer[1000];

    private readonly int2[] _randomIndices1 = new int2[1000];
    private readonly int2[] _randomIndices2 = new int2[1000];
    private readonly int[] _randomSlots = new int[1000];

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < 1000; i++)
        {
            var element = new BigStruct
            {
                a = i
            };

            var id = _concurrentMap.Add(element, out var generation);
            _randomIndices1[i] = new int2(id, generation);

            id = _slotMap.Add(element, out generation);
            _randomIndices2[i] = new int2(id, generation);

            _slots[i] = element;
            _objects[i] = new Integer { value = element };

            _randomSlots[i] = i;
        }

        Random.Shared.Shuffle(_randomIndices1);
        Random.Shared.Shuffle(_randomIndices2);
        Random.Shared.Shuffle(_randomSlots);
    }

    [Benchmark]
    public void ConcurrentSlotMap()
    {
        for (var i = 0; i < 1000; i++)
        {
            var v = _randomIndices1[i];
            ref var element = ref _concurrentMap.GetElementReferenceAt(v.x, v.y, out var _);
            element.a += 1;
        }
    }

    [Benchmark]
    public void SlotMap()
    {
        for (var i = 0; i < 1000; i++)
        {
            var v = _randomIndices2[i];
            ref var element = ref _slotMap.GetElementReferenceAt(v.x, v.y, out var _);
            element.a += 1;
        }
    }

    [Benchmark]
    public void Array()
    {
        for (var i = 0; i < 1000; i++)
        {
            _slots[_randomSlots[i]].a += 1;
        }
    }

    [Benchmark]
    public void ObjectArray()
    {
        for (var i = 0; i < 1000; i++)
        {
            _objects[_randomSlots[i]].value.a += 1;
        }
    }
}
