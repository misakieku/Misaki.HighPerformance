using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Helpers;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

[TestClass]
public unsafe class TestJobSystem
{
    private JobScheduler _jobScheduler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _jobScheduler.Dispose();
    }

    [TestMethod]
    public void SingleJob()
    {
        var result = stackalloc float[1];
        var job = new TwoSumJob
        {
            value1 = 1.5f,
            value2 = 2.5f,
            result = result
        };

        var handle = _jobScheduler.Schedule(ref job, -1);
        _jobScheduler.WaitComplete(handle);

        Assert.AreEqual(4.0f, *result);
    }

    [TestMethod]
    public void JobDependency()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 1.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = _jobScheduler.Schedule(ref job1, -1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = _jobScheduler.Schedule(ref job2, -1, handle1);
        _jobScheduler.WaitComplete(handle2);

        Assert.AreEqual(8.0f, *result);
    }

    [TestMethod]
    public void CompletedDependency()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 1.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = _jobScheduler.Schedule(ref job1, -1);
        _jobScheduler.WaitComplete(handle1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = _jobScheduler.Schedule(ref job2, -1, handle1);
        _jobScheduler.WaitComplete(handle2);

        Assert.AreEqual(8.0f, *result);
    }

    [TestMethod]
    public void CombineDependencies()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 2.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = _jobScheduler.Schedule(ref job1, -1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = _jobScheduler.Schedule(ref job2, -1, handle1);

        var job3 = new AddJob
        {
            value = 10.0f,
            result = result
        };

        var combinedHandle = _jobScheduler.CombineDependencies(handle1, handle2);
        var handle3 = _jobScheduler.Schedule(ref job3, -1, combinedHandle);

        _jobScheduler.WaitComplete(handle3);

        Assert.AreEqual(19.0f, *result);
    }

    [TestMethod]
    public void SingleParallelJob()
    {
        const int size = 1000;
        var result = stackalloc float[size];
        MemoryUtilities.MemSet(result, 0, sizeof(float) * size);
        var job = new ParallelAddJob
        {
            value = 1.0f,
            inout = result
        };

        var handle = _jobScheduler.ScheduleParallel(ref job, size, 64, -1, JobHandle.Invalid);
        _jobScheduler.WaitComplete(handle);

        Assert.AreEqual(1.0f, result[500]);
    }

    private static float ComputeExpectedSum(int arraySize)
    {
        // Original sum: 1 + 2 + 3 + ... + n = n(n+1)/2
        var originalSum = arraySize * (arraySize + 1) / 2f;

        // After adding 10: each element increases by 10, so total increases by 10 * n
        var afterAdd = originalSum + (10f * arraySize);

        // After multiplying by 2: everything doubles
        var afterMultiply = afterAdd * 2f;

        return afterMultiply;
    }

    [TestMethod]
    public void ChainJob()
    {
        const int arraySize = 10000;

        using var array = new UnsafeArray<float>(arraySize, Allocator.Persistent);

        for (var i = 0; i < arraySize; i++)
        {
            array[i] = i + 1;
        }

        var addJob = new ParallelAddJob
        {
            value = 10f,
            inout = (float*)array.GetUnsafePtr()
        };

        var multiplyJob = new ParallelMultiplyJob
        {
            multiplier = 2f,
            inout = (float*)array.GetUnsafePtr()
        };

        var result = stackalloc float[1];
        var sumJob = new KahanSumJob
        {
            input = (float*)array.GetUnsafePtr(),
            length = arraySize,
            output = result
        };

        var handle1 = _jobScheduler.ScheduleParallel(ref addJob, arraySize, 64, -1, JobHandle.Invalid);
        var handle2 = _jobScheduler.ScheduleParallel(ref multiplyJob, arraySize, 64, -1, handle1);
        var handle3 = _jobScheduler.Schedule(ref sumJob, -1, handle2);

        _jobScheduler.WaitComplete(handle3);

        var expected = ComputeExpectedSum(arraySize);
        Assert.AreEqual(expected, *result, 0.01f);
    }
}