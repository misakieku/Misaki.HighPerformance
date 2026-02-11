using Misaki.HighPerformance.Jobs;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

internal unsafe struct TwoSumJob : IJob
{
    public float value1;
    public float value2;

    public float* result;

    public void Execute(int threadIndex)
    {
        *result = value1 + value2;
    }
}

internal unsafe struct AddJob : IJob
{
    public float value;

    public float* result;

    public void Execute(int threadIndex)
    {
        *result += value;
    }
}

internal unsafe struct KahanSumJob : IJob
{
    public float* input;
    public int length;
    public float* output;

    public void Execute(int threadIndex)
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

internal unsafe struct ParallelAddJob : IJobParallelFor
{
    public float value;
    public float* inout;

    public void Execute(int loopIndex, int threadIndex)
    {
        inout[loopIndex] += value;
    }
}

internal unsafe struct ParallelMultiplyJob : IJobParallelFor
{
    public float multiplier;
    public float* inout;

    public void Execute(int loopIndex, int threadIndex)
    {
        inout[loopIndex] *= multiplier;
    }
}

public unsafe struct WaitJob : IJob
{
    public bool* pSignal;

    public void Execute(int loopIndex)
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

    public void Execute(int loopIndex)
    {
        Interlocked.Increment(ref *pCounter);
    }
}