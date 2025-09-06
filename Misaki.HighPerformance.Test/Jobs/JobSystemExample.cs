using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.Jobs;

/// <summary>
/// Simple job that adds a value to each element in an array.
/// </summary>
public unsafe class AddValueJob : IJobParallelFor
{
    public float* Data;
    public float Value;

    public void Execute(int index)
    {
        Data[index] += Value;
    }
}

/// <summary>
/// Simple job that multiplies each element in an array by a value.
/// </summary>
public unsafe class MultiplyJob : IJobParallelFor
{
    public float* Data;
    public float Multiplier;

    public void Execute(int index)
    {
        Data[index] *= Multiplier;
    }
}

/// <summary>
/// Simple job that computes the sum of an array (single-threaded).
/// </summary>
/// <remarks>
/// This job uses the Kahan summation algorithm to reduce numerical error.
/// </remarks>
public unsafe class KahanSumJob : IJob
{
    public float* Data;
    public int Length;
    public float* Result;

    public void Execute()
    {
        var sum = 0f;
        var c = 0f;  // Compensation for lost low-order bits

        for (var i = 0; i < Length; i++)
        {
            var y = Data[i] - c;    // So far, so good: c is zero
            var t = sum + y;        // Alas, sum is big, y small, so low-order digits of y are lost
            c = (t - sum) - y;          // (t - sum) cancels the high-order part of y; subtracting y recovers negative (low part of y)
            sum = t;                    // Algebraically, c should always be zero. Beware overly-clever compilers!
        }

        *Result = sum;
    }
}

/// <summary>
/// Example program demonstrating the job system with dependencies.
/// </summary>
public static class JobSystemExample
{
    public static unsafe void RunExample()
    {
        Console.WriteLine("=== Job System Example ===");

        const int arraySize = 10000;

        // Create test data
        using var array = new UnsafeArray<float>(arraySize, Allocator.Persistent);

        // Initialize with values 1, 2, 3, ...
        for (var i = 0; i < arraySize; i++)
        {
            array[i] = i + 1;
        }

        Console.WriteLine($"Initial sum: {ComputeSum((float*)array.GetUnsafePtr(), arraySize)}");

        // Job 1: Add 10 to each element
        var addJob = new AddValueJob
        {
            Data = (float*)array.GetUnsafePtr(),
            Value = 10f
        };

        // Job 2: Multiply each element by 2 (depends on addJob)
        var multiplyJob = new MultiplyJob
        {
            Data = (float*)array.GetUnsafePtr(),
            Multiplier = 2f
        };

        // Job 3: Compute final sum (depends on multiplyJob)
        var result = stackalloc float[1];
        var sumJob = new KahanSumJob
        {
            Data = (float*)array.GetUnsafePtr(),
            Length = arraySize,
            Result = result
        };

        Console.WriteLine("Scheduling jobs with dependencies...");

        // Schedule jobs with dependencies
        var addHandle = addJob.ScheduleParallel(arraySize, 64);
        var multiplyHandle = multiplyJob.ScheduleParallel(arraySize, 64, addHandle);
        var sumHandle = sumJob.Schedule(multiplyHandle);

        // Wait for all jobs to complete
        sumHandle.Complete();

        Console.WriteLine($"Final sum: {*result}");
        Console.WriteLine($"Expected sum: {ComputeExpectedSum(arraySize)}");
        Console.WriteLine("Jobs completed successfully!");

        // Test dependency combination
        Console.WriteLine("\n=== Testing Dependency Combination ===");

        // Reset array
        for (var i = 0; i < arraySize; i++)
        {
            array[i] = 1f;
        }

        // Create multiple independent jobs
        var basePtr = (float*)array.GetUnsafePtr();
        var job1 = new AddValueJob { Data = basePtr, Value = 1f };
        var job2 = new AddValueJob { Data = basePtr + arraySize / 2, Value = 2f };
        var job3 = new AddValueJob { Data = basePtr + arraySize / 4, Value = 3f };

        var handle1 = job1.ScheduleParallel(arraySize / 2, 32);
        var handle2 = job2.ScheduleParallel(arraySize / 2, 32);
        var handle3 = job3.ScheduleParallel(arraySize / 4, 32);

        // Combine dependencies
        var combinedHandle = JobHandle.CombineDependencies(handle1, handle2, handle3);

        // Final job that depends on all previous jobs
        var finalSum = stackalloc float[1];
        var finalSumJob = new KahanSumJob
        {
            Data = (float*)array.GetUnsafePtr(),
            Length = arraySize,
            Result = finalSum
        };

        var finalHandle = finalSumJob.Schedule(combinedHandle);
        finalHandle.Complete();

        Console.WriteLine($"Final sum after combined dependencies: {*finalSum}");
        Console.WriteLine("Dependency combination test completed!");
    }

    private static unsafe float ComputeSum(float* data, int length)
    {
        var sum = 0f;
        for (var i = 0; i < length; i++)
        {
            sum += data[i];
        }
        return sum;
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
}
