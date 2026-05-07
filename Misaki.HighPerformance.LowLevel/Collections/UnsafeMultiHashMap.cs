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

        public readonly KeyValuePair<TKey, TValue> Current => _enumerator.GetCurrent<TValue>();

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

        public readonly TValue Current => UnsafeUtility.ReadArrayElement<TValue>(helper.Buffer, _entryIndex);

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

    public UnsafeMultiHashMap(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<TKey>(capacity, sizeof(TValue), (int)MemoryUtility.AlignOf<TValue>(), HashMapHelper<TKey>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeMultiHashMap(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref _helper);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(scoped in TKey key, TValue item)
    {
        var idx = _helper.Add(key);
        UnsafeUtility.WriteArrayElement(_helper.Buffer, idx, item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(scoped in TKey key)
    {
        return _helper.RemoveAll(key) != 0;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(scoped in TKey key, out TValue item)
    {
        return _helper.TryGetValue(key, out item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetValueOrDefault(scoped in TKey key, TValue defaultValue = default)
    {
        if (_helper.TryGetValue<TValue>(key, out var value))
        {
            return value;
        }

        return defaultValue;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ValueEnumerable GetValuesForKey(scoped in TKey key)
    {
        return new ValueEnumerable(ref _helper, key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountValuesForKey(scoped in TKey key)
    {
        return _helper.CountValuesForKey(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(scoped in TKey key)
    {
        return _helper.Find(key) != -1;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<TKey> GetKeyArray(AllocationHandle allocationHandle)
    {
        return _helper.GetKeyArray(allocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<TValue> GetValueArray(AllocationHandle allocationHandle)
    {
        return _helper.GetValueArray<TValue>(allocationHandle);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _helper.Dispose();
    }
}
