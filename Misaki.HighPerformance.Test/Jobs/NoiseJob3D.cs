using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using static Misaki.HighPerformance.Mathematics.math;

namespace Misaki.HighPerformance.Test.Jobs;

public static partial class noise
{
    // Modulo 289 without a division (only multiplications)
    public static float mod289(float x)
    {
        return x - floor(x * (1.0f / 289.0f)) * 289.0f;
    }
    public static float2 mod289(float2 x)
    {
        return x - floor(x * (1.0f / 289.0f)) * 289.0f;
    }
    public static float3 mod289(float3 x)
    {
        return x - floor(x * (1.0f / 289.0f)) * 289.0f;
    }
    public static float4 mod289(float4 x)
    {
        return x - floor(x * (1.0f / 289.0f)) * 289.0f;
    }

    // Modulo 7 without a division
    public static float3 mod7(float3 x)
    {
        return x - floor(x * (1.0f / 7.0f)) * 7.0f;
    }
    public static float4 mod7(float4 x)
    {
        return x - floor(x * (1.0f / 7.0f)) * 7.0f;
    }

    // Permutation polynomial: (34x^2 + x) math.mod 289
    public static float permute(float x)
    {
        return mod289((34.0f * x + 1.0f) * x);
    }
    public static float3 permute(float3 x)
    {
        return mod289((34.0f * x + 1.0f) * x);
    }
    public static float4 permute(float4 x)
    {
        return mod289((34.0f * x + 1.0f) * x);
    }

    public static float taylorInvSqrt(float r)
    {
        return 1.79284291400159f - 0.85373472095314f * r;
    }
    public static float4 taylorInvSqrt(float4 r)
    {
        return 1.79284291400159f - 0.85373472095314f * r;
    }

    public static float2 fade(float2 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }
    public static float3 fade(float3 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }
    public static float4 fade(float4 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    public static float4 grad4(float j, float4 ip)
    {
        var ones = float4(1.0f, 1.0f, 1.0f, -1.0f);
        var pxyz = floor(frac(float3(j) * ip.xyz) * 7.0f) * ip.z - 1.0f;
        float pw = 1.5f - dot(abs(pxyz), ones.xyz);
        var p = float4(pxyz, pw);
        var s = float4(p < 0.0f);
        p.xyz = p.xyz + (s.xyz * 2.0f - 1.0f) * s.www;
        return p;
    }

    // Hashed 2-D gradients with an extra rotation.
    // (The constant 0.0243902439 is 1/41)
    public static float2 rgrad2(float2 p, float rot)
    {
        // For more isotropic gradients, math.sin/math.cos can be used instead.
        float u = permute(permute(p.x) + p.y) * 0.0243902439f + rot; // Rotate by shift
        u = frac(u) * 6.28318530718f; // 2*pi
        return float2(cos(u), sin(u));
    }
}

internal unsafe struct NoiseJob3D : IJobParallelFor
{
    public float* buffers;

    public int size; // size x size x size

    public void Execute(int loopIndex, int threadIndex)
    {
        var v = float3(
            (loopIndex % size) / (float)size,
            ((loopIndex / size) % size) / (float)size,
            (loopIndex / (size * size)) / (float)size
        );

        var C = float2(1.0f / 6.0f, 1.0f / 3.0f);
        var D = float4(0.0f, 0.5f, 1.0f, 2.0f);

        // First corner
        var i = floor(v + dot(v, C.yyy));
        var x0 = v - i + dot(i, C.xxx);

        // Other corners
        var g = step(x0.yzx, x0.xyz);
        var l = 1.0f - g;
        var i1 = min(g.xyz, l.zxy);
        var i2 = max(g.xyz, l.zxy);

        //   x0 = x0 - 0.0 + 0.0 * C.xxx;
        //   x1 = x0 - i1  + 1.0 * C.xxx;
        //   x2 = x0 - i2  + 2.0 * C.xxx;
        //   x3 = x0 - 1.0 + 3.0 * C.xxx;
        var x1 = x0 - i1 + C.xxx;
        var x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
        var x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y

        // Permutations
        i = noise.mod289(i);
        var p = noise.permute(noise.permute(noise.permute(
                    i.z + float4(0.0f, i1.z, i2.z, 1.0f))
                    + i.y + float4(0.0f, i1.y, i2.y, 1.0f))
                    + i.x + float4(0.0f, i1.x, i2.x, 1.0f));

        // Gradients: 7x7 points over a square, mapped onto an octahedron.
        // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
        float n_ = 0.142857142857f; // 1.0/7.0
        var ns = n_ * D.wyz - D.xzx;

        var j = p - 49.0f * floor(p * ns.z * ns.z); //  math.mod(p,7*7)

        var x_ = floor(j * ns.z);
        var y_ = floor(j - 7.0f * x_); // math.mod(j,N)

        var x = x_ * ns.x + ns.yyyy;
        var y = y_ * ns.x + ns.yyyy;
        var h = 1.0f - abs(x) - abs(y);

        var b0 = float4(x.xy, y.xy);
        var b1 = float4(x.zw, y.zw);

        //float4 s0 = float4(math.lessThan(b0,0.0))*2.0 - 1.0;
        //float4 s1 = float4(math.lessThan(b1,0.0))*2.0 - 1.0;
        var s0 = floor(b0) * 2.0f + 1.0f;
        var s1 = floor(b1) * 2.0f + 1.0f;
        var sh = -step(h, float4(0.0f));

        var a0 = b0.xzyw + s0.xzyw * sh.xxyy;
        var a1 = b1.xzyw + s1.xzyw * sh.zzww;

        var p0 = float3(a0.xy, h.x);
        var p1 = float3(a0.zw, h.y);
        var p2 = float3(a1.xy, h.z);
        var p3 = float3(a1.zw, h.w);

        //Normalise gradients
        var norm = noise.taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
        p0 *= norm.x;
        p1 *= norm.y;
        p2 *= norm.z;
        p3 *= norm.w;

        // Mix final noise value
        var m = max(0.6f - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0f);
        m *= m;

        buffers[loopIndex] = 42.0f * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
    }
}
