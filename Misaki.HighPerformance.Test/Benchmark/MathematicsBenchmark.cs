#define ADD_BENCHMARK

using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Test.Benchmark;

public class MathematicsBenchmark
{
#if ADD_BENCHMARK
    private Vector4 _va = new Vector4(1, 2, 1, 2);
    private Vector4 _vb = new Vector4(3, 4, 3, 4);

    private float4 _fa = new float4(1, 2, 1, 2);
    private float4 _fb = new float4(3, 4, 3, 4);

    [Benchmark]
    public Vector4 VectorAdd()
    {
        for (var i = 0; i < 10; i++)
        {
            _va += _vb;
        }

        return _va;
    }

    [Benchmark]
    public float4 floatAdd()
    {
        for (var i = 0; i < 10; i++)
        {
            _fa += _fb;
        }

        return _fa;
    }
#endif

#if FMA_BENCHMARK
    private Vector4 _va = new Vector4(1, 2, 1, 2);
    private Vector4 _vb = new Vector4(3, 4, 3, 4);
    private Vector4 _vc = new Vector4(5, 6, 5, 6);

    private Vector128<float> _va128 = Vector128.Create(1f, 2f, 1f, 2f);
    private Vector128<float> _vb128 = Vector128.Create(3f, 4f, 3f, 4f);
    private Vector128<float> _vc128 = Vector128.Create(5f, 6f, 5f, 6f);

    private float4 _fa = new float4(1, 2, 1, 2);
    private float4 _fb = new float4(3, 4, 3, 4);
    private float4 _fc = new float4(5, 6, 5, 6);

    [Benchmark]
    public Vector4 Vector4()
    {
        for (var i = 0; i < 10; i++)
        {
            _va = _vb * _vc + _va;
        }

        return _va;
    }

    [Benchmark]
    public Vector128<float> VectorFMA()
    {
        for (var i = 0; i < 10; i++)
        {
            _va128 = System.Runtime.Intrinsics.X86.Fma.MultiplyAdd(_vb128, _vc128, _va128);
        }

        return _va128;
    }

    [Benchmark]
    public float4 floatFMA()
    {
        for (var i = 0; i < 10; i++)
        {
            _fa = _fb * _fc + _fa;
        }

        return _fa;
    }

#endif

#if MATRIX_BENCHMARK
    private float4x4 _a;
    private float4x4 _b;
    private Matrix4x4 _ma;
    private Matrix4x4 _mb;

    [GlobalSetup]
    public void Init()
    {
        _a = new float4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);
        _b = new float4x4(
            16, 15, 14, 13,
            12, 11, 10, 9,
            8, 7, 6, 5,
            4, 3, 2, 1);

        _ma = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);
        _mb = new Matrix4x4(
            16, 15, 14, 13,
            12, 11, 10, 9,
            8, 7, 6, 5,
            4, 3, 2, 1);
    }

    [Benchmark]
    public float4x4 Float4x4Multiplication()
    {
        return math.mul(_a, _b);
    }

    [Benchmark]
    public Matrix4x4 Matrix4x4Multiplication()
    {
        return Matrix4x4.Multiply(_ma, _mb);
    }
#endif
}