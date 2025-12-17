#define VECTOR_BENCHMARK

using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Test.Jobs;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Test.Benchmark;

public unsafe class MathematicsBenchmark
{
    public struct f2
    {
        public float x;
        public float y;

        public f2(float x, float y)
        {
            //this = Asf2(Vector128.Create(x, y, 0, 0));
            this.x = x;
            this.y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> AsVector128Unsafe(f2 value)
        {
            return Vector128.Create(value.x, value.y, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static f2 Asf2(Vector128<float> value)
        {
            //f2 result;
            //result.x = value.GetElement(0);
            //result.y = value.GetElement(1);
            //return result;

            ref byte address = ref Unsafe.As<Vector128<float>, byte>(ref value);
            return Unsafe.ReadUnaligned<f2>(ref address);
        }

        public static f2 operator +(f2 lhs, f2 rhs)
        {
            //return Asf2(AsVector128Unsafe(lhs) + AsVector128Unsafe(rhs));
            return new f2(lhs.x + rhs.x, lhs.y + rhs.y);
        }
    }

#if VECTOR_BENCHMARK
    private Vector2 _v2a = new Vector2(1, 2);
    private Vector2 _v2b = new Vector2(3, 4);

    private f2 _f2a = new f2(1, 2);
    private f2 _f2b = new f2(3, 4);

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
    public f2 f2Add()
    {
        var v = new f2(0, 0);

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