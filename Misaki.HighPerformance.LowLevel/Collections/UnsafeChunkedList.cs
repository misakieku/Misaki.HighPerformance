using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

internal class UnsafeChunkedListDebugView<T>
    where T : unmanaged
{
    private readonly UnsafeChunkedList<T> _list;

    public UnsafeChunkedListDebugView(UnsafeChunkedList<T> list)
    {
        _list = list;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var array = new T[_list.Count];
            _list.CopyTo(array);
            return array;
        }
    }
}

/// <summary>
/// A collection that stores elements in fixed-size chunks, enabling stable element addresses
/// and eliminating large reallocation during growth. Adding elements never moves existing ones.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the collection, constrained to unmanaged types for performance and safety.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeChunkedListDebugView<>))]
public unsafe struct UnsafeChunkedList<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public const int DEFAULT_CHUNK_SIZE_IN_BYTES = 16384;

    public ref struct Enumerator
    {
        private ref UnsafeChunkedList<T> _collection;
        private int _index;

        public readonly ref T Current
        {
            get
            {
                var (chunkIdx, offset) = SplitIndex(_index, _collection._chunkCapacity);
                return ref ((T*)_collection._chunks[chunkIdx])[offset];
            }
        }

        public Enumerator(ref UnsafeChunkedList<T> collection)
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
    /// A parallel reader for an UnsafeChunkedList.
    /// </summary>
    public readonly unsafe struct ParallelReader
    {
        public readonly UnsafeChunkedList<T>* listData;
        public readonly int Count => listData->_count;
        public readonly int ChunkCapacity => listData->_chunkCapacity;

        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var (chunkIdx, offset) = SplitIndex(index, listData->_chunkCapacity);
                return ref ((T*)listData->_chunks[chunkIdx])[offset];
            }
        }

        public ref readonly T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this[(int)index];
        }

        internal ParallelReader(UnsafeChunkedList<T>* list)
        {
            listData = list;
        }

        public readonly Enumerator GetEnumerator()
        {
            ref var list = ref Unsafe.AsRef<UnsafeChunkedList<T>>(listData);
            return new Enumerator(ref list);
        }
    }

    /// <summary>
    /// A parallel writer for an UnsafeChunkedList.
    /// </summary>
    /// <remarks>
    /// Adding elements is thread-safe and auto-allocates chunks as needed, since new chunks never move existing data.
    /// The chunk pointer array must be pre-sized via <see cref="EnsureCapacity"/> before dispatching parallel writes.
    /// </remarks>
    public readonly struct ParallelWriter
    {
        public readonly UnsafeChunkedList<T>* listData;

        internal ParallelWriter(UnsafeChunkedList<T>* list)
        {
            listData = list;
        }

        /// <summary>
        /// Thread-safely adds a value, auto-allocating new chunks as needed.
        /// </summary>
        public void Add(scoped in T value)
        {
            var idx = Interlocked.Increment(ref listData->_count) - 1;
            var (chunkIdx, offset) = SplitIndex(idx, listData->_chunkCapacity);
            listData->EnsureChunkParallel(chunkIdx);
            ((T*)listData->_chunks[chunkIdx])[offset] = value;
        }

        /// <summary>
        /// Thread-safely adds a range of elements, auto-allocating new chunks as needed.
        /// </summary>
        public void AddRange(ReadOnlySpan<T> collection)
        {
            var count = collection.Length;
            var index = Interlocked.Add(ref listData->_count, count) - count;

            fixed (T* pCollection = collection)
            {
                int remaining = count;
                T* srcPtr = pCollection;
                int currentIndex = index;

                while (remaining > 0)
                {
                    var (chunkIdx, offset) = SplitIndex(currentIndex, listData->_chunkCapacity);
                    var copyCount = Math.Min(remaining, listData->_chunkCapacity - offset);
                    listData->EnsureChunkParallel(chunkIdx);
                    var dstPtr = (T*)listData->_chunks[chunkIdx] + offset;
                    MemoryUtility.MemCpy(dstPtr, srcPtr, (nuint)(copyCount * sizeof(T)));
                    srcPtr += copyCount;
                    currentIndex += copyCount;
                    remaining -= copyCount;
                }
            }
        }
    }

    private UnsafeArray<nint> _chunks;
    private int _chunkCount;
    private int _count;
    private readonly int _chunkCapacity;
    private readonly AllocationHandle _allocationHandle;

    public readonly int Count => _count;
    public readonly int ChunkCapacity => _chunkCapacity;
    public readonly int ChunkCount => _chunkCount;
    public readonly int Capacity => _chunkCount * _chunkCapacity;
    public readonly bool IsCreated => _chunks.IsCreated;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var (chunkIdx, offset) = SplitIndex(index, _chunkCapacity);
            return ref ((T*)_chunks[chunkIdx])[offset];
        }
    }

    public readonly ref T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var (chunkIdx, offset) = SplitIndex((int)index, _chunkCapacity);
            return ref ((T*)_chunks[chunkIdx])[offset];
        }
    }

    /// <summary>
    /// Invalid constructor, use <see cref="UnsafeChunkedList(int, AllocationHandle, AllocationOption)"/> instead.
    /// </summary>
    public UnsafeChunkedList()
        : this(DEFAULT_CHUNK_SIZE_IN_BYTES / sizeof(T), AllocationHandle.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance with a specified chunk capacity and allocator.
    /// </summary>
    /// <param name="chunkCapacity">The maximum number of elements per chunk.</param>
    /// <param name="handle">A reference to an AllocationHandle that manages memory allocation.</param>
    /// <param name="allocationOption">Specifies how the memory should be allocated.</param>
    public UnsafeChunkedList(int chunkCapacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        chunkCapacity = Math.Max(1, chunkCapacity);
        _chunks = new UnsafeArray<nint>(4, handle, allocationOption);
        _chunkCount = 0;
        _count = 0;
        _chunkCapacity = chunkCapacity;
        _allocationHandle = handle;
    }

    /// <summary>
    /// Initializes a new instance with a specified chunk capacity and an allocation type.
    /// </summary>
    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeChunkedList(int chunkCapacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(chunkCapacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void CheckIndexBounds(int index)
    {
        if (index < 0 || index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
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

    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void ThrowIfNotCreated()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("The UnsafeChunkedList is not created.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int chunkIndex, int offset) SplitIndex(int index, int chunkCapacity)
    {
        return (index / chunkCapacity, index % chunkCapacity);
    }

    private void GrowChunkArray(int minCapacity)
    {
        var newCapacity = Math.Max(minCapacity, Math.Max(_chunks.Count * 2, 4));
        _chunks.Resize(newCapacity);
    }

    private void AllocateChunk(int chunkIndex)
    {
        if (chunkIndex >= _chunks.Count)
        {
            GrowChunkArray(chunkIndex + 1);
        }

        var sizeInBytes = (nuint)(_chunkCapacity * sizeof(T));
        _chunks[chunkIndex] = (nint)_allocationHandle.Alloc(sizeInBytes, MemoryUtility.AlignOf<T>());
        _chunkCount = Math.Max(_chunkCount, chunkIndex + 1);
    }

    private void EnsureChunkIndex(int elementIndex)
    {
        if (elementIndex < 0)
        {
            return;
        }

        var (chunkIdx, _) = SplitIndex(elementIndex, _chunkCapacity);
        while (_chunkCount <= chunkIdx)
        {
            AllocateChunk(_chunkCount);
        }
    }

    private void EnsureChunkParallel(int chunkIndex)
    {
        if (chunkIndex < Volatile.Read(ref _chunkCount))
        {
            return;
        }

        var chunksPtr = (nint*)_chunks.GetUnsafePtr();

        while (true)
        {
            var currentCount = Volatile.Read(ref _chunkCount);
            if (chunkIndex < currentCount)
            {
                return;
            }

            var toAlloc = currentCount;

            if (toAlloc >= _chunks.Count)
            {
                Thread.SpinWait(1);
                continue;
            }

            var sizeInBytes = (nuint)(_chunkCapacity * sizeof(T));
            var data = (nint)_allocationHandle.Alloc(sizeInBytes, MemoryUtility.AlignOf<T>());

            var old = Interlocked.CompareExchange(ref chunksPtr[toAlloc], data, 0);

            if (old == 0)
            {
                Interlocked.Increment(ref _chunkCount);
                if (chunkIndex >= currentCount + 1)
                {
                    continue;
                }

                return;
            }

            _allocationHandle.Free((void*)data);
        }
    }

    private void FreeChunk(int chunkIndex)
    {
        var ptr = (void*)_chunks[chunkIndex];
        if (ptr != null)
        {
            _allocationHandle.Free(ptr);
            _chunks[chunkIndex] = 0;
        }
    }

    private void FreeTrailingEmptyChunks()
    {
        var neededChunks = _count > 0 ? (_count + _chunkCapacity - 1) / _chunkCapacity : 0;
        while (_chunkCount > neededChunks)
        {
            _chunkCount--;
            FreeChunk(_chunkCount);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParallelReader AsParallelReader()
    {
        return new((UnsafeChunkedList<T>*)Unsafe.AsPointer(ref this));
    }

    /// <summary>
    /// Provides a parallel writer for the current list, enabling thread-safe additions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParallelWriter AsParallelWriter()
    {
        return new((UnsafeChunkedList<T>*)Unsafe.AsPointer(ref this));
    }

    /// <summary>
    /// Adds a new element to the end of the list, allocating new chunks as needed.
    /// </summary>
    public void Add(scoped in T value)
    {
        EnsureChunkIndex(_count);
        var (chunkIdx, offset) = SplitIndex(_count, _chunkCapacity);
        ((T*)_chunks[chunkIdx])[offset] = value;
        _count++;
    }

    /// <summary>
    /// Adds the specified value to the collection. For chunked lists, this is equivalent to <see cref="Add"/>,
    /// since allocating new chunks never moves existing elements.
    /// </summary>
    public void AddNoResize(scoped in T value)
    {
        EnsureChunkIndex(_count);
        var (chunkIdx, offset) = SplitIndex(_count, _chunkCapacity);
        ((T*)_chunks[chunkIdx])[offset] = value;
        _count++;
    }

    /// <summary>
    /// Adds a range of elements to the collection, allocating new chunks as needed.
    /// </summary>
    public void AddRange(ReadOnlySpan<T> values)
    {
        if (values.Length == 0)
        {
            return;
        }

        EnsureChunkIndex(_count + values.Length - 1);
        CopyFromSpan(values, _count);
        _count += values.Length;
    }

    /// <summary>
    /// Adds a range of elements from a pointer to the collection, allocating new chunks as needed.
    /// </summary>
    public void AddRange(T* ptr, int count)
    {
        if (count <= 0)
        {
            return;
        }

        EnsureChunkIndex(_count + count - 1);
        CopyFromPtr(ptr, _count, count);
        _count += count;
    }

    /// <summary>
    /// Adds a range of elements. For chunked lists, this is equivalent to <see cref="AddRange(ReadOnlySpan{T})"/>,
    /// since allocating new chunks never moves existing elements.
    /// </summary>
    public void AddRangeNoResize(ReadOnlySpan<T> collection)
    {
        if (collection.Length == 0)
        {
            return;
        }

        EnsureChunkIndex(_count + collection.Length - 1);
        CopyFromSpan(collection, _count);
        _count += collection.Length;
    }

    /// <summary>
    /// Adds a range of elements from a pointer. For chunked lists, this is equivalent to <see cref="AddRange(T*, int)"/>,
    /// since allocating new chunks never moves existing elements.
    /// </summary>
    public void AddRangeNoResize(T* ptr, int count)
    {
        if (count <= 0)
        {
            return;
        }

        EnsureChunkIndex(_count + count - 1);
        CopyFromPtr(ptr, _count, count);
        _count += count;
    }

    private void CopyFromSpan(ReadOnlySpan<T> source, int startIndex)
    {
        fixed (T* pSrc = source)
        {
            CopyFromPtr(pSrc, startIndex, source.Length);
        }
    }

    private void CopyFromPtr(T* srcPtr, int startIndex, int count)
    {
        var remaining = count;
        var src = srcPtr;
        var currentIndex = startIndex;

        while (remaining > 0)
        {
            var (chunkIdx, offset) = SplitIndex(currentIndex, _chunkCapacity);
            var dstPtr = (T*)_chunks[chunkIdx] + offset;
            var copyCount = Math.Min(remaining, _chunkCapacity - offset);
            MemoryUtility.MemCpy(dstPtr, src, (nuint)(copyCount * sizeof(T)));
            src += copyCount;
            currentIndex += copyCount;
            remaining -= copyCount;
        }
    }

    /// <summary>
    /// Removes a range of elements from the list starting at the specified index.
    /// </summary>
    public void RemoveRange(int start, int length)
    {
        CheckIndexCount(start, length);

        if (length <= 0)
        {
            return;
        }

        var copyFrom = Math.Min(start + length, _count);
        var numToMove = _count - copyFrom;

        for (var i = 0; i < numToMove; i++)
        {
            var (srcChunk, srcOffset) = SplitIndex(copyFrom + i, _chunkCapacity);
            var (dstChunk, dstOffset) = SplitIndex(start + i, _chunkCapacity);
            ((T*)_chunks[dstChunk])[dstOffset] = ((T*)_chunks[srcChunk])[srcOffset];
        }

        _count -= length;
        FreeTrailingEmptyChunks();
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        RemoveRange(index, 1);
    }

    /// <summary>
    /// Removes a range of elements by swapping them with elements from the end of the list.
    /// </summary>
    public void RemoveRangeSwapBack(int start, int length)
    {
        CheckIndexCount(start, length);

        if (length <= 0)
        {
            return;
        }

        var numToCopy = Math.Min(length, _count - (start + length));
        var copyFrom = _count - numToCopy;

        for (var i = 0; i < numToCopy; i++)
        {
            var (dstChunk, dstOffset) = SplitIndex(start + i, _chunkCapacity);
            var (srcChunk, srcOffset) = SplitIndex(copyFrom + i, _chunkCapacity);
            ((T*)_chunks[dstChunk])[dstOffset] = ((T*)_chunks[srcChunk])[srcOffset];
        }

        _count -= length;
        FreeTrailingEmptyChunks();
    }

    /// <summary>
    /// Removes the element at the specified index by swapping it with the last element.
    /// </summary>
    public void RemoveAtSwapBack(int index)
    {
        RemoveRangeSwapBack(index, 1);
    }

    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        if (newSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize));
        }

        if (newSize > _count)
        {
            EnsureChunkIndex(newSize - 1);
        }

        _count = newSize;
        FreeTrailingEmptyChunks();
    }

    /// <summary>
    /// Pre-allocates chunks to accommodate at least the specified number of elements.
    /// </summary>
    public void EnsureCapacity(int capacity)
    {
        if (capacity > 0)
        {
            EnsureChunkIndex(capacity - 1);
        }
    }

    public void Clear()
    {
        _count = 0;
        FreeTrailingEmptyChunks();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        ThrowIfNotCreated();

        if (_chunkCount == 1)
        {
            return (void*)_chunks[0];
        }

        throw new InvalidOperationException("Cannot get a single contiguous pointer for a multi-chunk UnsafeChunkedList. Use CopyTo instead.");
    }

    /// <summary>
    /// Copies all elements into a destination span.
    /// </summary>
    public readonly void CopyTo(Span<T> destination)
    {
        var size = Math.Min(destination.Length, Count);
        var remaining = size;
        var elementIndex = 0;

        fixed (T* pDest = destination)
        {
            var dst = pDest;

            while (remaining > 0)
            {
                var (chunkIdx, offset) = SplitIndex(elementIndex, _chunkCapacity);
                var srcPtr = (T*)_chunks[chunkIdx] + offset;
                var copyCount = Math.Min(remaining, _chunkCapacity - offset);
                MemoryUtility.MemCpy(dst, srcPtr, (nuint)(copyCount * sizeof(T)));
                elementIndex += copyCount;
                dst += copyCount;
                remaining -= copyCount;
            }
        }
    }

    /// <summary>
    /// Copies a range of elements from the list to a destination span.
    /// </summary>
    public readonly void CopyTo(Span<T> destination, int sourceIndex, int destinationIndex, int length)
    {
        if (sourceIndex + length > _count || destinationIndex + length > destination.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Source collection or destination span is too small for the specified range.");
        }

        fixed (T* pDest = destination)
        {
            var dst = pDest + destinationIndex;
            var remaining = length;
            var elementIndex = sourceIndex;

            while (remaining > 0)
            {
                var (chunkIdx, offset) = SplitIndex(elementIndex, _chunkCapacity);
                var srcPtr = (T*)_chunks[chunkIdx] + offset;
                var copyCount = Math.Min(remaining, _chunkCapacity - offset);
                MemoryUtility.MemCpy(dst, srcPtr, (nuint)(copyCount * sizeof(T)));
                elementIndex += copyCount;
                dst += copyCount;
                remaining -= copyCount;
            }
        }
    }

    /// <summary>
    /// Copies elements from a source span into the list, growing as needed.
    /// </summary>
    public void CopyFrom(ReadOnlySpan<T> source)
    {
        if (_count < source.Length)
        {
            Resize(source.Length);
        }

        CopyFromSpan(source, 0);
    }

    /// <summary>
    /// Copies a range of elements from a source span to the list.
    /// </summary>
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
            CopyFromPtr(pSrc + sourceIndex, destinationIndex, length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> containing the elements.
    /// </summary>
    public readonly List<T> ToList()
    {
        var list = new List<T>(_count);
        var remaining = _count;
        var elementIndex = 0;

        while (remaining > 0)
        {
            var (chunkIdx, offset) = SplitIndex(elementIndex, _chunkCapacity);
            var chunkSize = Math.Min(remaining, _chunkCapacity - offset);
            var srcPtr = (T*)_chunks[chunkIdx] + offset;
            var span = new ReadOnlySpan<T>(srcPtr, chunkSize);
            list.AddRange(span);
            elementIndex += chunkSize;
            remaining -= chunkSize;
        }

        return list;
    }

    public void Dispose()
    {
        for (var i = 0; i < _chunkCount; i++)
        {
            FreeChunk(i);
        }

        _chunks.Dispose();
        _chunkCount = 0;
        _count = 0;
    }
}
