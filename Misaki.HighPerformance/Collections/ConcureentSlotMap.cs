using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

public class ConcurrentSlotMap<T> : IEnumerable<T>
{
    private struct SlotEntry
    {
        public T value;
        public int generation;
        public int isValid;
    }

    private const int CHUNK_SHIFT = 8;
    private const int CHUNK_SIZE = 1 << CHUNK_SHIFT;
    private const int CHUNK_MASK = CHUNK_SIZE - 1;

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
                var chunks = _slotMap._chunks;
                int chunkIdx = _currentIndex >> CHUNK_SHIFT;
                int localIdx = _currentIndex & CHUNK_MASK;
                return chunks[chunkIdx][localIdx].value!;
            }
        }
        
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var maxIndex = Volatile.Read(ref _slotMap._nextSlotIndex);
            var chunks = _slotMap._chunks;
            
            while (++_currentIndex < maxIndex)
            {
                int chunkIdx = _currentIndex >> CHUNK_SHIFT;
                int localIdx = _currentIndex & CHUNK_MASK;
                
                if (chunkIdx < chunks.Length && Volatile.Read(ref chunks[chunkIdx][localIdx].isValid) == 1)
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

        public void Dispose()
        {
        }
    }

    private volatile SlotEntry[][] _chunks;
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

    public ConcurrentSlotMap(int initialCapacity = 256)
    {
        _count = 0;
        _nextSlotIndex = 0;
        _isResizing = 0;

        int initialChunks = (initialCapacity + CHUNK_MASK) / CHUNK_SIZE;
        if (initialChunks == 0) initialChunks = 1;

        _capacity = initialChunks * CHUNK_SIZE;
        _chunks = new SlotEntry[initialChunks][];
        for (int i = 0; i < initialChunks; i++)
        {
            _chunks[i] = new SlotEntry[CHUNK_SIZE];
        }

        _freeSlots = new();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureChunkExists(int requiredChunkIndex)
    {
        if (requiredChunkIndex < _chunks.Length) return;

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
            var oldChunks = _chunks;
            if (requiredChunkIndex < oldChunks.Length)
            {
                return; // Another thread already resized
            }

            int newChunkCount = oldChunks.Length;
            while (newChunkCount <= requiredChunkIndex)
            {
                newChunkCount *= 2;
            }

            var newChunks = new SlotEntry[newChunkCount][];
            Array.Copy(oldChunks, newChunks, oldChunks.Length);

            // Initialize new chunks
            for (var i = oldChunks.Length; i < newChunkCount; i++)
            {
                newChunks[i] = new SlotEntry[CHUNK_SIZE];
            }

            // Atomically update the array reference and capacity
            _chunks = newChunks;
            Volatile.Write(ref _capacity, newChunkCount * CHUNK_SIZE);
        }
        finally
        {
            // Release the resize lock
            Volatile.Write(ref _isResizing, 0);
        }
    }

    public int Add(T item, out int generation)
    {
        while (true)
        {
            // Try to get a free slot first
            if (_freeSlots.TryDequeue(out var slotIndex))
            {
                var chunks = _chunks;
                int chunkIdx = slotIndex >> CHUNK_SHIFT;
                int localIdx = slotIndex & CHUNK_MASK;

                if (chunkIdx < chunks.Length)
                {
                    ref var slot = ref chunks[chunkIdx][localIdx];

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
                        // Slot was somehow already valid, don't put it back in free pool
                        // Just loop and try again
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            // Need a new slot
            int newSlotIndex = Interlocked.Increment(ref _nextSlotIndex) - 1;
            int newChunkIdx = newSlotIndex >> CHUNK_SHIFT;
            int newLocalIdx = newSlotIndex & CHUNK_MASK;

            var currentChunks = _chunks;
            if (newChunkIdx >= currentChunks.Length)
            {
                EnsureChunkExists(newChunkIdx);
                currentChunks = _chunks; // Re-read after resize
            }

            // Initialize the new slot
            ref var newSlot = ref currentChunks[newChunkIdx][newLocalIdx];
            newSlot.value = item;
            newSlot.generation = 0;
            Volatile.Write(ref newSlot.isValid, 1);

            generation = 0;
            Interlocked.Increment(ref _count);
            return newSlotIndex;
        }
    }

    public bool Remove(int slotIndex, int generation)
    {
        return Remove(slotIndex, generation, out _);
    }

    public bool Remove(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        if (slotIndex < 0)
        {
            value = default;
            return false;
        }

        var chunks = _chunks;
        int chunkIdx = slotIndex >> CHUNK_SHIFT;
        int localIdx = slotIndex & CHUNK_MASK;

        if (chunkIdx >= chunks.Length)
        {
            value = default;
            return false;
        }

        ref var slot = ref chunks[chunkIdx][localIdx];

        // Check if slot is valid and generation matches
        if (Volatile.Read(ref slot.isValid) == 0 || Volatile.Read(ref slot.generation) != generation)
        {
            value = default;
            return false;
        }

        // Atomically mark as invalid
        if (Interlocked.CompareExchange(ref slot.isValid, 0, 1) == 1)
        {
            Interlocked.Increment(ref slot.generation);
            value = slot.value;
            slot.value = default!;

            _freeSlots.Enqueue(slotIndex);
            Interlocked.Decrement(ref _count);
            return true;
        }

        value = default;
        return false; // Another thread already removed it
    }

    public bool Contains(int slotIndex, int generation)
    {
        if (slotIndex < 0)
        {
            return false;
        }

        var chunks = _chunks;
        int chunkIdx = slotIndex >> CHUNK_SHIFT;
        int localIdx = slotIndex & CHUNK_MASK;

        if (chunkIdx >= chunks.Length)
        {
            return false;
        }

        ref var slot = ref chunks[chunkIdx][localIdx];

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
        if (!Contains(slotIndex, generation))
        {
            value = default;
            return false;
        }

        var chunks = _chunks;
        int chunkIdx = slotIndex >> CHUNK_SHIFT;
        int localIdx = slotIndex & CHUNK_MASK;

        value = chunks[chunkIdx][localIdx].value!;
        return true;
    }

    public T GetElementAt(int slotIndex, int generation)
    {
        if (!TryGetElement(slotIndex, generation, out var value))
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        return value;
    }

    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (!Contains(slotIndex, generation))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        var chunks = _chunks;
        int chunkIdx = slotIndex >> CHUNK_SHIFT;
        int localIdx = slotIndex & CHUNK_MASK;

        exist = true;
        return ref chunks[chunkIdx][localIdx].value!;
    }

    public bool UpdateElement(int slotIndex, int generation, T newValue)
    {
        if (!Contains(slotIndex, generation))
        {
            return false;
        }

        var chunks = _chunks;
        int chunkIdx = slotIndex >> CHUNK_SHIFT;
        int localIdx = slotIndex & CHUNK_MASK;

        chunks[chunkIdx][localIdx].value = newValue;
        return true;
    }

    public void Clear()
    {
        // Reset counters
        Volatile.Write(ref _count, 0);
        Volatile.Write(ref _nextSlotIndex, 0);

        // Clear all slots
        var chunks = _chunks;
        for (var c = 0; c < chunks.Length; c++)
        {
            var chunk = chunks[c];
            for (var i = 0; i < CHUNK_SIZE; i++)
            {
                ref var slot = ref chunk[i];
                Volatile.Write(ref slot.isValid, 0);
                slot.generation = 0;
                slot.value = default!;
            }
        }

        _freeSlots.Clear();
    }
}