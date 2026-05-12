using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

public class ConcurrentSlotMap<T> : IEnumerable<T>
{
    private const int _CHUNK_SHIFT = 8;
    private const int _CHUNK_SIZE = 1 << _CHUNK_SHIFT;
    private const int _CHUNK_MASK = _CHUNK_SIZE - 1;

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
                var values = _slotMap._values;
                int chunkIdx = _currentIndex >> _CHUNK_SHIFT;
                int localIdx = _currentIndex & _CHUNK_MASK;
                return values[chunkIdx][localIdx]!;
            }
        }
        
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var maxIndex = Volatile.Read(ref _slotMap._nextSlotIndex);
            var validBits = _slotMap._validBits;
            
            while (++_currentIndex < maxIndex)
            {
                int chunkIdx = _currentIndex >> _CHUNK_SHIFT;
                int localIdx = _currentIndex & _CHUNK_MASK;
                
                if (chunkIdx < validBits.Length && Volatile.Read(ref validBits[chunkIdx][localIdx]) == 1)
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

    private volatile T[][] _values;
    private volatile int[][] _generations;
    private volatile int[][] _validBits;
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

    public ConcurrentSlotMap(int initialCapacity)
    {
        _count = 0;
        _nextSlotIndex = 0;
        _isResizing = 0;

        int initialChunks = (initialCapacity + _CHUNK_MASK) / _CHUNK_SIZE;
        if (initialChunks == 0)
        {
            initialChunks = 1;
        }

        _capacity = initialChunks * _CHUNK_SIZE;
        _values = new T[initialChunks][];
        _generations = new int[initialChunks][];
        _validBits = new int[initialChunks][];
        for (int i = 0; i < initialChunks; i++)
        {
            _values[i] = new T[_CHUNK_SIZE];
            _generations[i] = new int[_CHUNK_SIZE];
            _validBits[i] = new int[_CHUNK_SIZE];
            _generations[i].AsSpan().Fill(1);
        }

        _freeSlots = new ConcurrentQueue<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureChunkExists(int requiredChunkIndex)
    {
        if (requiredChunkIndex < _values.Length)
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
            var oldValues = _values;
            if (requiredChunkIndex < oldValues.Length)
            {
                return; // Another thread already resized
            }

            int newChunkCount = oldValues.Length;
            while (newChunkCount <= requiredChunkIndex)
            {
                newChunkCount *= 2;
            }

            var newValues = new T[newChunkCount][];
            var newGenerations = new int[newChunkCount][];
            var newValidBits = new int[newChunkCount][];
            Array.Copy(oldValues, newValues, oldValues.Length);
            Array.Copy(_generations, newGenerations, _generations.Length);
            Array.Copy(_validBits, newValidBits, _validBits.Length);

            // Initialize new chunks
            for (var i = oldValues.Length; i < newChunkCount; i++)
            {
                newValues[i] = new T[_CHUNK_SIZE];
                newGenerations[i] = new int[_CHUNK_SIZE];
                newValidBits[i] = new int[_CHUNK_SIZE];
                newGenerations[i].AsSpan().Fill(1);
            }

            // Atomically update the array references and capacity
            _values = newValues;
            _generations = newGenerations;
            _validBits = newValidBits;
            Volatile.Write(ref _capacity, newChunkCount * _CHUNK_SIZE);
        }
        finally
        {
            // Release the resize lock
            Volatile.Write(ref _isResizing, 0);
        }
    }

    public int Add(scoped in T item, out int generation)
    {
        while (true)
        {
            // Try to get a free slot first
            if (_freeSlots.TryDequeue(out var slotIndex))
            {
                var values = _values;
                var generations = _generations;
                var validBits = _validBits;
                int chunkIdx = slotIndex >> _CHUNK_SHIFT;
                int localIdx = slotIndex & _CHUNK_MASK;

                if (chunkIdx < values.Length)
                {
                    ref var slotValue = ref values[chunkIdx][localIdx];
                    ref var slotGeneration = ref generations[chunkIdx][localIdx];
                    ref var slotValid = ref validBits[chunkIdx][localIdx];

                    // Atomically mark as valid and get the current generation
                    var currentGeneration = Volatile.Read(ref slotGeneration);
                    slotValue = item;

                    // Use CAS to mark as valid atomically
                    if (Interlocked.CompareExchange(ref slotValid, 1, 0) == 0)
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
            int newChunkIdx = newSlotIndex >> _CHUNK_SHIFT;
            int newLocalIdx = newSlotIndex & _CHUNK_MASK;

            var currentValues = _values;
            var currentGenerations = _generations;
            var currentValidBits = _validBits;
            if (newChunkIdx >= currentValues.Length)
            {
                EnsureChunkExists(newChunkIdx);
                currentValues = _values; // Re-read after resize
                currentGenerations = _generations;
                currentValidBits = _validBits;
            }

            // Initialize the new slot
            ref var newValue = ref currentValues[newChunkIdx][newLocalIdx];
            ref var newGeneration = ref currentGenerations[newChunkIdx][newLocalIdx];
            ref var newValid = ref currentValidBits[newChunkIdx][newLocalIdx];
            newValue = item;
            newGeneration = 0;
            Volatile.Write(ref newValid, 1);

            generation = 0;
            Interlocked.Increment(ref _count);
            return newSlotIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        var values = _values;
        var generations = _generations;
        var validBits = _validBits;
        int chunkIdx = slotIndex >> _CHUNK_SHIFT;
        int localIdx = slotIndex & _CHUNK_MASK;

        if (chunkIdx >= values.Length)
        {
            value = default;
            return false;
        }

        ref var slotValue = ref values[chunkIdx][localIdx];
        ref var slotGeneration = ref generations[chunkIdx][localIdx];
        ref var slotValid = ref validBits[chunkIdx][localIdx];

        // Check if slot is valid and generation matches
        if (Volatile.Read(ref slotValid) == 0 || Volatile.Read(ref slotGeneration) != generation)
        {
            value = default;
            return false;
        }

        // Atomically mark as invalid
        if (Interlocked.CompareExchange(ref slotValid, 0, 1) == 1)
        {
            Interlocked.Increment(ref slotGeneration);
            value = slotValue;
            slotValue = default!;

            _freeSlots.Enqueue(slotIndex);
            Interlocked.Decrement(ref _count);
            return true;
        }

        value = default;
        return false; // Another thread already removed it
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int slotIndex, int generation)
    {
        GetElementReferenceAt(slotIndex, generation, out var exist);
        return exist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetElement(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        value = GetElementReferenceAt(slotIndex, generation, out var exist);
        return exist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetElementAt(int slotIndex, int generation)
    {
        if (!TryGetElement(slotIndex, generation, out var value))
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (slotIndex < 0)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        var values = _values;
        var generations = _generations;
        var validBits = _validBits;
        int chunkIdx = slotIndex >> _CHUNK_SHIFT;
        int localIdx = slotIndex & _CHUNK_MASK;

        if (chunkIdx >= values.Length)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        ref var slotGeneration = ref generations[chunkIdx][localIdx];

        var currentGeneration = Volatile.Read(ref slotGeneration);
        var isValid = Volatile.Read(ref validBits[chunkIdx][localIdx]) == 1;

        if (isValid && currentGeneration == generation)
        {
            if (Volatile.Read(ref validBits[chunkIdx][localIdx]) == 1 && Volatile.Read(ref slotGeneration) == generation)
            {
                exist = true;
                return ref values[chunkIdx][localIdx]!;
            }
        }

        exist = false;
        return ref Unsafe.NullRef<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    public void Clear()
    {
        // Reset counters
        Volatile.Write(ref _count, 0);
        Volatile.Write(ref _nextSlotIndex, 0);

        // Clear all slots
        var values = _values;
        var generations = _generations;
        var validBits = _validBits;
        for (var c = 0; c < values.Length; c++)
        {
            var chunkValues = values[c];
            var chunkValidBits = validBits[c];
            for (var i = 0; i < _CHUNK_SIZE; i++)
            {
                Volatile.Write(ref chunkValidBits[i], 0);
                chunkValues[i] = default!;
            }
        }

        _freeSlots.Clear();
    }
}