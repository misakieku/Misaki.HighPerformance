using BenchmarkDotNet.Attributes;
using System.Numerics;

namespace Misaki.HighPerformance.Test.Benchmark;

public class HashCodeBenchmark
{
    private struct Component
    {
        public int Value;
        public int Value2;
        public float Value3;
        public Guid Value4;
        public Matrix4x4 Value5;
        public Vector4 Value6;
    }

    [Params(100, 1000, 10000)]
    public int count;

    private Component _component = new Component()
    {
        Value = 0,
        Value2 = 1,
        Value3 = 2,
        Value4 = Guid.NewGuid(),
        Value5 = Matrix4x4.Identity,
        Value6 = Vector4.One
    };

    private Dictionary<Type, int> _hashCache = new();
    //private UnsafeHashMap<Guid, int> _hashMap = new(16);

    private bool _disposed;

    //~HashCodeBenchmark()
    //{
    //    Dispose();
    //}

    [Benchmark]
    public void Hash()
    {
        for (var i = 0; i < count; i++)
        {
            _ = _component.GetHashCode();
        }
    }

    [Benchmark]
    public void HashWithCache()
    {
        for (var i = 0; i < count; i++)
        {
            var type = typeof(Component);
            if (!_hashCache.TryGetValue(type, out var hash))
            {
                hash = type.GetHashCode();
                _hashCache[type] = hash;
            }

            _ = hash;
        }
    }

    //[Benchmark]
    //public void HashWithUnsafeHashMap()
    //{
    //    for (var i = 0; i < count; i++)
    //    {
    //        var type = _component.GetType();
    //        var guid = type.GUID;
    //        if (!_hashMap.TryGetValue(guid, out var hash))
    //        {
    //            hash = type.GetHashCode();
    //            _hashMap.Add(guid, hash);
    //            _hashMap.Test(ref _hashMap._hashMap);
    //        }

    //        _ = hash;
    //    }
    //}

    //public void Dispose()
    //{
    //    if (_disposed)
    //    {
    //        return;
    //    }

    //    _hashMap.Dispose();

    //    GC.SuppressFinalize(this);
    //}
}