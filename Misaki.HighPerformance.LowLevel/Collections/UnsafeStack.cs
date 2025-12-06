using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// Provides a high-performance, unsafe stack data structure for unmanaged types, supporting manual memory management
/// and allocation control.
/// </summary>
/// <typeparam name="T">The type of elements stored in the stack. Must be an unmanaged type.</typeparam>
public unsafe struct UnsafeStack<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeStack<T>* _collection;
        private int _index;

        public readonly ref T Current => ref _collection->_array[_index];
        readonly T IEnumerator<T>.Current => Current;
        readonly object IEnumerator.Current => Current;

        public Enumerator(UnsafeStack<T>* collection)
        {
            _collection = collection;
            _index = collection->Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index--;
            return _index >= 0;
        }

        public void Reset()
        {
            _index = _collection->Count;
        }

        public readonly void Dispose()
        {
        }
    }

    private UnsafeArray<T> _array;
    private int _count;

    public readonly int Count => _count;
    public readonly int Capacity => _array.Count;
    public readonly bool IsCreated => _array.IsCreated;

    public Enumerator GetEnumerator() => new((UnsafeStack<T>*)UnsafeUtility.AddressOf(ref this));
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Invalid constructor, use <see cref="UnsafeStack(int, Allocator, AllocationOption)"/> or <see cref="UnsafeStack(int, ref AllocationHandle, AllocationOption)"/> instead.
    /// </summary>
    public UnsafeStack()
        : this(0, Allocator.Invalid)
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeStack class with the specified initial capacity and allocation options.
    /// </summary>
    /// <param name="capacity">The number of elements the stack can initially hold. Must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle used to manage the underlying memory allocation for the stack.</param>
    /// <param name="allocationOption">Specifies additional options for memory allocation. The default is AllocationOption.None.</param>
    public UnsafeStack(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(capacity, handle, allocationOption);
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeStack class with the specified initial capacity, allocator, and
    /// allocation options.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the stack can hold. Must be greater than zero.</param>
    /// <param name="allocator">The allocator to use for memory management of the stack's storage.</param>
    /// <param name="allocationOption">The allocation option that determines how memory is allocated for the stack. The default is AllocationOption.None.</param>
    public UnsafeStack(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Adds an element to the top of the stack.
    /// </summary>
    /// <param name="value">The element to add to the stack.</param>
    public void Push(T value)
    {
        if (_count >= Capacity)
        {
            Resize((int)(Capacity * 1.5f));
        }

        UnsafeUtility.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
        _count++;
    }

    /// <summary>
    /// Removes and returns the object at the top of the stack.
    /// </summary>
    /// <returns>The object removed from the top of the stack.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
    public T Pop()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        _count--;
        return _array[_count];
    }

    /// <summary>
    /// Attempts to remove and return the object at the top of the stack.
    /// </summary>
    /// <param name="value">When this method returns, contains the object removed from the top of the stack, if the operation succeeded;
    /// otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns>true if an object was successfully removed and returned from the stack; otherwise, false.</returns>
    public bool TryPop(out T value)
    {
        if (_count == 0)
        {
            value = default;
            return false;
        }

        _count--;
        value = _array[_count];
        return true;
    }

    /// <summary>
    /// Returns the item at the top of the stack without removing it.
    /// </summary>
    /// <returns>The item of type <typeparamref name="T"/> at the top of the stack.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
    public readonly T Peek()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        return _array[_count - 1];
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
