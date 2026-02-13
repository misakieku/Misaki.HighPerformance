using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

internal unsafe struct DotProductJob : IJobSPMD<float>
{
    public float3* arrayA;    // source array 1
    public float3* arrayB;    // source array 2
    public float* results;    // output array (dot products)

    public readonly void Execute<TLane>(int baseIndex, int threadIndex)
        where TLane : ISPMD<TLane, float>
    {
        var vecA = MathV.LoadVector3<TLane, float>((float*)(arrayA + baseIndex));
        var vecB = MathV.LoadVector3<TLane, float>((float*)(arrayB + baseIndex));

        var dotResult = MathV.Dot(vecA, vecB);
        dotResult.Store(results + baseIndex);
    }
}

[TestClass]
public class SPMDTest
{
    [TestMethod]
    public unsafe void TestSPMDVectorDot()
    {
        const int count = 1000;

        var arrayA = (float3*)NativeMemory.Alloc((nuint)(sizeof(float3) * count));
        var arrayB = (float3*)NativeMemory.Alloc((nuint)(sizeof(float3) * count));
        var results = (float*)NativeMemory.Alloc(sizeof(float) * count);

        for (var i = 0; i < count; i++)
        {
            arrayA[i] = new float3(i, i + 1, i + 2);
            arrayB[i] = new float3(1, 2, 3);
        }

        var job = new DotProductJob
        {
            arrayA = arrayA,
            arrayB = arrayB,
            results = results
        };

        job.Run<DotProductJob, float>(count, -1);

        // Verify first result: dot([0,1,2], [1,2,3]) = 0*1 + 1*2 + 2*3 = 8
        Assert.AreEqual(8.0f, results[0], 0.001f);
        // Verify last result: dot([999,1000,1001], [1,2,3]) = 999*1 + 1000*2 + 1001*3 = 6002
        Assert.AreEqual(6002.0f, results[count - 1], 0.001f);

        NativeMemory.Free(arrayA);
        NativeMemory.Free(arrayB);
        NativeMemory.Free(results);
    }
}