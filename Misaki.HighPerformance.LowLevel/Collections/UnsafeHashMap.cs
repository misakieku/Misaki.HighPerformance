using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnsafeHashMap<TKey, TValue> : IUnsafeHashCollection<KeyValuePair<TKey, TValue>>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    public ref struct Enumerator
    {
        internal HashMapHelper<TKey>.Enumerator _enumerator;

        public KeyValuePair<TKey, TValue> Current => _enumerator.GetCurrent<TValue>();

        public Enumerator(ref HashMapHelper<TKey> data)
        {
            _enumerator = new HashMapHelper<TKey>.Enumerator(ref data);
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

        public void Dispose()
        {
        }
    }

    private HashMapHelper<TKey> _helper;

    public readonly int Count => _helper.Count;
    public readonly int Capacity => _helper.Capacity;
    public readonly bool IsCreated => _helper.IsCreated;

    /// <summary>
    /// Gets and sets values by key.
    /// </summary>
    /// <remarks>Getting a key that is not present will throw. Setting a key that is not already present will add the key.</remarks>
    /// <param name="key">The key to look up.</param>
    /// <value>The value associated with the key.</value>
    /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
    public TValue this[TKey key]
    {
        get
        {
            if (!_helper.TryGetValue<TValue>(key, out var result))
            {
                throw new ArgumentException($"Key: {key} is not present.");
            }

            return result;
        }
        set
        {
            var idx = _helper.Find(key);
            if (-1 != idx)
            {
                UnsafeUtility.WriteArrayElement(_helper.Buffer, idx, value);
                return;
            }

            TryAdd(key, value);
        }
    }

    /// <summary>
    /// Initializes a new instance of UnsafeHashMap with a default size of 1 and a persistent allocation handle.
    /// </summary>
    public UnsafeHashMap()
        : this(1, AllocationHandle.Persistent)
    {
    }

    public UnsafeHashMap(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<TKey>(capacity, sizeof(TValue), (int)MemoryUtility.AlignOf<TValue>(), HashMapHelper<TKey>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeHashMap(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref _helper);
    }

    /// <summary>
    /// Adds a new key-value pair.
    /// </summary>
    /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
    /// <param name="key">The key to add.</param>
    /// <param name="item">The value to add.</param>
    /// <returns>True if the key-value pair was added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(scoped in TKey key, TValue item)
    {
        var idx = _helper.TryAdd(key);
        if (idx != -1)
        {
            UnsafeUtility.WriteArrayElement(_helper.Buffer, idx, item);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds a new key-value pair.
    /// </summary>
    /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
    /// <param name="key">The key to add.</param>
    /// <param name="item">The value to add.</param>
    /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(scoped in TKey key, TValue item)
    {
        var result = TryAdd(key, item);
        if (!result)
        {
            throw new ArgumentException($"An item with the same key has already been added: {key}");
        }
    }

    /// <summary>
    /// Removes a particular key and its value.
    /// </summary>
    /// <param name="item">The value to remove.</param>
    /// <returns>True if the value was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(scoped in TKey key)
    {
        return -1 != _helper.TryRemove(key);
    }

    /// <summary>
    /// Returns the value associated with a key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
    /// <returns>True if the key was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(scoped in TKey key, out TValue item)
    {
        return _helper.TryGetValue(key, out item);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key, or returns a default value if the key is not found.
    /// </summary>
    /// <param name="key">The key whose value to retrieve.</param>
    /// <param name="defaultValue">The value to return if the specified key does not exist. If not specified, the default value for the type is used.</param>
    /// <returns>The value associated with the specified key if the key is found; otherwise, the specified default value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetValueOrDefault(scoped in TKey key, TValue defaultValue = default)
    {
        if (_helper.TryGetValue<TValue>(key, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Returns a reference to the value associated with the specified key, and a boolean indicating whether the key exists in the hash map.
    /// </summary>
    /// <param name="key">The key whose value to retrieve.</param>
    /// <param name="exists">Outputs true if the key exists in the hash map; otherwise, false.</param>
    /// <returns>A reference to the value associated with the specified key.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetValueRef(scoped in TKey key, out bool exists)
    {
        return ref _helper.GetValueRef<TValue>(key, out exists);
    }

    /// <summary>
    /// Returns a reference to the value associated with the specified key if it exists; otherwise, adds a new key with a default value and returns a reference to that value. The method also outputs a boolean indicating whether the key already existed in the hash map.
    /// </summary>
    /// <param name="key">The key whose value to retrieve or add.</param>
    /// <param name="exists">Outputs true if the key already existed in the hash map; otherwise, false.</param>
    /// <returns>A reference to the value associated with the specified key.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetValueRefOrAddDefault(scoped in TKey key, out bool exists)
    {
        return ref _helper.GetValueRefOrAddDefault<TValue>(key, out exists);
    }

    /// <summary>
    /// Returns true if a given key is present in this hash map.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>True if the key was present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(scoped in TKey key)
    {
        return -1 != _helper.Find(key);
    }

    /// <summary>
    /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrimExcess()
    {
        _helper.TrimExcess();
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

    /// <summary>
    /// Retrieves an array of keys from the hash map.
    /// </summary>
    /// <param name="allocationHandle">The allocation handle to use to allocate the array.</param>
    /// <returns>An array containing the keys stored in the hash map.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<TKey> GetKeyArray(AllocationHandle allocationHandle)
    {
        return _helper.GetKeyArray(allocationHandle);
    }

    /// <summary>
    /// Retrieves an array of values from the underlying hash map.
    /// </summary>
    /// <param name="allocationHandle">The allocation handle to use to allocate the array.</param>
    /// <returns>An UnsafeArray containing the values stored in the hash map.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<TValue> GetValueArray(AllocationHandle allocationHandle)
    {
        return _helper.GetValueArray<TValue>(allocationHandle);
    }

    /// <summary>
    /// Retrieves an array of key-value pairs from the hash map. The keys are of type TKey and the values are of type TValue.
    /// </summary>
    /// <param name="allocationHandle">The allocation handle to use to allocate the array.</param>
    /// <returns>Returns an UnsafeArray containing KeyValuePair objects.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<KeyValuePair<TKey, TValue>> GetKeyValueArrays(AllocationHandle allocationHandle)
    {
        return _helper.GetKeyValueArrays<TValue>(allocationHandle);
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
