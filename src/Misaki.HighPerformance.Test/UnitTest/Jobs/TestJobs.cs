using Misaki.HighPerformance.Jobs;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

internal unsafe struct TwoSumJob : IJob
{
    public float value1;
    public float value2;

    public float* result;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *result = value1 + value2;
    }
}

internal unsafe struct AddJob : IJob
{
    public float value;

    public float* result;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *result += value;
    }
}

internal unsafe struct KahanSumJob : IJob
{
    public float* input;
    public int length;
    public float* output;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var sum = 0f;
        var c = 0f;  // Compensation for lost low-order bits

        for (var i = 0; i < length; i++)
        {
            var y = input[i] - c;    // So far, so good: c is zero
            var t = sum + y;        // Alas, sum is big, y small, so low-order digits of y are lost
            c = (t - sum) - y;          // (t - sum) cancels the high-order part of y; subtracting y recovers negative (low part of y)
            sum = t;                    // Algebraically, c should always be zero. Beware overly-clever compilers!
        }

        *output = sum;
    }
}

internal unsafe struct ParallelAddJob : IJobParallel
{
    public float value;
    public float* inout;

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (var i = startIndex; i < endIndex; i++)
        {
            inout[i] += value;
        }
    }
}

internal unsafe struct ParallelMultiplyJob : IJobParallel
{
    public float multiplier;
    public float* inout;

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (var i = startIndex; i < endIndex; i++)
        {
            inout[i] *= multiplier;
        }
    }
}

public unsafe struct WaitJob : IJob
{
    public bool* pSignal;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var spin = new SpinWait();
        while (!Volatile.Read(ref *pSignal))
        {
            spin.SpinOnce();
        }
    }
}

public unsafe struct IncrementJob : IJob
{
    public int* pCounter;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        Interlocked.Increment(ref *pCounter);
    }
}

public unsafe struct CustomJob : ICustomJob<CustomJob>
{
    public int* value;

    public static void Execute(ref CustomJob job, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
    {
        *job.value += 1;
    }

    public static void Free(ref CustomJob job)
    {
        // No resources to free in this example.
    }
}

internal unsafe struct ParentSpawnsChildJob : IJob
{
    public float childValue;
    public float* result;
    public int sleepMs;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var childJob = new AddJob
        {
            value = childValue,
            result = result,
        };

        ctx.ScheduleChild(in childJob, true, JobPriority.Normal);

        if (sleepMs > 0)
        {
            Thread.Sleep(sleepMs);
        }
    }
}

internal unsafe struct SlowChildJob : IJob
{
    public float value;
    public float* result;
    public int sleepMs;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        if (sleepMs > 0)
        {
            Thread.Sleep(sleepMs);
        }
        *result += value;
    }
}

internal unsafe struct ParentSpawnsSlowChildJob : IJob
{
    public float childValue;
    public float* result;
    public int childSleepMs;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var childJob = new SlowChildJob
        {
            value = childValue,
            result = result,
            sleepMs = childSleepMs,
        };

        ctx.ScheduleChild(in childJob, true, JobPriority.Normal);
    }
}

internal unsafe struct ParallelIncrementJob : IJobParallelFor
{
    public int* pBuffer;

    public void Execute(int index, ref readonly JobExecutionContext ctx)
    {
        Interlocked.Increment(ref pBuffer[index]);
    }
}

internal unsafe struct ParentSpawnsParallelChildJob : IJob
{
    public int* pBuffer;
    public int totalIterations;
    public int batchSize;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var childJob = new ParallelIncrementJob
        {
            pBuffer = pBuffer,
        };

        ctx.ScheduleChildParallelFor(in childJob, totalIterations, batchSize, true, JobPriority.Normal);
    }
}

internal unsafe struct GrandchildJob : IJob
{
    public float* result;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *result += 100f;
    }
}

internal unsafe struct ChildSpawnerJob : IJob
{
    public float* result;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *result += 10f;
        var gc = new GrandchildJob { result = result };
        ctx.ScheduleChild(in gc, true, JobPriority.Normal);
    }
}

internal unsafe struct RootParentSpawnerJob : IJob
{
    public float* result;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *result += 1f;
        var child = new ChildSpawnerJob { result = result };
        ctx.ScheduleChild(in child, true, JobPriority.Normal);
    }
}

internal unsafe struct ParentSpawnsMultipleChildrenJob : IJob
{
    public int childCount;
    public int* pCounter;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        for (var i = 0; i < childCount; i++)
        {
            var child = new IncrementJob { pCounter = pCounter };
            ctx.ScheduleChild(in child, true, JobPriority.Normal);
        }
    }
}

internal unsafe struct SumBufferJob : IJob
{
    public int* pBuffer;
    public int count;
    public int* pSum;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var total = 0;
        for (var i = 0; i < count; i++)
        {
            total += pBuffer[i];
        }
        *pSum = total;
    }
}

internal unsafe struct ThreadIdRecordingJob : IJob
{
    public int* pThreadId;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *pThreadId = Environment.CurrentManagedThreadId;
    }
}

internal unsafe struct FlagJob : IJob
{
    public bool* pFlag;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *pFlag = true;
    }
}