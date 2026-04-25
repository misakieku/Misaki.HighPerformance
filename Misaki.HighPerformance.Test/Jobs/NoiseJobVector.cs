using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Test.Jobs;

internal unsafe struct NoiseJobVectorFor : IJobParallelFor
{
    public float* buffers;
    public int width;
    public int height;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var x = loopIndex % width;
        var y = loopIndex / height;
        var uv = new Vector2(x, y) / new Vector2(width, height);
        buffers[loopIndex] = NoiseJobVector.GradientNoise(uv);
    }
}

internal unsafe struct NoiseJobVector : IJobParallel
{
    public float* buffers;
    public int width;
    public int height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Frac(float x)
    {
        return x - MathF.Floor(x);
    }

    private static float Mod289(float x)
    {
        return x - MathF.Floor(x / 289) * 289;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 GradientNoiseDirect(Vector2 uv)
    {
        uv.X = Mod289(uv.X);
        uv.Y = Mod289(uv.Y);
        var x = (34 * uv.X + 1) * Mod289(uv.X) + uv.Y;
        x = (34 * x + 1) * Mod289(x);
        x = Frac(x / 41) * 2 - 1;
        return Vector2.Normalize(new Vector2(x - MathF.Floor(x + 0.5f), MathF.Abs(x) - 0.5f));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GradientNoise(Vector2 uv)
    {
        var ip = new Vector2(MathF.Floor(uv.X), MathF.Floor(uv.Y));
        var fp = new Vector2(Frac(uv.X), Frac(uv.Y));

        var d00 = Vector2.Dot(GradientNoiseDirect(ip), fp);
        var d01 = Vector2.Dot(GradientNoiseDirect(ip + new Vector2(0, 1)), fp - new Vector2(0, 1));
        var d10 = Vector2.Dot(GradientNoiseDirect(ip + new Vector2(1, 0)), fp - new Vector2(1, 0));
        var d11 = Vector2.Dot(GradientNoiseDirect(ip + new Vector2(1, 1)), fp - new Vector2(1, 1));

        fp = fp * fp * fp * (fp * (fp * new Vector2(6.0f) - new Vector2(15.0f)) + new Vector2(10.0f));
        return float.Lerp(float.Lerp(d00, d10, fp.Y), float.Lerp(d01, d11, fp.Y), fp.X);
    }

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (var i = startIndex; i < endIndex; i++)
        {
            var x = i % width;
            var y = i / height;
            var uv = new Vector2(x, y) / new Vector2(width, height);
            buffers[i] = GradientNoise(uv);
        }
    }
}

internal unsafe struct NoiseJobMath : IJobParallel
{
    public float* buffers;
    public int width;
    public int height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float2 GradientNoiseDirect(float2 uv)
    {
        uv = noise.mod289(uv);
        var x = (34 * uv.x + 1) * noise.mod289(uv.x) + uv.y;
        x = (34 * x + 1) * noise.mod289(x);
        x = math.frac(x / 41) * 2 - 1;
        return math.normalize(new float2(x - math.floor(x + 0.5f), math.abs(x) - 0.5f));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GradientNoise(float2 uv)
    {
        var ip = new float2(math.floor(uv.x), math.floor(uv.y));
        var fp = new float2(math.frac(uv.x), math.frac(uv.y));

        var d00 = math.dot(GradientNoiseDirect(ip), fp);
        var d01 = math.dot(GradientNoiseDirect(ip + new float2(0, 1)), fp - new float2(0, 1));
        var d10 = math.dot(GradientNoiseDirect(ip + new float2(1, 0)), fp - new float2(1, 0));
        var d11 = math.dot(GradientNoiseDirect(ip + new float2(1, 1)), fp - new float2(1, 1));

        fp = fp * fp * fp * (fp * (fp * new float2(6.0f) - new float2(15.0f)) + new float2(10.0f));
        return float.Lerp(float.Lerp(d00, d10, fp.y), float.Lerp(d01, d11, fp.y), fp.x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(int loopIndex, int threadIndex)
    {
        var x = loopIndex % width;
        var y = loopIndex / height;
        var uv = new float2(x, y) / new float2(width, height);
        buffers[loopIndex] = GradientNoise(uv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (var i = startIndex; i < endIndex; i++)
        {
            var x = i % width;
            var y = i / height;
            var uv = new float2(x, y) / new float2(width, height);
            buffers[i] = GradientNoise(uv);
        }
    }
}

internal unsafe struct NoiseJobMathV : IJobParallel
{
    public float* buffers;
    public int width;
    public int height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Mod289(Vector256<float> x)
    {
        var div = x / Vector256.Create(289.0f);
        var flr = Vector256.Truncate(div);
        return x - (flr * Vector256.Create(289.0f));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Fade(Vector256<float> t)
    {
        return t * t * t * (t * (t * Vector256.Create(6.0f) - Vector256.Create(15.0f)) + Vector256.Create(10.0f));
    }

    // HELPER: Calculate gradients for 8 pixels at once
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> GradDot(Vector256<float> ix, Vector256<float> iy, Vector256<float> fx, Vector256<float> fy)
    {
        var hx = Mod289(ix);
        var hy = Mod289(iy);

        var p = hx * Vector256.Create(34.0f) + Vector256.Create(1.0f);
        p = Mod289(p * hx) + hy;
        var pPrev = p;
        p = p * Vector256.Create(34.0f) + Vector256.Create(1.0f);
        p = Mod289(p * pPrev);

        var r = (p / 41.0f);
        r = (r - Vector256.Truncate(r)) * 2.0f - Vector256<float>.One;

        var gx = r - Vector256.Floor(r + Vector256.Create(0.5f));
        var gy = Vector256.Abs(r) - Vector256.Create(0.5f);

        // Normalize
        var len = Vector256.Sqrt(gx * gx + gy * gy);
        gx /= len;
        gy /= len;

        return gx * fx + gy * fy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> GradientNoiseAVX(Vector256<float> uvX, Vector256<float> uvY)
    {
        var ipX = Vector256.Floor(uvX);
        var ipY = Vector256.Floor(uvY);
        var fpX = uvX - ipX;
        var fpY = uvY - ipY;

        var uX = Fade(fpX);
        var uY = Fade(fpY);

        var d00 = GradDot(ipX, ipY, fpX, fpY);
        var d01 = GradDot(ipX, ipY + Vector256<float>.One, fpX, fpY - Vector256<float>.One);
        var d10 = GradDot(ipX + Vector256<float>.One, ipY, fpX - Vector256<float>.One, fpY);
        var d11 = GradDot(ipX + Vector256<float>.One, ipY + Vector256<float>.One, fpX - Vector256<float>.One, fpY - Vector256<float>.One);

        var lerpY1 = d00 + (d10 - d00) * uY;
        var lerpY2 = d01 + (d11 - d01) * uY;

        return lerpY1 + (lerpY2 - lerpY1) * uX;
    }

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (var i = startIndex; i < endIndex; i++)
        {
            var baseIndex = i * 8;

            // Safety check
            if (baseIndex + 7 >= width * height)
            {
                return;
            }

            // Calculate Coords
            var y = baseIndex / width;
            var x = baseIndex % width;

            // Sequence: 0, 1, 2, 3, 4, 5, 6, 7
            var vSeqX = Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f);
            var vBaseX = Vector256.Create((float)x) + vSeqX;
            var vBaseY = Vector256.Create((float)y);

            var vWidth = Vector256.Create((float)width);
            var vHeight = Vector256.Create((float)height);

            var result = GradientNoiseAVX(vBaseX / vWidth, vBaseY / vHeight);

            // Store 8 floats (32 bytes)
            result.Store(buffers + baseIndex);
        }
    }
}

internal unsafe struct NoiseJobMathSPMD : IJobSPMD<float>
{
    public float* buffers;
    public int width;
    public int height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T GradDot<T>(T ix, T iy, T fx, T fy)
        where T : unmanaged, ISPMDLane<T, float>
    {
        var c289 = T.Create(289f);
        var c34 = T.Create(34f);
        var c1 = T.Create(1f);
        var c41 = T.Create(41f);
        var c2 = T.Create(2f);
        var half = T.Create(0.5f);

        ix %= c289;
        iy %= c289;
        var x = (c34 * ix + c1) * ix % c289 + iy;
        x = (c34 * x + c1) * x % c289;
        x = T.Frac(x / c41) * c2 - c1;

        var gx = x - T.Floor(x + half);
        var gy = T.Abs(x) - half;

        // normalize
        var len = T.Sqrt(gx * gx + gy * gy);
        gx /= len;
        gy /= len;

        return gx * fx + gy * fy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Noise<T>(T uvX, T uvY)
        where T : unmanaged, ISPMDLane<T, float>
    {
        var c1 = T.Create(1f);
        var c6 = T.Create(6f);
        var c10 = T.Create(10f);
        var c15 = T.Create(15f);

        var ipX = T.Floor(uvX);
        var ipY = T.Floor(uvY);
        var fpX = uvX - ipX;
        var fpY = uvY - ipY;

        var d00 = GradDot(ipX, ipY, fpX, fpY);
        var d01 = GradDot(ipX, ipY + c1, fpX, fpY - c1);
        var d10 = GradDot(ipX + c1, ipY, fpX - c1, fpY);
        var d11 = GradDot(ipX + c1, ipY + c1, fpX - c1, fpY - c1);

        // fade
        var uX = fpX * fpX * fpX * (fpX * (fpX * c6 - c15) + c10);
        var uY = fpY * fpY * fpY * (fpY * (fpY * c6 - c15) + c10);

        return T.Lerp(T.Lerp(d00, d10, uY), T.Lerp(d01, d11, uY), uX);
    }

    public readonly void Execute<TLane>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane : unmanaged, ISPMDLane<TLane, float>
    {
        var indices = TLane.Sequence(baseIndex, 1f);
        var w = TLane.Create(width);
        var h = TLane.Create(height);

        var uvX = (indices % w) / w;
        var uvY = TLane.Floor(indices / w) / h;

        var result = Noise(uvX, uvY);
        result.Store(buffers + baseIndex);
    }
}