using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnsafeMultiHashMap<TKey, TValue> : IUnsafeHashCollection<KeyValuePair<TKey, TValue>>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    public ref struct Enumerator
    {
        internal HashMapHelper<TKey>.Enumerator _enumerator;

        public readonly KeyValueRefPair<TKey, TValue> Current => _enumerator.GetCurrent<TValue>();

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
    }

    public struct Iterator
    {
        internal TKey _key;
        internal int _entryIndex;

        internal Iterator(scoped in TKey key, int entryIndex)
        {
            _key = key;
            _entryIndex = entryIndex;
        }
    }

    public ref struct ValueEnumerable
    {
        private ref HashMapHelper<TKey> _helper;
        private readonly TKey _key;

        internal ValueEnumerable(ref HashMapHelper<TKey> data, scoped in TKey key)
        {
            _helper = ref data;
            _key = key;
        }

        public readonly ValueEnumerator GetEnumerator()
        {
            return new ValueEnumerator(ref _helper, _key);
        }
    }

    public ref struct ValueEnumerator
    {
        private ref HashMapHelper<TKey> helper;
        private readonly TKey _key;
        private int _entryIndex;
        private bool _started;

        public readonly ref TValue Current => ref UnsafeUtility.ReadArrayElementRef<TValue>(helper.Buffer, _entryIndex);

        internal ValueEnumerator(ref HashMapHelper<TKey> data, scoped in TKey key)
        {
            helper = ref data;
            _key = key;
            _entryIndex = -1;
            _started = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _entryIndex = helper.Find(_key);
                _started = true;
                return _entryIndex != -1;
            }

            if (_entryIndex == -1)
            {
                return false;
            }

            _entryIndex = helper.FindNext(_entryIndex, _key);
            return _entryIndex != -1;
        }

        public void Reset()
        {
            _entryIndex = -1;
            _started = false;
        }
    }

    private HashMapHelper<TKey> _helper;

    public readonly int Count => _helper.Count;
    public readonly int Capacity => _helper.Capacity;
    public readonly bool IsCreated => _helper.IsCreated;

    /// <summary>
    /// Initializes a new instance of UnsafeMultiHashMap with a default size of 1 and a persistent allocation handle.
    /// </summary>
    public UnsafeMultiHashMap()
        : this(1, AllocationHandle.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeMultiHashMap with the specified initial capacity and allocation handle.
    /// </summary>
    /// <param name="capacity">The initial capacity of the hash map.</param>
    /// <param name="handle">The allocation handle.</param>
    /// <param name="allocationOption">The allocation option.</param>
    public UnsafeMultiHashMap(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<TKey>(capacity, sizeof(TValue), (int)MemoryUtility.AlignOf<TValue>(), HashMapHelper<TKey>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref _helper);
    }

    /// <summary>
    /// Adds a key-value pair to the UnsafeMultiHashMap. If the key already exists, the new value will be added alongside the existing value(s) for that key, allowing multiple values to be associated with the same key.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="item">The value to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(scoped in TKey key, TValue item)
    {
        var idx = _helper.Add(key);
        UnsafeUtility.WriteArrayElement(_helper.Buffer, idx, item);
    }

    /// <summary>
    /// Removes all values associated with the specified key from the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="key">The key for which to remove values.</param>
    /// <returns><see cref="bool"/> indicating whether any values were removed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(scoped in TKey key)
    {
        return _helper.RemoveAll(key) != 0;
    }

    /// <summary>
    /// Tries to get the first value associated with the specified key.
    /// </summary>
    /// <param name="key">The key for which to get the first value.</param>
    /// <param name="item">When this method returns, contains the first value associated with the specified key, if the key is found; otherwise, the default value for the type of the item parameter.</param>
    /// <param name="iterator">When this method returns, contains the iterator for the first value associated with the specified key, if the key is found; otherwise, an invalid iterator.</param>
    /// <returns>true if the key was found and the first value was retrieved; otherwise, false.</returns>
    public bool TryGetFirstValue(scoped in TKey key, out TValue item, out Iterator iterator)
    {
        var entryIndex = _helper.Find(key);
        if (entryIndex == -1)
        {
            item = default;
            iterator = new(default, -1);
            return false;
        }

        item = UnsafeUtility.ReadArrayElement<TValue>(_helper.Buffer, entryIndex);
        iterator = new(key, entryIndex);
        return true;
    }

    /// <summary>
    /// Tries to get the next value associated with the key from the UnsafeMultiHashMap using the provided iterator.
    /// </summary>
    /// <param name="item">When this method returns, contains the next value associated with the specified key, if the key is found; otherwise, the default value for the type of the item parameter.</param>
    /// <param name="iterator">The iterator to use for finding the next value.</param>
    /// <returns>true if a value was found for the specified key; otherwise, false.</returns>
    public bool TryGetNextValue(out TValue item, ref Iterator iterator)
    {
        if (iterator._entryIndex == -1)
        {
            item = default;
            return false;
        }

        var entryIndex = _helper.FindNext(iterator._entryIndex, iterator._key);
        if (entryIndex == -1)
        {
            item = default;
            iterator._entryIndex = -1;
            return false;
        }

        iterator._entryIndex = entryIndex;
        item = UnsafeUtility.ReadArrayElement<TValue>(_helper.Buffer, entryIndex);
        return true;
    }

    /// <summary>
    /// Tries to get the first value associated with the specified key.
    /// </summary>
    /// <param name="key">The key for which to get the first value.</param>
    /// <param name="item">When this method returns, contains the first value associated with the specified key, if the key is found; otherwise, the default value for the type of the item parameter.</param>
    /// <returns>true if the key was found and the first value was retrieved; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(scoped in TKey key, out TValue item)
    {
        return _helper.TryGetValue(key, out item);
    }

    /// <summary>
    /// Gets the first value associated with the specified key, or returns a default value if the key is not found in the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="key">The key for which to get the value.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The first value associated with the specified key, or the default value if the key is not found.</returns>
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
    /// Gets an enumerable of all values associated with the specified key in the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="key">The key for which to get the values.</param>
    /// <returns>An enumerable of all values associated with the specified key.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ValueEnumerable GetValuesForKey(scoped in TKey key)
    {
        return new ValueEnumerable(ref _helper, key);
    }

    /// <summary>
    /// Counts the number of values associated with the specified key in the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="key">The key for which to count values.</param>
    /// <returns>The number of values associated with the specified key.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountValuesForKey(scoped in TKey key)
    {
        return _helper.CountValuesForKey(key);
    }

    /// <summary>
    /// Checks if the UnsafeMultiHashMap contains at least one value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns>true if the key is found; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(scoped in TKey key)
    {
        return _helper.Find(key) != -1;
    }

    /// <summary>
    /// Trim the excess capacity of the UnsafeMultiHashMap, reducing the capacity to match the current count of key-value pairs.
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
    /// Gets an unsafe array containing all keys in the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="allocationHandle">The handle for the allocation.</param>
    /// <returns>An unsafe array containing all keys in the UnsafeMultiHashMap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<TKey> GetKeyArray(AllocationHandle allocationHandle)
    {
        return _helper.GetKeyArray(allocationHandle);
    }

    /// <summary>
    /// Gets an unsafe array containing all values in the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="allocationHandle">The handle for the allocation.</param>
    /// <returns>An unsafe array containing all values in the UnsafeMultiHashMap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<TValue> GetValueArray(AllocationHandle allocationHandle)
    {
        return _helper.GetValueArray<TValue>(allocationHandle);
    }

    /// <summary>
    /// Gets an unsafe array containing all key-value pairs in the UnsafeMultiHashMap.
    /// </summary>
    /// <param name="allocationHandle">The handle for the allocation.</param>
    /// <returns>An unsafe array containing all key-value pairs in the UnsafeMultiHashMap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<KeyValueRefPair<TKey, TValue>> GetKeyValueArrays(AllocationHandle allocationHandle)
    {
        return _helper.GetKeyValueArrays<TValue>(allocationHandle);
    }

    /// <summary>
    /// Get a pointer to the internal buffer of the UnsafeMultiHashMap, which contains the key, values, and buckets. The caller must ensure that the pointer is not used after the UnsafeMultiHashMap has been disposed.
    /// </summary>
    /// <returns>A pointer to the internal buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        return _helper.Buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _helper.Dispose();
    }
}
