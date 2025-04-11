using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Test.Jobs;
using Misaki.HighPerformance.Unsafe.Collections;
using System.Numerics;

namespace Misaki.HighPerformance.Test;

[MemoryDiagnoser]
public class ParallelNoiseBenchmark
{
    private const int _WIDTH = 512;
    private const int _HEIGHT = 512;
    private const int _LENGTH = _WIDTH * _HEIGHT;

    [Benchmark]
    public static void JobSystem()
    {
        using var buffers = new UnsafeArray<float>(_LENGTH, Allocator.Persistent, AllocationOption.None);
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
    public static void ParallelFor()
    {
        using var buffers = new UnsafeArray<float>(_LENGTH, Allocator.Persistent, AllocationOption.None);

        Parallel.For(0, _LENGTH, i =>
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y);
            buffers[i] = NoiseJob.GradientNoise(uv);
        });
    }

    [Benchmark]
    public static void For()
    {
        using var buffers = new UnsafeArray<float>(_LENGTH, Allocator.Persistent, AllocationOption.None);
        for (var i = 0; i < _LENGTH; i++)
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y);
            buffers[i] = NoiseJob.GradientNoise(uv);
        }
    }
}