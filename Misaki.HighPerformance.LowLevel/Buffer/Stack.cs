using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Provides a stack-based memory allocator for unmanaged memory, enabling fast allocation and deallocation of memory
/// blocks within a preallocated buffer.
/// </summary>
/// <remarks>This is not a thread-safe implementation.</remarks>
public unsafe partial struct Stack : IMemoryAllocator<Stack, Stack.CreationOptions>
{
    public struct CreationOptions
    {
        public nuint size;
    }

    public static Stack Create(in CreationOptions opts)
    {
        return new Stack(opts.size);
    }

    public readonly ref struct Scope : IDisposable
    {
        private readonly Stack* _allocator;
        private readonly AllocationHandle _handle;
        private readonly nuint _originalOffset;

        public readonly AllocationHandle AllocationHandle => _handle;

        internal Scope(Stack* allocator, AllocationHandle handle)
        {
            _allocator = allocator;
            _handle = handle;
            _originalOffset = allocator->_offset;
        }

        public void Dispose()
        {
            if (_allocator != null)
            {
                _allocator->_offset = _allocator->_offset > _originalOffset ? _originalOffset : _allocator->_offset;
            }
        }
    }

    private byte* _buffer;
    private nuint _size;
    private nuint _offset;

    public readonly byte* Buffer => _buffer;
    public readonly nuint Size => _size;
    public readonly nuint Offset => _offset;

    /// <summary>
    /// Initializes a new instance of the StackAllocator class with a buffer of the specified size.
    /// </summary>
    /// <param name="size">The size, in bytes, of the memory buffer to allocate for stack-based allocations. Must be greater than zero.</param>
    public Stack(nuint size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        _buffer = (byte*)NativeMemory.Alloc(size);
        _size = size;
        _offset = 0;
    }

    /// <summary>
    /// Creates a new scope instance associated with the current stack context.
    /// </summary>
    /// <remarks>
    /// The instance of <see cref="Stack"/> must be pinned or allocated on the native heap to ensure that the pointer remains valid for the lifetime of the scope.
    /// </remarks>
    /// <returns>A <see cref="Scope"/> object that represents a scope tied to this stack.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scope CreateScope(AllocationHandle handle)
    {
        return new Scope((Stack*)Unsafe.AsPointer(ref this), handle);
    }

    /// <summary>
    /// Allocates a block of memory of the specified size and alignment from the buffer.
    /// </summary>
    /// <param name="size">The number of bytes to allocate. Must be greater than zero and less than or equal to the remaining buffer size.</param>
    /// <param name="alignment">The alignment, in bytes, for the allocated memory block. Must be a power of two and greater than zero.</param>
    /// <param name="allocationOption">An option specifying additional allocation behavior, such as whether the allocated memory should be cleared. The
    /// default is <see cref="AllocationOption.None"/>.</param>
    /// <returns>A pointer to the beginning of the allocated memory block if successful; otherwise, <see langword="null"/> if
    /// there is insufficient space in the buffer.</returns>
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (size == 0)
        {
            return null;
        }

        if ((alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        }

        var alignedOffset = (_offset + alignment - 1) & ~(alignment - 1);
        var newOffset = alignedOffset + size;

        if (newOffset > _size)
        {
            throw new OutOfMemoryException("Insufficient memory in stack allocator.");
        }

        var ptr = _buffer + alignedOffset;

        _offset = newOffset;

        if (allocationOption.HasOption(AllocationOption.Clear))
        {
            MemoryUtility.MemClear(ptr, size);
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

        var oldBase = _buffer + _offset - oldSize;
        if (ptr == oldBase)
        {
            if (newSize > oldSize)
            {
                var diff = newSize - oldSize;
                _offset += diff;
                if (allocationOption.HasOption(AllocationOption.Clear))
                {
                    MemoryUtility.MemClear(_buffer + _offset - diff, diff);
                }
            }

            return ptr;
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemoryUtility.MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));
        return newPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Free(void* ptr)
    {
    }

    /// <summary>
    /// Resets the internal offset to its initial position.
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
        _offset = 0;
        _size = 0;

        NativeMemory.Free(ptr);
    }
}
