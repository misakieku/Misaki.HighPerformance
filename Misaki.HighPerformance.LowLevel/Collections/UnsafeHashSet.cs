using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A collection that provides fast, unsafe operations for managing a set of unmanaged types. It supports adding,
/// removing, and checking for values.
/// </summary>
/// <typeparam name="T">Represents an unmanaged type that can be compared for equality.</typeparam>
public unsafe struct UnsafeHashSet<T> : IUnsafeHashCollection<T>, IEnumerable<T>
    where T : unmanaged, IEquatable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        internal HashMapHelper<T>.Enumerator _enumerator;

        public readonly T Current => _enumerator.buffer->_keys[_enumerator.index];
        readonly object IEnumerator.Current => Current;

        public Enumerator(HashMapHelper<T>* hashMap)
        {
            _enumerator = new HashMapHelper<T>.Enumerator(hashMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public readonly void Dispose()
        {
        }
    }

    private HashMapHelper<T> _helper;

    public readonly int Count => _helper.Count;
    public readonly int Capacity => _helper.Capacity;
    public readonly bool IsCreated => _helper.IsCreated;

    public Enumerator GetEnumerator()
    {
        return new((HashMapHelper<T>*)UnsafeUtility.AddressOf(ref this));
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Invalid constructor. Use <see cref="UnsafeHashSet(int, Allocator, AllocationOption)"/> or <see cref="UnsafeHashSet(int, AllocationHandle, AllocationOption)"/> instead."/>
    /// </summary>
    public UnsafeHashSet()
        : this(0, Allocator.Invalid)
    {
    }

    public UnsafeHashSet(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<T>(capacity, 0, 0, HashMapHelper<T>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    public UnsafeHashSet(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Adds a new value (unless it is already present).
    /// </summary>
    /// <param name="item">The value to add.</param>
    /// <returns>True if the value was not already present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T item)
    {
        return -1 != _helper.TryAdd(item);
    }

    /// <summary>
    /// Removes a particular value.
    /// </summary>
    /// <param name="item">The value to remove.</param>
    /// <returns>True if the value was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
    {
        return -1 != _helper.TryRemove(item);
    }

    /// <summary>
    /// Returns true if a particular value is present.
    /// </summary>
    /// <param name="item">The value to check for.</param>
    /// <returns>True if the value was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        return -1 != _helper.Find(item);
    }

    /// <summary>
    /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrimExcess()
    {
        _helper.TrimExcess();
    }

    /// <summary>
    /// Returns an array with a copy of this set's values (in no particular order).
    /// </summary>
    /// <param name="allocator">The allocator to use.</param>
    /// <returns>An array with a copy of the set's values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<T> ToNativeArray(Allocator allocator)
    {
        return _helper.GetKeyArray(allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        _helper.Resize(newSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _helper.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _helper.Buffer;
    }

    public void Dispose()
    {
        _helper.Dispose();
    }
}
