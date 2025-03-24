using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Unsafe.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Test;

[MemoryDiagnoser]
public class ParallelNoiseBenchmark
{
    private struct NoiseJob : IJobParallelFor
    {
        public UnsafeArray<float> buffers;
        public int width;
        public int height;
        public void Execute(int index)
        {
            var x = index % width;
            var y = index / height;
            var uv = new Vector2(x, y);
            buffers[index] = GradientNoise(uv);
        }
    }

    private const int _WIDTH = 512;
    private const int _HEIGHT = 512;
    private const int _LENGTH = _WIDTH * _HEIGHT;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Frac(float x)
    {
        return x - MathF.Truncate(x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    private static Vector2 GradientNoiseDirect(Vector2 uv)
    {
        uv.X %= 289;
        uv.Y %= 289;
        var x = (34 * uv.X + 1) * uv.X % 289 + uv.Y;
        x = (34 * x + 1) * x % 289;
        x = Frac(x / 41) * 2 - 1;
        return Vector2.Normalize(new Vector2(x - MathF.Floor(x + 0.5f), MathF.Abs(x) - 0.5f));
    }

    private static float GradientNoise(Vector2 uv)
    {
        var ip = new Vector2(MathF.Floor(uv.X), MathF.Floor(uv.Y));
        var fp = new Vector2(Frac(uv.X), Frac(uv.Y));

        var d00 = Vector2.Dot(GradientNoiseDirect(ip), fp);
        var d01 = Vector2.Dot(GradientNoiseDirect(ip + new Vector2(0, 1)), fp - new Vector2(0, 1));
        var d10 = Vector2.Dot(GradientNoiseDirect(ip + new Vector2(1, 0)), fp - new Vector2(1, 0));
        var d11 = Vector2.Dot(GradientNoiseDirect(ip + new Vector2(1, 1)), fp - new Vector2(1, 1));

        fp = fp * fp * fp * (fp * (fp * new Vector2(6.0f) - new Vector2(15.0f)) + new Vector2(10.0f));
        return Lerp(Lerp(d00, d10, fp.Y), Lerp(d01, d11, fp.Y), fp.X);
    }

    [Benchmark]
    public void JobSystem()
    {
        using var buffers = new UnsafeArray<float>(_WIDTH * _HEIGHT, AllocationType.UnInitialized);
        var job = new NoiseJob()
        {
            buffers = buffers,
            width = _WIDTH,
            height = _HEIGHT
        };

        using var handle = job.Schedule(_LENGTH, 64);
        handle.WaitComplete();
    }

    [Benchmark]
    public void ParallelFor()
    {
        using var buffers = new UnsafeArray<float>(_WIDTH * _HEIGHT, AllocationType.UnInitialized);

        Parallel.For(0, _LENGTH, i =>
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y);
            buffers[i] = GradientNoise(uv);
        });
    }
}