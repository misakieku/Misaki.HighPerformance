#define VECTOR_BENCHMARK

using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Misaki.HighPerformance.Test.Benchmark;

public class MathematicsBenchmark
{
#if VECTOR_BENCHMARK
    private Vector2 _v2a = new Vector2(1, 2);
    private Vector2 _v2b = new Vector2(3, 4);

    private float2 _f2a = new float2(1, 2);
    private float2 _f2b = new float2(3, 4);

    [Benchmark]
    public Vector2 VectorAdd()
    {
        var v = new Vector2(0, 0);

        for (var i = 0; i < 10; i++)
        {
            v = _v2a + _v2b;
        }

        return v;
    }

    [Benchmark]
    public float2 float2Add()
    {
        var v = new float2(0, 0);

        for (var i = 0; i < 10; i++)
        {
            v = _f2a + _f2b;
        }

        return v;
    }
#endif

#if NOISE_BENCHMARK

    private const int _SIZE = 32;

    [Benchmark]
    public void VectorNoise()
    {
        var buf = stackalloc float[_SIZE * _SIZE];
        var job = new Misaki.HighPerformance.Test.Jobs.NoiseJobVector
        {
            buffers = buf,
            width = _SIZE,
            height = _SIZE,
        };

        for (var i = 0; i < _SIZE * _SIZE; i++)
        {
            job.Execute(i, 0);
        }
    }

    [Benchmark]
    public void MathNoise()
    {
        var buf = stackalloc float[_SIZE * _SIZE];
        var job = new Misaki.HighPerformance.Test.Jobs.NoiseJobMath
        {
            buffers = buf,
            width = _SIZE,
            height = _SIZE,
        };

        for (var i = 0; i < _SIZE * _SIZE; i++)
        {
            job.Execute(i, 0);
        }
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