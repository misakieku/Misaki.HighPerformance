using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

internal sealed class UnsafeHashSetDebugView<T>
    where T : unmanaged, IEquatable<T>
{
    private readonly UnsafeHashSet<T> _hashSet;
    public UnsafeHashSetDebugView(UnsafeHashSet<T> hashSet)
    {
        _hashSet = hashSet;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var array = new T[_hashSet.Count];
            var index = 0;
            foreach (var item in _hashSet)
            {
                array[index++] = item;
            }

            return array;
        }
    }
}

/// <summary>
/// A collection that provides fast, unsafe operations for managing a set of unmanaged types. It supports adding,
/// removing, and checking for values.
/// </summary>
/// <typeparam name="T">Represents an unmanaged type that can be compared for equality.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeHashSetDebugView<>))]
public unsafe struct UnsafeHashSet<T> : IUnsafeHashCollection<T>
    where T : unmanaged, IEquatable<T>
{
    public ref struct Enumerator
    {
        internal HashMapHelper<T>.Enumerator _enumerator;

        public readonly T Current => _enumerator.helper._keys[_enumerator.index];

        public Enumerator(ref HashMapHelper<T> hashMap)
        {
            _enumerator = new HashMapHelper<T>.Enumerator(ref hashMap);
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
    }

    private HashMapHelper<T> _helper;

    public readonly int Count => _helper.Count;
    public readonly int Capacity => _helper.Capacity;
    public readonly bool IsCreated => _helper.IsCreated;

    /// <summary>
    /// Initializes a new instance of UnsafeHashSet with a default size of 1 and a persistent allocation handle.
    /// </summary>
    public UnsafeHashSet()
        : this(1, AllocationHandle.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeHashSet with the specified initial capacity and allocation handle.
    /// </summary>
    /// <param name="capacity">The initial capacity of the set.</param>
    /// <param name="handle">The allocation handle to use for managing the set's memory.</param>
    /// <param name="allocationOption">The allocation options for the set.</param>
    public UnsafeHashSet(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<T>(capacity, 0, 0, HashMapHelper<T>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref _helper);
    }

    /// <summary>
    /// Adds a new value (unless it is already present).
    /// </summary>
    /// <param name="item">The value to add.</param>
    /// <returns>True if the value was not already present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(scoped in T item)
    {
        return -1 != _helper.TryAdd(item);
    }

    /// <summary>
    /// Removes a particular value.
    /// </summary>
    /// <param name="item">The value to remove.</param>
    /// <returns>True if the value was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(scoped in T item)
    {
        return -1 != _helper.TryRemove(item);
    }

    /// <summary>
    /// Returns true if a particular value is present.
    /// </summary>
    /// <param name="item">The value to check for.</param>
    /// <returns>True if the value was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(scoped in T item)
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
    /// <param name="allocationHandle">The allocation handle to use to allocate the array.</param>
    /// <returns>An array with a copy of the set's values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<T> ToUnsafeArray(AllocationHandle allocationHandle)
    {
        return _helper.GetKeyArray(allocationHandle);
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

    public HashSet<T> ToHashSet()
    {
        var hashSet = new HashSet<T>();

        if (!IsCreated)
        {
            return hashSet;
        }

        foreach (var item in this)
        {
            hashSet.Add(item);
        }

        return hashSet;
    }

    public void Dispose()
    {
        _helper.Dispose();
    }
}
