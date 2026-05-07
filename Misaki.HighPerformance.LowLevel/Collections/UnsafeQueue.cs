using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A structure that implements a queue using unmanaged types for efficient memory management.
/// </summary>
/// <typeparam name="T">Represents the type of elements stored in the queue, which must be an unmanaged type for performance and safety.</typeparam>
public unsafe struct UnsafeQueue<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public ref struct Enumerator
    {
        private readonly ref UnsafeQueue<T> _collection;
        private int _currentIndex;

        // We assume _currentIndex will always be in range when accessed.
        public readonly ref T Current => ref _collection._array[(_collection._offset + _currentIndex) % _collection.Capacity];

        public Enumerator(ref UnsafeQueue<T> collection)
        {
            _collection = ref collection;
            _currentIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _currentIndex++;
            return _currentIndex < _collection._count;
        }

        public void Reset()
        {
            _currentIndex = -1;
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

    /// <summary>
    /// Initializes a new instance of UnsafeQueue with a default size of 1 and a persistent allocation handle.
    /// </summary>
    public UnsafeQueue()
        : this(1, AllocationHandle.Persistent)
    {
    }

    public UnsafeQueue(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(capacity, handle, allocationOption);
        _count = 0;
        _offset = 0;
    }

    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeQueue(int capacity, Allocator allocator, AllocationOption allocationType = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationType)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref this);
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
    /// Attempts to return the object at the top of the collection without removing it.
    /// </summary>
    /// <param name="value">The item at the front of the queue if the operation is successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if an object was returned successfully; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryPeek(out T value)
    {
        if (_count == 0)
        {
            value = default;
            return false;
        }

        value = _array[_offset];
        return true;
    }

    /// <summary>
    /// Adds an element to the end of a collection, resizing if the current capacity is reached. The new element is
    /// stored in a circular buffer.
    /// </summary>
    /// <param name="value">The item to be added to the collection.</param>
    public void Enqueue(scoped in T value)
    {
        if (_count >= Capacity)
        {
            Resize(Math.Max(1, Capacity * 2));
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
        if (newSize < _count)
        {
            newSize = _count;
        }

        var newArray = new UnsafeArray<T>(newSize, _array.AllocationHandle, option);

        if (_count > 0)
        {
            if (_offset + _count <= Capacity)
            {
                // No wrap-around, single copy
                var sizeToCopy = (uint)(_count * sizeof(T));
                MemoryUtility.MemCpy(newArray.GetUnsafePtr(), (byte*)_array.GetUnsafePtr() + _offset * sizeof(T), sizeToCopy);
            }
            else
            {
                // Wrap-around, two copies required
                var firstPartElements = Capacity - _offset;
                var secondPartElements = _count - firstPartElements;

                // Copy from _offset to the end of the old array
                MemoryUtility.MemCpy(newArray.GetUnsafePtr(), (byte*)_array.GetUnsafePtr() + _offset * sizeof(T), (uint)(firstPartElements * sizeof(T)));

                // Copy from the start of the old array to the remaining count
                MemoryUtility.MemCpy((byte*)newArray.GetUnsafePtr() + firstPartElements * sizeof(T), _array.GetUnsafePtr(), (uint)(secondPartElements * sizeof(T)));
            }
        }

        _array.Dispose();
        _array = newArray;

        _offset = 0;
    }

    public void Clear()
    {
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
