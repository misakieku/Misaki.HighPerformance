using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnTypedArray : IUnTypedCollection
{
    private void* _buffer;
    private nuint _size;
    private nuint _alignment;

    private MemoryHandle _memoryHandle;
    private AllocationHandle _allocationHandle;

    public readonly nuint Size => _size;
    public readonly nuint Alignment => _alignment;

    public readonly bool IsCreated
    {
        get
        {
#if MHP_ENABLE_SAFETY_CHECKS
            if (_buffer != null)
            {
                if (_allocationHandle.IsValid != null)
                {
                    return _allocationHandle.IsValid(_allocationHandle.State, _memoryHandle);
                }
                else
                {
                    return true;
                }
            }

            return false;
#else
            return _buffer != null;
#endif
        }
    }

    /// <summary>
    /// Constructs an UnsafeArray with a default size of 1 and uses the Persistent allocator.
    /// </summary>
    public UnTypedArray()
        : this(0, 8, Allocator.Invalid)
    {
    }

    public UnTypedArray(nuint size, nuint alignment, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Count must be greater than zero.");
        }

        if (handle.Alloc == null)
        {
            throw new InvalidOperationException("Target allocation handle does not support allocation.");
        }

#if MHP_ENABLE_SAFETY_CHECKS
        MemoryHandle memHandle;
#endif
        var buff = handle.Alloc(handle.State, size, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , &memHandle
#endif
            );
        _size = size;
        _alignment = alignment;

#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle = memHandle;
#endif
        _allocationHandle = handle;
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnTypedArray(nuint size, nuint alignment, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(size, alignment, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Initializes an UnsafeArray with a pointer to a buffer and a count of elements. This does not copy the data.
    /// </summary>
    /// <param name="buffer">A pointer to the memory location that holds the elements of the array.</param>
    /// <param name="count">The total size of the data.</param>
    /// <remarks>
    /// When using this constructor, the user is responsible for managing the memory pointed to by the buffer.
    /// Disposing of the UnsafeArray does not free the memory and only release the reference. The memory should be freed manually when no longer needed.
    /// Use <see cref="UnsafeArray(int, Allocator, AllocationOption)"/> constructor and <see cref="MemCpy(void*, void*, nuint)"/> if you are not sure what you are doing.
    /// </remarks>
    public UnTypedArray(void* buffer, uint size)
    {
        _buffer = buffer;
        _size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T GetElementAt<T>(nuint index)
        where T : unmanaged
    {
        return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
    }

    /// <inheritdoc/>
    public void Resize(uint newSize, AllocationOption option = AllocationOption.None)
    {
        if (newSize == _size)
        {
            return;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        var memHandle = _memoryHandle;
#endif
        _buffer = _allocationHandle.Realloc(_allocationHandle.State, _buffer, _size, newSize, _alignment, option
#if MHP_ENABLE_SAFETY_CHECKS
            , &memHandle
#endif
            );
        _size = newSize;
#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle = memHandle;
#endif
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        MemClear(_buffer, _size);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _buffer;
    }

    /// <summary>
    /// Copies elements from an untyped source collection to a destination span of a specific type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the destination span, which must be unmanaged.</typeparam>
    /// <param name="destination">The typed span where the data will be copied to.</param>
    /// <exception cref="ArgumentException">Thrown when the source collection size exceeds the destination span capacity.</exception>
    public readonly void CopyTo<C, T>(Span<T> destination)
        where T : unmanaged
    {
        var destSize = (uint)destination.Length * (uint)sizeof(T);
        if (_size > destSize)
        {
            throw new ArgumentException("Source collection is larger than the destination span.");
        }

        fixed (T* pDest = destination)
        {
            MemCpy(pDest, _buffer, _size);
        }
    }

    /// <summary>
    /// Copies a range of bytes from an untyped source collection to a destination span, interpreting the bytes as elements of type T.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the destination span, which must be unmanaged.</typeparam>
    /// <param name="destination">The typed span where the elements will be placed.</param>
    /// <param name="sourceOffset">The byte offset in the source collection from which to start copying.</param>
    /// <param name="destinationIndex">The element index in the destination span where copying will begin.</param>
    /// <param name="length">The number of elements of type T to copy.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source collection or destination span.</exception>
    public readonly void CopyTo<T>(Span<T> destination, uint sourceOffset, uint destinationIndex, uint length)
        where T : unmanaged
    {
        var sizeOfElement = (uint)sizeof(T);
        if (sourceOffset + (length * sizeOfElement) > _size || destinationIndex + length > destination.Length)
        {
            throw new ArgumentException("Source collection or destination span is too small for the specified range.");
        }

        fixed (T* pDest = destination)
        {
            MemCpy(pDest + destinationIndex, (byte*)_buffer + sourceOffset, length * sizeOfElement);
        }
    }

    /// <summary>
    /// Copies elements from a typed source span to an untyped destination collection.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the source span, which must be unmanaged.</typeparam>
    /// <param name="source">The typed span containing the elements to be copied.</param>
    /// <exception cref="ArgumentException">Thrown when the destination collection is smaller than the source span data size.</exception>
    public readonly void CopyFrom<T>(ReadOnlySpan<T> source)
        where T : unmanaged
    {
        var sourceSize = (uint)(source.Length * sizeof(T));

        if (_size < sourceSize)
        {
            throw new ArgumentException("Destination collection is smaller than the source span.");
        }

        fixed (T* pSrc = source)
        {
            MemCpy(_buffer, pSrc, sourceSize);
        }
    }

    /// <summary>
    /// Copies a range of elements from a typed source span to an untyped destination collection at a specified byte offset.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the source span, which must be unmanaged.</typeparam>
    /// <param name="source">The typed span containing the elements to be copied.</param>
    /// <param name="sourceIndex">The starting element index in the source span from which to begin copying.</param>
    /// <param name="destinationOffset">The byte offset in the destination collection where the data will be placed.</param>
    /// <param name="length">The number of elements to copy from the source span.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source span or destination collection.</exception>
    public readonly void CopyFrom<T>(ReadOnlySpan<T> source, uint sourceIndex, uint destinationOffset, uint length)
        where T : unmanaged
    {
        var sizeOfElement = (uint)sizeof(T);
        if (sourceIndex + length > source.Length || destinationOffset + (length * sizeOfElement) > _size)
        {
            throw new ArgumentException("Source span or destination collection is too small for the specified range.");
        }

        fixed (T* pSrc = source)
        {
            MemCpy((byte*)_buffer + destinationOffset, pSrc + sourceIndex, length * sizeOfElement);
        }
    }


    /// <summary>
    /// Converts into a Span for efficient memory access.
    /// </summary>
    /// <returns>A Span that provides a view over the elements of the UnsafeCollection.</returns>
    public readonly Span<byte> AsSpan<C>()
    {
        return new(_buffer, (int)_size);
    }

    /// <summary>
    /// Creates a span over a contiguous region of elements in the specified unsafe collection, starting at the given index and covering the specified number of elements.
    /// </summary>
    /// <param name="start">The zero-based index of the first element in the collection to include in the span. Must be greater than or equal to zero and less than the number of elements in the collection.</param>
    /// <param name="length">The number of elements to include in the span. Must be greater than or equal to zero and the range defined by
    /// <paramref name="start"/> and <paramref name="length"/> must not exceed the bounds of the collection.</param>
    /// <returns>A <see cref="Span{byte}"/> representing the specified region of the collection.</returns>
    public readonly Span<byte> AsSpan(int start, int length)
    {
        if (start < 0 || length < 0 || (nuint)(start + length) > _size)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "The specified range is out of bounds of the collection.");
        }

        return new Span<byte>((byte*)_buffer + start, length);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!IsCreated)
        {
            return;
        }

        if (_allocationHandle.Free != null)
        {
            _allocationHandle.Free(_allocationHandle.State, _buffer
#if MHP_ENABLE_SAFETY_CHECKS
                , _memoryHandle
#endif
                );
        }

        _buffer = null;
        _size = 0;
        _alignment = 0;
    }
}
