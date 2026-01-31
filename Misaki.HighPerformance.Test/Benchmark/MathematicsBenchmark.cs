#define NOISE_BENCHMARK

using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Test.Benchmark;

public class MathematicsBenchmark
{
#if VECTOR_BENCHMARK
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

#if NOISE_BENCHMARK

    private const int _SIZE = 32;

    [Benchmark]
    public unsafe void VectorNoise()
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
    public unsafe void MathNoise()
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

    [Benchmark]
    // This is 10x faster than VectorNoise and MathNoise, but writing a burst like compiler to compile MathNoise into this is incredibly hard.
    public unsafe void MathVNoise()
    {
        var buf = stackalloc float[_SIZE * _SIZE];
        var job = new Misaki.HighPerformance.Test.Jobs.NoiseJobMathV
        {
            buffers = buf,
            width = _SIZE,
            height = _SIZE,
        };

        for (var i = 0; i < _SIZE * _SIZE / 8; i++)
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