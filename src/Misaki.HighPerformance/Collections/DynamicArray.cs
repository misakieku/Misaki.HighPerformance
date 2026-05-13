using System.Collections;

namespace Misaki.HighPerformance.Collections;

/// <summary>
/// A dynamically sized array that grows as elements are added and exposes span access to the active range.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the dynamic array.</typeparam>
public class DynamicArray<T> : IEnumerable<T>, IList<T>
{
    private T[] _array;
    private int _count;

    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    public ref T this[int index] => ref _array[index];
    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    public ref T this[uint index] => ref _array[index];

    /// <summary>
    /// Gets the number of elements currently stored in the array.
    /// </summary>
    public int Count => _count;
    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the allocated capacity of the underlying storage.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of <see cref="DynamicArray{T}"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial size of the backing array.</param>
    public DynamicArray(int initialCapacity = 4)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative.");
        }

        _array = new T[initialCapacity];
        _count = 0;
    }

    /// <summary>
    /// Returns an enumerator that iterates over the stored elements.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
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

    /// <summary>
    /// Adds an element to the end of the array.
    /// </summary>
    /// <param name="item">The element to add.</param>
    public void Add(T item)
    {
        EnsureCapacity(_count + 1);
        _array[_count++] = item;
    }

    /// <summary>
    /// Inserts an element at the specified index.
    /// </summary>
    /// <param name="index">The position at which to insert the element.</param>
    /// <param name="item">The element to insert.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the valid range.</exception>
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

    /// <summary>
    /// Removes all elements from the array.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_array, 0, _count);
        _count = 0;
    }

    /// <summary>
    /// Determines whether the specified item exists in the array.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>True if the item is found; otherwise, false.</returns>
    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    /// <summary>
    /// Searches for the specified item and returns its zero-based index.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index of the item if found; otherwise, -1.</returns>
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

    /// <summary>
    /// Copies the active elements to the specified array starting at the given index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The index in the destination array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_array, 0, array, arrayIndex, _count);
    }

    /// <summary>
    /// Removes the first occurrence of the specified item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
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

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the valid range.</exception>
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

    /// <summary>
    /// Returns a span over the active portion of the array.
    /// </summary>
    /// <returns>A span containing the stored elements.</returns>
    public Span<T> AsSpan()
    {
        return _array.AsSpan(0, _count);
    }
}