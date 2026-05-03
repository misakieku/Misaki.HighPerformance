using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

internal unsafe struct DotProductJob : IJobSPMD<float>
{
    public float3* arrayA;    // source array 1
    public float3* arrayB;    // source array 2
    public float* results;    // output array (dot products)

    public readonly void Execute<TLane>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane : unmanaged, ISPMDLane<TLane, float>
    {
        var vecA = MathV.LoadVector3<TLane, float>((float*)(arrayA + baseIndex));
        var vecB = MathV.LoadVector3<TLane, float>((float*)(arrayB + baseIndex));

        var dotResult = MathV.Dot(vecA, vecB);
        dotResult.Store(results + baseIndex);
    }

    public void Execute<TLane0>(TLane0 indices, TLane0 mask, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, float>
    {
        var vecA = MathV.LoadVector3<TLane0, float>((float*)(arrayA + (int)indices[0]));
        var vecB = MathV.LoadVector3<TLane0, float>((float*)(arrayB + (int)indices[0]));

        var dotResult = MathV.Dot(vecA, vecB);
        dotResult.Store(results + (int)indices[0]);
    }
}

internal struct Vector2LerpJob : IJobSPMD<float>
{
    public float2[] arrayA;
    public float2[] arrayB;
    public float[] results;

    public readonly void Execute<TLane0>(TLane0 indices, TLane0 mask, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, float>
    {
        var a = MathV.LoadVector2<TLane0, float>(ref arrayA[(int)indices[0]].x);
        var b = MathV.LoadVector2<TLane0, float>(ref arrayB[(int)indices[0]].x);

        var t = TLane0.Create(0.5f);
        var lerped = MathV.Lerp(a, b, t);
        var len = TLane0.Sqrt(MathV.LengthSquared(lerped));

        len.Store(ref results[(int)indices[0]]);
    }
}

internal struct Vector4NormalizeJob : IJobSPMD<float>
{
    public float4[] input;
    public float4[] output;

    public readonly void Execute<TLane0>(TLane0 indices, TLane0 mask, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, float>
    {
        var vec = MathV.LoadVector4<TLane0, float>(ref input[(int)indices[0]].x);
        var normalized = MathV.Normalize(vec);
        normalized.Store(ref output[(int)indices[0]].x);
    }
}

internal struct Vector3CrossJob : IJobSPMD<float>
{
    public float3[] arrayA;
    public float3[] arrayB;
    public float3[] results;

    public readonly void Execute<TLane0>(TLane0 indices, TLane0 mask, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, float>
    {
        var a = MathV.LoadVector3<TLane0, float>(ref arrayA[(int)indices[0]].x);
        var b = MathV.LoadVector3<TLane0, float>(ref arrayB[(int)indices[0]].x);

        var cross = MathV.Cross(a, b);
        cross.Store(ref results[(int)indices[0]].x);
    }
}

internal struct MinMaxClampJob : IJobSPMD<float>
{
    public float3[] values;
    public float3[] mins;
    public float3[] maxs;
    public float3[] results;

    public readonly void Execute<TLane0>(TLane0 indices, TLane0 mask, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, float>
    {
        var val = MathV.LoadVector3<TLane0, float>(ref values[(int)indices[0]].x);
        var min = MathV.LoadVector3<TLane0, float>(ref mins[(int)indices[0]].x);
        var max = MathV.LoadVector3<TLane0, float>(ref maxs[(int)indices[0]].x);

        var clamped = MathV.Clamp(val, min, max);
        clamped.Store(ref results[(int)indices[0]].x);
    }
}

internal struct DistanceJob : IJobSPMD<float>
{
    public float3[] arrayA;
    public float3[] arrayB;
    public float[] results;

    public readonly void Execute<TLane0>(TLane0 indices, TLane0 mask, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, float>
    {
        var a = MathV.LoadVector3<TLane0, float>(ref arrayA[(int)indices[0]].x);
        var b = MathV.LoadVector3<TLane0, float>(ref arrayB[(int)indices[0]].x);

        var dist = MathV.Distance(a, b);
        dist.Store(ref results[(int)indices[0]]);
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


        job.Run<DotProductJob, float>(count, default);

        // Verify first result: dot([0,1,2], [1,2,3]) = 0*1 + 1*2 + 2*3 = 8
        Assert.AreEqual(8.0f, results[0], 0.001f);
        // Verify last result: dot([999,1000,1001], [1,2,3]) = 999*1 + 1000*2 + 1001*3 = 6002
        Assert.AreEqual(6002.0f, results[count - 1], 0.001f);

        NativeMemory.Free(arrayA);
        NativeMemory.Free(arrayB);
        NativeMemory.Free(results);
    }

    [TestMethod]
    public void TestSPMDVector2Lerp()
    {
        const int count = 100;

        var arrayA = new float2[count];
        var arrayB = new float2[count];
        var results = new float[count];

        for (var i = 0; i < count; i++)
        {
            arrayA[i] = new float2(i, i + 1);
            arrayB[i] = new float2(i + 10, i + 11);
        }

        var job = new Vector2LerpJob
        {
            arrayA = arrayA,
            arrayB = arrayB,
            results = results
        };

        job.Run<Vector2LerpJob, float>(count, default);

        // Verify first result: lerp([0,1], [10,11], 0.5) = [5,6], length = sqrt(25+36) = sqrt(61)
        var expectedFirst = math.sqrt(5 * 5 + 6 * 6);
        Assert.AreEqual(expectedFirst, results[0], 0.001f);

        // Verify result at index 50
        var expected50 = math.sqrt(55 * 55 + 56 * 56);
        Assert.AreEqual(expected50, results[50], 0.001f);
    }

    [TestMethod]
    public void TestSPMDVector4Normalize()
    {
        const int count = 100;

        var input = new float4[count];
        var output = new float4[count];

        for (var i = 0; i < count; i++)
        {
            input[i] = new float4(i + 1, i + 2, i + 3, i + 4);
        }

        var job = new Vector4NormalizeJob
        {
            input = input,
            output = output
        };

        job.Run<Vector4NormalizeJob, float>(count, default);

        // Verify first result: normalize([1,2,3,4])
        var len0 = math.sqrt(1 * 1 + 2 * 2 + 3 * 3 + 4 * 4);
        var expected0 = new float4(1 / len0, 2 / len0, 3 / len0, 4 / len0);
        Assert.AreEqual(expected0.x, output[0].x, 0.001f);
        Assert.AreEqual(expected0.y, output[0].y, 0.001f);
        Assert.AreEqual(expected0.z, output[0].z, 0.001f);
        Assert.AreEqual(expected0.w, output[0].w, 0.001f);

        // Verify all normalized vectors have length ~1
        for (var i = 0; i < count; i++)
        {
            var length = math.sqrt(output[i].x * output[i].x + output[i].y * output[i].y +
                                   output[i].z * output[i].z + output[i].w * output[i].w);
            Assert.AreEqual(1.0f, length, 0.001f, $"Vector at index {i} is not normalized");
        }
    }

    [TestMethod]
    public void TestSPMDVector3Cross()
    {
        const int count = 100;

        var arrayA = new float3[count];
        var arrayB = new float3[count];
        var results = new float3[count];

        for (var i = 0; i < count; i++)
        {
            arrayA[i] = new float3(1, 0, 0);
            arrayB[i] = new float3(0, 1, 0);
        }

        var job = new Vector3CrossJob
        {
            arrayA = arrayA,
            arrayB = arrayB,
            results = results
        };

        job.Run<Vector3CrossJob, float>(count, default);

        // cross([1,0,0], [0,1,0]) = [0,0,1]
        for (var i = 0; i < count; i++)
        {
            Assert.AreEqual(0.0f, results[i].x, 0.001f);
            Assert.AreEqual(0.0f, results[i].y, 0.001f);
            Assert.AreEqual(1.0f, results[i].z, 0.001f);
        }
    }

    [TestMethod]
    public void TestSPMDMinMaxClamp()
    {
        const int count = 100;

        var values = new float3[count];
        var mins = new float3[count];
        var maxs = new float3[count];
        var results = new float3[count];

        for (var i = 0; i < count; i++)
        {
            values[i] = new float3(i - 50, i + 10, i - 25);
            mins[i] = new float3(-10, 0, -5);
            maxs[i] = new float3(10, 50, 25);
        }

        var job = new MinMaxClampJob
        {
            values = values,
            mins = mins,
            maxs = maxs,
            results = results
        };

        job.Run<MinMaxClampJob, float>(count, default);

        // Verify clamping works correctly
        for (var i = 0; i < count; i++)
        {
            var val = values[i];
            var min = mins[i];
            var max = maxs[i];
            var expected = math.clamp(val, min, max);

            Assert.AreEqual(expected.x, results[i].x, 0.001f);
            Assert.AreEqual(expected.y, results[i].y, 0.001f);
            Assert.AreEqual(expected.z, results[i].z, 0.001f);
        }
    }

    [TestMethod]
    public void TestSPMDDistance()
    {
        const int count = 100;

        var arrayA = new float3[count];
        var arrayB = new float3[count];
        var results = new float[count];

        for (var i = 0; i < count; i++)
        {
            arrayA[i] = new float3(0, 0, 0);
            arrayB[i] = new float3(3, 4, 0);
        }

        var job = new DistanceJob
        {
            arrayA = arrayA,
            arrayB = arrayB,
            results = results
        };

        job.Run<DistanceJob, float>(count, default);

        // distance([0,0,0], [3,4,0]) = 5
        for (var i = 0; i < count; i++)
        {
            Assert.AreEqual(5.0f, results[i], 0.001f);
        }
    }
}