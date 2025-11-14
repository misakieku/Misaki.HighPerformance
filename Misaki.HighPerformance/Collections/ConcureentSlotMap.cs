using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

public class ConcurrentSlotMap<T> : IEnumerable<T>
{
    private struct SlotEntry
    {
        public T? value;
        public int generation;
        public int isValid;
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

        public readonly T Current => _slotMap._data[_currentIndex].value!;
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var capacity = Volatile.Read(ref _slotMap._capacity);
            while (++_currentIndex < capacity)
            {
                if (Volatile.Read(ref _slotMap._data[_currentIndex].isValid) == 1)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset() => _currentIndex = -1;

        public void Dispose()
        {
        }
    }

    private volatile SlotEntry[] _data;
    private readonly ConcurrentQueue<int> _freeSlots;

    private int _count;
    private int _capacity;
    private int _nextSlotIndex;

    // For lock-free resizing
    private int _isResizing;

    public int Count => Volatile.Read(ref _count);
    public int Capacity => Volatile.Read(ref _capacity);

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ConcurrentSlotMap(int initialCapacity = 16)
    {
        _capacity = initialCapacity;
        _count = 0;
        _nextSlotIndex = 0;
        _isResizing = 0;

        _data = new SlotEntry[initialCapacity];
        _freeSlots = new();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TryResize(int requiredCapacity)
    {
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
            var currentCapacity = Volatile.Read(ref _capacity);
            if (currentCapacity >= requiredCapacity)
            {
                return; // Another thread already resized
            }

            var newCapacity = currentCapacity;
            while (newCapacity < requiredCapacity)
            {
                newCapacity *= 2;
            }

            var newData = new SlotEntry[newCapacity];
            var oldData = _data;

            // Copy existing data
            Array.Copy(oldData, newData, currentCapacity);

            // Initialize new slots
            for (var i = currentCapacity; i < newCapacity; i++)
            {
                newData[i] = new SlotEntry();
            }

            // Atomically update the array reference and capacity
            _data = newData;
            Volatile.Write(ref _capacity, newCapacity);
        }
        finally
        {
            // Release the resize lock
            Volatile.Write(ref _isResizing, 0);
        }
    }

    public int Add(T item, out int generation)
    {
        // Try to get a free slot first
        if (_freeSlots.TryDequeue(out var slotIndex))
        {
            ref var slot = ref _data[slotIndex];

            // Atomically mark as valid and get the current generation
            var currentGeneration = Volatile.Read(ref slot.generation);
            slot.value = item;

            // Use CAS to mark as valid atomically
            if (Interlocked.CompareExchange(ref slot.isValid, 1, 0) == 0)
            {
                generation = currentGeneration;
                Interlocked.Increment(ref _count);
                return slotIndex;
            }
            else
            {
                // Slot was somehow already valid, put it back and try again
                _freeSlots.Enqueue(slotIndex);
                return Add(item, out generation);
            }
        }

        // Need a new slot
        slotIndex = Interlocked.Increment(ref _nextSlotIndex) - 1;

        // Check if we need to resize
        var currentCapacity = Volatile.Read(ref _capacity);
        if (slotIndex >= currentCapacity)
        {
            TryResize(slotIndex + 1);
        }

        // Initialize the new slot
        ref var newSlot = ref _data[slotIndex];
        newSlot.value = item;
        newSlot.generation = 0;
        Volatile.Write(ref newSlot.isValid, 1);

        generation = 0;
        Interlocked.Increment(ref _count);
        return slotIndex;
    }

    public bool Remove(int slotIndex, int generation)
    {
        var capacity = Volatile.Read(ref _capacity);

        if (slotIndex < 0 || slotIndex >= capacity)
        {
            return false;
        }

        ref var slot = ref _data[slotIndex];

        // Check if slot is valid and generation matches
        if (Volatile.Read(ref slot.isValid) == 0 || Volatile.Read(ref slot.generation) != generation)
        {
            return false;
        }

        // Atomically mark as invalid
        if (Interlocked.CompareExchange(ref slot.isValid, 0, 1) == 1)
        {
            Interlocked.Increment(ref slot.generation);
            slot.value = default;

            _freeSlots.Enqueue(slotIndex);
            Interlocked.Decrement(ref _count);
            return true;
        }

        return false; // Another thread already removed it
    }

    public bool Contains(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            return false;
        }

        ref var slot = ref _data[slotIndex];

        var currentGeneration = Volatile.Read(ref slot.generation);
        var isValid = Volatile.Read(ref slot.isValid) == 1;

        if (isValid && currentGeneration == generation)
        {
            if (Volatile.Read(ref slot.isValid) == 1 && Volatile.Read(ref slot.generation) == generation)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetElement(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        value = default;

        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            return false;
        }

        ref var slot = ref _data[slotIndex];

        // Read generation first, then validity, then value for consistency
        var currentGeneration = Volatile.Read(ref slot.generation);
        var isValid = Volatile.Read(ref slot.isValid) == 1;

        if (isValid && currentGeneration == generation)
        {
            // Double-check that the slot is still valid with same generation
            // to avoid race condition where slot gets removed between reads
            if (Volatile.Read(ref slot.isValid) == 1 && Volatile.Read(ref slot.generation) == generation)
            {
                value = slot.value!;
                return true;
            }
        }

        return false;
    }

    public T GetElementAt(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index is out of range.");
        }

        ref var slot = ref _data[slotIndex];

        if (Volatile.Read(ref slot.isValid) == 0 || Volatile.Read(ref slot.generation) != generation)
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        return slot.value!;
    }

    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        ref var slot = ref _data[slotIndex];

        if (Volatile.Read(ref slot.isValid) == 0 || Volatile.Read(ref slot.generation) != generation)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        exist = true;
        return ref slot.value!;
    }

    public void UpdateElement(int slotIndex, int generation, T newValue)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index is out of range.");
        }

        ref var slot = ref _data[slotIndex];
        if (Volatile.Read(ref slot.isValid) == 0 || Volatile.Read(ref slot.generation) != generation)
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        slot.value = newValue;
    }

    public void Clear()
    {
        // Reset counters
        Volatile.Write(ref _count, 0);
        Volatile.Write(ref _nextSlotIndex, 0);

        // Clear all slots
        var capacity = Volatile.Read(ref _capacity);
        for (var i = 0; i < capacity; i++)
        {
            ref var slot = ref _data[i];
            Volatile.Write(ref slot.isValid, 0);
            slot.generation = 0;
            slot.value = default;
        }

        // Clear free slots queue
        while (_freeSlots.TryDequeue(out _))
        {
        }
    }
}