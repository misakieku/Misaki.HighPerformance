using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Helpers;
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
        private UnsafeQueue<T>* _collection;
        private int _index;
        private T _value;

        public Enumerator(UnsafeQueue<T>* collection)
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
                _value = UnsafeUtilities.ReadArrayElement<T>(_collection->_array.GetUnsafePtr(), _index);
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

    public IEnumerator<T> GetEnumerator() => new Enumerator((UnsafeQueue<T>*)UnsafeUtilities.AddressOf(ref this));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public UnsafeQueue() : this(1, Allocator.Persistent)
    {
    }

    public UnsafeQueue(int capacity, Allocator allocator, AllocationOption allocationType = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(capacity, allocator, allocationType);
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
            Resize(Capacity + (int)(Capacity * 0.5f));
        }

        UnsafeUtilities.WriteArrayElement(_array.GetUnsafePtr(), (_offset + _count) % Capacity, value);
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

        var value = UnsafeUtilities.ReadArrayElement<T>(_array.GetUnsafePtr(), _offset);
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

    public void Resize(int newSize)
    {
        _array.Resize(newSize);

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
