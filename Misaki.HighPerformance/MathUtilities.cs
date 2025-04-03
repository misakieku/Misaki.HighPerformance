using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance;

public static class MathUtilities
{
    /// <summary>Returns the smallest power of two that is greater than or equal to the specified number.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilPow2(int x)
    {
        x -= 1;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }
}