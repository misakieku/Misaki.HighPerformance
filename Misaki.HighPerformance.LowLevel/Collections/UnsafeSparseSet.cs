using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A sparse set data structure that provides O(1) insertion, deletion, and lookup operations.
/// The sparse set uses three arrays: a dense array for storing values, a sparse array for mapping indices,
/// and a reverse array for mapping dense indices back to sparse indices.
/// Sparse indices work like entity IDs and are automatically generated.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in the sparse set, constrained to unmanaged types for performance and safety.</typeparam>
public unsafe struct UnsafeSparseSet<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    private struct SlotEntry
    {
        public T value;
        public int generation;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeSparseSet<T>* _collection;
        private int _index;
        private T _value;

        public Enumerator(UnsafeSparseSet<T>* collection)
        {
            _collection = collection;
            _index = -1;
            _value = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            if (_index < _collection->_sparse.Count)
            {
                var index = _collection->_sparse[_index];
                if (index >= 0 && index < _collection->_count)
                {
                    _value = _collection->_dense[index].value;
                    return true;
                }
            }

            _value = default;
            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }

        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }

        public unsafe readonly void Dispose()
        {
        }
    }

    private UnsafeArray<SlotEntry> _dense;
    private UnsafeArray<int> _sparse;
    private UnsafeArray<int> _reverse; // Maps dense index to sparse index
    private UnsafeArray<int> _freeList; // Stack of available sparse indices
    private int _count;
    private int _nextId; // Next available sparse index
    private int _freeCount; // Number of items in the free list

    public readonly int Count => _count;
    public readonly int Capacity => _dense.Count;
    public readonly bool IsCreated => _dense.IsCreated && _sparse.IsCreated && _reverse.IsCreated && _freeList.IsCreated;

    public IEnumerator<T> GetEnumerator() => new Enumerator((UnsafeSparseSet<T>*)UnsafeUtilities.AddressOf(ref this));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Constructs an UnsafeSparseSet with a default size of 1 and uses the Persistent allocator.
    /// </summary>
    public UnsafeSparseSet()
        : this(1, Allocator.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeSparseSet with a specified capacity and an allocation handle.
    /// </summary>
    /// <param name="capacity">Specifies the initial capacity of the sparse set, which must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle that manages the memory allocation for the sparse set.</param>
    /// <param name="allocationOption">Specifies how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified capacity is less than or equal to zero.</exception>
    public UnsafeSparseSet(int capacity, ref AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        _dense = new UnsafeArray<SlotEntry>(capacity, ref handle, allocationOption);
        _sparse = new UnsafeArray<int>(capacity, ref handle, allocationOption);
        _reverse = new UnsafeArray<int>(capacity, ref handle, allocationOption);
        _freeList = new UnsafeArray<int>(capacity, ref handle, allocationOption);
        _count = 0;
        _nextId = 0;
        _freeCount = 0;

        Clear();
    }

    /// <summary>
    /// Initializes a new instance of UnsafeSparseSet with a specified capacity and an allocation type.
    /// </summary>
    /// <param name="capacity">Specifies the initial capacity of the sparse set, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified capacity is less than or equal to zero.</exception>
    public UnsafeSparseSet(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, ref AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Adds a value to the sparse set and returns a unique sparse index for the value.
    /// </summary>
    /// <param name="value">The value to add to the sparse set.</param>
    /// <param name="generation">Outputs the generation number associated with the added value.</param>
    /// <returns>A unique sparse index that can be used to reference this value.</returns>
    public int Add(T value, out int generation)
    {
        int sparseIndex;

        // Try to reuse a freed sparse index first
        if (_freeCount > 0)
        {
            _freeCount--;
            sparseIndex = _freeList[_freeCount];
        }
        else
        {
            // Use the next available ID
            sparseIndex = _nextId++;

            // Resize sparse array if necessary
            if (sparseIndex >= _sparse.Count)
            {
                ResizeSparse(sparseIndex + 1);
            }
        }

        // Resize dense arrays if necessary
        if (_count >= _dense.Count)
        {
            var newCapacity = _dense.Count + Math.Max(1, _dense.Count / 2);
            _dense.Resize(newCapacity);
            _reverse.Resize(newCapacity);
        }

        // Add the value to the dense array and update mappings
        ref var entry = ref _dense[_count];
        entry.value = value;

        _sparse[sparseIndex] = _count;
        _reverse[_count] = sparseIndex;
        _count++;

        generation = entry.generation;

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
        _dense[lastIndex].generation++; // Increment generation to invalidate old references

        // Add the freed sparse index to the free list for reuse
        if (_freeCount >= _freeList.Count)
        {
            _freeList.Resize(_freeList.Count + Math.Max(1, _freeList.Count / 2));
        }

        _freeList[_freeCount] = sparseIndex;
        _freeCount++;

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
    public readonly bool Contains(int sparseIndex, int generation)
    {
        if (sparseIndex < 0 || sparseIndex >= _sparse.Count)
        {
            return false;
        }

        var denseIndex = _sparse[sparseIndex];
        return denseIndex >= 0 && denseIndex < _count && _dense[denseIndex].generation == generation;
    }

    /// <summary>
    /// Gets the value at the specified sparse index and generation.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <param name="value">When this method returns, contains the value at the specified sparse index, if found.</param>
    /// <returns>True if the sparse index contains a value, false otherwise.</returns>
    public readonly bool TryGetValue(int sparseIndex, int generation, out T value)
    {
        if (Contains(sparseIndex, generation))
        {
            var denseIndex = _sparse[sparseIndex];
            ref var entry = ref _dense[denseIndex];

            value = entry.value;
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
    public readonly T GetValue(int sparseIndex, int generation)
    {
        if (!Contains(sparseIndex, generation))
        {
            throw new ArgumentOutOfRangeException(nameof(sparseIndex), "Sparse index and feneration not found in the set.");
        }

        var denseIndex = _sparse[sparseIndex];
        ref var entry = ref _dense[denseIndex];

        return entry.value;
    }

    /// <summary>
    /// Gets reference of the value at the specified sparse index and generation.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <param name="generation">The generation number to validate against the stored generation.</param>
    /// <param name="exist">Outputs whether the sparse index exists in the set.</param>
    /// <returns>Reference of the value at the specified sparse index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the sparse index is not found.</exception>
    public readonly ref T GetValueReference(int sparseIndex, int generation, out bool exist)
    {
        if (!Contains(sparseIndex, generation))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        var denseIndex = _sparse[sparseIndex];
        ref var entry = ref _dense[denseIndex];

        exist = true;

        return ref entry.value;
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

        var denseIndex = _sparse[sparseIndex];
        ref var entry = ref _dense[denseIndex];
        entry.value = value;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResizeSparse(int newSize)
    {
        var oldSize = _sparse.Count;
        _sparse.Resize(newSize);
        _sparse.AsSpan()[oldSize..newSize].Fill(-1);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (!IsCreated)
        {
            return;
        }

        _sparse.AsSpan().Fill(-1);
        _count = 0;
        _nextId = 0;
        _freeCount = 0;
    }

    /// <inheritdoc/>
    public void Resize(int newSize)
    {
        if (newSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), "New size must be greater than zero.");
        }

        _dense.Resize(newSize);
        _reverse.Resize(newSize);
        _freeList.Resize(newSize);

        if (newSize > _sparse.Count)
        {
            ResizeSparse(newSize);
        }

        if (_count > newSize)
        {
            _count = newSize;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _dense.GetUnsafePtr();
    }

    /// <summary>
    /// Converts the current sparse set to an UnsafeArray representation using its dense array.
    /// </summary>
    /// <returns>Returns a new UnsafeArray instance initialized with the dense array's pointer and count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UnsafeArray<T> AsUnsafeArray()
    {
        return new UnsafeArray<T>((T*)_dense.GetUnsafePtr(), _count);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _dense.Dispose();
        _sparse.Dispose();
        _reverse.Dispose();
        _freeList.Dispose();
        _count = 0;
        _nextId = 0;
        _freeCount = 0;
    }
}