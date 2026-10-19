using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

/// <summary>
/// A thread-safe slot map that supports concurrent add, remove, lookup, and update operations.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the concurrent slot map.</typeparam>
public class ConcurrentSlotMap<T> : IEnumerable<T>
{
    private const int _CHUNK_SHIFT = 8;
    private const int _CHUNK_SIZE = 1 << _CHUNK_SHIFT;
    private const int _CHUNK_MASK = _CHUNK_SIZE - 1;

    private struct Slot
    {
        public T Value;
        public int Generation; // Odd = active, Even = free (starts at 0)
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly ConcurrentSlotMap<T> _slotMap;
        private int _currentIndex;

        public Enumerator(ConcurrentSlotMap<T> slotMap)
        {
            _slotMap = slotMap;
            _currentIndex = -1;
        }

        public readonly T Current
        {
            get
            {
                var slots = _slotMap._slots;
                var chunkIdx = _currentIndex >> _CHUNK_SHIFT;
                var localIdx = _currentIndex & _CHUNK_MASK;
                return slots[chunkIdx][localIdx].Value!;
            }
        }

        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var maxIndex = Volatile.Read(ref _slotMap._nextSlotIndex);
            var slots = _slotMap._slots;

            while (++_currentIndex < maxIndex)
            {
                var chunkIdx = _currentIndex >> _CHUNK_SHIFT;
                var localIdx = _currentIndex & _CHUNK_MASK;

                if (chunkIdx < slots.Length && (Volatile.Read(ref slots[chunkIdx][localIdx].Generation) & 1) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _currentIndex = -1;
        }

        public readonly void Dispose()
        {
        }
    }

    private volatile Slot[][] _slots;
    private readonly ConcurrentQueue<int> _freeSlots;

    private int _count;
    private int _capacity;
    private int _nextSlotIndex;

    // For lock-free resizing
    private int _isResizing;

    public int Count => Volatile.Read(ref _count);
    public int Capacity => Volatile.Read(ref _capacity);

    /// <summary>
    /// Gets an enumerator that iterates over the valid items in the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrentSlotMap{T}"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the collection.</param>
    public ConcurrentSlotMap(int initialCapacity)
    {
        _count = 0;
        _nextSlotIndex = 0;
        _isResizing = 0;

        var initialChunks = (initialCapacity + _CHUNK_MASK) / _CHUNK_SIZE;
        if (initialChunks == 0)
        {
            initialChunks = 1;
        }

        _capacity = initialChunks * _CHUNK_SIZE;
        _slots = new Slot[initialChunks][];
        for (var i = 0; i < initialChunks; i++)
        {
            _slots[i] = new Slot[_CHUNK_SIZE];
        }

        _freeSlots = new ConcurrentQueue<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureChunkExists(int requiredChunkIndex)
    {
        if (requiredChunkIndex < _slots.Length)
        {
            return;
        }

        // Use CAS to ensure only one thread does the resize
        if (Interlocked.CompareExchange(ref _isResizing, 1, 0) != 0)
        {
            // Another thread is resizing, wait for it to complete
            var spinWait = new SpinWait();
            while (Volatile.Read(ref _isResizing) == 1)
            {
                spinWait.SpinOnce();
            }
            return;
        }

        try
        {
            var oldSlots = _slots;
            if (requiredChunkIndex < oldSlots.Length)
            {
                return; // Another thread already resized
            }

            var newChunkCount = oldSlots.Length;
            while (newChunkCount <= requiredChunkIndex)
            {
                newChunkCount *= 2;
            }

            var newSlots = new Slot[newChunkCount][];
            Array.Copy(oldSlots, newSlots, oldSlots.Length);

            // Initialize new chunks
            for (var i = oldSlots.Length; i < newChunkCount; i++)
            {
                newSlots[i] = new Slot[_CHUNK_SIZE];
            }

            // Atomically update the array reference and capacity
            _slots = newSlots;
            Volatile.Write(ref _capacity, newChunkCount * _CHUNK_SIZE);
        }
        finally
        {
            // Release the resize lock
            Volatile.Write(ref _isResizing, 0);
        }
    }

    /// <summary>
    /// Adds an item to the collection and returns the slot index used to store it.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <param name="generation">Outputs the generation value associated with the stored item.</param>
    /// <returns>The slot index assigned to the item.</returns>
    public int Add(scoped in T item, out int generation)
    {
        while (true)
        {
            // Try to get a free slot first
            if (_freeSlots.TryDequeue(out var slotIndex))
            {
                var slots = _slots;
                var chunkIdx = slotIndex >> _CHUNK_SHIFT;
                var localIdx = slotIndex & _CHUNK_MASK;

                if (chunkIdx < slots.Length)
                {
                    ref var slot = ref slots[chunkIdx][localIdx];
                    var currentGeneration = Volatile.Read(ref slot.Generation);

                    if ((currentGeneration & 1) != 0)
                    {
                        // Slot is somehow already occupied; try next
                        continue;
                    }

                    slot.Value = item;
                    var nextGeneration = currentGeneration + 1; // Even -> Odd

                    // Use CAS to atomically transition from free (even) to active (odd)
                    if (Interlocked.CompareExchange(ref slot.Generation, nextGeneration, currentGeneration) == currentGeneration)
                    {
                        generation = nextGeneration;
                        Interlocked.Increment(ref _count);
                        return slotIndex;
                    }
                    else
                    {
                        // Race detected, try again
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            // Need a new slot
            var newSlotIndex = Interlocked.Increment(ref _nextSlotIndex) - 1;
            var newChunkIdx = newSlotIndex >> _CHUNK_SHIFT;
            var newLocalIdx = newSlotIndex & _CHUNK_MASK;

            var currentSlots = _slots;
            if (newChunkIdx >= currentSlots.Length)
            {
                EnsureChunkExists(newChunkIdx);
                currentSlots = _slots; // Re-read after resize
            }

            // Initialize the new slot
            ref var newSlot = ref currentSlots[newChunkIdx][newLocalIdx];
            newSlot.Value = item;
            Volatile.Write(ref newSlot.Generation, 1);

            generation = 1;
            Interlocked.Increment(ref _count);
            return newSlotIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Removes the item at the specified slot index when the generation matches.
    /// </summary>
    /// <param name="slotIndex">The slot index to remove.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    public bool Remove(int slotIndex, int generation)
    {
        return Remove(slotIndex, generation, out _);
    }

    /// <summary>
    /// Removes the item at the specified slot index and returns its value when the generation matches.
    /// </summary>
    /// <param name="slotIndex">The slot index to remove.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="value">When this method returns, contains the removed item if found.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    public bool Remove(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        if (slotIndex < 0 || (generation & 1) == 0)
        {
            value = default;
            return false;
        }

        var slots = _slots;
        var chunkIdx = slotIndex >> _CHUNK_SHIFT;
        var localIdx = slotIndex & _CHUNK_MASK;

        if (chunkIdx >= slots.Length)
        {
            value = default;
            return false;
        }

        ref var slot = ref slots[chunkIdx][localIdx];

        // Fast check: generation must match and must be active (odd)
        if (Volatile.Read(ref slot.Generation) != generation)
        {
            value = default;
            return false;
        }

        // Atomically mark as invalid by incrementing from odd to even
        var nextGeneration = generation + 1;
        if (Interlocked.CompareExchange(ref slot.Generation, nextGeneration, generation) == generation)
        {
            value = slot.Value;
            slot.Value = default!;

            _freeSlots.Enqueue(slotIndex);
            Interlocked.Decrement(ref _count);
            return true;
        }

        value = default;
        return false; // Another thread already removed it
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Determines whether the specified slot index and generation refer to a valid item.
    /// </summary>
    /// <param name="slotIndex">The slot index to check.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <returns>True if the slot contains a valid item; otherwise, false.</returns>
    public bool Contains(int slotIndex, int generation)
    {
        GetElementReferenceAt(slotIndex, generation, out var exist);
        return exist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Tries to get the item at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The slot index to retrieve.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="value">When this method returns, contains the stored item if found.</param>
    /// <returns>True if the item was found; otherwise, false.</returns>
    public bool TryGetElement(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        value = GetElementReferenceAt(slotIndex, generation, out var exist);
        return exist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Gets the item at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The slot index to retrieve.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <returns>The item stored at the slot.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the slot is not occupied or the generation does not match.</exception>
    public T GetElementAt(int slotIndex, int generation)
    {
        if (!TryGetElement(slotIndex, generation, out var value))
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Gets a reference to the item at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The slot index to retrieve.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="exist">When this method returns, indicates whether the slot was found.</param>
    /// <returns>A reference to the stored item when found; otherwise, a null reference.</returns>
    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (slotIndex < 0 || (generation & 1) == 0)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        var slots = _slots;
        var chunkIdx = slotIndex >> _CHUNK_SHIFT;
        var localIdx = slotIndex & _CHUNK_MASK;

        if (chunkIdx >= slots.Length)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        ref var slot = ref slots[chunkIdx][localIdx];

        if (Volatile.Read(ref slot.Generation) == generation)
        {
            ref var val = ref slot.Value;
            if (Volatile.Read(ref slot.Generation) == generation)
            {
                exist = true;
                return ref val!;
            }
        }

        exist = false;
        return ref Unsafe.NullRef<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Updates the item at the specified slot index when the generation matches.
    /// </summary>
    /// <param name="slotIndex">The slot index to update.</param>
    /// <param name="generation">The generation to validate.</param>
    /// <param name="newValue">The replacement value.</param>
    /// <returns>True if the item was updated; otherwise, false.</returns>
    public bool UpdateElement(int slotIndex, int generation, T newValue)
    {
        ref var slotRef = ref GetElementReferenceAt(slotIndex, generation, out var exist);
        if (!exist)
        {
            return false;
        }

        slotRef = newValue;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        Volatile.Write(ref _count, 0);
        Volatile.Write(ref _nextSlotIndex, 0);

        var slots = _slots;
        for (var c = 0; c < slots.Length; c++)
        {
            var chunk = slots[c];
            for (var i = 0; i < _CHUNK_SIZE; i++)
            {
                ref var slot = ref chunk[i];
                var gen = Volatile.Read(ref slot.Generation);
                if ((gen & 1) != 0)
                {
                    Volatile.Write(ref slot.Generation, gen + 1);
                }
                slot.Value = default!;
            }
        }

        _freeSlots.Clear();
    }
}