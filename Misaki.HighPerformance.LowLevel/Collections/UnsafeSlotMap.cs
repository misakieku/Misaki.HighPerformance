using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Diagnostics;
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
/// removal, and lookup by slot index and generation.
/// </summary>
/// <typeparam name="T">The type of value to store in the slot map. Must be unmanaged.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeSlotMapDebugView<>))]
public unsafe struct UnsafeSlotMap<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeSlotMap<T>* _collection;
        private int _currentIndex;

        public readonly ref T Current => ref _collection->_data[_currentIndex];
        readonly T IEnumerator<T>.Current => Current;
        readonly object? IEnumerator.Current => Current;

        public Enumerator(UnsafeSlotMap<T>* collection)
        {
            _collection = collection;
            _currentIndex = 0;
        }

        public bool MoveNext()
        {
            _currentIndex = _collection->_validBits.NextSetBit(_currentIndex + 1);
            return _currentIndex != -1;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }

        public void Dispose()
        {
        }
    }

    private UnsafeArray<T> _data;
    private UnsafeArray<int> _generations;
    private UnsafeQueue<int> _freeSlots;
    private UnsafeBitSet _validBits;

    private int _count;
    private int _capacity;

    public readonly int Count => _count - 1;
    public readonly int Capacity => _capacity;

    public readonly bool IsCreated => _data.IsCreated && _generations.IsCreated && _freeSlots.IsCreated && _validBits.IsCreated;

    public Enumerator GetEnumerator() => new((UnsafeSlotMap<T>*)UnsafeUtility.AddressOf(ref this));
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Invalid constructor. Use <see cref="UnsafeSlotMap(int, Allocator, AllocationOption)"/> or <see cref="UnsafeSlotMap(int, ref AllocationHandle, AllocationOption)"/> instead."/>
    /// </summary>
    public UnsafeSlotMap()
        : this(0, Allocator.Invalid)
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

        _data = new UnsafeArray<T>(capacity, handle, allocationOption);
        _generations = new UnsafeArray<int>(capacity, handle, allocationOption);
        _freeSlots = new UnsafeQueue<int>(capacity, handle, allocationOption);
        _validBits = new UnsafeBitSet(capacity, handle, allocationOption);

        if (!allocationOption.HasFlag(AllocationOption.Clear))
        {
            _generations.AsSpan().Clear();
        }
        _validBits.ClearAll();

        _count = 0;
        _capacity = capacity;

        // Add a default item for invalid slot
        Add(default, out _);
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeSlotMap class with the specified capacity, allocator, and allocation
    /// options.
    /// </summary>
    /// <param name="capacity">The initial number of slots to allocate for the map. Must be greater than zero.</param>
    /// <param name="allocator">The allocator to use for memory management of the slot map.</param>
    /// <param name="allocationOption">The allocation option that determines how memory is allocated. The default is AllocationOption.None.</param>
    public UnsafeSlotMap(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Adds the specified item to the collection and returns the index of the slot where it was stored.
    /// </summary>
    /// <param name="item">The item to add to the collection.</param>
    /// <param name="generation">When this method returns, contains the generation number associated with the slot where the item was stored.</param>
    /// <returns>The index of the slot in which the item was stored.</returns>
    public int Add(T item, out int generation)
    {
        if (_count >= _capacity)
        {
            Resize(Math.Max(1, _capacity * 2));
        }

        int index;
        if (_freeSlots.Count == 0)
        {
            index = _count;
        }
        else
        {
            index = _freeSlots.Dequeue();
        }

        _data[index] = item;
        _validBits.SetBit(index);

        _count++;

        generation = _generations[index];
        return index;
    }

    /// <summary>
    /// Attempts to remove the item at the specified slot index and generation from the collection.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to remove. Must be within the valid range of slot indices.</param>
    /// <param name="generation">The generation value associated with the slot. Removal succeeds only if this matches the current generation of
    /// the slot.</param>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    public bool Remove(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            return false;
        }

        ref var gen = ref _generations[slotIndex];
        if (gen != generation)
        {
            return false;
        }

        gen++;
        _validBits.ClearBit(slotIndex);
        _freeSlots.Enqueue(slotIndex);

        _count--;

        return true;
    }

    /// <summary>
    /// Determines whether the specified slot index contains a valid entry with the given generation.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to check. Must be greater than or equal to 0 and less than the current capacity.</param>
    /// <param name="generation">The generation value to compare against the slot's generation.</param>
    /// <returns>true if the slot at the specified index is valid and its generation matches the specified value; otherwise, false.</returns>
    public readonly bool Contains(int slotIndex, int generation)
    {
        // 0 is reserved for invalid slot
        if (slotIndex <= 0 || slotIndex >= _capacity)
        {
            return false;
        }

        if (_validBits.IsSet(slotIndex) && _generations[slotIndex] == generation)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the element at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to retrieve. Must be within the valid range of slots.</param>
    /// <param name="generation">The generation identifier associated with the slot. Used to verify that the slot has not been replaced or
    /// invalidated.</param>
    /// <param name="value">When this method returns, contains the element at the specified slot and generation if found; otherwise, the
    /// default value for type <typeparamref name="T"/>.</param>
    /// <returns>true if the element at the specified slot index and generation is found; otherwise, false.</returns>
    public readonly bool TryGetElementAt(int slotIndex, int generation, out T value)
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
    /// Retrieves the element stored at the specified slot index and generation.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot from which to retrieve the element. Must be within the valid range of allocated slots.</param>
    /// <param name="generation">The generation identifier associated with the slot. Used to ensure the element has not been replaced or removed since allocation.</param>
    /// <returns>The element of type <see cref="T"/> stored at the specified slot and generation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slotIndex"/> is less than zero or greater than or equal to the capacity.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the specified slot is not occupied or the generation does not match.</exception>
    public readonly T GetElementAt(int slotIndex, int generation)
    {
        if (!Contains(slotIndex, generation))
        {
            throw new InvalidOperationException("The specified slot is not occupied or the generation does not match.");
        }

        return _data[slotIndex];
    }

    /// <summary>
    /// Returns a reference to the element at the specified slot index and generation, if it exists; otherwise, returns
    /// a null reference.
    /// </summary>
    /// <param name="slotIndex">The zero-based index of the slot to retrieve. Must be within the valid range of allocated slots.</param>
    /// <param name="generation">The expected generation value for the slot. Used to verify that the slot has not been recycled or replaced.</param>
    /// <param name="exist">When this method returns, contains <see langword="true"/> if a valid element exists at the specified slot and generation; otherwise, <see langword="false"/>.</param>
    /// <returns>A reference to the element of type <typeparamref name="T"/> at the specified slot and generation if it exists; otherwise, a null reference.</returns>
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

    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        _data.Resize(newSize, option);
        _generations.Resize(newSize, option | AllocationOption.Clear);
        _freeSlots.Resize(newSize, option);
        _validBits.Resize(newSize, option);

        _capacity = newSize;
    }

    public void Clear()
    {
        _generations.Clear();
        _freeSlots.Clear();
        _validBits.ClearAll();

        _count = 0;

        Add(default, out _);
    }

    public readonly void* GetUnsafePtr()
    {
        return _data.GetUnsafePtr();
    }

    public void Dispose()
    {
        _data.Dispose();
        _generations.Dispose();
        _freeSlots.Dispose();
        _validBits.Dispose();

        _count = 0;
        _capacity = 0;
    }
}
