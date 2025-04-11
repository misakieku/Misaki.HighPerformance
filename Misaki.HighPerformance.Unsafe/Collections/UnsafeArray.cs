using Misaki.HighPerformance.Unsafe.Buffer;
using Misaki.HighPerformance.Unsafe.Collections.Contracts;
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
    public struct Enumerator : IEnumerator<T>
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

    private readonly Allocator _allocator;

    public readonly int Count => _count;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _buffer[index];
    }

    public readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer != null;
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator((UnsafeArray<T>*)UnsafeUtilities.AddressOf(ref this));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Constructs an UnsafeArray with a default size of 1 and uses the Persistent allocator.
    /// </summary>
    public UnsafeArray() : this(1, Allocator.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnsafeArray(int count, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        _buffer = AllocationManager.Allocate<T>((uint)count, (uint)AlignOf<T>(), allocator, allocationOption);
        _count = count;
        _allocator = allocator;
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
    public UnsafeArray(void* buffer, int count)
    {
        _buffer = (T*)buffer;
        _count = count;
        _allocator = Allocator.External;
    }

    /// <inheritdoc/>
    public void Resize(int newSize)
    {
        if (newSize == _count)
        {
            return;
        }

        _buffer = AllocationManager.Realloc(_buffer, (uint)newSize, (uint)AlignOf<T>(), _allocator);
        _count = newSize;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        MemClear(_buffer, (nuint)(_count * sizeof(T)));
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

        AllocationManager.Free(_buffer, _allocator);

        _buffer = null;
        _count = 0;
    }
}

