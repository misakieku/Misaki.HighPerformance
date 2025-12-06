using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A structure that implements a queue using unmanaged types for efficient memory management.
/// </summary>
/// <typeparam name="T">Represents the type of elements stored in the queue, which must be an unmanaged type for performance and safety.</typeparam>
public unsafe struct UnsafeQueue<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeQueue<T>* _collection;
        private int _currentIndex;

        // We assume _currentIndex will always be in range when accessed.
        public readonly ref T Current => ref _collection->_array[(_collection->_offset + _currentIndex) % _collection->Capacity];
        readonly T IEnumerator<T>.Current => Current;
        readonly object IEnumerator.Current => Current;

        public Enumerator(UnsafeQueue<T>* collection)
        {
            _collection = collection;
            _currentIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _currentIndex++;
            return _currentIndex < _collection->_count;
        }

        public void Reset()
        {
            _currentIndex = -1;
        }

        public readonly void Dispose()
        {
        }
    }

    private UnsafeArray<T> _array;
    private int _count;
    private int _offset;

    public readonly int Count => _count;
    public readonly int Capacity => _array.Count;
    public readonly bool IsCreated => _array.IsCreated;

    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _array[index] = value;
    }

    public Enumerator GetEnumerator() => new((UnsafeQueue<T>*)UnsafeUtility.AddressOf(ref this));
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Invalid constructor. Use <see cref="UnsafeQueue(int, Allocator, AllocationOption)"/> or <see cref="UnsafeQueue(int, ref AllocationHandle, AllocationOption)"/> instead."/>
    /// </summary>
    public UnsafeQueue()
        : this(0, Allocator.Invalid)
    {
    }

    public UnsafeQueue(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(capacity, handle, allocationOption);
        _count = 0;
        _offset = 0;
    }

    public UnsafeQueue(int capacity, Allocator allocator, AllocationOption allocationType = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationType)
    {
    }

    /// <summary>
    /// Returns a reference to the item at the front of the queue without removing it.
    /// </summary>
    /// <returns>A reference to the item at the front of the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
    public readonly ref T Peek()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return ref UnsafeUtility.ReadArrayElementRef<T>(_array.GetUnsafePtr(), _offset);
    }

    /// <summary>
    /// Adds an element to the end of a collection, resizing if the current capacity is reached. The new element is
    /// stored in a circular buffer.
    /// </summary>
    /// <param name="value">The item to be added to the collection.</param>
    public void Enqueue(T value)
    {
        if (_count >= Capacity)
        {
            Resize((int)(Capacity * 1.5f));
        }

        UnsafeUtility.WriteArrayElement(_array.GetUnsafePtr(), (_offset + _count) % Capacity, value);
        _count++;
    }

    /// <summary>
    /// Removes and returns the element at the front of the queue. If the queue is empty, an exception is thrown.
    /// </summary>
    /// <returns>The element that was removed from the front of the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to dequeue from an empty queue.</exception>
    public T Dequeue()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        var value = UnsafeUtility.ReadArrayElement<T>(_array.GetUnsafePtr(), _offset);
        _offset = (_offset + 1) % Capacity;
        _count--;

        return value;
    }

    /// <summary>
    /// Attempts to remove and return an item from a collection. Returns a boolean indicating success or failure.
    /// </summary>
    /// <param name="value">The output variable that will hold the dequeued item if the operation is successful.</param>
    /// <returns>True if an item was successfully dequeued, otherwise false.</returns>
    public bool TryDequeue(out T value)
    {
        if (_count == 0)
        {
            value = default;
            return false;
        }

        value = Dequeue();
        return true;
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
        _offset = 0;
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
        _offset = 0;
    }
}
