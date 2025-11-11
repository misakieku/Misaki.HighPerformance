using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A structure for managing an array of unmanaged types with unsafe memory operations.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in an unmanaged memory context.</typeparam>
public unsafe struct UnsafeArray<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeArray<T>* _collection;
        private int _index;

        public readonly ref T Current => ref _collection->_buffer[_index];
        readonly T IEnumerator<T>.Current => Current;
        readonly object IEnumerator.Current => Current;

        public Enumerator(UnsafeArray<T>* collection)
        {
            _collection = collection;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _collection->_count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }
    }

    private T* _buffer;
    private int _count;
    private AllocationHandle* _handle;

    public readonly int Count => _count;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if ENABLE_COLLECTION_CHECKS
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }
#endif

            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public readonly ref T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if ENABLE_COLLECTION_CHECKS
            if (index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }
#endif

            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer != null;
    }

    public Enumerator GetEnumerator() => new ((UnsafeArray<T>*)UnsafeUtility.AddressOf(ref this));
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Invalid constructor, use <see cref="UnsafeArray(int, Allocator, AllocationOption)"/> or <see cref="UnsafeArray(int, ref AllocationHandle, AllocationOption)"/> instead.
    /// </summary>
    public UnsafeArray()
        : this(0, Allocator.Invalid)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation handle.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle that manages the memory allocation for the array.</param>
    /// <param name="allocationOption">Specifies how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnsafeArray(int count, ref AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        _handle = (AllocationHandle*)Unsafe.AsPointer(ref handle);
        _buffer = (T*)handle.Alloc(_handle->Allocator, (uint)count * (uint)sizeof(T), (uint)AlignOf<T>(), allocationOption);
        _count = count;
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnsafeArray(int count, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(count, ref AllocationManager.GetAllocationHandle(allocator), allocationOption)
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
    public UnsafeArray(T* buffer, int count)
    {
        _buffer = buffer;
        _count = count;
    }

    public ReadOnlyUnsafeCollection<T> AsReadOnly()
    {
        return new ReadOnlyUnsafeCollection<T>(_buffer, _count);
    }

    /// <inheritdoc/>
    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        if (newSize == Count)
        {
            return;
        }

        var elemSize = SizeOf<T>();
        _buffer = (T*)_handle->Realloc(_handle->Allocator, _buffer, (nuint)Count * elemSize, (nuint)newSize * elemSize, AlignOf<T>(), option);
        _count = newSize;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        MemClear(_buffer, (nuint)(Count * sizeof(T)));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _buffer;
    }

    /// <summary>
    /// Reinterprets the underlying buffer as an array of a different unmanaged type without copying the data.
    /// </summary>
    /// <typeparam name="U">The unmanaged type to reinterpret the buffer as.</typeparam>
    /// <returns>An UnsafeArray<U> that views the same memory as the original array, but as elements of type U.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the total size of the buffer in bytes is not a multiple of the size of type U.</exception>
    public readonly UnsafeArray<U> Reinterpret<U>()
        where U : unmanaged
    {
        var totalSize = (nuint)(Count * sizeof(T));
        if (totalSize % (nuint)sizeof(U) != 0)
        {
            throw new InvalidOperationException("Cannot reinterpret array: size mismatch.");
        }

        var newCount = (int)(totalSize / (nuint)sizeof(U));
        return new UnsafeArray<U>((U*)_buffer, newCount);
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
        _count = 0;
    }
}