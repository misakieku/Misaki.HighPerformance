using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.Benchmark;

public unsafe class SPMDBenchmark
{
    private const int _SIZE = 512;

    private JobScheduler _scheduler = null!;
    private float* _buf;

    [GlobalSetup]
    public void Setup()
    {
        _scheduler = new JobScheduler(Environment.ProcessorCount);
        _buf = (float*)NativeMemory.Alloc(sizeof(float) * _SIZE * _SIZE);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scheduler.Dispose();
        NativeMemory.Free(_buf);
    }

    [Benchmark]
    public void VectorNoiseSingleThread()
    {
        var job = new Jobs.NoiseJobVector
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        job.Run(_SIZE * _SIZE, 0);
    }

    //[Benchmark]
    public void VectorNoise()
    {
        var job = new Jobs.NoiseJobVector
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        var handle = _scheduler.ScheduleParallel(ref job, _SIZE * _SIZE, 64);
        _scheduler.WaitComplete(handle);
    }

    //[Benchmark]
    public void MathNoise()
    {
        var job = new Jobs.NoiseJobMath
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        var handle = _scheduler.ScheduleParallel(ref job, _SIZE * _SIZE, 64);
        _scheduler.WaitComplete(handle);
    }

    //[Benchmark(Baseline = true)]
    public void ManualSPMDNoise()
    {
        var job = new Jobs.NoiseJobMathV
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        var iterations = (_SIZE * _SIZE + 8 - 1) / 8;
        var handle = _scheduler.ScheduleParallel(ref job, iterations, 64);
        _scheduler.WaitComplete(handle);
    }

    [Benchmark(Baseline = true)]
    public void SPMDNoise()
    {
        var job = new Jobs.NoiseJobMathSPMD
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        var handle = _scheduler.ScheduleParallelSPDM<Jobs.NoiseJobMathSPMD, float>(ref job, _SIZE * _SIZE, 64, -1, JobHandle.Invalid);
        _scheduler.WaitComplete(handle);
    }
}
