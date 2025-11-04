using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Test.Benchmark;

public unsafe class MathematicsBenchmark
{
    public struct f4
    {
        private Vector128<float> _vec;

        public f4(float x, float y, float z, float w)
        {
            _vec = Vector128.Create(x, y, z, w);
        }

        public f4(Vector128<float> vec)
        {
            _vec = vec;
        }

        public static f4 operator +(f4 a, f4 b)
        {
            var result = a._vec + b._vec;
            return Unsafe.As<Vector128<float>, f4>(ref result);
        }
    }

    [Params(100)]
    public int count;

    [Benchmark]
    public Vector2 Vector2Add()
    {
        var a = new Vector2(1, 2);
        var b = new Vector2(3, 4);
        var c = new Vector2(5, 6);

        for (var i = 0; i < count; i++)
        {
            c += a + b;
        }

        return c;
    }

    [Benchmark]
    public float2 Float2Add()
    {
        var a = new float2(1, 2);
        var b = new float2(3, 4);
        var c = new float2(5, 6);

        for (var i = 0; i < count; i++)
        {
            c += a + b;
        }

        return c;
    }

    [Benchmark]
    public Vector4 Vector4Add()
    {
        var a = new Vector4(1, 2, 3, 4);
        var b = new Vector4(5, 6, 7, 8);
        var result = new Vector4();

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }

        return result;
    }

    [Benchmark]
    public float4 Float4Add()
    {
        var a = new float4(1, 2, 3, 4);
        var b = new float4(5, 6, 7, 8);
        var result = new float4();

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }

        return result;
    }

    [Benchmark]
    public f4 f4Add()
    {
        var a = new f4(1, 2, 3, 4);
        var b = new f4(5, 6, 7, 8);
        var result = new f4(0, 0, 0, 0);

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }

        return result;
    }

    [Benchmark]
    public unsafe Vector128<float> v128Add()
    {
        var a = Vector128.Create(1f, 2f, 3f, 4f);
        var b = Vector128.Create(5f, 6f, 7f, 8f);
        var result = Vector128<float>.Zero;

        for (var i = 0; i < count; i++)
        {
            result += a + b;
        }

        return result;
    }
}