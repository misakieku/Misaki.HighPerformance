using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

/// <summary>
/// A slot-based collection that stores values in reusable positions and validates access by generation.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the slot map.</typeparam>
public class SlotMap<T> : IEnumerable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly SlotMap<T> _slotMap;
        private int _currentIndex;

        public Enumerator(SlotMap<T> slotMap)
        {
            _slotMap = slotMap;
            _currentIndex = 0;
        }

        public readonly T Current => _slotMap._data[_currentIndex];
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (++_currentIndex < _slotMap._capacity)
            {
                if (_slotMap._isOccupiedBits[_currentIndex])
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset() => _currentIndex = 0;

        public void Dispose()
        {
        }
    }

    private T[] _data;
    private int[] _generations;
    private readonly BitArray _isOccupiedBits;
    private readonly Queue<int> _freeSlots;

    private int _count;
    private int _capacity;

    public int Count => _count;
    public int Capacity => _capacity;

    /// <summary>
    /// Gets an enumerator that iterates over the occupied slots in the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Initializes a new instance of <see cref="SlotMap{T}"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial number of slots to allocate.</param>
    public SlotMap(int initialCapacity = 16)
    {
        _capacity = initialCapacity;

        _data = new T[initialCapacity];
        _generations = new int[initialCapacity];
        _isOccupiedBits = new BitArray(initialCapacity);
        _freeSlots = new(initialCapacity);

        _generations.AsSpan().Fill(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize()
    {
        var newCapacity = _capacity * 2;

        Array.Resize(ref _data, newCapacity);
        Array.Resize(ref _generations, newCapacity);

        _isOccupiedBits.Length = newCapacity;
        _freeSlots.EnsureCapacity(newCapacity);
        _generations.AsSpan(_capacity).Fill(1);

        _capacity = newCapacity;
    }

    /// <summary>
    /// Adds an item to the slot map and returns the slot index used to store it.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <param name="generation">Outputs the generation value associated with the stored item.</param>
    /// <returns>The slot index assigned to the item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(T item, out int generation)
    {
        if (_count >= _capacity)
        {
            Resize();
        }

        int slotIndex;
        if (_freeSlots.Count == 0)
        {
            slotIndex = _count;
        }
        else
        {
            slotIndex = _freeSlots.Dequeue();
        }

        _data[slotIndex] = item;
        _isOccupiedBits[slotIndex] = true;

        _count++;

        generation = _generations[slotIndex];
        return slotIndex;
    }

    /// <summary>
    /// Determines whether the specified slot index and generation refer to a valid item.
    /// </summary>
    /// <param name="slotIndex">The slot index to check.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <returns>True if the slot contains a valid item; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            return false;
        }

        if (_isOccupiedBits[slotIndex] && _generations[slotIndex] == generation)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the item at the specified slot index when the generation matches.
    /// </summary>
    /// <param name="slotIndex">The slot index to remove.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int slotIndex, int generation)
    {
        if (!Contains(slotIndex, generation))
        {
            return false;
        }

        _generations[slotIndex]++;
        _isOccupiedBits[slotIndex] = false;

        _freeSlots.Enqueue(slotIndex);
        _count--;

        return true;
    }

    /// <summary>
    /// Tries to get the item at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The slot index to retrieve.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="value">When this method returns, contains the stored item if found.</param>
    /// <returns>True if the item was found; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetElement(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        if (!Contains(slotIndex, generation))
        {
            value = default;
            return false;
        }

        value = _data[slotIndex];
        return true;
    }

    /// <summary>
    /// Gets the item at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The slot index to retrieve.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <returns>The item stored at the slot.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the slot is not occupied or the generation does not match.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetElementAt(int slotIndex, int generation)
    {
        if (!Contains(slotIndex, generation))
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        return _data[slotIndex];
    }

    /// <summary>
    /// Gets a reference to the item at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The slot index to retrieve.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="exist">When this method returns, indicates whether the slot was found.</param>
    /// <returns>A reference to the stored item when found; otherwise, a null reference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (!Contains(slotIndex, generation))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        exist = true;
        return ref _data[slotIndex];
    }

    /// <summary>
    /// Updates the item at the specified slot index when the generation matches.
    /// </summary>
    /// <param name="slotIndex">The slot index to update.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="newValue">The replacement value.</param>
    /// <returns>True if the item was updated; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool UpdateElement(int slotIndex, int generation, T newValue)
    {
        if (!Contains(slotIndex, generation))
        {
            return false;
        }

        _data[slotIndex] = newValue;
        return true;
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _count = 0;
        _freeSlots.Clear();
    }

    /// <summary>
    /// Returns a span over the occupied portion of the underlying storage.
    /// </summary>
    /// <returns>A span containing the active items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return _data.AsSpan(0, _count);
    }
}
