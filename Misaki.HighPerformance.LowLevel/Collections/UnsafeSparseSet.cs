using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Helpers;
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
    public struct Enumerator : IEnumerator<T>
    {
        private UnsafeSparseSet<T>* _collection;
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
            if (_index < _collection->_count)
            {
                _value = UnsafeUtilities.ReadArrayElement<T>(_collection->_dense.GetUnsafePtr(), _index);
                return true;
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

    public struct ParallelWriter
    {
        private UnsafeSparseSet<T>* _sparseSet;

        internal ParallelWriter(UnsafeSparseSet<T>* sparseSet)
        {
            _sparseSet = sparseSet;
        }

        /// <summary>
        /// Adds a value to the sparse set without resizing the internal arrays.
        /// </summary>
        /// <param name="value">The value to add to the sparse set.</param>
        /// <returns> Returns the sparse index assigned to the value. -1 if the sparse index is out of bounds.</returns>
        public int AddNoResize(T value)
        {
            int sparseIndex;

            if (_sparseSet->_freeCount > 0)
            {
                var index = Interlocked.Decrement(ref _sparseSet->_freeCount);
                sparseIndex = _sparseSet->_freeList[index];
            }
            else
            {
                sparseIndex = Interlocked.Increment(ref _sparseSet->_nextId) - 1;
            }

            if (sparseIndex >= _sparseSet->_sparse.Count)
            {
                return -1;
            }

            var count = Interlocked.Increment(ref _sparseSet->_count) - 1;

            _sparseSet->_dense[count] = value;
            _sparseSet->_sparse[sparseIndex] = count;
            _sparseSet->_reverse[count] = sparseIndex;


            return sparseIndex;
        }

        /// <summary>
        /// Attempts to add a value at the specified sparse index without resizing the underlying collection.
        /// </summary>
        /// <param name="sparseIndex">The index in the sparse array where the value should be added. Must be within the valid range of the sparse
        /// array.</param>
        /// <param name="value">The value to add to the collection.</param>
        /// <returns><see langword="true"/> if the value was successfully added at the specified index; otherwise, <see
        /// langword="false"/>.</returns>
        public bool AddAtNoResize(int sparseIndex, T value)
        {
            if (sparseIndex < 0 || sparseIndex >= _sparseSet->_sparse.Count)
            {
                return false;
            }

            if (_sparseSet->Contains(sparseIndex))
            {
                return false;
            }

            if (_sparseSet->_count >= _sparseSet->_dense.Count)
            {
                return false;
            }

            var count = Interlocked.Increment(ref _sparseSet->_count) - 1;

            _sparseSet->_dense[count] = value;
            _sparseSet->_sparse[sparseIndex] = count;
            _sparseSet->_reverse[count] = sparseIndex;

            return true;
        }

        /// <summary>
        /// Removes a value at the specified sparse index without resizing the internal arrays.
        /// </summary>
        /// <param name="sparseIndex">The sparse index of the value to remove.</param>
        /// <returns>Returns <see langword="true"/> if the value was successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveNoResize(int sparseIndex)
        {
            if (!_sparseSet->Contains(sparseIndex))
            {
                return false;
            }

            var denseIndex = _sparseSet->_sparse[sparseIndex];
            var lastIndex = _sparseSet->_count - 1;

            if (denseIndex != lastIndex)
            {
                var lastValue = _sparseSet->_dense[lastIndex];
                var lastSparseIndex = _sparseSet->_reverse[lastIndex];
                _sparseSet->_dense[denseIndex] = lastValue;
                _sparseSet->_reverse[denseIndex] = lastSparseIndex;
                _sparseSet->_sparse[lastSparseIndex] = denseIndex;
            }

            _sparseSet->_sparse[sparseIndex] = -1;
            if (_sparseSet->_freeCount >= _sparseSet->_freeList.Count)
            {
                return false;
            }

            _sparseSet->_freeList[_sparseSet->_freeCount] = sparseIndex;

            Interlocked.Increment(ref _sparseSet->_freeCount);
            Interlocked.Decrement(ref _sparseSet->_count);

            return true;
        }
    }

    private UnsafeArray<T> _dense;
    private UnsafeArray<int> _sparse;
    private UnsafeArray<int> _reverse; // Maps dense index to sparse index
    private UnsafeArray<int> _freeList; // Stack of available sparse indices
    private int _count;
    private int _nextId; // Next available sparse index
    private int _freeCount; // Number of items in the free list

    public readonly int Count => _count;
    public readonly int Capacity => _dense.Count;
    public readonly bool IsCreated => _dense.IsCreated && _sparse.IsCreated && _reverse.IsCreated && _freeList.IsCreated;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _dense[index];
    }

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

        _dense = new UnsafeArray<T>(capacity, ref handle, allocationOption);
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
    /// <returns>A unique sparse index that can be used to reference this value.</returns>
    public int Add(T value)
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
        _dense[_count] = value;
        _sparse[sparseIndex] = _count;
        _reverse[_count] = sparseIndex;
        _count++;

        return sparseIndex;
    }

    /// <summary>
    /// Adds a value to the sparse set at the specified sparse index.
    /// This method is provided for compatibility when you need to specify the exact sparse index.
    /// </summary>
    /// <param name="sparseIndex">The index in the sparse array where the value should be mapped.</param>
    /// <param name="value">The value to add to the sparse set.</param>
    /// <returns>True if the value was added, false if the sparse index is already occupied.</returns>
    public bool AddAt(int sparseIndex, T value)
    {
        if (sparseIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sparseIndex), "Sparse index must be non-negative.");
        }

        if (sparseIndex >= _sparse.Count)
        {
            ResizeSparse(sparseIndex + 1);
        }

        if (Contains(sparseIndex))
        {
            return false;
        }

        if (_count >= _dense.Count)
        {
            var newCapacity = _dense.Count + Math.Max(1, _dense.Count / 2);
            _dense.Resize(newCapacity);
            _reverse.Resize(newCapacity);
        }

        // Add the value to the dense array and update mappings
        _dense[_count] = value;
        _sparse[sparseIndex] = _count;
        _reverse[_count] = sparseIndex; // Store reverse mapping
        _count++;

        // Update _nextId if we're using a higher ID
        if (sparseIndex >= _nextId)
        {
            _nextId = sparseIndex + 1;
        }

        return true;
    }

    /// <summary>
    /// Removes the value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index of the value to remove.</param>
    /// <returns>True if the value was removed, false if the sparse index was not found.</returns>
    public bool Remove(int sparseIndex)
    {
        if (!Contains(sparseIndex))
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
    /// <returns>True if the sparse index is valid and contains a value, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(int sparseIndex)
    {
        if (sparseIndex < 0 || sparseIndex >= _sparse.Count)
        {
            return false;
        }

        var denseIndex = _sparse[sparseIndex];
        return denseIndex >= 0 && denseIndex < _count;
    }

    /// <summary>
    /// Gets the value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <param name="value">When this method returns, contains the value at the specified sparse index, if found.</param>
    /// <returns>True if the sparse index contains a value, false otherwise.</returns>
    public readonly bool TryGetValue(int sparseIndex, out T value)
    {
        if (Contains(sparseIndex))
        {
            var denseIndex = _sparse[sparseIndex];
            value = _dense[denseIndex];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index to retrieve the value from.</param>
    /// <returns>The value at the specified sparse index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the sparse index is not found.</exception>
    public readonly T GetValue(int sparseIndex)
    {
        if (!Contains(sparseIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(sparseIndex), "Sparse index not found in the set.");
        }

        var denseIndex = _sparse[sparseIndex];
        return _dense[denseIndex];
    }

    /// <summary>
    /// Updates the value at the specified sparse index.
    /// </summary>
    /// <param name="sparseIndex">The sparse index of the value to update.</param>
    /// <param name="value">The new value.</param>
    /// <returns>True if the value was updated, false if the sparse index was not found.</returns>
    public bool SetValue(int sparseIndex, T value)
    {
        if (!Contains(sparseIndex))
        {
            return false;
        }

        var denseIndex = _sparse[sparseIndex];
        _dense[denseIndex] = value;
        return true;
    }

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