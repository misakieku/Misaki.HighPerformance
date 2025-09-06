using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.Benchmark;

[MemoryDiagnoser]
public unsafe class FunctionPtrBenchmark
{
    private delegate float FunctionPointerDelegate(float a, float b);

    private float _sink;

    private FunctionPointer<FunctionPointerDelegate> _addManaged;
    private delegate* unmanaged<float, float, float> _addUnmanaged;

    public FunctionPtrBenchmark()
    {
        _addManaged = new(Add);
        _addUnmanaged = &AddUnmanaged;
    }

    private float Add(float a, float b)
    {
        return a + b;
    }

    [UnmanagedCallersOnly]
    private static float AddUnmanaged(float a, float b)
    {
        return a + b;
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void InvokeManaged()
    {
        _sink = _addManaged.Delegate(1.0f, 2.0f);
    }

    [Benchmark]
    public void InvokeUnmanaged()
    {
        _sink = _addUnmanaged(1.0f, 2.0f);
    }
}