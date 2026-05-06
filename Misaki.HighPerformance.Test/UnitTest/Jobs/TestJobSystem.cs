using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.HPC;
using Misaki.HighPerformance.Test.Jobs;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

[TestClass]
[DoNotParallelize]
public class TestJobSystem
{
    private static JobScheduler s_jobScheduler = null!;

    public TestContext TestContext
    {
        get;
        set;
    }

    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        s_jobScheduler = new JobScheduler(Environment.ProcessorCount);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        s_jobScheduler.Dispose();
    }

    [TestMethod]
    public unsafe void SingleJob()
    {
        var result = stackalloc float[1];
        var job = new TwoSumJob
        {
            value1 = 1.5f,
            value2 = 2.5f,
            result = result
        };

        var handle = s_jobScheduler.Schedule(ref job);
        s_jobScheduler.Wait(handle);

        Assert.AreEqual(4.0f, *result);
    }

    [TestMethod]
    public unsafe void JobDependency()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 1.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = s_jobScheduler.Schedule(ref job1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = s_jobScheduler.Schedule(ref job2, handle1);
        s_jobScheduler.Wait(handle2);

        Assert.AreEqual(8.0f, *result);
    }

    [TestMethod]
    public unsafe void CompletedDependency()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 1.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = s_jobScheduler.Schedule(ref job1);
        s_jobScheduler.Wait(handle1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = s_jobScheduler.Schedule(ref job2, handle1);
        s_jobScheduler.Wait(handle2);

        Assert.AreEqual(8.0f, *result);
    }

    [TestMethod]
    public unsafe void MultipleDependencies()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 2.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = s_jobScheduler.Schedule(ref job1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = s_jobScheduler.Schedule(ref job2, handle1);

        var job3 = new AddJob
        {
            value = 10.0f,
            result = result
        };

        var handle3 = s_jobScheduler.Schedule(ref job3, handle1, handle2);

        s_jobScheduler.Wait(handle3);

        Assert.AreEqual(19.0f, *result);
    }

    [TestMethod]
    public unsafe void CombinedDependencies()
    {
        var result = stackalloc float[1];
        var job1 = new TwoSumJob
        {
            value1 = 2.5f,
            value2 = 2.5f,
            result = result
        };

        var handle1 = s_jobScheduler.Schedule(ref job1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = s_jobScheduler.Schedule(ref job2, handle1);

        var job3 = new AddJob
        {
            value = 10.0f,
            result = result
        };

        var combinedHandle = s_jobScheduler.CombineDependencies(handle1, handle2);
        var handle3 = s_jobScheduler.Schedule(ref job3, combinedHandle);

        s_jobScheduler.Wait(handle3);

        Assert.AreEqual(19.0f, *result);
    }

    [TestMethod]
    public unsafe void SingleParallelJob()
    {
        const int size = 1000;
        var result = stackalloc float[size];
        MemoryUtility.MemSet(result, 0, sizeof(float) * size);
        var job = new ParallelAddJob
        {
            value = 1.0f,
            inout = result
        };

        var handle = s_jobScheduler.ScheduleParallel(ref job, size, 64);
        s_jobScheduler.Wait(handle);

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
    public unsafe void ChainJob()
    {
        const int arraySize = 10000;

        using var array = new UnsafeArray<float>(arraySize, AllocationHandle.Persistent);

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

        var handle1 = s_jobScheduler.ScheduleParallel(ref addJob, arraySize, 64);
        var handle2 = s_jobScheduler.ScheduleParallel(ref multiplyJob, arraySize, 64, handle1);
        var handle3 = s_jobScheduler.Schedule(ref sumJob, handle2);

        s_jobScheduler.Wait(handle3);

        var expected = ComputeExpectedSum(arraySize);
        Assert.AreEqual(expected, *result, 0.01f);
    }

    [TestMethod]
    public unsafe void WaitAll()
    {
        var result1 = stackalloc float[1];
        var result2 = stackalloc float[1];

        var job1 = new AddJob
        {
            value = 1.0f,
            result = result1
        };

        var job2 = new AddJob
        {
            value = 1.0f,
            result = result2
        };

        var handle1 = s_jobScheduler.Schedule(ref job1);
        var handle2 = s_jobScheduler.Schedule(ref job2);

        s_jobScheduler.WaitAll(handle1, handle2);

        Assert.AreEqual(JobState.Completed, s_jobScheduler.GetJobStatus(handle1));
        Assert.AreEqual(JobState.Completed, s_jobScheduler.GetJobStatus(handle2));
    }


    [TestMethod]
    public async Task WaitAllAsync()
    {
        AddJob job1;
        AddJob job2;

        unsafe
        {
            var result1 = stackalloc float[1];
            var result2 = stackalloc float[1];

            job1 = new AddJob
            {
                value = 1.0f,
                result = result1
            };

            job2 = new AddJob
            {
                value = 1.0f,
                result = result2
            };
        }

        var handle1 = s_jobScheduler.Schedule(ref job1);
        var handle2 = s_jobScheduler.Schedule(ref job2);

        await s_jobScheduler.WaitAllAsync(new Memory<JobHandle>(new[] { handle1, handle2 }));

        Assert.AreEqual(JobState.Completed, s_jobScheduler.GetJobStatus(handle1));
        Assert.AreEqual(JobState.Completed, s_jobScheduler.GetJobStatus(handle2));
    }

    [TestMethod]
    public unsafe void WaitAny()
    {
        var result1 = stackalloc float[1];
        var result2 = stackalloc float[1];

        var job1 = new AddJob
        {
            value = 1.0f,
            result = result1
        };

        var job2 = new AddJob
        {
            value = 1.0f,
            result = result2
        };

        var handle1 = s_jobScheduler.Schedule(ref job1);
        var handle2 = s_jobScheduler.Schedule(ref job2);

        var completedHandle = s_jobScheduler.WaitAny(handle1, handle2);

        Assert.AreEqual(JobState.Completed, s_jobScheduler.GetJobStatus(completedHandle));
    }

    [TestMethod]
    public unsafe void SPMDCorrectness()
    {
        const int size = 8;

        var vectorBuf = stackalloc float[size * size];
        var vs = new Span<float>(vectorBuf, size * size);
        var vectorJob = new NoiseJobVector
        {
            buffers = vectorBuf,
            width = size,
            height = size,
        };

        vectorJob.Run(size * size, default);

        var spmdBuf = stackalloc float[size * size];
        var ss = new Span<float>(spmdBuf, size * size);
        var spmdJob = new NoiseJobMath
        {
            buffers = spmdBuf,
            width = size,
            height = size,
        };

        spmdJob.Run(size * size, default);

        var eq = vs.SequenceCompareTo(ss);
        Assert.AreEqual(0, eq);
    }

    [TestMethod]
    public void DynamicDispatch()
    {
        using var arr = new UnsafeArray<UnsafeArray<int>>(256, AllocationHandle.Persistent);
        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = new UnsafeArray<int>(256, AllocationHandle.Persistent);
            for (var j = 0; j < arr[i].Length; j++)
            {
                arr[i][j] = j;
            }
        }

        using var handles = new UnsafeList<JobHandle>(arr.Length, AllocationHandle.Persistent);

        var job = new JobDispatchingJob
        {
            data = arr,
            handles = handles.AsParallelWriter()
        };

        var handle = s_jobScheduler.ScheduleParallelFor(ref job, arr.Length, 64);

        s_jobScheduler.Wait(handle);
        s_jobScheduler.WaitAll(handles.AsSpan());

        for (var i = 0; i < arr.Length; i++)
        {
            if (i % 2 == 0)
            {
                for (var j = 0; j < arr[i].Length; j++)
                {
                    Assert.AreEqual(j * 2, arr[i][j]);
                }
            }
        }

        for (var i = 0; i < arr.Length; i++)
        {
            arr[i].Dispose();
        }
    }

    [TestMethod]
    public unsafe void ScheduleCustomJob()
    {
        var value = stackalloc int[1];
        *value = 0;

        var customJob = new CustomJob
        {
            value = value
        };

        var customJobDesc = new CustomJobDesc<CustomJob>
        {
            data = ref customJob,
            pExecutionFunc = &CustomJob.Execute,
            pFreeFunc = &CustomJob.Free,
            jobRanges = JobRanges.Single,
            priority = JobPriority.Normal,
        };

        var handle = s_jobScheduler.ScheduleCustom(ref customJobDesc);
        s_jobScheduler.Wait(handle);

        Assert.AreEqual(1, *value);
    }
}