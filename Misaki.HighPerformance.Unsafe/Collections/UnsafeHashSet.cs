using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Unsafe.Collections;

/// <summary>
/// A collection that provides fast, unsafe operations for managing a set of unmanaged types. It supports adding,
/// removing, and checking for values.
/// </summary>
/// <typeparam name="T">Represents an unmanaged type that can be compared for equality.</typeparam>
public unsafe struct UnsafeHashSet<T> : IUnsafeCollection<T>, IEnumerable<T>
    where T : unmanaged, IEquatable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        internal HashMapHelper<T>.Enumerator _enumerator;

        public Enumerator(HashMapHelper<T>* hashMap)
        {
            _enumerator = new HashMapHelper<T>.Enumerator(hashMap);
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _enumerator.buffer->_keys[_enumerator.index];
        }

        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public readonly void Dispose()
        {
        }
    }

    private HashMapHelper<T> _hashMap;

    public readonly int Count => _hashMap.Count;
    public readonly int Capacity => _hashMap.Capacity;
    public readonly bool IsCreated => _hashMap.IsCreated;

    public IEnumerator<T> GetEnumerator() => new Enumerator((HashMapHelper<T>*)UnsafeUtilities.AddressOf(ref _hashMap));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public UnsafeHashSet(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
    {
        _hashMap = new HashMapHelper<T>(capacity, 0, HashMapHelper<T>.MINIMAL_CAPACITY, allocator, allocationOption);
    }

    /// <summary>
    /// Adds a new value (unless it is already present).
    /// </summary>
    /// <param name="item">The value to add.</param>
    /// <returns>True if the value was not already present.</returns>
    public bool Add(T item)
    {
        return -1 != _hashMap.TryAdd(item);
    }

    /// <summary>
    /// Removes a particular value.
    /// </summary>
    /// <param name="item">The value to remove.</param>
    /// <returns>True if the value was present.</returns>
    public bool Remove(T item)
    {
        return -1 != _hashMap.TryRemove(item);
    }

    /// <summary>
    /// Returns true if a particular value is present.
    /// </summary>
    /// <param name="item">The value to check for.</param>
    /// <returns>True if the value was present.</returns>
    public bool Contains(T item)
    {
        return -1 != _hashMap.Find(item);
    }

    /// <summary>
    /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
    /// </summary>
    public void TrimExcess() => _hashMap.TrimExcess();

    /// <summary>
    /// Returns an array with a copy of this set's values (in no particular order).
    /// </summary>
    /// <param name="allocator">The allocator to use.</param>
    /// <returns>An array with a copy of the set's values.</returns>
    public UnsafeArray<T> ToNativeArray(Allocator allocator)
    {
        return _hashMap.GetKeyArray(allocator);
    }

    public void Resize(int newSize)
    {
        _hashMap.Resize(newSize);
    }

    public void Clear()
    {
        _hashMap.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _hashMap.Buffer;
    }

    public void Dispose()
    {
        _hashMap.Dispose();
    }
}
