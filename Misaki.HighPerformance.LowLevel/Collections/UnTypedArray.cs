using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnTypedArray : IUnTypedCollection
{
    private void* _buffer;
    private nuint _size;
    private nuint _alignment;

    private MemoryHandle _memoryHandle;
    private AllocationHandle* _allocationHandle;

    public readonly nuint Size => _size;
    public readonly nuint Alignment => _alignment;

    public readonly bool IsCreated => _buffer != null && _allocationHandle != null && _memoryHandle.IsValid;

    /// <summary>
    /// Constructs an UnsafeArray with a default size of 1 and uses the Persistent allocator.
    /// </summary>
    public UnTypedArray()
        : this(0, 8, Allocator.Invalid)
    {
    }

    public UnTypedArray(nuint size, nuint alignment, ref AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Count must be greater than zero.");
        }

        MemoryHandle memHandle;
        _buffer = handle.Alloc(_allocationHandle->Allocator, size, alignment, allocationOption, &memHandle);
        _size = size;
        _alignment = alignment;

        _memoryHandle = memHandle;
        _allocationHandle = (AllocationHandle*)Unsafe.AsPointer(ref handle);
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnTypedArray(nuint size, nuint alignment, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
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

        MemoryHandle memHandle = _memoryHandle;
        _buffer = _allocationHandle->Realloc(_allocationHandle->Allocator, _buffer, _size, newSize, _alignment, option, &memHandle);
        _size = newSize;
        _memoryHandle = memHandle;
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

        if (_allocationHandle != null)
        {
            _allocationHandle->Free(_allocationHandle->Allocator, _buffer, _memoryHandle);
        }

        _allocationHandle = null;
        _buffer = null;
        _size = 0;
        _alignment = 0;
    }
}