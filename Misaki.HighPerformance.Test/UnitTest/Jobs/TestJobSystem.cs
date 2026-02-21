using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

[TestClass]
[DoNotParallelize]
public unsafe class TestJobSystem
{
    private JobScheduler _jobScheduler = null!;

    public TestContext TestContext
    {
        get;
        set;
    }

    [TestInitialize]
    public void Initialize()
    {
        _jobScheduler = new JobScheduler(3);
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

        var handle1 = _jobScheduler.Schedule(ref job1);
        _jobScheduler.WaitComplete(handle1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = _jobScheduler.Schedule(ref job2, handle1);
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

        var handle1 = _jobScheduler.Schedule(ref job1);

        var job2 = new AddJob
        {
            value = 4.0f,
            result = result
        };

        var handle2 = _jobScheduler.Schedule(ref job2, handle1);

        var job3 = new AddJob
        {
            value = 10.0f,
            result = result
        };

        var combinedHandle = _jobScheduler.CombineDependencies(handle1, handle2);
        var handle3 = _jobScheduler.Schedule(ref job3, combinedHandle);

        _jobScheduler.WaitComplete(handle3);

        Assert.AreEqual(19.0f, *result);
    }

    [TestMethod]
    public void SingleParallelJob()
    {
        const int size = 1000;
        var result = stackalloc float[size];
        MemoryUtility.MemSet(result, 0, sizeof(float) * size);
        var job = new ParallelAddJob
        {
            value = 1.0f,
            inout = result
        };

        var handle = _jobScheduler.ScheduleParallel(ref job, size, 64);
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

        var handle1 = _jobScheduler.ScheduleParallel(ref addJob, arraySize, 64);
        var handle2 = _jobScheduler.ScheduleParallel(ref multiplyJob, arraySize, 64);
        var handle3 = _jobScheduler.Schedule(ref sumJob, handle2);

        _jobScheduler.WaitComplete(handle3);

        var expected = ComputeExpectedSum(arraySize);
        Assert.AreEqual(expected, *result, 0.01f);
    }

    [TestMethod]
    public void WaitAll()
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

        var handle1 = _jobScheduler.Schedule(ref job1);
        var handle2 = _jobScheduler.Schedule(ref job2);

        _jobScheduler.WaitAll(handle1, handle2);

        Assert.AreEqual(JobState.Completed, _jobScheduler.GetJobStatus(handle1));
        Assert.AreEqual(JobState.Completed, _jobScheduler.GetJobStatus(handle2));
    }

    [TestMethod]
    public void WaitAny()
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

        var handle1 = _jobScheduler.Schedule(ref job1);
        var handle2 = _jobScheduler.Schedule(ref job2);

        var completedHandle = _jobScheduler.WaitAny(handle1, handle2);

        Assert.AreEqual(JobState.Completed, _jobScheduler.GetJobStatus(completedHandle));
    }

    [TestMethod]
    public void RaceConditionTest()
    {
        const int jobCount = 20000;
        
        var pExecutedCount = (int*)NativeMemory.Alloc(sizeof(int));
        *pExecutedCount = 0;

        var startSignal = false;

        // 1. Create a "Gatekeeper" vectorJob that spins/blocks a worker thread until signaled.
        //    This allows us to control exactly when the dependency completes.
        var rootJob = new WaitJob { pSignal = &startSignal };
        var rootHandle = _jobScheduler.Schedule(ref rootJob);

        // 2. Start a background task to flood the scheduler with dependencies on the Gatekeeper.
        using var barrier = new Barrier(2);
        var scheduleTask = Task.Run(() =>
        {
            var depJob = new IncrementJob { pCounter = pExecutedCount };
            barrier.SignalAndWait(TestContext.CancellationTokenSource.Token); // Synchronize start with main thread

            for (var i = 0; i < jobCount; i++)
            {
                // CONTENTION POINT: 
                // Trying to add a dependency to 'rootHandle'. 
                // Eventually, this will happen exactly while 'rootHandle' is transitioning to Completed.
                _jobScheduler.Schedule(ref depJob, rootHandle);
            }
        }, TestContext.CancellationTokenSource.Token);

        barrier.SignalAndWait(TestContext.CancellationTokenSource.Token); // Wait for scheduler task to be ready

        // Allow the scheduling loop to get a head start and queue some readers
        Thread.Sleep(5);

        // 3. Open the gate.
        // This triggers the Gatekeeper to complete. It will change its State and iterate its dependency list.
        // This happens CONCURRENTLY with the loop above adding more items to that same list.
        startSignal = true;

        scheduleTask.Wait(TestContext.CancellationTokenSource.Token);

        // 4. Validate results
        // If the lock-free logic works, every single dependent vectorJob must eventually execute.
        // If there is a race (e.g., missed notification), pExecutedCount will stick below jobCount.
        var spin = new SpinWait();
        var timeout = DateTime.Now.AddSeconds(10);

        while (Volatile.Read(ref *pExecutedCount) < jobCount)
        {
            if (DateTime.Now > timeout)
            {
                break;
            }

            spin.SpinOnce();
        }

        // Ensure the root vectorJob is officially cleaned up
        _jobScheduler.WaitComplete(rootHandle);

        Assert.AreEqual(jobCount, *pExecutedCount, "Race condition detected: Some dependent jobs failed to execute (Wait timeout).");

        NativeMemory.Free(pExecutedCount);
    }

    [TestMethod]
    public void SPMDCorrectness()
    {
        const int size = 8;

        var vectorBuf = stackalloc float[size * size];
        var vs = new Span<float>(vectorBuf, size * size);
        var vectorJob = new Misaki.HighPerformance.Test.Jobs.NoiseJobVector
        {
            buffers = vectorBuf,
            width = size,
            height = size,
        };

        vectorJob.Run(size * size, -1);

        var spmdBuf = stackalloc float[size * size];
        var ss = new Span<float>(spmdBuf, size * size);
        var spmdJob = new Misaki.HighPerformance.Test.Jobs.NoiseJobMath
        {
            buffers = spmdBuf,
            width = size,
            height = size,
        };

        //spmdJob.Run(size * size, -1);

        var eq = vs.SequenceCompareTo(ss);
        Assert.AreEqual(0, eq);
    }
}