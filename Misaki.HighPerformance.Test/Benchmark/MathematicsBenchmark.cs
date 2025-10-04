using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Misaki.HighPerformance.Test.Benchmark;

public class MathematicsBenchmark
{
    [Params(10)]
    public int count = 10;

    private unsafe static Vector128<float> CreateVector128(float2 value)
    {
        return Vector128.AsSingle(Sse2.LoadScalarVector128((double*)&value));
    }

    [Benchmark]
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
        var a = new float2(1, 2);
        var b = new float2(5, 6);
        var result = new float2();
        //var vr = CreateVector128(result);
        //var va = CreateVector128(a);
        //var vb = CreateVector128(b);

        for (var i = 0; i < count; i++)
        {
            result += a + b;
            //vr = Sse.Add(va, vb);
        }
    }

    //[Benchmark]
    public void Vector4Add()
    {
        var a = new Vector4(1, 2, 3, 4);
        var b = new Vector4(5, 6, 7, 8);
        var result = new Vector4();

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }
    }

    //[Benchmark]
    public void Float4Add()
    {
        var a = new float4(1, 2, 3, 4);
        var b = new float4(5, 6, 7, 8);
        var result = new float4();

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }
    }
}