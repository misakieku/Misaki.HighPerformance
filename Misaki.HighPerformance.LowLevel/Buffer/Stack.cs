using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Provides a stack-based memory allocator for unmanaged memory, enabling fast allocation and deallocation of memory
/// blocks within a preallocated buffer.
/// </summary>
/// <remarks>This is not a thread-safe implementation.</remarks>
public unsafe partial struct Stack : IMemoryAllocator<Stack, Stack.CreationOpts>
{
    public struct CreationOpts
    {
        public nuint size;
    }

    public static Stack Create(in CreationOpts opts)
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
#if MHP_ENABLE_SAFETY_CHECKS
            _allocator->_activeScopeCount++;
#endif
        }

        public void Dispose()
        {
            if (_allocator != null)
            {
                _allocator->_offset = _allocator->_offset > _originalOffset ? _originalOffset : _allocator->_offset;
#if MHP_ENABLE_SAFETY_CHECKS
                _allocator->_activeScopeCount--;
#endif
            }
        }
    }

    private byte* _buffer;
    private nuint _size;
    private nuint _offset;
#if MHP_ENABLE_SAFETY_CHECKS
    private uint _activeScopeCount;
#endif

    internal readonly byte* Buffer => _buffer;
    internal nuint Offset
    {
        readonly get => _offset;
        set => _offset = value;
    }

    /// <summary>
    /// Initializes a new instance of the StackAllocator class with a buffer of the specified size.
    /// </summary>
    /// <param name="size">The size, in bytes, of the memory buffer to allocate for stack-based allocations. Must be greater than zero.</param>
    public Stack(nuint size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        _buffer = (byte*)Malloc(size);
        _size = size;
        _offset = 0;
#if MHP_ENABLE_SAFETY_CHECKS
        _activeScopeCount = 0;
#endif
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
#if MHP_ENABLE_SAFETY_CHECKS
        if (_activeScopeCount == 0)
        {
            throw new InvalidOperationException("Allocations can only be made within an active memory scope.");
        }
#endif

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

        if (allocationOption.HasFlag(AllocationOption.Clear))
        {
            MemClear(ptr, size);
        }

        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free(void* ptr)
    {
        if (ptr < _buffer && ptr >= _buffer + _size)
        {
            Debug.Fail("Attempting to free a pointer that is out of bounds of the current stack allocation.");
            return; // Pointer is out of bounds, ignore
        }

        var offset = (nuint)((byte*)ptr - _buffer);
        _offset = offset < _offset ? offset : _offset;
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

        Free(_buffer);

        _buffer = null;
        _size = 0;
        _offset = 0;
    }
}
