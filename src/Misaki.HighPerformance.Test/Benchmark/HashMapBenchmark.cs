using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.Benchmark;

public class HashMapBenchmark
{
    private UnsafeHashMap<int, float> _unsafeHashMap;
    private Dictionary<int, float> _dictionary = null!;

    [Params(10, 100, 1000)]
    public int count;

    [IterationSetup]
    public void Setup()
    {
        //_unsafeHashMap = new UnsafeHashMap<int, float>(count, AllocationHandle.Persistent);
        _dictionary = new Dictionary<int, float>(count);

        for (var i = 0; i < count; i++)
        {
            //_unsafeHashMap.Add(i, i);
            _dictionary.Add(i, i);
        }
    }

    [Benchmark]
    public void UnsafeHashMapAdd()
    {
        for (var i = 0; i < count; i++)
        {
            _unsafeHashMap.Add(count + i, i);
        }
    }

    [Benchmark(Baseline = true)]
    public void DictionaryAdd()
    {
        for (var i = 0; i < count; i++)
        {
            _dictionary.Add(count + i, i);
        }
    }

    public void UnsafeHashMapRemove()
    {
        for (var i = 0; i < count; i++)
        {
            _unsafeHashMap.Remove(i);
        }
    }

    public void DictionaryRemove()
    {
        for (var i = 0; i < count; i++)
        {
            _dictionary.Remove(i);
        }
    }

    public void UnsafeHashMapRandomRead()
    {
        for (var i = 0; i < count; i++)
        {
            var value = Random.Shared.Next(0, count);
            if (_unsafeHashMap.TryGetValue(value, out var result))
            {
                var r2 = result + result;
            }
        }
    }

    public void DictionaryRandomRead()
    {
        for (var i = 0; i < count; i++)
        {
            var value = Random.Shared.Next(0, count);
            if (_dictionary.TryGetValue(value, out var result))
            {
                var r2 = result + result;
            }
        }
    }

    public void UnsafeHashMapRandomWrite()
    {
        for (var i = 0; i < count; i++)
        {
            var value = Random.Shared.Next(0, count);
            if (_unsafeHashMap.TryGetValue(value, out var result))
            {
                _unsafeHashMap[value] = result + 1;
            }
        }
    }

    public void DictionaryRandomWrite()
    {
        for (var i = 0; i < count; i++)
        {
            var value = Random.Shared.Next(0, count);
            if (_dictionary.TryGetValue(value, out var result))
            {
                _dictionary[value] = result + 1;
            }
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        //_unsafeHashMap.Dispose();
    }
}