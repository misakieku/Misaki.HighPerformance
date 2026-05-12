using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

/// <summary>
/// A sparse set data structure that provides O(1) insertion, deletion, and lookup operations.
/// The sparse set uses three arrays: a dense array for storing values, a sparse array for mapping indices,
/// and a reverse array for mapping dense indices back to sparse indices.
/// Sparse indices work like entity IDs and are automatically generated.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the sparse set.</typeparam>
public class SparseSet<T> : IEnumerable<T>
    where T : notnull
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly SparseSet<T> _collection;
        private int _currentIndex;

        public readonly ref T Current => ref _collection._dense[_currentIndex];
        readonly T IEnumerator<T>.Current => Current;
        readonly object IEnumerator.Current => Current;

        public Enumerator(SparseSet<T> collection)
        {
            _collection = collection;
            _currentIndex = 0;
        }

        public bool MoveNext()
        {
            _currentIndex++;
            return _currentIndex < _collection._count;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }

        public readonly void Dispose()
        {
        }
    }

    private T[] _dense;
    private int[] _generations;
    private int[] _sparse;
    private int[] _reverse;
    private readonly Stack<int> _freeSparse;

    private int _count;
    private int _nextId; // Next available sparse index
    private int _capacity;

    public int Count => _count;
    public int Capacity => _capacity;

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Initializes a new instance of SparseSet with a specified capacity and an allocation handle.
    /// </summary>
    /// <param name="capacity">Specifies the initial capacity of the sparse set, which must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified capacity is less than or equal to zero.</exception>
    public SparseSet(int capacity = 4)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        _dense = new T[capacity];
        _generations = new int[capacity];
        _sparse = new int[capacity];
        _reverse = new int[capacity];
        _freeSparse = new Stack<int>(capacity);

        _count = 0;
        _nextId = 0;
        _capacity = capacity;

        _generations.AsSpan().Fill(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref T GetDenseReferenceUnchecked(int sparseIndex)
    {
        return ref _dense[_sparse[sparseIndex]];
    }

    /// <summary>
    /// Adds a value to the sparse set and returns a unique sparse index for the value.
    /// </summary>
    /// <param name="value">The value to add to the sparse set.</param>
    /// <param name="generation">Outputs the generation number associated with the added value.</param>
    /// <returns>A unique sparse index that can be used to reference this value.</returns>
    public int Add(T value, out int generation)
    {
        if (!_freeSparse.TryPop(out var sparseIndex))
        {
            // Use the next available ID
            sparseIndex = _nextId++;

            // Resize sparse array if necessary
            if (sparseIndex >= _sparse.Length)
            {
                ResizeSparse(sparseIndex + 1);
            }
        }

        // Resize dense arrays if necessary
        if (_count >= _capacity)
        {
            Resize((int)(_capacity * 1.5f));
        }

        // Add the value to the dense array and update mappings
        _dense[_count] = value;

        _sparse[sparseIndex] = _count;
        _reverse[_count] = sparseIndex;
        _count++;

        generation = _generations[sparseIndex];
        return sparseIndex;
    }

    /// <summary>
    /// Removes the value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index of the value to remove.</param>
    /// <param name="generation">The generation number associated with the sparse index to validate.</param>
    /// <returns>True if the value was removed, false if the sparse index was not found.</returns>
    public bool Remove(int sparseIndex, int generation)
    {
        if (!Contains(sparseIndex, generation))
        {
            return false;
        }

        var denseIndex = _sparse[sparseIndex];
        var lastIndex = _count - 1;

        if (denseIndex != lastIndex)
        {
            // Move the last element to the position of the removed element
            var lastValue = _dense[lastIndex];
            var lastSparseIndex = _reverse[lastIndex]; // Get sparse index of last element

            _dense[denseIndex] = lastValue;
            _reverse[denseIndex] = lastSparseIndex;

            // Update the sparse mapping for the moved element
            _sparse[lastSparseIndex] = denseIndex;
        }

        // Mark the sparse index as unused and add to free list
        _sparse[sparseIndex] = -1;
        _generations[sparseIndex]++; // Increment generation to invalidate old references

        _freeSparse.Push(sparseIndex);
        _count--;

        return true;
    }

    /// <summary>
    /// Checks if the sparse set contains a value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to check.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <returns>True if the sparse index is valid and contains a value, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int sparseIndex, int generation)
    {
        if (sparseIndex < 0 || sparseIndex >= _sparse.Length)
        {
            return false;
        }

        var denseIndex = _sparse[sparseIndex];
        return denseIndex >= 0 && denseIndex < _count && _generations[denseIndex] == generation;
    }

    /// <summary>
    /// Gets the value at the specified sparse index and generation.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <param name="value">When this method returns, contains the value at the specified sparse index, if found.</param>
    /// <returns>True if the sparse index contains a value, false otherwise.</returns>
    public bool TryGetValue(int sparseIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        if (Contains(sparseIndex, generation))
        {
            value = GetDenseReferenceUnchecked(sparseIndex);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the value at the specified sparse index and generation.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <returns>The value at the specified sparse index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the sparse index is not found.</exception>
    public T GetValue(int sparseIndex, int generation)
    {
        if (!Contains(sparseIndex, generation))
        {
            throw new ArgumentOutOfRangeException(nameof(sparseIndex), "Sparse index and feneration not found in the set.");
        }

        return GetDenseReferenceUnchecked(sparseIndex);
    }

    /// <summary>
    /// Gets reference of the value at the specified sparse index and generation.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <param name="exist">Outputs whether the sparse index exists in the set.</param>
    /// <returns>Reference of the value at the specified sparse index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the sparse index is not found.</exception>
    public ref T GetValueReference(int sparseIndex, int generation, out bool exist)
    {
        if (!Contains(sparseIndex, generation))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        exist = true;
        return ref GetDenseReferenceUnchecked(sparseIndex);
    }

    /// <summary>
    /// Updates the value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index of the value to update.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <param name="value">The new value.</param>
    /// <returns>True if the value was updated, false if the sparse index was not found.</returns>
    public bool SetValue(int sparseIndex, int generation, T value)
    {
        if (!Contains(sparseIndex, generation))
        {
            return false;
        }

        GetDenseReferenceUnchecked(sparseIndex) = value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResizeSparse(int newSize)
    {
        var oldSize = _sparse.Length;
        Array.Resize(ref _sparse, newSize);
        _sparse.AsSpan()[oldSize..newSize].Fill(-1);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _sparse.AsSpan().Fill(-1);

        _count = 0;
        _nextId = 0;
    }

    /// <inheritdoc/>
    public void Resize(int newSize)
    {
        if (newSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), "New size must be greater than zero.");
        }

        Array.Resize(ref _dense, newSize);
        Array.Resize(ref _generations, newSize);
        Array.Resize(ref _reverse, newSize);

        if (newSize > _sparse.Length)
        {
            ResizeSparse(newSize);
        }

        _generations.AsSpan(_capacity).Fill(1);
        _capacity = newSize;
    }
}
