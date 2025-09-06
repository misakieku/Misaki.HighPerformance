using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Helpers;

public static unsafe partial class MemoryUtilities
{
    [StructLayout(LayoutKind.Sequential)]
    private struct AlignOfHelper<T>
        where T : struct
    {
        public byte dummy;
        public T data;
    }

    /// <summary>
    /// Allocates a block of memory of the specified size in bytes.
    /// </summary>
    /// <param name="size">Specifies the number of bytes to allocate in memory.</param>
    /// <returns>Returns a pointer to the allocated memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Malloc(nuint size)
    {
        return NativeMemory.Alloc(size);
    }

    /// <summary>
    /// Allocates a block of memory with a specified size and alignment.
    /// </summary>
    /// <param name="size">Specifies the total number of bytes to allocate for the memory block.</param>
    /// <param name="alignment">Defines the required alignment for the allocated memory address.</param>
    /// <returns>Returns a pointer to the allocated memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AlignedAlloc(nuint size, nuint alignment)
    {
        return NativeMemory.AlignedAlloc(size, alignment);
    }

    /// <summary>
    /// Resizes a previously allocated memory block to a new size. It returns a pointer to the reallocated memory.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block that needs to be resized.</param>
    /// <param name="size">The new size for the memory block after resizing.</param>
    /// <returns>A pointer to the reallocated memory block, or null if the operation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Realloc(void* ptr, nuint size)
    {
        return NativeMemory.Realloc(ptr, size);
    }

    /// <summary>
    /// Reallocates memory to a specified size with a given alignment. It returns a pointer to the newly allocated
    /// memory.
    /// </summary>
    /// <param name="ptr">The pointer to the existing memory block that needs to be reallocated.</param>
    /// <param name="size">The new size for the memory allocation.</param>
    /// <param name="alignment">The required alignment for the new memory allocation.</param>
    /// <returns>A pointer to the reallocated memory block, or null if the allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AlignedRealloc(void* ptr, nuint size, nuint alignment)
    {
        return NativeMemory.AlignedRealloc(ptr, size, alignment);
    }

    /// <summary>
    /// Releases the allocated memory pointed to by the given pointer. This helps in managing memory usage effectively.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block that needs to be freed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(void* ptr)
    {
        if (ptr == null)
        {
            return;
        }

        NativeMemory.Free(ptr);
    }

    /// <summary>
    /// Releases memory that was allocated with alignment requirements. It ensures proper deallocation of aligned memory
    /// blocks.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block that needs to be freed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AlignedFree(void* ptr)
    {
        if (ptr == null)
        {
            return;
        }

        NativeMemory.AlignedFree(ptr);
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
        if (ptr == null || size == 0)
        {
            return;
        }

        NativeMemory.Clear(ptr, size);
    }

    /// <summary>
    /// Sets a block of memory to a specified byte value for a given size.
    /// </summary>
    /// <param name="ptr">The memory address where the byte value will be set.</param>
    /// <param name="size">The number of bytes to set to the specified value.</param>
    /// <param name="value">The byte value to which the memory block will be initialized.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemSet(void* ptr, byte value, nuint size)
    {
        if (ptr == null || size == 0)
        {
            return;
        }

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
        if (source == null || destination == null || size == 0)
        {
            return;
        }

        NativeMemory.Copy(source, destination, size);
    }

    /// <summary>
    /// Calculates the size in bytes of a specified unmanaged type.
    /// </summary>
    /// <typeparam name="T">Represents an unmanaged type for which the size is being calculated.</typeparam>
    /// <returns>Returns the size of the specified type as an unsigned integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint SizeOf<T>()
        where T : unmanaged
    {
        return (nuint)sizeof(T);
    }

    /// <summary>
    /// Calculates the alignment size of a specified unmanaged type.
    /// </summary>
    /// <typeparam name="T">Represents an unmanaged type for which the alignment size is being calculated.</typeparam>
    /// <returns>Returns the difference in size between a helper structure and the specified type.</returns>
    public static nuint AlignOf<T>()
        where T : unmanaged
    {
        return (nuint)(sizeof(AlignOfHelper<T>) - sizeof(T));
    }

    /// <summary>
    /// Calculates the alignment size of a specified struct.
    /// </summary>
    /// <typeparam name="T">Represents a value type that is used to determine the alignment size.</typeparam>
    /// <returns>Returns the size difference in bytes as an integer.</returns>
    public static int MarshalAlignOf<T>()
        where T : struct
    {
        return Marshal.SizeOf<AlignOfHelper<T>>() - Marshal.SizeOf<T>();
    }
}

