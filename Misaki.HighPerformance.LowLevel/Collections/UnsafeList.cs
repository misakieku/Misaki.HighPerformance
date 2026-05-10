using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

internal class UnsafeListDebugView<T>
    where T : unmanaged
{
    private readonly UnsafeList<T> _list;
    public UnsafeListDebugView(UnsafeList<T> list)
    {
        _list = list;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var array = new T[_list.Count];
            for (var i = 0; i < _list.Count; i++)
            {
                array[i] = _list[i];
            }
            return array;
        }
    }
}

/// <summary>
/// A collection that allows for unsafe operations on a list of unmanaged types.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the collection, constrained to unmanaged types for performance and safety.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeListDebugView<>))]
public unsafe struct UnsafeList<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public ref struct Enumerator
    {
        private ref UnsafeList<T> _collection;
        private int _index;

        public readonly ref T Current => ref _collection._array[_index];

        public Enumerator(ref UnsafeList<T> collection)
        {
            _collection = ref collection;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _collection._count;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    /// <summary>
    /// A Parallel reader for an UnsafeList.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AsParallelReader"/> to create a parallel reader for a list.
    /// The list must live and the address of the list remain stable at least as long as the parallel reader, and the parallel reader must not be used after the list is disposed.
    /// </remarks>
    public readonly unsafe struct ParallelReader
    {
        public readonly UnsafeList<T>* listData;
        public readonly int Count => listData->_count;

        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref listData->_array[index];
        }

        public ref readonly T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref listData->_array[index];
        }

        internal ParallelReader(UnsafeList<T>* list)
        {
            listData = list;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(ref *listData);
        }

        public readonly ReadOnlySpan<T> AsSpan()
        {
            return new ReadOnlySpan<T>(listData->_array.GetUnsafePtr(), listData->_count);
        }
    }

    /// <summary>
    /// A parallel writer for an UnsafeList.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
    /// The list must live and the address of the list remain stable at least as long as the parallel writer, and the parallel writer must not be used after the list is disposed.
    /// </remarks>
    public readonly struct ParallelWriter
    {
        public readonly UnsafeList<T>* listData;

        internal ParallelWriter(UnsafeList<T>* list)
        {
            listData = list;
        }

        /// <summary>
        /// Adds a value to a collection without resizing it, ensuring capacity is checked before insertion.
        /// </summary>
        /// <param name="value">The value to be added to the collection.</param>
        public void AddNoResize(scoped in T value)
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
                MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(listData->_array.GetUnsafePtr(), index), pCollection, (uint)(count * sizeof(T)));
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

    /// <summary>
    /// Invalid constructor, use <see cref="UnsafeList(int, Allocator, AllocationOption)"/> or <see cref="UnsafeList(int, AllocationHandle, AllocationOption)"/> instead.
    /// </summary>
    public UnsafeList()
        : this(1, AllocationHandle.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeList with a specified number of initial capacity and an allocation handle.
    /// </summary>
    /// <param name="capacity">Specifies the number of initial capacity to allocate in the list, which must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle that manages the memory allocation for the array.</param>
    /// <param name="allocationOption">Specifies how the memory should be allocated.</param>
    public UnsafeList(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(capacity, handle, allocationOption);
        _count = 0;
    }

    /// <summary>
    /// Initializes a new instance of UnsafeList with a specified number of initial capacity and an allocation type.
    /// </summary>
    /// <param name="capacity">Specifies the number of initial capacity to allocate in the list, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeList(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void CheckNoResizeCapacity(int count)
    {
        CheckNoResizeCapacity(count, Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void CheckNoResizeCapacity(int index, int count)
    {
        if (index + count > Capacity)
        {
            throw new Exception($"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Size {Count}), requested count {count}!");
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    /// <summary>
    /// Provides a parallel reader for the current list, enabling thread-safe read operations.
    /// </summary>
    /// <remarks>
    /// The list must live at least as long as the parallel reader, and the parallel reader must not be used after the list is disposed.
    /// For example, if you need to access the list in job system and wait that job in another stack frame, please always allocate the list struct itself on heap.
    /// Otherwise the parallel reader will be invalid after the stack frame that creates the list is popped, even if the list's internal array is still valid.
    /// </remarks>
    /// <returns>A <see cref="ParallelReader"/> instance that can be used to read items from the list in a thread-safe manner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParallelReader AsParallelReader()
    {
        return new((UnsafeList<T>*)UnsafeUtility.AddressOf(ref this));
    }

    /// <summary>
    /// Provides a parallel writer for the current list, enabling thread-safe additions to the list.
    /// </summary>
    /// <remarks>
    /// The list must live at least as long as the parallel writer, and the parallel writer must not be used after the list is disposed.
    /// For example, if you need to access the list in job system and wait that job in another stack frame, please always allocate the list struct itself on heap.
    /// Otherwise the parallel writer will be invalid after the stack frame that creates the list is popped, even if the list's internal array is still valid.
    /// </remarks>
    /// <returns>A <see cref="ParallelWriter"/> instance that can be used to add items to the list in a thread-safe manner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParallelWriter AsParallelWriter()
    {
        return new((UnsafeList<T>*)UnsafeUtility.AddressOf(ref this));
    }

    /// <summary>
    /// Converts the current list to an UnsafeArray representation.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="UnsafeArray{T}"/> shares the same underlying data as the list and does not own the memory.
    /// </remarks>
    /// <returns>A new <see cref="UnsafeArray{T}"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UnsafeArray<T> AsUnsafeArray()
    {
        return new UnsafeArray<T>((T*)_array.GetUnsafePtr(), _count);
    }

    /// <summary>
    /// Converts the current list to a read-only collection that provides unsafe access to its elements.
    /// </summary>
    /// <returns>A new <see cref="ReadOnlyUnsafeCollection{T}"/> instance that allows for read-only access to the list's elements without copying.</returns>
    public readonly ReadOnlyUnsafeCollection<T> AsReadOnly()
    {
        return new ReadOnlyUnsafeCollection<T>((T*)_array.GetUnsafePtr(), _count);
    }

    /// <summary>
    /// Adds a new element to the end of the list, resizing the internal array if necessary.
    /// </summary>
    /// <param name="value">The element to be added to the list.</param>
    public void Add(scoped in T value)
    {
        if (_count >= Capacity)
        {
            Resize(Math.Max(1, Capacity * 2));
        }

        UnsafeUtility.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
        _count++;
    }

    /// <summary>
    /// Adds the specified value to the collection without resizing the underlying storage.
    /// </summary>
    /// <param name="value">The value to add to the collection.</param>
    public void AddNoResize(scoped in T value)
    {
        CheckNoResizeCapacity(1);

        UnsafeUtility.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
        _count++;
    }

    /// <summary>
    /// Adds a range of elements to the collection.
    /// </summary>
    /// <param name="values">A span containing the elements to add.</param>
    public void AddRange(ReadOnlySpan<T> values)
    {
        var newSize = _count + values.Length;
        if (newSize > Capacity)
        {
            Resize(Capacity + values.Length);
        }

        fixed (T* ptr = values)
        {
            MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), ptr, (uint)(values.Length * sizeof(T)));
        }

        _count += values.Length;
    }

    /// <summary>
    /// Adds a range of elements from a pointer to the collection.
    /// </summary>
    /// <param name="ptr">Points to the source data to be copied into the collection.</param>
    /// <param name="count">Indicates the number of elements to be added from the source data.</param>
    public void AddRange(T* ptr, int count)
    {
        var newSize = _count + count;
        if (newSize > Capacity)
        {
            Resize(Capacity + count);
        }

        MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), ptr, (uint)(count * sizeof(T)));
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
            MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), pCollection, (uint)(collection.Length * sizeof(T)));
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

        MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), _count), ptr, (uint)(count * sizeof(T)));
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
        MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), start),
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

        var numToCopy = Math.Min(length, _count - (start + length));
        var copyFrom = _count - numToCopy;

        if (numToCopy > 0)
        {
            MemoryUtility.MemCpy(UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), start),
                UnsafeUtility.ReadArrayElementUnsafe<T>(_array.GetUnsafePtr(), copyFrom),
                (uint)((_count - copyFrom) * sizeof(T)));
        }

        _count -= length;
    }

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

    /// <summary>
    /// Sets the count of the collection to a new value without modifying the underlying storage.
    /// </summary>
    /// <remarks>
    /// This method will not initialize new elements, so it should be used with caution. The new count must be between 0 and the current capacity of the collection.
    /// </remarks>
    /// <param name="newCount">The new count value to set for the collection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the new count is outside the valid range.</exception>
    public void UnsafeSetCount(int newCount)
    {
        if (newCount < 0 || newCount > Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(newCount), $"Value for newCount {newCount} must be between 0 and Capacity {Capacity}.");
        }

        _count = newCount;
    }

    public void Clear()
    {
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _array.GetUnsafePtr();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan()
    {
        return _array.AsSpan(0, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan(int start, int length)
    {
        CheckIndexCount(start, length);
        return _array.AsSpan(start, length);
    }


    /// <summary>
    /// Copies elements from a source UnsafeCollection to a destination Span, ensuring both have the same size.
    /// </summary>
    /// <param name="destination">Represents the target span where elements are copied to.</param>
    public readonly void CopyTo(Span<T> destination)
    {
        var size = Math.Min(destination.Length, Count);
        fixed (T* pDest = destination)
        {
            MemoryUtility.MemCpy(pDest, _array.GetUnsafePtr(), (uint)(size * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies a range of elements from a source collection to a destination span, ensuring both are adequately sized.
    /// </summary>
    /// <param name="destination">The span where the elements will be copied to.</param>
    /// <param name="sourceIndex">The starting index in the source collection for the copy operation.</param>
    /// <param name="destinationIndex">The starting index in the destination span where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source to the destination.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified range exceeds the bounds of the source collection or destination span.</exception>
    public readonly void CopyTo(Span<T> destination, int sourceIndex, int destinationIndex, int length)
    {
        if (sourceIndex + length > _count || destinationIndex + length > destination.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Source collection or destination span is too small for the specified range.");
        }

        fixed (T* pDest = destination)
        {
            MemoryUtility.MemCpy(pDest + destinationIndex, (byte*)_array.GetUnsafePtr() + sourceIndex * sizeof(T), (nuint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies elements from a source span to a destination unsafe collection, ensuring both have the same size.
    /// </summary>
    /// <param name="source">Represents the span containing the elements to be copied to the unsafe collection.</param>
    public void CopyFrom(ReadOnlySpan<T> source)
    {
        if (_count < source.Length)
        {
            Resize(source.Length);
        }

        fixed (T* pSrc = source)
        {
            MemoryUtility.MemCpy(_array.GetUnsafePtr(), pSrc, (nuint)(source.Length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies a specified range of elements from a source span to a destination collection.
    /// </summary>
    /// <param name="source">The span containing the elements to be copied.</param>
    /// <param name="sourceIndex">The starting index in the source span from which to begin copying.</param>
    /// <param name="destinationIndex">The starting index in the destination collection where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source span to the destination collection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified range exceeds the bounds of the source span or destination collection.</exception>
    public void CopyFrom(ReadOnlySpan<T> source, int sourceIndex, int destinationIndex, int length)
    {
        if (sourceIndex + length > source.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Source span or destination collection is too small for the specified range.");
        }

        if (destinationIndex + length > _count)
        {
            Resize(destinationIndex + length);
        }

        fixed (T* pSrc = source)
        {
            MemoryUtility.MemCpy((byte*)_array.GetUnsafePtr() + destinationIndex * sizeof(T), pSrc + sourceIndex, (nuint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> containing the elements.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> containing all elements.</returns>
    public readonly List<T> ToList()
    {
        var list = new List<T>(_count);
        var span = new Span<T>(_array.GetUnsafePtr(), _count);
        list.AddRange(span);
        return list;
    }

    public void Dispose()
    {
        _array.Dispose();
        _count = 0;
    }

    public static implicit operator ReadOnlyUnsafeCollection<T>(UnsafeList<T> list)
    {
        return list.AsReadOnly();
    }

    public static implicit operator Span<T>(UnsafeList<T> list)
    {
        return list.AsSpan();
    }
}
