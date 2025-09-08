using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Test.Jobs;

internal struct NoiseJob : IJobParallelFor
{
    public UnsafeArray<float> buffers;
    public int width;
    public int height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Frac(float x)
    {
        return x - MathF.Truncate(x);
    }

    private static Vector2 GradientNoiseDirect(Vector2 uv)
    {
        uv.X %= 289;
        uv.Y %= 289;
        var x = (34 * uv.X + 1) * uv.X % 289 + uv.Y;
        x = (34 * x + 1) * x % 289;
        x = Frac(x / 41) * 2 - 1;
        return Vector2.Normalize(new Vector2(x - MathF.Floor(x + 0.5f), MathF.Abs(x) - 0.5f));
    }

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

    public void Execute(int loopIndex, int threadIndex)
    {
        var x = loopIndex % width;
        var y = loopIndex / height;
        var uv = new Vector2(x, y) / new Vector2(width, height);
        buffers[loopIndex] = GradientNoise(uv);
    }
}