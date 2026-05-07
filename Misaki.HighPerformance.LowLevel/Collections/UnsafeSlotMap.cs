using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

internal class UnsafeSlotMapDebugView<T>
    where T : unmanaged
{
    private readonly UnsafeSlotMap<T> _slotMap;
    public UnsafeSlotMapDebugView(UnsafeSlotMap<T> slotMap)
    {
        _slotMap = slotMap;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var items = new List<T>(_slotMap.Count);
            var enumerator = _slotMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                items.Add(enumerator.Current);
            }

            return items.ToArray();
        }
    }
}

/// <summary>
/// Provides an unsafe, high-performance slot map for storing and managing unmanaged values, supporting fast insertion,
/// removal, and lookup by slot index and Generation.
/// </summary>
/// <typeparam name="T">The type of value to store in the slot map. Must be unmanaged.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeSlotMapDebugView<>))]
public unsafe struct UnsafeSlotMap<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    private struct SlotEntry
    {
        public T value;
        public int generation;
    }

    private const int _CHUNK_SHIFT = 8;
    private const int _CHUNK_SIZE = 1 << _CHUNK_SHIFT;
    private const int _CHUNK_MASK = _CHUNK_SIZE - 1;

    public ref struct Enumerator
    {
        private ref UnsafeSlotMap<T> _collection;
        private int _currentIndex;

        public Enumerator(ref UnsafeSlotMap<T> collection)
        {
            _collection = ref collection;
            _currentIndex = -1;
        }

        public readonly ref T Current
        {
            get
            {
                var chunks = _collection._chunks;
                var chunkIdx = _currentIndex >> _CHUNK_SHIFT;
                var localIdx = _currentIndex & _CHUNK_MASK;
                return ref chunks[chunkIdx][localIdx].value;
            }
        }

        public bool MoveNext()
        {
            _currentIndex = _collection._validBits.NextSetBit(_currentIndex + 1);
            return _currentIndex != -1;
        }

        public void Reset()
        {
            _currentIndex = -1;
        }
    }

    private UnsafeArray<UnsafeArray<SlotEntry>> _chunks;
    private UnsafeQueue<int> _freeSlots;
    private UnsafeBitSet _validBits;
    private AllocationHandle _handle;
    private AllocationOption _allocationOption;

    private int _count;
    private int _capacity;
    private int _nextSlotIndex;

    public readonly int Count => _count;
    public readonly int Capacity => _capacity;

    public readonly bool IsCreated => _chunks.IsCreated && _freeSlots.IsCreated && _validBits.IsCreated;

    /// <summary>
    /// Initializes a new instance of UnsafeSlotMap with a default size of 1 and a persistent allocation handle.
    /// </summary>
    public UnsafeSlotMap()
        : this(1, AllocationHandle.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeSlotMap class with the specified capacity, allocation handle, and
    /// allocation options.
    /// </summary>
    /// <param name="capacity">The number of slots to allocate for the map. Must be greater than zero.</param>
    /// <param name="handle">A reference to the allocation handle used to manage memory for the slot map.</param>
    /// <param name="allocationOption">The allocation options to use when creating internal data structures. The default is AllocationOption.None.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than or equal to zero.</exception>
    public UnsafeSlotMap(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        _handle = handle;
        _allocationOption = allocationOption;

        var initialChunks = (capacity + _CHUNK_MASK) / _CHUNK_SIZE;
        if (initialChunks == 0)
            initialChunks = 1;

        _capacity = initialChunks * _CHUNK_SIZE;
        _chunks = new UnsafeArray<UnsafeArray<SlotEntry>>(initialChunks, handle, allocationOption);
        for (var i = 0; i < initialChunks; i++)
        {
            _chunks[i] = new UnsafeArray<SlotEntry>(_CHUNK_SIZE, handle, allocationOption);
            if (!allocationOption.HasOption(AllocationOption.Clear))
            {
                _chunks[i].AsSpan().Clear();
            }
        }

        _freeSlots = new UnsafeQueue<int>(capacity, handle, allocationOption);
        _validBits = new UnsafeBitSet(_capacity, handle, allocationOption);

        if (!allocationOption.HasOption(AllocationOption.Clear))
        {
            _validBits.ClearAll();
        }

        _count = 0;
        _nextSlotIndex = 0;
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeSlotMap class with the specified capacity, allocator, and allocation
    /// options.
    /// </summary>
    /// <param name="capacity">The initial number of slots to allocate for the map. Must be greater than zero.</param>
    /// <param name="allocator">The allocator to use for memory management of the slot map.</param>
    /// <param name="allocationOption">The allocation option that determines how memory is allocated. The default is AllocationOption.None.</param>
    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeSlotMap(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureChunkExists(int requiredChunkIndex)
    {
        if (requiredChunkIndex < _chunks.Length)
            return;

        var newChunkCount = _chunks.Length;
        while (newChunkCount <= requiredChunkIndex)
        {
            newChunkCount *= 2;
        }

        _chunks.Resize(newChunkCount, _allocationOption);

        for (var i = _capacity / _CHUNK_SIZE; i < newChunkCount; i++)
        {
            _chunks[i] = new UnsafeArray<SlotEntry>(_CHUNK_SIZE, _handle, _allocationOption);
            if (!_allocationOption.HasOption(AllocationOption.Clear))
            {
                _chunks[i].AsSpan().Clear();
            }
        }

        _capacity = newChunkCount * _CHUNK_SIZE;
        _validBits.Resize(_capacity, _allocationOption);
    }

    /// <summary>
    /// Adds the specified item to the collection and returns the index of the slot where it was stored.
    /// </summary>
    /// <param name="item">The item to add to the collection.</param>
    /// <param name="generation">When this method returns, contains the Generation number associated with the slot where the item was stored.</param>
    /// <returns>The index of the slot in which the item was stored.</returns>
    public int Add(scoped in T item, out int generation)
    {
        if (_freeSlots.Count > 0)
        {
            var slotIndex = _freeSlots.Dequeue();
            var chunkIdx = slotIndex >> _CHUNK_SHIFT;
            var localIdx = slotIndex & _CHUNK_MASK;

            ref var slot = ref _chunks[chunkIdx][localIdx];

            generation = slot.generation;
            slot.value = item;
            _validBits.SetBit(slotIndex);

            _count++;
            return slotIndex;
        }

        var newSlotIndex = _nextSlotIndex++;
        var newChunkIdx = newSlotIndex >> _CHUNK_SHIFT;
        var newLocalIdx = newSlotIndex & _CHUNK_MASK;

        if (newChunkIdx >= _chunks.Length)
        {
            EnsureChunkExists(newChunkIdx);
        }

        ref var newSlot = ref _chunks[newChunkIdx][newLocalIdx];
        newSlot.value = item;
        newSlot.generation = 0;

        _validBits.SetBit(newSlotIndex);

        generation = 0;
        _count++;
        return newSlotIndex;
    }

    /// <summary>
    /// Attempts to remove the item at the specified slot index and Generation from the collection.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to remove. Must be within the valid range of slot indices.</param>
    /// <param name="generation">The Generation value associated with the slot. Removal succeeds only if this matches the current Generation of the slot.</param>
    /// <param name="item">When this method returns, contains the item that was removed if the removal was successful; otherwise, the default value for type <typeparamref name="T"/>.</param>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    public bool Remove(int slotIndex, int generation, out T item)
    {
        item = default;
        if (slotIndex < 0)
        {
            return false;
        }

        var chunkIdx = slotIndex >> _CHUNK_SHIFT;
        var localIdx = slotIndex & _CHUNK_MASK;

        if (chunkIdx >= _chunks.Length)
        {
            return false;
        }

        ref var slot = ref _chunks[chunkIdx][localIdx];

        if (!_validBits.IsSet(slotIndex) || slot.generation != generation)
        {
            return false;
        }

        slot.generation++;
        _validBits.ClearBit(slotIndex);
        item = slot.value;
        slot.value = default;

        _freeSlots.Enqueue(slotIndex);
        _count--;

        return true;
    }

    /// <summary>
    /// Attempts to remove the item at the specified slot index and Generation from the collection.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to remove. Must be within the valid range of slot indices.</param>
    /// <param name="generation">The Generation value associated with the slot. Removal succeeds only if this matches the current Generation of
    /// the slot.</param>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    public bool Remove(int slotIndex, int generation)
    {
        return Remove(slotIndex, generation, out _);
    }

    /// <summary>
    /// Determines whether the specified slot index contains a valid entry with the given Generation.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to check. Must be greater than or equal to 0 and less than the current capacity.</param>
    /// <param name="generation">The Generation value to compare against the slot's Generation.</param>
    /// <returns>true if the slot at the specified index is valid and its Generation matches the specified value; otherwise, false.</returns>
    public readonly bool Contains(int slotIndex, int generation)
    {
        GetElementReferenceAt(slotIndex, generation, out var exist);
        return exist;
    }

    /// <summary>
    /// Attempts to retrieve the element at the specified slot index and Generation.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to retrieve. Must be within the valid range of slots.</param>
    /// <param name="generation">The Generation identifier associated with the slot. Used to verify that the slot has not been replaced or
    /// invalidated.</param>
    /// <param name="value">When this method returns, contains the element at the specified slot and Generation if found; otherwise, the
    /// default value for type <typeparamref name="T"/>.</param>
    /// <returns>true if the element at the specified slot index and Generation is found; otherwise, false.</returns>
    public readonly bool TryGetElementAt(int slotIndex, int generation, out T value)
    {
        ref var val = ref GetElementReferenceAt(slotIndex, generation, out var exist);
        if (exist)
        {
            value = val;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Retrieves the element stored at the specified slot index and Generation.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot from which to retrieve the element. Must be within the valid range of allocated slots.</param>
    /// <param name="generation">The Generation identifier associated with the slot. Used to ensure the element has not been replaced or removed since allocation.</param>
    /// <returns>The element of type <see cref="T"/> stored at the specified slot and Generation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slotIndex"/> is less than zero or greater than or equal to the capacity.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the specified slot is not occupied or the Generation does not match.</exception>
    public readonly T GetElementAt(int slotIndex, int generation)
    {
        if (!TryGetElementAt(slotIndex, generation, out var value))
        {
            throw new InvalidOperationException("The specified slot is not occupied or the generation does not match.");
        }

        return value;
    }

    /// <summary>
    /// Returns a reference to the element at the specified slot index and Generation, if it exists; otherwise, returns
    /// a null reference.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to retrieve. Must be within the valid range of allocated slots.</param>
    /// <param name="generation">The expected Generation value for the slot. Used to verify that the slot has not been recycled or replaced.</param>
    /// <param name="exist">When this method returns, contains <see langword="true"/> if a valid element exists at the specified slot and Generation; otherwise, <see langword="false"/>.</param>
    /// <returns>A reference to the element of type <typeparamref name="T"/> at the specified slot and Generation if it exists; otherwise, a null reference.</returns>
    public readonly ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (slotIndex < 0)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        var chunkIdx = slotIndex >> _CHUNK_SHIFT;
        var localIdx = slotIndex & _CHUNK_MASK;

        if (chunkIdx >= _chunks.Length)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        ref var slot = ref _chunks[chunkIdx][localIdx];

        if (_validBits.IsSet(slotIndex) && slot.generation == generation)
        {
            exist = true;
            return ref slot.value;
        }

        exist = false;
        return ref Unsafe.NullRef<T>();
    }

    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        var requiredChunkIndex = (newSize + _CHUNK_MASK) / _CHUNK_SIZE - 1;
        EnsureChunkExists(requiredChunkIndex);
        _freeSlots.Resize(newSize, option);
        _validBits.Resize(newSize, option);
    }

    public void Clear()
    {
        for (var i = 0; i < _chunks.Length; i++)
        {
            if (_chunks[i].IsCreated)
            {
                var chunk = _chunks[i];
                for (var slot = 0; slot < _CHUNK_SIZE; slot++)
                {
                    chunk[slot].generation = 0;
                    chunk[slot].value = default;
                }
            }
        }
        _freeSlots.Clear();
        _validBits.ClearAll();
        _count = 0;
        _nextSlotIndex = 0;
    }

    public readonly void* GetUnsafePtr()
    {
        return null;
    }

    public void Dispose()
    {
        for (var i = 0; i < _chunks.Length; i++)
        {
            if (_chunks[i].IsCreated)
            {
                _chunks[i].Dispose();
            }
        }
        _chunks.Dispose();
        _freeSlots.Dispose();
        _validBits.Dispose();

        _count = 0;
        _capacity = 0;
        _nextSlotIndex = 0;
    }
}
