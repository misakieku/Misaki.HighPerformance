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
    private const int _WIDTH = 256;
    private const int _HEIGHT = 256;
    private const int _LENGTH = _WIDTH * _HEIGHT;

    internal JobScheduler _jobScheduler = null!;
    private UnsafeArray<float> _buffers;

    [GlobalSetup]
    public void Setup()
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);

        var desc = new JobSchedulerDesc
        {
            ThreadCount = Environment.ProcessorCount,
            ThreadPriority = ThreadPriority.Normal,
            DependencyChainCapacity = 64,
        };

        _jobScheduler = new JobScheduler(in desc);
        _buffers = new UnsafeArray<float>(_LENGTH, AllocationHandle.Persistent);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _jobScheduler.Dispose();
        _buffers.Dispose();

        AllocationManager.Dispose();
    }

    [Benchmark(Baseline = true)]
    public unsafe void JobSystem()
    {
        var job = new NoiseJobVector()
        {
            buffers = (float*)_buffers.GetUnsafePtr(),
            width = _WIDTH,
            height = _HEIGHT
        };

        var handle = _jobScheduler.ScheduleParallel(ref job, _LENGTH, 64);
        _jobScheduler.Wait(handle);
    }

    [Benchmark]
    public void ParallelFor()
    {
        Parallel.For(0, _LENGTH, i =>
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y) / new Vector2(_WIDTH, _HEIGHT);
            _buffers[i] = NoiseJobVector.GradientNoise(uv);
        });
    }

    [Benchmark]
    public void For()
    {
        for (var i = 0; i < _LENGTH; i++)
        {
            var x = i % _WIDTH;
            var y = i / _HEIGHT;
            var uv = new Vector2(x, y) / new Vector2(_WIDTH, _HEIGHT);
            _buffers[i] = NoiseJobVector.GradientNoise(uv);
        }
    }
}