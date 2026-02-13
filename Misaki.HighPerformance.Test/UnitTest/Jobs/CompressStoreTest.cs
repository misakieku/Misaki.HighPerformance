using Misaki.HighPerformance.Mathematics.SPMD;
using System.Numerics;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

public static class CompressStoreTest
{
    public static void Run()
    {
        Console.WriteLine("--- Testing CompressStore (Double) ---");

        // Test 1: Simple Pattern (True, False, True, False...)
        TestPattern_Double(
            input: new double[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            // Mask: Keep only even numbers (values > 0)
            // We simulate a mask by comparing against 0 or -1
            keepPattern: new bool[] { true, false, true, false, true, false, true, false }
        );

        // Test 2: All True
        TestPattern_Double(
            input: new double[] { 10, 20, 30, 40, 50, 60, 70, 80 },
            keepPattern: new bool[] { true, true, true, true, true, true, true, true }
        );

        // Test 3: All False
        TestPattern_Double(
            input: new double[] { 10, 20, 30, 40, 50, 60, 70, 80 },
            keepPattern: new bool[] { false, false, false, false, false, false, false, false }
        );

        // Test 4: Sparse (First and Last only)
        TestPattern_Double(
            input: new double[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            keepPattern: new bool[] { true, false, false, false, false, false, false, true }
        );
    }

    private unsafe static void TestPattern_Double(double[] input, bool[] keepPattern)
    {
        // 1. Setup Input Vector
        // Handle case where Vector<TLane> is smaller than 8 (e.g. 2 or 4)
        var vecSize = Vector<double>.Count;
        var safeInput = new double[vecSize];
        var safeMaskVal = new double[vecSize];

        // Expected Output Calculation
        var expected = new double[vecSize];
        var expectedCount = 0;

        for (var i = 0; i < vecSize; i++)
        {
            safeInput[i] = input[i];
            // If we want to keep it, make mask "GreaterThan" true
            // We'll compare X > 0. 
            // If keep=true, val=1. If keep=false, val=-1.
            safeMaskVal[i] = keepPattern[i] ? 1 : -1;

            if (keepPattern[i])
            {
                expected[expectedCount++] = input[i];
            }
        }

        // 2. Create WideLanes
        var vInput = WideLane<double>.Load(ref safeInput.AsSpan().GetPinnableReference());

        // Create Mask: greater than 0
        var vMaskVal = WideLane<double>.Load(ref safeMaskVal.AsSpan().GetPinnableReference());
        var vZero = WideLane<double>.Create(0);
        var vMask = WideLane<double>.GreaterThan(vMaskVal, vZero);

        // 3. Run CompressStore
        var outputBuffer = new double[vecSize];
        var actualCount = 0;

        fixed (double* ptr = outputBuffer)
        {
            actualCount = vInput.CompressStore(vMask, ptr);
        }

        // 4. Verify
        var pass = actualCount == expectedCount;
        for (var i = 0; i < expectedCount; i++)
        {
            if (outputBuffer[i] != expected[i])
                pass = false;
        }

        // 5. Report
        var hardware = (vecSize == 4) ? "AVX2 (256-bit)" : (vecSize == 2) ? "SSE/NEON (128-bit)" : "Scalar";
        Console.Write($"[{hardware}] Pattern: ");
        for (var i = 0; i < vecSize; i++)
            Console.Write(keepPattern[i] ? "1" : "0");

        if (pass)
        {
            Console.WriteLine($" -> PASS (Count: {actualCount})");
        }
        else
        {
            Console.WriteLine($" -> FAIL!");
            Console.WriteLine($"   Expected Count: {expectedCount}, Actual: {actualCount}");
            Console.Write("   Expected Data: ");
            foreach (var d in expected)
                Console.Write($"{d} ");
            Console.WriteLine();
            Console.Write("   Actual Data:   ");
            foreach (var d in outputBuffer)
                Console.Write($"{d} ");
            Console.WriteLine();
        }
    }
}