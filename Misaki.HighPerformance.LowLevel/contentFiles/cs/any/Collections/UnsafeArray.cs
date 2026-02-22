using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

internal class UnsafeArrayDebugView<T>
    where T : unmanaged
{
    private readonly UnsafeArray<T> _array;

    public UnsafeArrayDebugView(UnsafeArray<T> array)
    {
        _array = array;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var count = _array.Count;
            var result = new T[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = _array[i];
            }

            return result;
        }
    }
}

/// <summary>
/// A structure for managing an array of unmanaged types with unsafe memory operations.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in an unmanaged memory context.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeArrayDebugView<>))]
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
    private MemoryHandle _memoryHandle;
    private AllocationHandle _allocationHandle;

    public readonly int Count => _count;
    public readonly int Length => _count;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndexBounds(index);
            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public readonly ref T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndexBounds((int)index);
            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public readonly bool IsCreated
    {
        get
        {
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
        }
    }

    public Enumerator GetEnumerator() => new((UnsafeArray<T>*)UnsafeUtility.AddressOf(ref this));
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
    public UnsafeArray(int count, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count can not be less than zero.");
        }

        if (handle.Alloc == null)
        {
            throw new InvalidOperationException("Target allocation handle does not support allocation.");
        }

        MemoryHandle memHandle;
        var buff = handle.Alloc(handle.State, (nuint)(count * sizeof(T)), AlignOf<T>(), allocationOption, &memHandle);

        _buffer = (T*)buff;
        _memoryHandle = memHandle;
        _allocationHandle = handle;
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
        : this(count, AllocationManager.GetAllocationHandle(allocator), allocationOption)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("ENABLE_COLLECTION_CHECKS")]
    private readonly void ThrowIfNotCreated()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("The UnsafeArray is not created.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("ENABLE_COLLECTION_CHECKS")]
    private readonly void CheckIndexBounds(int index)
    {
        ThrowIfNotCreated();
        if (index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }
    }

    /// <summary>
    /// Returns a read-only view of the current collection.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyUnsafeCollection{T}"/> that provides a read-only view of the elements in the current collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlyUnsafeCollection<T> AsReadOnly()
    {
        return new ReadOnlyUnsafeCollection<T>(_buffer, _count);
    }

    /// <inheritdoc/>
    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        ThrowIfNotCreated();

        if (_allocationHandle.Realloc == null)
        {
            throw new InvalidOperationException("Target allocation handle does not support reallocation.");
        }

        if (newSize == Count)
        {
            return;
        }

        MemoryHandle memHandle = _memoryHandle;
        var elemSize = SizeOf<T>();
        _buffer = (T*)_allocationHandle.Realloc(_allocationHandle.State, _buffer, (nuint)Count * elemSize, (nuint)newSize * elemSize, AlignOf<T>(), option, &memHandle);
        _memoryHandle = memHandle;
        _count = newSize;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        ThrowIfNotCreated();
        MemClear(_buffer, (nuint)(Count * sizeof(T)));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        ThrowIfNotCreated();
        return _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan()
    {
        ThrowIfNotCreated();
        return new Span<T>(_buffer, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan(int start, int length)
    {
        ThrowIfNotCreated();
        return new Span<T>(_buffer + start, length);
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
        ThrowIfNotCreated();

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

        if (_allocationHandle.Free != null)
        {
            _allocationHandle.Free(_allocationHandle.State, _buffer, _memoryHandle);
        }

        _buffer = null;
        _count = 0;
    }
}
