using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A collection that allows for unsafe operations on a list of unmanaged types.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the collection, constrained to unmanaged types for performance and safety.</typeparam>
public unsafe struct UnsafeList<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeList<T>* _collection;
        private int _index;
        private T _value;

        public Enumerator(UnsafeList<T>* collection)
        {
            _collection = collection;
            _index = -1;
            _value = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            if (_index < _collection->_count)
            {
                _value = UnsafeUtility.ReadArrayElement<T>(_collection->_array.GetUnsafePtr(), _index);
                return true;
            }

            _value = default;
            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        // Let NativeArray indexer check for out of range.
        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _value;
            }
        }

        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Current;
            }
        }

        public readonly void Dispose()
        {
        }
    }

    /// <summary>
    /// A parallel writer for an UnsafeList.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
    /// </remarks>
    public unsafe struct ParallelWriter
    {
        /// <summary>
        /// The UnsafeList to write to.
        /// </summary>
        public UnsafeList<T>* listData;

        internal ParallelWriter(UnsafeList<T>* list)
        {
            listData = list;
        }

        /// <summary>
        /// Adds a value to a collection without resizing it, ensuring capacity is checked before insertion.
        /// </summary>
        /// <param name="value">The value to be added to the collection.</param>
        public void AddNoResize(T value)
        {
            var idx = Interlocked.Increment(ref listData->_count) - 1;
            listData->CheckNoResizeCapacity(idx, 1);
            UnsafeUtility.WriteArrayElement(listData->_array.GetUnsafePtr(), idx, value);
        }

        /// <summary>
        /// Adds a specified number of elements from a pointer to a buffer without resizing the underlying storage.
        /// </summary>
        /// <param name="ptr">Points to the source data to be copied into the buffer.</param>
        /// <param name="count">Indicates the number of elements to be added from the source data.</param>
        public void AddRangeNoResize(ReadOnlySpan<T> collection, int count)
        {
            var index = Interlocked.Add(ref listData->_count, count) - count;
            listData->CheckNoResizeCapacity(index, count);

            fixed (T* pCollection = collection)
            {
                MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(listData->_array.GetUnsafePtr(), index), pCollection, (uint)(count * sizeof(T)));
            }
        }
    }

    private UnsafeArray<T> _array;

    private int _count;

    public readonly int Count => _count;
    public readonly int Capacity => _array.Count;
    public readonly bool IsCreated => _array.IsCreated;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _array[index];
    }

    public readonly ref T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _array[index];
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator((UnsafeList<T>*)UnsafeUtility.AddressOf(ref this));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Provides a parallel writer for the current list, enabling thread-safe additions to the list.
    /// </summary>
    /// <returns>A <see cref="ParallelWriter"/> instance that can be used to add items to the list in a thread-safe manner.</returns>
    public ParallelWriter AsParallelWriter() => new((UnsafeList<T>*)UnsafeUtility.AddressOf(ref this));

    /// <summary>
    /// Converts the current list to an UnsafeArray representation.
    /// </summary>
    /// <returns>A new <see cref="UnsafeArray{T}"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UnsafeArray<T> AsUnsafeArray() => new((T*)_array.GetUnsafePtr(), _count);

    /// <summary>
    /// Invalid constructor, use <see cref="UnsafeList(int, Allocator, AllocationOption)"/> or <see cref="UnsafeList(int, ref AllocationHandle, AllocationOption)"/> instead.
    /// </summary>
    public UnsafeList()
        : this(0, Allocator.Invalid)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeList with a specified number of initial capacity and an allocation handle.
    /// </summary>
    /// <param name="capacity">Specifies the number of initial capacity to allocate in the list, which must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle that manages the memory allocation for the array.</param>
    /// <param name="allocationOption">Specifies how the memory should be allocated.</param>
    public UnsafeList(int capacity, ref AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(capacity, ref handle, allocationOption);
        _count = 0;
    }

    /// <summary>
    /// Initializes a new instance of UnsafeList with a specified number of initial capacity and an allocation type.
    /// </summary>
    /// <param name="capacity">Specifies the number of initial capacity to allocate in the list, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    public UnsafeList(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, ref AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    private readonly void CheckNoResizeCapacity(int count)
    {
        CheckNoResizeCapacity(count, Count);
    }

    private readonly void CheckNoResizeCapacity(int index, int count)
    {
        if (index + count > Capacity)
        {
            throw new Exception(
                $"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Size {Count}), requested count {count}!"
            );
        }
    }

    private readonly void CheckIndexCount(int index, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException($"Value for count {count} must be positive.");
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException($"Value for index {index} must be positive.");
        }

        if (index > Count)
        {
            throw new ArgumentOutOfRangeException($"Value for index {index} is out of bounds.");
        }

        if (index + count > Count)
        {
            throw new ArgumentOutOfRangeException($"Value for count {count} is out of bounds.");
        }
    }

    public readonly ReadOnlyUnsafeCollection<T> AsReadOnly()
    {
        return new ReadOnlyUnsafeCollection<T>((T*)_array.GetUnsafePtr(), _count);
    }

    /// <summary>
    /// Adds a new element to the end of the list, resizing the internal array if necessary.
    /// </summary>
    /// <param name="value">The element to be added to the list.</param>
    public void Add(T value)
    {
        if (_count >= Capacity)
        {
            Resize(Capacity + (int)(Capacity * 0.5f));
        }

        UnsafeUtility.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
        _count++;
    }

    /// <summary>
    /// Adds the specified value to the collection without resizing the underlying storage.
    /// </summary>
    /// <param name="value">The value to add to the collection.</param>
    public void AddNoResize(T value)
    {
        CheckNoResizeCapacity(1);

        UnsafeUtility.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
        _count++;
    }

    /// <summary>
    /// Adds a range of elements to the collection.
    /// </summary>
    /// <param name="values">A span containing the elements to add. The span must not exceed the specified <paramref name="count"/>.</param>
    /// <param name="count">The number of elements to add from the <paramref name="values"/> span. Must be non-negative and less than or
    /// equal to the length of <paramref name="values"/>.</param>
    public void AddRange(Span<T> values, int count)
    {
        var newSize = _count + count;
        if (newSize > Capacity)
        {
            Resize(Capacity + count);
        }

        fixed (T* ptr = values)
        {
            MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), ptr, (uint)(count * sizeof(T)));
        }

        _count += count;
    }

    /// <summary>
    /// Adds the elements of the specified collection to the current list without resizing the underlying storage.
    /// </summary>
    /// <param name="collection">A read-only span containing the elements to add. The span must not exceed the available capacity.</param>
    public void AddRangeNoResize(ReadOnlySpan<T> collection)
    {
        CheckNoResizeCapacity(collection.Length);

        fixed (T* pCollection = collection)
        {
            MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), pCollection, (uint)(collection.Length * sizeof(T)));
        }

        _count += collection.Length;
    }

    /// <summary>
    /// Adds a range of elements from a pointer to the collection without resizing the underlying storage.
    /// </summary>
    /// <param name="ptr">Points to the source data to be copied into the collection.</param>
    /// <param name="count">Indicates the number of elements to be added from the source data.</param>
    public void AddRangeNoResize(T* ptr, int count)
    {
        CheckNoResizeCapacity(count);

        MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), ptr, (uint)(count * sizeof(T)));
        _count += count;
    }

    /// <summary>
    /// Removes a range of elements from the list starting at the specified index.
    /// </summary>
    /// <param name="start">The zero-based index at which to start removing elements.</param>
    /// <param name="length">The number of elements to remove.</param>
    public void RemoveRange(int start, int length)
    {
        CheckIndexCount(start, length);

        if (length <= 0)
        {
            return;
        }

        var copyFrom = Math.Min(start + length, _count);
        MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), start),
            UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), copyFrom),
            (uint)((_count - copyFrom) * sizeof(T))
        );
        _count -= length;
    }

    /// <summary>
    /// Removes the element at the specified index from the collection.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        RemoveRange(index, 1);
    }

    /// <summary>
    /// Removes a range of elements from the list starting at the specified index by swapping them with the last elements.
    /// </summary>
    /// <param name="start">The zero-based index at which to start removing elements.</param>
    /// <param name="length">The number of elements to remove.</param>
    public void RemoveRangeSwapBack(int start, int length)
    {
        CheckIndexCount(start, length);

        if (length <= 0)
        {
            return;
        }

        var copyFrom = Math.Min(_count - length, start + length);
        MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), start),
            UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), copyFrom),
            (uint)((_count - copyFrom) * sizeof(T))
        );
        _count -= length;
    }

    /// <summary>
    /// Removes the element at the specified index by swapping it with the last element and reducing the collection
    /// size.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove. Must be within the bounds of the collection.</param>
    public void RemoveAtSwapBack(int index)
    {
        RemoveRangeSwapBack(index, 1);
    }

    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        _array.Resize(newSize, option);

        if (_count > newSize)
        {
            _count = newSize;
        }
    }

    public void Clear()
    {
        _array.Clear();
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _array.GetUnsafePtr();
    }

    public void Dispose()
    {
        _array.Dispose();
        _count = 0;
    }
}