using System.Diagnostics.CodeAnalysis;
using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;

namespace Misaki.HighPerformance.Unsafe.Collections;

public unsafe struct UnsafeQueue<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    private UnsafeArray<T> _array;
    private int _size;
    private int _offset;

    public T* Buffer => _array.Buffer;
    public int Size => _size;
    public int Capacity => _array.Size;

    public T this[int index] => _array[index];

    public void Enqueue(T value)
    {
        if (_size >= Capacity)
        {
            ReAlloc(Capacity + (int)(Capacity * 0.5f));
        }

        UnsafeUtilities.WriteArrayElement(Buffer, (_offset + _size) % Capacity, value);
        _size++;
    }

    public T Dequeue()
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        var value = UnsafeUtilities.ReadArrayElement<T>(Buffer, _offset);
        _offset = (_offset + 1) % Capacity;
        _size--;

        return value;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T? value)
    {
        if (_offset > _size)
        {
            value = null;
            return false;
        }

        value = Dequeue();
        return true;
    }

    public void ReAlloc(int newSize)
    {
        _array.ReAlloc(newSize);
    }

    public void Clear()
    {
        _array.Clear();
    }

    public void Dispose()
    {
        _array.Dispose();
    }
}
