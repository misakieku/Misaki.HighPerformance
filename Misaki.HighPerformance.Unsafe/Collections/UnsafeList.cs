using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Unsafe.Collections;

public unsafe struct UnsafeList<T> : IUnsafeCollection<T>, IEnumerable<T> where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
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
            UnsafeUtilities.WriteArrayElement(listData->_buffer, idx, value);
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
            MemCpy(listData->_buffer + idx, ptr, (uint)(count * sizeof(T)));
        }
    }

    private T* _buffer;

    private int _size;
    private int _capacity;

    public readonly T* Buffer => _buffer;
    public readonly int Size => _size;
    public readonly int Capacity => _capacity;

    public readonly ref T this[int index] => ref UnsafeUtilities.ReadArrayElementRef<T>(_buffer, index);

    public IEnumerator<T> GetEnumerator() => new Enumerator(ref this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ParallelWriter AsParallelWriter() => new((UnsafeList<T>*)UnsafeUtilities.AddressOf(ref this));

    public UnsafeList(int capacity, AllocationType allocationType)
    {
        _buffer = (T*)NativeMemory.AlignedAlloc((nuint)(capacity * sizeof(T)), (nuint)AlignOf<T>());
        _size = 0;
        _capacity = capacity;

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
        if (index + count > _capacity)
        {
            throw new Exception($"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Size {Size}), requested count {count}!");
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
        if (_size >= _capacity)
        {
            ReAlloc(_capacity + (int)(_capacity * 0.5f));
        }

        UnsafeUtilities.WriteArrayElement(_buffer, _size, value);
        _size++;
    }

    public void AddNoResize(T value)
    {
        CheckNoResizeCapacity(1);

        UnsafeUtilities.WriteArrayElement(_buffer, _size, value);
        _size++;
    }

    public void AddRange(Span<T> values, int count)
    {
        var newSize = _size + count;
        if (newSize > _capacity)
        {
            ReAlloc(_capacity + count);
        }

        fixed (T* ptr = values)
        {
            MemCpy(_buffer + _size, ptr, (uint)(count * sizeof(T)));
        }

        _size += count;
    }

    public void AddRangeNoResize(ReadOnlySpan<T> values)
    {
        CheckNoResizeCapacity(values.Length);

        fixed (T* ptr = values)
        {
            MemCpy(_buffer + _size, ptr, (uint)(values.Length * sizeof(T)));
        }

        _size += values.Length;
    }

    public void AddRangeNoResize(T* ptr, int count)
    {
        CheckNoResizeCapacity(count);

        MemCpy(_buffer + _size, ptr, (uint)(count * sizeof(T)));
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
        MemCpy(_buffer + start, _buffer + copyFrom, (uint)((_size - copyFrom) * sizeof(T)));
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
        MemCpy(_buffer + start, _buffer + copyFrom, (uint)((_size - copyFrom) * sizeof(T)));
        _size -= length;
    }

    public void RemoveAtSwapBack(int index)
    {
        RemoveRangeSwapBack(index, 1);
    }

    public void ReAlloc(int newSize)
    {
        if (newSize == _size)
        {
            return;
        }

        _buffer = (T*)NativeMemory.AlignedRealloc(_buffer, (nuint)(newSize * sizeof(T)), (nuint)AlignOf<T>());
        _size = newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        MemClear(_buffer, (uint)(_size * sizeof(T)));
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(_buffer);

        _buffer = null;
        _size = 0;
    }
}