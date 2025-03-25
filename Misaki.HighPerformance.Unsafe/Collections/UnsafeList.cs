using System.Collections;
using System.Runtime.CompilerServices;
using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;

namespace Misaki.HighPerformance.Unsafe.Collections;

public unsafe struct UnsafeList<T> : IUnsafeCollection<T>, IEnumerable<T>
    where T : unmanaged
{
    private struct Enumerator : IEnumerator<T>
    {
        private UnsafeList<T> _collection;
        private int _index;
        private T _value;

        public Enumerator(ref UnsafeList<T> collection)
        {
            _collection = collection;
            _index = -1;
            _value = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            if (_index < _collection.Size)
            {
                _value = UnsafeUtilities.ReadArrayElement<T>(_collection.Buffer, _index);
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
            get { return _value; }
        }

        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Current; }
        }

        public readonly void Dispose() { }
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

        internal unsafe ParallelWriter(UnsafeList<T>* list)
        {
            listData = list;
        }

        /// <summary>
        /// Adds a value to a collection without resizing it, ensuring capacity is checked before insertion.
        /// </summary>
        /// <param name="value">The value to be added to the collection.</param>
        public void AddNoResize(T value)
        {
            var idx = Interlocked.Increment(ref listData->_size) - 1;
            listData->CheckNoResizeCapacity(idx, 1);
            UnsafeUtilities.WriteArrayElement(listData->Buffer, idx, value);
        }

        /// <summary>
        /// Adds a specified number of elements from a pointer to a buffer without resizing the underlying storage.
        /// </summary>
        /// <param name="ptr">Points to the source data to be copied into the buffer.</param>
        /// <param name="count">Indicates the number of elements to be added from the source data.</param>
        public void AddRangeNoResize(T* ptr, int count)
        {
            var idx = Interlocked.Add(ref listData->_size, count) - count;
            listData->CheckNoResizeCapacity(idx, count);
            MemCpy(listData->Buffer + idx, ptr, (uint)(count * sizeof(T)));
        }
    }

    private UnsafeArray<T> _array;

    private int _size;

    public readonly T* Buffer => _array.Buffer;
    public readonly int Size => _size;
    public readonly int Capacity => _array.Size;

    public readonly T this[int index] => _array[index];

    public IEnumerator<T> GetEnumerator() => new Enumerator(ref this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ParallelWriter AsParallelWriter() =>
        new((UnsafeList<T>*)UnsafeUtilities.AddressOf(ref this));

    public UnsafeList(int capacity, AllocationType allocationType)
    {
        _array = new UnsafeArray<T>(capacity, allocationType);
        _size = 0;

        if (allocationType == AllocationType.Clear)
        {
            Clear();
        }
    }

    private readonly void CheckNoResizeCapacity(int count)
    {
        CheckNoResizeCapacity(count, count);
    }

    private readonly void CheckNoResizeCapacity(int index, int count)
    {
        if (index + count > Capacity)
        {
            throw new Exception(
                $"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Size {Size}), requested count {count}!"
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

        if (index > Size)
        {
            throw new ArgumentOutOfRangeException($"Value for index {index} is out of bounds.");
        }

        if (index + count > Size)
        {
            throw new ArgumentOutOfRangeException($"Value for count {count} is out of bounds.");
        }
    }

    public void Add(T value)
    {
        if (_size >= Capacity)
        {
            ReAlloc(Capacity + (int)(Capacity * 0.5f));
        }

        UnsafeUtilities.WriteArrayElement(Buffer, _size, value);
        _size++;
    }

    public void AddNoResize(T value)
    {
        CheckNoResizeCapacity(1);

        UnsafeUtilities.WriteArrayElement(Buffer, _size, value);
        _size++;
    }

    public void AddRange(Span<T> values, int count)
    {
        var newSize = _size + count;
        if (newSize > Capacity)
        {
            ReAlloc(Capacity + count);
        }

        fixed (T* ptr = values)
        {
            MemCpy(_array.Buffer + _size, ptr, (uint)(count * sizeof(T)));
        }

        _size += count;
    }

    public void AddRangeNoResize(ReadOnlySpan<T> values)
    {
        CheckNoResizeCapacity(values.Length);

        fixed (T* ptr = values)
        {
            MemCpy(_array.Buffer + _size, ptr, (uint)(values.Length * sizeof(T)));
        }

        _size += values.Length;
    }

    public void AddRangeNoResize(T* ptr, int count)
    {
        CheckNoResizeCapacity(count);

        MemCpy(_array.Buffer + _size, ptr, (uint)(count * sizeof(T)));
        _size += count;
    }

    public void RemoveRange(int start, int length)
    {
        CheckIndexCount(start, length);

        if (length <= 0)
        {
            return;
        }

        var copyFrom = Math.Min(start + length, _size);
        MemCpy(
            _array.Buffer + start,
            _array.Buffer + copyFrom,
            (uint)((_size - copyFrom) * sizeof(T))
        );
        _size -= length;
    }

    public void RemoveAt(int index)
    {
        RemoveRange(index, 1);
    }

    public void RemoveRangeSwapBack(int start, int length)
    {
        CheckIndexCount(start, length);

        if (length <= 0)
        {
            return;
        }

        var copyFrom = Math.Min(_size - length, start + length);
        MemCpy(
            _array.Buffer + start,
            _array.Buffer + copyFrom,
            (uint)((_size - copyFrom) * sizeof(T))
        );
        _size -= length;
    }

    public void RemoveAtSwapBack(int index)
    {
        RemoveRangeSwapBack(index, 1);
    }

    public void ReAlloc(int newSize)
    {
        _array.ReAlloc(newSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        _array.Clear();
    }

    public void Dispose()
    {
        _array.Dispose();
        _size = 0;
    }
}

