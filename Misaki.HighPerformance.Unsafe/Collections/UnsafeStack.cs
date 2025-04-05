using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Unsafe.Collections;

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

    public UnsafeStack() : this(1, Allocator.Persistent)
    {
    }

    public UnsafeStack(int initialSize, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
    {
        _array = new UnsafeArray<T>(initialSize, allocator, allocationOption);
    }

    public void Push(T value)
    {
        if (_count >= _array.Count)
        {
            Resize(_array.Count + (int)(_array.Count * 0.5f));
        }

        UnsafeUtilities.WriteArrayElement(_array.GetUnsafePtr(), _count, value);
        _count++;
    }

    public T Pop()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        _count--;
        return _array[_count];
    }

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