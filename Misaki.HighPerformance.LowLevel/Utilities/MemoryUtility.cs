using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
#if MHP_ENABLE_MIMALLOC
using TerraFX.Interop.Mimalloc;
#endif

namespace Misaki.HighPerformance.LowLevel.Utilities;

[Flags]
public enum VirtualAllocationFlags
{
    Reserve = 1 << 0,
    Commit = 1 << 1,
}

public static unsafe partial class MemoryUtility
{
    private const uint _MEM_COMMIT = 0x00001000;
    private const uint _MEM_RESERVE = 0x00002000;
    private const uint _MEM_RELEASE = 0x00008000;
    private const uint _PAGE_READWRITE = 0x04;

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern void* VirtualAlloc(void* lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern int VirtualFree(void* lpAddress, nuint dwSize, uint dwFreeType);

    private const int _PROT_NONE = 0x0;
    private const int _PROT_READ = 0x1;
    private const int _PROT_WRITE = 0x2;
    private const int _MAP_PRIVATE = 0x02;

    // Note: MAP_ANONYMOUS varies by OS. Linux is 0x20, macOS is 0x1000.
    private static int GetMapAnonymousFlag() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 0x1000 : 0x20;

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [DllImport("libc")]
    private static extern void* mmap(void* addr, nuint length, int prot, int flags, int fd, nint offset);

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [DllImport("libc")]
    private static extern int munmap(void* addr, nuint length);

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [DllImport("libc")]
    private static extern int mprotect(void* addr, nuint len, int prot);


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
#if MHP_ENABLE_MIMALLOC
        var ptr = Mimalloc.mi_malloc(size);
        if (ptr == null)
        {
            throw new OutOfMemoryException("Failed to allocate memory using Malloc.");
        }

        return ptr;
#elif NET6_0_OR_GREATER
        return NativeMemory.Alloc(size);
#else
        return Marshal.AllocHGlobal((IntPtr)size).ToPointer();
#endif
    }

    /// <summary>
    /// Allocates a block of memory of the specified size in bytes and initializes it to zero.
    /// </summary>
    /// <param name="size">Specifies the number of bytes to allocate in memory.</param>
    /// <returns>Returns a pointer to the allocated and zero-initialized memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Calloc(nuint size)
    {
#if MHP_ENABLE_MIMALLOC
        var ptr = Mimalloc.mi_zalloc(size);
        if (ptr == null)
        {
            throw new OutOfMemoryException("Failed to allocate zero-initialized memory using Calloc.");
        }

        return ptr;
#elif NET6_0_OR_GREATER
        return NativeMemory.AllocZeroed(size);
#else
        var ptr = Marshal.AllocHGlobal((IntPtr)size).ToPointer();
        Unsafe.InitBlock(ptr, 0, (uint)size);
        return ptr;
#endif
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
#if MHP_ENABLE_MIMALLOC
        var ptr = Mimalloc.mi_aligned_alloc(alignment, size);
        if (ptr == null)
        {
            throw new OutOfMemoryException("Failed to allocate aligned memory using AlignedAlloc.");
        }

        return ptr;
#elif NET6_0_OR_GREATER
        return NativeMemory.AlignedAlloc(size, alignment);
#else
        return Marshal.AllocHGlobal((IntPtr)(size + alignment - 1)).ToPointer();
#endif
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
#if MHP_ENABLE_MIMALLOC
        var ptr = Mimalloc.mi_realloc(ptr, size);
        if (ptr == null)
        {
            throw new OutOfMemoryException("Failed to reallocate memory using Realloc.");
        }

        return ptr;
#elif NET6_0_OR_GREATER
        return NativeMemory.Realloc(ptr, size);
#else
        return Marshal.ReAllocHGlobal((IntPtr)ptr, (IntPtr)size).ToPointer();
#endif
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
#if MHP_ENABLE_MIMALLOC
        var ptr = Mimalloc.mi_realloc_aligned(ptr, size, alignment);
        if (ptr == null)
        {
            throw new OutOfMemoryException("Failed to reallocate aligned memory using AlignedRealloc.");
        }

        return ptr;
#elif NET6_0_OR_GREATER
        return NativeMemory.AlignedRealloc(ptr, size, alignment);
#else
        var newPtr = Marshal.AllocHGlobal((IntPtr)(size + alignment - 1)).ToPointer();
        if (newPtr == null)
        {
            return null;
        }

        if (ptr != null)
        {
            Unsafe.CopyBlock(newPtr, ptr, (uint)size);
            Marshal.FreeHGlobal((IntPtr)ptr);
        }

        return newPtr;
#endif
    }

    /// <summary>
    /// Releases the allocated memory pointed to by the given pointer. This helps in managing memory usage effectively.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block that needs to be freed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(void* ptr)
    {
#if MHP_ENABLE_MIMALLOC
        Mimalloc.mi_free(ptr);
#elif NET6_0_OR_GREATER
        NativeMemory.Free(ptr);
#else
        Marshal.FreeHGlobal((IntPtr)ptr);
#endif
    }

    /// <summary>
    /// Releases memory that was allocated with alignment requirements. It ensures proper deallocation of aligned memory
    /// blocks.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block that needs to be freed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AlignedFree(void* ptr)
    {
#if MHP_ENABLE_MIMALLOC
        Mimalloc.mi_free(ptr);
#elif NET6_0_OR_GREATER
        NativeMemory.AlignedFree(ptr);
#else
        Marshal.FreeHGlobal((IntPtr)ptr);
#endif
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
#if NET6_0_OR_GREATER
        NativeMemory.Clear(ptr, size);
#else
        Unsafe.InitBlock(ptr, 0, (uint)size);
#endif
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
#if NET6_0_OR_GREATER
        NativeMemory.Fill(ptr, size, value);
#else
        Unsafe.InitBlock(ptr, value, (uint)size);
#endif
    }

    /// <summary>
    /// Copies a block of memory from a source location to a destination location.
    /// </summary>
    /// <param name="source">Indicates the memory address from which data will be copied.</param>
    /// <param name="destination">Specifies the memory address where the copied data will be stored.</param>
    /// <param name="size">Defines the number of bytes to be copied from the source to the destination.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemCpy(void* destination, void* source, nuint size)
    {
#if NET6_0_OR_GREATER
        NativeMemory.Copy(source, destination, size);
#else
        Unsafe.CopyBlock(destination, source, (uint)size);
#endif
    }

    /// <summary>
    /// Moves a block of memory from a source location to a destination location, handling overlapping regions correctly.
    /// </summary>
    /// <param name="destination">Indicates the memory address where the data will be moved to.</param>
    /// <param name="source">Specifies the memory address from which data will be moved.</param>
    /// <param name="size">Defines the number of bytes to be moved from the source to the destination.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemMove(void* destination, void* source, nuint size)
    {
#if NET6_0_OR_GREATER
        // NativeMemory.Copy use memmove internally.
        NativeMemory.Copy(source, destination, size);
#else
        Unsafe.CopyBlock(destination, source, (uint)size);
#endif
    }

    /// <summary>
    /// Compares two blocks of memory byte by byte for a specified length.
    /// </summary>
    /// <param name="ptr1">A pointer to the first block of memory to compare.</param>
    /// <param name="ptr2">A pointer to the second block of memory to compare.</param>
    /// <param name="size">The number of bytes to compare. Must not exceed the length of either memory block.</param>
    /// <returns>A signed integer that indicates the relative order of the memory blocks: less than zero if the first differing
    ///     byte in ptr1 is less than the corresponding byte in ptr2; zero if all compared bytes are equal; greater than
    ///     zero if the first differing byte in ptr1 is greater than the corresponding byte in ptr2.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MemCmp(void* ptr1, void* ptr2, nuint size)
    {
        if (ptr1 == ptr2)
        {
            return 0;
        }

        var span1 = new ReadOnlySpan<byte>(ptr1, (int)size);
        var span2 = new ReadOnlySpan<byte>(ptr2, (int)size);

        return span1.SequenceCompareTo(span2);
    }

    public static void* Mmap(void* addr, nuint size, VirtualAllocationFlags flags)
    {
        if (OperatingSystem.IsWindows())
        {
            var allocFlags = 0u;
            var protect = _PAGE_READWRITE;

            if (flags.HasFlag(VirtualAllocationFlags.Reserve))
            {
                allocFlags |= _MEM_RESERVE;
            }

            if (flags.HasFlag(VirtualAllocationFlags.Commit))
            {
                allocFlags |= _MEM_COMMIT;
            }

            var ptr = VirtualAlloc(addr, size, allocFlags, protect);
            if (ptr == null)
            {
                throw new OutOfMemoryException("Failed to allocate memory using MMap.");
            }

            return ptr;
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            if (flags == VirtualAllocationFlags.Commit)
            {
                // POSIX commit changes protection of already-reserved memory
                if (mprotect(addr, size, _PROT_READ | _PROT_WRITE) == -1)
                {
                    throw new OutOfMemoryException("Failed to commit physical RAM via mprotect.");
                }

                return addr;
            }

            // Reserve or Reserve|Commit creates a new mapping
            var prot = flags.HasFlag(VirtualAllocationFlags.Commit) ? _PROT_READ | _PROT_WRITE : _PROT_NONE;
            var ptr = mmap(addr, size, prot, _MAP_PRIVATE | GetMapAnonymousFlag(), -1, 0);

            if (ptr == (void*)-1)
            {
                throw new OutOfMemoryException("Failed to allocate memory using MMap.");
            }

            return ptr;
        }

        throw new PlatformNotSupportedException("Mmap is not supported on this platform.");
    }


    public static bool Munmap(void* ptr, nuint size)
    {
        if (ptr == null)
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            return VirtualFree(ptr, 0, _MEM_RELEASE) != 0;
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return munmap(ptr, size) == 0;
        }

        throw new PlatformNotSupportedException("Munmap is not supported on this platform.");
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

    public static nuint AlignUp(nuint value, nuint alignment)
    {
        if (alignment == 0)
        {
            throw new ArgumentException("Alignment must be greater than zero.", nameof(alignment));
        }

        var mask = alignment - 1;
        return (value + mask) & ~mask;
    }
}

