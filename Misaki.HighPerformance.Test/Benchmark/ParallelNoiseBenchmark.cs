using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Test.Jobs;
using System.Numerics;

namespace Misaki.HighPerformance.Test.Benchmark;

[MemoryDiagnoser]
public class ParallelNoiseBenchmark
{
    private const int _WIDTH = 64;
    private const int _HEIGHT = 64;
    private const int _LENGTH = _WIDTH * _HEIGHT;

    internal JobScheduler _jobScheduler = null!;
    private UnsafeArray<float> _buffers;

    [GlobalSetup]
    public void Setup()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount - 1);
        _buffers = new UnsafeArray<float>(_LENGTH, Allocator.Persistent);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _jobScheduler.Dispose();
        _buffers.Dispose();
    }

    [Benchmark]
    public unsafe void JobSystem()
    {
        var job = new NoiseJobVector()
        {
            buffers = (float*)_buffers.GetUnsafePtr(),
            width = _WIDTH,
            height = _HEIGHT
        };

        var handle = _jobScheduler.ScheduleParallel(ref job, _LENGTH, 64, -1);
        _jobScheduler.WaitComplete(handle);
    }

    [Benchmark]
    public void ParallelFor()
    {
        Parallel.For(0, _LENGTH, i =>
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y);
            _buffers[i] = NoiseJobVector.GradientNoise(uv);
        });
    }

    [Benchmark(Baseline = true)]
    public void For()
    {
        for (var i = 0; i < _LENGTH; i++)
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y);
            _buffers[i] = NoiseJobVector.GradientNoise(uv);
        }
    }
}