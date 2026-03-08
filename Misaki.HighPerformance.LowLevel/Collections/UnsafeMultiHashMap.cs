using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnsafeMultiHashMap<TKey, TValue> : IUnsafeHashCollection<KeyValuePair<TKey, TValue>>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        internal HashMapHelper<TKey>.Enumerator _enumerator;

        public readonly KeyValuePair<TKey, TValue> Current => _enumerator.GetCurrent<TValue>();
        readonly object IEnumerator.Current => Current;

        public Enumerator(HashMapHelper<TKey>* data)
        {
            _enumerator = new HashMapHelper<TKey>.Enumerator(data);
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

    public struct Iterator
    {
        internal TKey Key;
        internal int EntryIndex;

        internal Iterator(in TKey key, int entryIndex)
        {
            Key = key;
            EntryIndex = entryIndex;
        }
    }

    public struct ValueEnumerable
    {
        private readonly HashMapHelper<TKey>* _data;
        private readonly TKey _key;

        internal ValueEnumerable(HashMapHelper<TKey>* data, in TKey key)
        {
            _data = data;
            _key = key;
        }

        public readonly ValueEnumerator GetEnumerator()
        {
            return new(_data, _key);
        }
    }

    public struct ValueEnumerator : IEnumerator<TValue>
    {
        private readonly HashMapHelper<TKey>* _data;
        private readonly TKey _key;
        private int _entryIndex;
        private bool _started;

        public readonly TValue Current => UnsafeUtility.ReadArrayElement<TValue>(_data->Buffer, _entryIndex);
        readonly object IEnumerator.Current => Current;

        internal ValueEnumerator(HashMapHelper<TKey>* data, in TKey key)
        {
            _data = data;
            _key = key;
            _entryIndex = -1;
            _started = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _entryIndex = _data->Find(_key);
                _started = true;
                return _entryIndex != -1;
            }

            if (_entryIndex == -1)
            {
                return false;
            }

            _entryIndex = _data->FindNext(_entryIndex, _key);
            return _entryIndex != -1;
        }

        public void Reset()
        {
            _entryIndex = -1;
            _started = false;
        }

        public readonly void Dispose()
        {
        }
    }

    private HashMapHelper<TKey> _helper;

    public readonly int Count => _helper.Count;
    public readonly int Capacity => _helper.Capacity;
    public readonly bool IsCreated => _helper.IsCreated;

    public Enumerator GetEnumerator()
    {
        return new((HashMapHelper<TKey>*)UnsafeUtility.AddressOf(ref this));
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public UnsafeMultiHashMap()
        : this(0, Allocator.Invalid)
    {
    }

    public UnsafeMultiHashMap(int capacity, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _helper = new HashMapHelper<TKey>(capacity, sizeof(TValue), (int)AlignOf<TValue>(), HashMapHelper<TKey>.MINIMAL_CAPACITY, handle, allocationOption);
    }

    public UnsafeMultiHashMap(int capacity, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacity, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    public void Add(in TKey key, TValue item)
    {
        var idx = _helper.Add(key);
        UnsafeUtility.WriteArrayElement(_helper.Buffer, idx, item);
    }

    public bool Remove(in TKey key)
    {
        return _helper.RemoveAll(key) != 0;
    }

    public bool TryGetFirstValue(in TKey key, out TValue item, out Iterator iterator)
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
        if (iterator.EntryIndex == -1)
        {
            item = default;
            return false;
        }

        var entryIndex = _helper.FindNext(iterator.EntryIndex, iterator.Key);
        if (entryIndex == -1)
        {
            item = default;
            iterator.EntryIndex = -1;
            return false;
        }

        iterator.EntryIndex = entryIndex;
        item = UnsafeUtility.ReadArrayElement<TValue>(_helper.Buffer, entryIndex);
        return true;
    }

    public bool TryGetValue(in TKey key, out TValue item)
    {
        return _helper.TryGetValue(key, out item);
    }

    public TValue GetValueOrDefault(in TKey key, TValue defaultValue = default)
    {
        if (_helper.TryGetValue<TValue>(key, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public ValueEnumerable GetValuesForKey(in TKey key)
    {
        return new((HashMapHelper<TKey>*)UnsafeUtility.AddressOf(ref this), key);
    }

    public int CountValuesForKey(in TKey key)
    {
        return _helper.CountValuesForKey(key);
    }

    public bool ContainsKey(in TKey key)
    {
        return _helper.Find(key) != -1;
    }

    public void TrimExcess()
    {
        _helper.TrimExcess();
    }

    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        _helper.Resize(newSize);
    }

    public void Clear()
    {
        _helper.Clear();
    }

    public UnsafeArray<TKey> GetKeyArray(Allocator allocator)
    {
        return _helper.GetKeyArray(allocator);
    }

    public UnsafeArray<TValue> GetValueArray(Allocator allocator)
    {
        return _helper.GetValueArray<TValue>(allocator);
    }

    public UnsafeArray<KeyValuePair<TKey, TValue>> GetKeyValueArrays(Allocator allocator)
    {
        return _helper.GetKeyValueArrays<TValue>(allocator);
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
