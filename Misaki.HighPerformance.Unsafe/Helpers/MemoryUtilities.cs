using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Unsafe.Helpers;
public unsafe static class MemoryUtilities
{
    [StructLayout(LayoutKind.Sequential)]
    private struct AlignOfHelper<T> where T : struct
    {
        public byte dummy;
        public T data;
    }

    /// <summary>
    /// Clears a block of memory by setting it to zero. It initializes a specified number of bytes at a given memory
    /// address.
    /// </summary>
    /// <param name="ptr">Specifies the memory address where the clearing operation will begin.</param>
    /// <param name="size">Indicates the number of bytes to be cleared in the memory block.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemClear(void* ptr, nuint size)
    {
        NativeMemory.Clear(ptr, size);
    }

    /// <summary>
    /// Sets a block of memory to a specified byte value for a given size.
    /// </summary>
    /// <param name="ptr">The memory address where the byte value will be set.</param>
    /// <param name="size">The number of bytes to set to the specified value.</param>
    /// <param name="value">The byte value to which the memory block will be initialized.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemSet(void* ptr, nuint size, byte value)
    {
        NativeMemory.Fill(ptr, size, value);
    }

    /// <summary>
    /// Copies a block of memory from a source location to a destination location.
    /// </summary>
    /// <param name="source">Indicates the memory address from which data will be copied.</param>
    /// <param name="destination">Specifies the memory address where the copied data will be stored.</param>
    /// <param name="size">Defines the number of bytes to be copied from the source to the destination.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemCpy(void* source, void* destination, nuint size)
    {
        NativeMemory.Copy(source, destination, size);
    }

    /// <summary>
    /// Calculates the size in bytes of a specified unmanaged type.
    /// </summary>
    /// <typeparam name="T">Represents an unmanaged type for which the size is being calculated.</typeparam>
    /// <returns>Returns the size of the specified type as an unsigned integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint SizeOf<T>() where T : unmanaged
    {
        return (nuint)sizeof(T);
    }

    /// <summary>
    /// Calculates the alignment size of a specified unmanaged type.
    /// </summary>
    /// <typeparam name="T">Represents an unmanaged type for which the alignment size is being calculated.</typeparam>
    /// <returns>Returns the difference in size between a helper structure and the specified type.</returns>
    public static int AlignOf<T>() where T : unmanaged
    {
        return sizeof(AlignOfHelper<T>) - sizeof(T);
    }

    /// <summary>
    /// Calculates the alignment size difference between a specified struct and a helper struct.
    /// </summary>
    /// <typeparam name="T">Represents a value type that is used to determine the alignment size.</typeparam>
    /// <returns>Returns the size difference in bytes as an integer.</returns>
    public static int MarshalAlignOf<T>() where T : struct
    {
        return Marshal.SizeOf<AlignOfHelper<T>>() - Marshal.SizeOf<T>();
    }
}