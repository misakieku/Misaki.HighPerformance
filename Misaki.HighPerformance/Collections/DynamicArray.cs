using System.Collections;

namespace Misaki.HighPerformance.Collections;

public class DynamicArray<T> : IEnumerable<T>, IList<T>
{
    private T[] _array;
    private int _count;

    public ref T this[int index] => ref _array[index];
    public ref T this[uint index] => ref _array[index];

    public int Count => _count;
    public bool IsReadOnly => false;

    public int Capacity
    {
        get => _array.Length;
        set
        {
            if (value < _count)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be set to a value less than Count.");
            }

            if (value != _array.Length)
            {
                Array.Resize(ref _array, value);
            }
        }
    }

    T IList<T>.this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    public DynamicArray(int initialCapacity = 4)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative.");
        }

        _array = new T[initialCapacity];
        _count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return _array[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void EnsureCapacity(int min)
    {
        if (_array.Length < min)
        {
            var newCapacity = _array.Length == 0 ? 4 : _array.Length * 2;
            if (newCapacity < min)
                newCapacity = min;
            Array.Resize(ref _array, newCapacity);
        }
    }

    public void Add(T item)
    {
        EnsureCapacity(_count + 1);
        _array[_count++] = item;
    }

    public void Insert(int index, T item)
    {
        if (index < 0 || index > _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        EnsureCapacity(_count + 1);
        Array.Copy(_array, index, _array, index + 1, _count - index);

        _array[index] = item;
        _count++;
    }

    public void Clear()
    {
        Array.Clear(_array, 0, _count);
        _count = 0;
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public int IndexOf(T item)
    {
        for (var i = 0; i < _count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_array[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_array, 0, array, arrayIndex, _count);
    }

    public bool Remove(T item)
    {
        for (var i = 0; i < _count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_array[i], item))
            {
                _count--;
                Array.Copy(_array, i + 1, _array, i, _count - i);
                _array[_count] = default!;
                return true;
            }
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        Array.Copy(_array, index + 1, _array, index, _count - index);

        _count--;
        _array[_count] = default!;
    }

    public Span<T> AsSpan()
    {
        return _array.AsSpan(0, _count);
    }
}