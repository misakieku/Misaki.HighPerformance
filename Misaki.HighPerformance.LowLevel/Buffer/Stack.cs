using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Provides a stack-based memory allocator for unmanaged memory, enabling fast allocation and deallocation of memory
/// blocks within a preallocated buffer.
/// </summary>
/// <remarks>This is not a thread-safe implementation.</remarks>
public unsafe struct Stack : IDisposable
{
    private const nuint _DEFAULT_SIZE = 1024 * 1024; // 1MB

    public readonly ref struct Scope : IDisposable
    {
        private readonly Stack* _allocator;
        private readonly nuint _originalOffset;

        internal Scope(Stack* allocator)
        {
            _allocator = allocator;
            _originalOffset = allocator->_offset;
            _allocator->_activeScopeCount++;
        }

        public void Dispose()
        {
            if (_allocator != null)
            {
                _allocator->_offset = _allocator->_offset > _originalOffset ? _originalOffset : _allocator->_offset;
                _allocator->_activeScopeCount--;
            }
        }
    }

    private byte* _buffer;
    private nuint _size;
    private nuint _offset;
    private uint _activeScopeCount;

    /// <summary>
    /// Initializes a new instance of the StackAllocator class with a buffer of the specified size.
    /// </summary>
    /// <param name="size">The size, in bytes, of the memory buffer to allocate for stack-based allocations. Must be greater than zero.</param>
    public Stack(nuint size)
    {
        if (size == 0)
        {
            throw new ArgumentException("Size must be greater than zero.", nameof(size));
        }

        Init(size);
    }

    private void Init(nuint size)
    {
        if (_buffer != null)
        {
            Free(_buffer);
        }

        _buffer = (byte*)Malloc(size);
        _size = size;
        _offset = 0;
        _activeScopeCount = 0;
    }

    private readonly void ThrowIfNoScope()
    {
        if (_activeScopeCount == 0)
        {
            throw new InvalidOperationException("Allocations can only be made within an active memory scope.");
        }
    }

    private void EnsureInitialize()
    {
        if (_buffer == null || _size == 0)
        {
            Init(_DEFAULT_SIZE);
        }
    }

    /// <summary>
    /// Creates a new scope instance associated with the current stack context.
    /// </summary>
    /// <returns>A <see cref="Scope"/> object that represents a scope tied to this stack.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scope CreateScope()
    {
        EnsureInitialize();
        return new Scope((Stack*)Unsafe.AsPointer(ref this));
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
        ThrowIfNoScope();

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

    /// <summary>
    /// Resets the internal offset to its initial position.
    /// </summary>
    public void Reset()
    {
        _offset = 0;
    }

    public void Dispose()
    {
        Free(_buffer);

        _buffer = null;
        _size = 0;
        _offset = 0;
    }
}