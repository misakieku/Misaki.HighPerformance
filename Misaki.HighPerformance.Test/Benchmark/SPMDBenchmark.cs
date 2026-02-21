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

    [Benchmark]
    public void JobNoise()
    {
        var job = new Jobs.NoiseJobVectorFor
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };
        
        var handle = _scheduler.ScheduleParallelFor(ref job, _SIZE * _SIZE, 64, -1, JobHandle.Invalid);
        _scheduler.WaitComplete(handle);
    }

    //[Benchmark]
    public void MathJobNoise()
    {
        var job = new Jobs.NoiseJobMath
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        var handle = _scheduler.ScheduleParallel(ref job, _SIZE * _SIZE, 64, -1, JobHandle.Invalid);
        _scheduler.WaitComplete(handle);
    }

    //[Benchmark]
    public void ParallelNoise()
    {
        var job = new Jobs.NoiseJobVectorFor
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        Parallel.For(0, _SIZE * _SIZE, (i) =>
        {
            job.Execute(i, 0);
        });
    }

    [Benchmark]
    public void SingleThreadNoise()
    {
        var job = new Jobs.NoiseJobVectorFor
        {
            buffers = _buf,
            width = _SIZE,
            height = _SIZE,
        };

        job.Run(_SIZE * _SIZE, 0);
    }
}
