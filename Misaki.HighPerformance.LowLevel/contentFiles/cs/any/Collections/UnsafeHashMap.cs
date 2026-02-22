using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnsafeHashMap<TKey, TValue> : IUnsafeHashCollection<KeyValuePair<TKey, TValue>>
    where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
{
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        internal HashMapHelper<TKey>.Enumerator _enumerator;

        public KeyValuePair<TKey, TValue> Current => _enumerator.GetCurrent<TValue>();
        object IEnumerator.Current => Current;

        public Enumerator(HashMapHelper<TKey>* data)
        {
            _enumerator = new HashMapHelper<TKey>.Enumerator(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

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

    public Enumerator GetEnumerator() => new((HashMapHelper<TKey>*)UnsafeUtility.AddressOf(ref this));
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Invalid constructor, use <see cref="UnsafeHashMap(int, Allocator, AllocationOption)"/> or <see cref="UnsafeHashMap(int, ref AllocationHandle, AllocationOption)"/> instead.
    /// </summary>
    public UnsafeHashMap()
        : this(0, Allocator.Invalid)
    {
    }

    public UnsafeHashMap(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<TKey>(capacity, sizeof(TValue), (int)AlignOf<TValue>(), HashMapHelper<TKey>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    public UnsafeHashMap(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Adds a new key-value pair.
    /// </summary>
    /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
    /// <param name="key">The key to add.</param>
    /// <param name="item">The value to add.</param>
    /// <returns>True if the key-value pair was added.</returns>
    public bool TryAdd(in TKey key, TValue item)
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
    public void Add(in TKey key, TValue item)
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
    public bool Remove(in TKey key)
    {
        return -1 != _helper.TryRemove(key);
    }

    /// <summary>
    /// Returns the value associated with a key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
    /// <returns>True if the key was present.</returns>
    public bool TryGetValue(in TKey key, out TValue item)
    {
        return _helper.TryGetValue(key, out item);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key, or returns a default value if the key is not found.
    /// </summary>
    /// <param name="key">The key whose value to retrieve.</param>
    /// <param name="defaultValue">The value to return if the specified key does not exist. If not specified, the default value for the type is used.</param>
    /// <returns>The value associated with the specified key if the key is found; otherwise, the specified default value.</returns>
    public TValue GetValueOrDefault(in TKey key, TValue defaultValue = default)
    {
        if (_helper.TryGetValue<TValue>(key, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public ref TValue GetValueRef(in TKey key, out bool exists)
    {
        return ref _helper.GetValueRef<TValue>(key, out exists);
    }

    /// <summary>
    /// Returns true if a given key is present in this hash map.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>True if the key was present.</returns>
    public bool ContainsKey(in TKey key)
    {
        return -1 != _helper.Find(key);
    }

    /// <summary>
    /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
    /// </summary>
    public void TrimExcess() => _helper.TrimExcess();

    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        _helper.Resize(newSize);
    }

    public void Clear()
    {
        _helper.Clear();
    }

    /// <summary>
    /// Retrieves an array of keys from the hash map.
    /// </summary>
    /// <returns>An array containing the keys stored in the hash map.</returns>
    public UnsafeArray<TKey> GetKeyArray(Allocator allocator) => _helper.GetKeyArray(allocator);

    /// <summary>
    /// Retrieves an array of values from the underlying hash map.
    /// </summary>
    /// <returns>An UnsafeArray containing the values stored in the hash map.</returns>
    public UnsafeArray<TValue> GetValueArray(Allocator allocator) => _helper.GetValueArray<TValue>(allocator);

    /// <summary>
    /// Retrieves an array of key-value pairs from the hash map. The keys are of type TKey and the values are of type
    /// TValue.
    /// </summary>
    /// <returns>Returns an UnsafeArray containing KeyValuePair objects.</returns>
    public UnsafeArray<KeyValuePair<TKey, TValue>> GetKeyValueArrays(Allocator allocator) => _helper.GetKeyValueArrays<TValue>(allocator);

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
