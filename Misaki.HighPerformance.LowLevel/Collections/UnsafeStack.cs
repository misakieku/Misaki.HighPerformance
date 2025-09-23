using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
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
    private UnsafeArray<T> _array;
    private int _count;

    public readonly int Count => _count;
    public readonly bool IsCreated => _array.IsCreated;

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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
    /// <param name="initialCapacity">The number of elements the stack can initially hold. Must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle used to manage the underlying memory allocation for the stack.</param>
    /// <param name="allocationOption">Specifies additional options for memory allocation. The default is AllocationOption.None.</param>
    public UnsafeStack(int initialCapacity, ref AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(initialCapacity, ref handle, allocationOption);
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeStack class with the specified initial capacity, allocator, and
    /// allocation options.
    /// </summary>
    /// <param name="initialCapacity">The initial number of elements that the stack can hold. Must be greater than zero.</param>
    /// <param name="allocator">The allocator to use for memory management of the stack's storage.</param>
    /// <param name="allocationOption">The allocation option that determines how memory is allocated for the stack. The default is AllocationOption.None.</param>
    public UnsafeStack(int initialCapacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(initialCapacity, ref AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Adds an element to the top of the stack.
    /// </summary>
    /// <param name="value">The element to add to the stack.</param>
    public void Push(T value)
    {
        if (_count >= _array.Count)
        {
            Resize(_array.Count + (int)(_array.Count * 0.5f));
        }

        UnsafeUtilities.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
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