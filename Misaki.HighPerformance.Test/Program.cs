//using BenchmarkDotNet.Running;
//using Misaki.HighPerformance.Test;

//BenchmarkRunner.Run<ParallelNoiseBenchmark>();

using Misaki.HighPerformance.Unsafe.Collections;

using var test = new UnsafeArray<Test>(10, AllocationType.UnInitialized);

for (var i = 0; i < 10; i++)
{
    var t = new Test();
    t.buffers[0] = i;
    test[i] = t;
}

test.ReAlloc(20);

for (var i = 0; i < 10; i++)
{
    Console.WriteLine(test[i].buffers[0]);
}

struct Test : IDisposable
{
    public UnsafeArray<float> buffers;

    public Test()
    {
        buffers = new UnsafeArray<float>(1, AllocationType.UnInitialized);
    }

    public void Dispose()
    {
        buffers.Dispose();
    }
}