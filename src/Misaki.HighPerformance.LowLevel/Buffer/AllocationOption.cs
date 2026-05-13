using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

[Flags]
public enum AllocationOption : byte
{
    /// <summary>
    /// Default allocation option. Values are uninitialized.
    /// </summary>
    None = 0,
    /// <summary>
    /// Clear the memory to zero upon allocation.
    /// </summary>
    Clear = 1 << 0
}

internal static class AllocationOptionExtensions
{
    // HasFlag still cuase boxing in debug mode, so we implement our own version of HasFlag to avoid boxing.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasOption(this AllocationOption options, AllocationOption flag)
    {
        return (options & flag) != 0;
    }
}
