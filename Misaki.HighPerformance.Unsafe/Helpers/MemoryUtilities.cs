using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Unsafe.Helpers;
public unsafe static class MemoryUtilities
{
    /// <summary>
    /// Clears a block of memory by setting it to zero. It initializes a specified number of bytes at a given memory
    /// address.
    /// </summary>
    /// <param name="ptr">Specifies the memory address where the clearing operation will begin.</param>
    /// <param name="size">Indicates the number of bytes to be cleared in the memory block.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemClear(void* ptr, uint size)
    {
        SystemUnsfae.InitBlock(ptr, 0, size);
    }

    /// <summary>
    /// Sets a block of memory to a specified byte value for a given size.
    /// </summary>
    /// <param name="ptr">The memory address where the byte value will be set.</param>
    /// <param name="value">The byte value to which the memory block will be initialized.</param>
    /// <param name="size">The number of bytes to set to the specified value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemSet(void* ptr, byte value, uint size)
    {
        SystemUnsfae.InitBlock(ptr, value, size);
    }

    /// <summary>
    /// Copies a block of memory from a source location to a destination location.
    /// </summary>
    /// <param name="destination">Specifies the memory address where the copied data will be stored.</param>
    /// <param name="source">Indicates the memory address from which data will be copied.</param>
    /// <param name="size">Defines the number of bytes to be copied from the source to the destination.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemCpy(void* destination, void* source, uint size)
    {
        SystemUnsfae.CopyBlock(destination, source, size);
    }
}