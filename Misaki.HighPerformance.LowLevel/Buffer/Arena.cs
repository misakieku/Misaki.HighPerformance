using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A memory management structure that allocates and resets memory blocks with specified alignment.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)] // Cache line aligned to prevent false sharing
public unsafe struct Arena : IMemoryAllocator<Arena, Arena.CreationOptions>
{
    public struct CreationOptions
    {
        public nuint size;
    }

    public static Arena Create(in CreationOptions opts)
    {
        return new Arena(opts.size);
    }

    [FieldOffset(0)]
    private byte* _buffer;
    [FieldOffset(8)]
    private nuint _size;
    [FieldOffset(16)]
    private nuint _offset;

    public readonly byte* Buffer => _buffer;
    public readonly nuint Size => _size;
    public readonly nuint Offset => _offset;

    public Arena(nuint size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        if (_buffer != null)
        {
            return;
        }

        _buffer = (byte*)Malloc(size);
        _size = size;
        _offset = 0;
    }

    /// <summary>
    /// Allocates a block of memory of a specified size with a given alignment.
    /// </summary>
    /// <remarks>
    /// This is thread safe.
    /// </remarks>
    /// <param name="size">Specifies the amount of memory to allocate in bytes.</param>
    /// <param name="alignment">Defines the alignment requirement for the allocated memory.</param>
    /// <param name="allocationOption">The option when allocating memory.</param>
    /// <returns>A pointer to the allocated memory block or null if the allocation cannot be fulfilled.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption)
    {
        if (_buffer == null)
        {
            return null;
        }

        if (size == 0)
        {
            return null;
        }

        if ((alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        }

        nuint currentOffset, newOffset, alignedOffset;

        do
        {
            currentOffset = _offset;
            alignedOffset = (currentOffset + alignment - 1) & ~(alignment - 1);
            newOffset = alignedOffset + size;

            if (newOffset > _size)
            {
                return null;
            }

        } while (Interlocked.CompareExchange(ref _offset, newOffset, currentOffset) != currentOffset);

        var ptr = _buffer + alignedOffset;
        if (allocationOption.HasOption(AllocationOption.Clear))
        {
            MemClear(ptr, size);
        }

        return ptr;
    }

    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
    {
        if (_buffer == null)
        {
            return null;
        }

        if (ptr == null)
        {
            return Allocate(newSize, alignment, allocationOption);
        }

        var additionalSize = newSize - oldSize;
        var currentOffset = Volatile.Read(ref _offset);

        if ((byte*)ptr + oldSize == _buffer + currentOffset)
        {
            if (currentOffset + additionalSize <= _size)
            {
                if (Interlocked.CompareExchange(ref _offset, currentOffset + additionalSize, currentOffset) == currentOffset)
                {
                    if (allocationOption.HasOption(AllocationOption.Clear) && additionalSize > 0)
                    {
                        MemClear((byte*)ptr + oldSize, additionalSize);
                    }

                    return ptr;
                }
            }
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));
        return newPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Free(void* ptr)
    {
    }

    /// <summary>
    /// Resets the arena.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _offset = 0;
    }

    public void Dispose()
    {
        if (_buffer == null)
        {
            return;
        }

        var ptr = _buffer;

        _buffer = null;
        _size = 0;
        _offset = 0;

        MemoryUtility.Free(ptr);
    }
}