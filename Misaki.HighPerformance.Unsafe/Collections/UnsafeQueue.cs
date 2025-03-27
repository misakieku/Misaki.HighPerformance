using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Unsafe.Collections;

public unsafe struct UnsafeQueue<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    private UnsafeArray<T> _array;
    private int _size;
    private int _offset;

    public readonly T* Buffer => _array.Buffer;
    public readonly int Size => _size;
    public readonly int Capacity => _array.Size;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _array[index];
    }

    public UnsafeQueue(int capacity, AllocationType allocationType)
    {
        _array = new UnsafeArray<T>(capacity, allocationType);
        _size = 0;
        _offset = 0;

        if (allocationType == AllocationType.Clear)
        {
            Clear();
        }
    }

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

    public bool TryDequeue([MaybeNullWhen(false)] out T value)
    {
        if (_size == 0)
        {
            value = default;
            return false;
        }

        value = Dequeue();
        return true;
    }

    public void ReAlloc(int newSize)
    {
        _array.ReAlloc(newSize);

        if (_size > newSize)
        {
            _size = newSize;
        }
    }

    public void Clear()
    {
        _array.Clear();
        _size = 0;
        _offset = 0;
    }

    public void Dispose()
    {
        _array.Dispose();
        _size = 0;
        _offset = 0;
    }
}
