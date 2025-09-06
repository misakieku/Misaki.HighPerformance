using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Misaki.HighPerformance.Test.Benchmark;

public class MathematicsBenchmark
{
    [Params(10, 100)]
    public int count = 10;

    [Benchmark(Baseline = true)]
    public void Vector2Add()
    {
        var a = new Vector2(1, 2);
        var b = new Vector2(5, 6);
        var result = new Vector2();

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }
    }

    [Benchmark]
    public void Float2Add()
    {
        var a = new float2(1);
        var b = new float2(5);
        var result = new float2();

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }
    }
}