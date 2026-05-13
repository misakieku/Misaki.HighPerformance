using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnsafeParallelHashMapData<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    public byte* buffer;

    public TKey* keys;
    public TValue* values;
    public int* next;
    public int* buckets;

    public int count;
    public int capacity;
    public int bucketCapacityMask;
    public int allocatedIndex;
    public int firstFreeIndex;

    public int alignment;
    public int log2MinGrowth;

#if MHP_ENABLE_SAFETY_CHECKS
    public MemoryHandle memoryHandle;
#endif
    public AllocationHandle allocationHandle;
}

public unsafe struct UnsafeParallelHashMap<TKey, TValue> : IDisposable
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal UnsafeParallelHashMapData<TKey, TValue>* _data;

    public const int MINIMAL_CAPACITY = 64;

    public readonly int Count => _data != null ? _data->count : 0;

    public readonly int Capacity => _data != null ? _data->capacity : 0;

    public readonly bool IsEmpty => !IsCreated || _data->count == 0;

    public readonly bool IsCreated
    {
        get
        {
#if MHP_ENABLE_SAFETY_CHECKS
            if (_data != null)
            {
                if (_data->buffer != null)
                {
                    return _data->memoryHandle.IsValid;
                }
            }

            return false;
#else
            return _data != null && _data->buffer != null;
#endif
        }
    }

    public UnsafeParallelHashMap(int capacity, uint minGrowth, AllocationHandle handle, AllocationOption allocationOption)
    {
        if (capacity <= 0)
        {
            Debug.Assert(capacity >= 0);
            return;
        }

        _data = (UnsafeParallelHashMapData<TKey, TValue>*)handle.Alloc((uint)sizeof(UnsafeParallelHashMapData<TKey, TValue>), (nuint)AlignOf<UnsafeParallelHashMapData<TKey, TValue>>(), AllocationOption.Clear);

        if (_data == null)
        {
            throw new OutOfMemoryException("Failed to allocate UnsafeParallelHashMapData.");
        }

        _data->capacity = capacity;
        _data->bucketCapacityMask = capacity * 2 - 1;

        var alignOfKey = (int)AlignOf<TKey>();
        var alignOfTValue = (int)AlignOf<TValue>();
        var alignOfInt = (int)AlignOf<int>();
        var maxDataAlign = Math.Max(Math.Max(alignOfTValue, alignOfKey), alignOfInt);

        _data->alignment = maxDataAlign;
        _data->log2MinGrowth = BitOperations.Log2(minGrowth);
        _data->allocationHandle = handle;

        var totalSize = CalculateDataSize(capacity, capacity * 2, out var keyOffset, out var valueOffset, out var nextOffset, out var bucketOffset);

        allocationOption &= ~AllocationOption.Clear;
        AllocateBuffer(_data, totalSize, keyOffset, valueOffset, nextOffset, bucketOffset, allocationOption);

#if MHP_ENABLE_SAFETY_CHECKS
        _data->memoryHandle = MemoryHandle.Create(_data->buffer, (nuint)totalSize);
#endif

        Clear();
    }

    public void Dispose()
    {
        if (!IsCreated)
        {
            return;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        _data->memoryHandle.Dispose();
#endif

        if (_data->buffer != null)
        {
            _data->allocationHandle.Free(_data->buffer);
            _data->buffer = null;
        }

        if (_data != null)
        {
            _data->allocationHandle.Free(_data);
            _data = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void ThrowIfNotCreated()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("The UnsafeParallelHashMap is not created.");
        }
    }

    private static int CalculateDataSize(int capacity, int bucketCapacity, out int outKeyOffset, out int outValueOffset, out int outNextOffset, out int outBucketOffset)
    {
        var sizeOfTKey = sizeof(TKey);
        var sizeOfTValue = sizeof(TValue);
        var sizeOfInt = sizeof(int);

        var keysSize = sizeOfTKey * capacity;
        var valuesSize = sizeOfTValue * capacity;
        var nextSize = sizeOfInt * capacity;
        var bucketSize = sizeOfInt * bucketCapacity;
        var totalSize = keysSize + valuesSize + nextSize + bucketSize;

        outKeyOffset = 0;
        outValueOffset = outKeyOffset + keysSize;
        outNextOffset = outValueOffset + valuesSize;
        outBucketOffset = outNextOffset + nextSize;

        return totalSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint AlignOf<T>() where T : unmanaged
    {
        return (uint)Unsafe.SizeOf<T>(); // Temporary substitute for alignment util
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CeilPow2(int x)
    {
        x -= 1;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int CalcCapacityCeilPow2(int capacity)
    {
        capacity = Math.Max(Math.Max(1, _data->count), capacity);
        var newCapacity = Math.Max(capacity, 1 << _data->log2MinGrowth);
        var result = CeilPow2(newCapacity);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void AllocateBuffer(UnsafeParallelHashMapData<TKey, TValue>* data, int totalSize, int keyOffset, int valueOffset, int nextOffset, int bucketOffset, AllocationOption allocationOption)
    {
        var buf = (byte*)data->allocationHandle.Alloc((uint)totalSize, (nuint)data->alignment, allocationOption);

        data->buffer = buf;
        data->keys = (TKey*)(buf + keyOffset);
        data->values = (TValue*)(buf + valueOffset);
        data->next = (int*)(buf + nextOffset);
        data->buckets = (int*)(buf + bucketOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetBucket(int hash)
    {
        return hash & _data->bucketCapacityMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetBucket(scoped in TKey key)
    {
        return GetBucket(key.GetHashCode());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void CheckIndexOutOfBounds(int idx)
    {
        if ((uint)idx >= (uint)_data->capacity)
        {
            throw new InvalidOperationException($"Index {idx} is out of bounds for the hash map with capacity {_data->capacity}.");
        }
    }

    public void Clear()
    {
        ThrowIfNotCreated();

        _data->count = 0;
        _data->allocatedIndex = 0;
        _data->firstFreeIndex = -1;

        if (_data->buffer == null)
        {
            return;
        }

        var bucketCapacity = _data->bucketCapacityMask + 1;
        MemoryUtility.MemSet(_data->buckets, (byte)0xFF, (nuint)(bucketCapacity * sizeof(int)));
        MemoryUtility.MemSet(_data->next, (byte)0xFF, (nuint)(_data->capacity * sizeof(int)));
    }

    public int Add(scoped in TKey key, scoped in TValue value)
    {
        ThrowIfNotCreated();

        if (Find(in key) != -1)
        {
            return -1; // Or throw depending on semantics you want
        }

        return AllocateEntry(key, value);
    }

    public bool TryGetValue(scoped in TKey key, out TValue item)
    {
        var idx = Find(key);
        if (idx != -1)
        {
            item = _data->values[idx];
            return true;
        }

        item = default;
        return false;
    }

    public int Find(scoped in TKey key)
    {
        ThrowIfNotCreated();

        if (_data->allocatedIndex <= 0)
        {
            return -1;
        }

        var bucket = GetBucket(key);
        var entryIdx = _data->buckets[bucket];

        if ((uint)entryIdx < (uint)_data->capacity)
        {
            var nextPtrs = _data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(_data->keys, entryIdx).Equals(key))
            {
                entryIdx = nextPtrs[entryIdx];
                if ((uint)entryIdx >= (uint)_data->capacity)
                {
                    return -1;
                }
            }

            return entryIdx;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AllocateEntry(scoped in TKey key, scoped in TValue value)
    {
        int idx;

        if (_data->allocatedIndex >= _data->capacity && _data->firstFreeIndex < 0)
        {
            var newCap = CalcCapacityCeilPow2(_data->capacity + (1 << _data->log2MinGrowth));
            Resize(newCap);
        }

        idx = _data->firstFreeIndex;

        if (idx >= 0)
        {
            _data->firstFreeIndex = _data->next[idx];
        }
        else
        {
            idx = _data->allocatedIndex++;
        }

        CheckIndexOutOfBounds(idx);

        UnsafeUtility.WriteArrayElement(_data->keys, idx, key);
        UnsafeUtility.WriteArrayElement(_data->values, idx, value);

        var bucket = GetBucket(key);

        _data->next[idx] = _data->buckets[bucket];
        _data->buckets[bucket] = idx;
        _data->count++;

        return idx;
    }

    public bool Remove(scoped in TKey key)
    {
        ThrowIfNotCreated();

        if (_data->capacity == 0)
        {
            return false;
        }

        var removed = false;
        var bucket = GetBucket(key);

        var prevEntry = -1;
        var entryIdx = _data->buckets[bucket];

        while (entryIdx >= 0 && entryIdx < _data->capacity)
        {
            if (UnsafeUtility.ReadArrayElement<TKey>(_data->keys, entryIdx).Equals(key))
            {
                removed = true;

                if (prevEntry < 0)
                {
                    _data->buckets[bucket] = _data->next[entryIdx];
                }
                else
                {
                    _data->next[prevEntry] = _data->next[entryIdx];
                }

                var nextIdx = _data->next[entryIdx];
                _data->next[entryIdx] = _data->firstFreeIndex;
                _data->firstFreeIndex = entryIdx;
                entryIdx = nextIdx;

                break;
            }
            else
            {
                prevEntry = entryIdx;
                entryIdx = _data->next[entryIdx];
            }
        }

        if (removed)
        {
            _data->count--;
        }

        return removed;
    }

    private void ResizeExact(int newCapacity, int newBucketCapacity)
    {
        var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, out var keyOffset, out var valueOffset, out var nextOffset, out var bucketOffset);

        var oldBuffer = _data->buffer;
        var oldKeys = _data->keys;
        var oldValues = _data->values;
        var oldNext = _data->next;
        var oldBuckets = _data->buckets;
        var oldBucketCapacity = _data->bucketCapacityMask + 1;

        AllocateBuffer(_data, totalSize, keyOffset, valueOffset, nextOffset, bucketOffset, AllocationOption.None);

        _data->capacity = newCapacity;
        _data->bucketCapacityMask = newBucketCapacity - 1;

        Clear();

        for (int i = 0, num = oldBucketCapacity; i < num; ++i)
        {
            for (var idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
            {
                Add(oldKeys[idx], oldValues[idx]);
            }
        }

        if (oldBuffer != null)
        {
            _data->allocationHandle.Free(oldBuffer);
        }

#if MHP_ENABLE_SAFETY_CHECKS
        _data->memoryHandle.Update(_data->buffer, (nuint)totalSize);
#endif
    }

    public void Resize(int newCapacity)
    {
        ThrowIfNotCreated();

        newCapacity = Math.Max(newCapacity, _data->count);
        var newBucketCapacity = CeilPow2(newCapacity * 2);

        if (_data->capacity == newCapacity && (_data->bucketCapacityMask + 1) == newBucketCapacity)
        {
            return;
        }

        ResizeExact(newCapacity, newBucketCapacity);
    }

    public ParallelWriter AsParallelWriter()
    {
        ThrowIfNotCreated();
        return new ParallelWriter(_data);
    }

    public unsafe struct ParallelWriter
    {
        internal UnsafeParallelHashMapData<TKey, TValue>* _data;

        internal ParallelWriter(UnsafeParallelHashMapData<TKey, TValue>* data)
        {
            _data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, TValue item)
        {
            if (_data == null || _data->buffer == null)
            {
                throw new InvalidOperationException("The UnsafeParallelHashMap is not created.");
            }

            var hash = key.GetHashCode();
            var bucket = hash & _data->bucketCapacityMask;
            ref var bucketValue = ref _data->buckets[bucket];

            var entryIdx = bucketValue;
            var nextPtrs = _data->next;

            // Optional Fast path Check if item exists. (Does not lock)
            if ((uint)entryIdx < (uint)_data->capacity)
            {
                while (!UnsafeUtility.ReadArrayElement<TKey>(_data->keys, entryIdx).Equals(key))
                {
                    entryIdx = nextPtrs[entryIdx];
                    if ((uint)entryIdx >= (uint)_data->capacity)
                    {
                        break;
                    }
                }

                if ((uint)entryIdx < (uint)_data->capacity)
                {
                    // Item already exists
                    return false;
                }
            }

            // Allocate a new slot from the contiguous array atomically
            var idx = Interlocked.Increment(ref _data->allocatedIndex) - 1;

            if (idx >= _data->capacity)
            {
                // UnsafeParallelHashMap does not resize concurrently. Must pre-allocate enough memory.
                Interlocked.Decrement(ref _data->allocatedIndex);
                throw new InvalidOperationException($"Hash map capacity ({_data->capacity}) exceeded during parallel writing.");
            }

            // Write our data
            UnsafeUtility.WriteArrayElement(_data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(_data->values, idx, item);

            ref var b = ref _data->buckets[bucket];

            // Atomically link into bucket linked list
            while (true)
            {
                var bucketHead = Volatile.Read(ref b);
                UnsafeUtility.WriteArrayElement(_data->next, idx, bucketHead);

                if (Interlocked.CompareExchange(ref b, idx, bucketHead) == bucketHead)
                {
                    Interlocked.Increment(ref _data->count);
                    return true;
                }
            }
        }
    }
}