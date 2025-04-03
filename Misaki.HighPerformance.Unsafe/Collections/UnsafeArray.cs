using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Collections.Services;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Unsafe.Collections;

/// <summary>
/// A structure for managing an array of unmanaged types with unsafe memory operations.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in an unmanaged memory context.</typeparam>
public unsafe struct UnsafeArray<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    private struct Enumerator : IEnumerator<T>
    {
        private UnsafeArray<T>* _collection;
        private int _index;
        private T _value;

        public Enumerator(UnsafeArray<T>* collection)
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
                _value = UnsafeUtilities.ReadArrayElement<T>(_collection->_buffer, _index);
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
            get => _value;
        }

        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }

        public void Dispose()
        {
        }
    }

    private T* _buffer;
    private int _count;

    public readonly int Count => _count;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref UnsafeUtilities.ReadArrayElementRef<T>(_buffer, index);
    }

    public readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer != null;
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator((UnsafeArray<T>*)UnsafeUtilities.AddressOf(ref this));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type. It
    /// allocates memory and optionally clears it.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocationType">Determines how the allocated memory should be initialized, either uninitialized or cleared.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnsafeArray(int count, Allocator allocator, AllocationType allocationType = AllocationType.UnInitialized)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        _buffer = AllocationManager.Allocate<T>((uint)count, (uint)AlignOf<T>(), allocator, allocationType);
        _count = count;

        if (allocationType == AllocationType.Clear)
        {
            Clear();
        }
    }

    /// <summary>
    /// Initializes an UnsafeArray with a pointer to a buffer and a count of elements. The count is adjusted based on
    /// the size of the type T.
    /// </summary>
    /// <param name="buffer">A pointer to the memory location that holds the elements of the array.</param>
    /// <param name="count">The total size of the data in bytes, which is divided by the size of type T to determine the number of elements.</param>
    public UnsafeArray(void* buffer, int count)
    {
        _buffer = (T*)buffer;
        _count = count;
    }

    public void Resize(int newSize)
    {
        if (newSize == _count)
        {
            return;
        }

        _buffer = (T*)AlignedRealloc(_buffer, (nuint)(newSize * sizeof(T)), AlignOf<T>());
        _count = newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        MemClear(_buffer, (uint)(_count * sizeof(T)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _buffer;
    }

    public void Dispose()
    {
        AlignedFree(_buffer);

        _buffer = null;
        _count = 0;
    }
}

