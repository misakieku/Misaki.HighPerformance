using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnTypedArray : IUnTypedCollection
{
    private void* _buffer;
    private uint _size;
    private uint _alignment;

    private AllocationHandle* _handle;

    public readonly uint Size => _size;
    public readonly uint Alignment => _alignment;

    public readonly bool IsCreated
    {
        get => _buffer != null;
    }

    /// <summary>
    /// Constructs an UnsafeArray with a default size of 1 and uses the Persistent allocator.
    /// </summary>
    public UnTypedArray()
        : this(0, 8, Allocator.Invalid)
    {
    }

    public UnTypedArray(uint size, uint alignment, ref AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Count must be greater than zero.");
        }

        _handle = (AllocationHandle*)Unsafe.AsPointer(ref handle);
        _buffer = handle.Alloc(_handle->Allocator, size, alignment, allocationOption);
        _size = size;
        _alignment = alignment;
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnTypedArray(uint size, uint alignment, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(size, alignment, ref AllocationManager.GetAllocationHandle(allocator), allocationOption)
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
    public readonly ref T GetElementAt<T>(uint index)
        where T : unmanaged
    {
        return ref UnsafeUtilities.ReadArrayElementRef<T>(_buffer, index);
    }

    /// <inheritdoc/>
    public void Resize(uint newSize)
    {
        if (newSize == _size)
        {
            return;
        }

        _buffer = _handle->Realloc(_handle->Allocator, _buffer, newSize, _alignment);
        _size = newSize;
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!IsCreated)
        {
            return;
        }

        if (_handle != null)
        {
            _handle->Free(_handle->Allocator, _buffer);
        }

        _handle = null;
        _buffer = null;
        _size = 0;
        _alignment = 0;
    }
}