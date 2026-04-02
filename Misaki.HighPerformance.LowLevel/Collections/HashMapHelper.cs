using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct HashMapHelper<TKey> : IDisposable
    where TKey : unmanaged, IEquatable<TKey>
{
    internal struct Enumerator
    {
        public HashMapHelper<TKey>* buffer;
        public int index;
        public int bucketIndex;
        public int nextIndex;

        public Enumerator(HashMapHelper<TKey>* data)
        {
            buffer = data;
            index = -1;
            bucketIndex = 0;
            nextIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return buffer->MoveNext(ref bucketIndex, ref nextIndex, out index);
        }

        public void Reset()
        {
            index = -1;
            bucketIndex = 0;
            nextIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, TValue> GetCurrent<TValue>()
            where TValue : unmanaged
        {
            return new KeyValuePair<TKey, TValue>(buffer->_keys[index], UnsafeUtility.ReadArrayElementRef<TValue>(buffer->_buffer, index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey GetCurrentKey()
        {
            if (index != -1)
            {
                return buffer->_keys[index];
            }

            return default;
        }

        public void Dispose()
        {
        }
    }

    // This buffer has 4 parts: TValue, TKey, Next, Buckets.
    private byte* _buffer;

    internal TKey* _keys;
    internal int* _next;
    internal int* _buckets;

    private int _count;
    private int _capacity;
    private int _bucketCapacity;
    private int _allocatedIndex;
    private int _firstFreeIndex;

    private readonly int _alignment;
    private readonly int _sizeOfTValue;
    private readonly int _log2MinGrowth;

#if MHP_ENABLE_SAFETY_CHECKS
    private MemoryHandle _memoryHandle;
#endif
    private AllocationHandle _allocationHandle;

    public const int MINIMAL_CAPACITY = 64;

    public readonly byte* Buffer => _buffer;

    public readonly int Count => _count;

    public readonly int Capacity => _capacity;

    public readonly bool IsEmpty => !IsCreated || _count == 0;

    public readonly bool IsCreated
    {
        get
        {
#if MHP_ENABLE_SAFETY_CHECKS
            if (_buffer != null)
            {
                if (_allocationHandle.IsValid != null)
                {
                    return _allocationHandle.IsValid(_allocationHandle.State, _memoryHandle);
                }
                else
                {
                    return true;
                }
            }

            return false;
#else
            return _buffer != null;
#endif
        }
    }

    private static int CalculateDataSize(int capacity, int bucketCapacity, int sizeOfTValue, out int outKeyOffset, out int outNextOffset, out int outBucketOffset)
    {
        var sizeOfTKey = sizeof(TKey);
        var sizeOfInt = sizeof(int);

        var valuesSize = sizeOfTValue * capacity;
        var keysSize = sizeOfTKey * capacity;
        var nextSize = sizeOfInt * capacity;
        var bucketSize = sizeOfInt * bucketCapacity;
        var totalSize = valuesSize + keysSize + nextSize + bucketSize;

        outKeyOffset = 0 + valuesSize;
        outNextOffset = outKeyOffset + keysSize;
        outBucketOffset = outNextOffset + nextSize;

        return totalSize;
    }

    public HashMapHelper(int capacity, int sizeOfTValue, int alignOfTValue, uint minGrowth, AllocationHandle handle, AllocationOption allocationOption)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeOfTValue);
        ArgumentOutOfRangeException.ThrowIfNegative(alignOfTValue);

        _capacity = CalcCapacityCeilPow2(capacity);
        _bucketCapacity = _capacity * 2;

        var alignOfKey = (int)AlignOf<TKey>();
        var alignOfInt = (int)AlignOf<int>();
        var maxDataAlign = Math.Max(Math.Max(alignOfTValue, alignOfKey), alignOfInt);

        _alignment = maxDataAlign;
        _sizeOfTValue = sizeOfTValue;
        _log2MinGrowth = BitOperations.Log2(minGrowth);

        _allocationHandle = handle;

        var totalSize = CalculateDataSize(_capacity, _bucketCapacity, sizeOfTValue,
            out var keyOffset, out var nextOffset, out var bucketOffset);

        AllocateBuffer(totalSize, keyOffset, nextOffset, bucketOffset, allocationOption);
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void ThrowIfNotCreated()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("The HashMapHelper is not created.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CeilPow2(int x)
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
        capacity = Math.Max(Math.Max(1, _count), capacity);
        var newCapacity = Math.Max(capacity, 1 << _log2MinGrowth);
        var result = CeilPow2(newCapacity);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetBucket(int hash)
    {
        return hash & (_bucketCapacity - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetBucket(scoped in TKey key)
    {
        var h = key.GetHashCode();
        return GetBucket(h);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void CheckIndexOutOfBounds(int idx)
    {
        if ((uint)idx >= (uint)_capacity)
        {
            throw new InvalidOperationException($"Index {idx} is out of bounds for the hash map with capacity {_capacity}.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AllocateEntry(scoped in TKey key)
    {
        int idx;

        if (_allocatedIndex >= _capacity && _firstFreeIndex < 0)
        {
            var newCap = CalcCapacityCeilPow2(_capacity + (1 << _log2MinGrowth));
            Resize(newCap);
        }

        idx = _firstFreeIndex;

        if (idx >= 0)
        {
            _firstFreeIndex = _next[idx];
        }
        else
        {
            idx = _allocatedIndex++;
        }

        CheckIndexOutOfBounds(idx);

        UnsafeUtility.WriteArrayElement(_keys, idx, key);
        var bucket = GetBucket(key);

        _next[idx] = _buckets[bucket];
        _buckets[bucket] = idx;
        _count++;

        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AllocateBuffer(int totalSize, int keyOffset, int nextOffset, int bucketOffset, AllocationOption allocationOption)
    {
        if (_allocationHandle.Alloc == null)
        {
            throw new InvalidOperationException("Target allocation handle does not support allocation.");
        }

#if MHP_ENABLE_SAFETY_CHECKS
        MemoryHandle memHandle;
#endif
        var buf = (byte*)_allocationHandle.Alloc(_allocationHandle.State, (uint)totalSize, (nuint)_alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , &memHandle
#endif
            );

        _buffer = buf;
        _keys = (TKey*)(_buffer + keyOffset);
        _next = (int*)(_buffer + nextOffset);
        _buckets = (int*)(_buffer + bucketOffset);
#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle = memHandle;
#endif
    }

    private void ResizeExact(int newCapacity, int newBucketCapacity)
    {
        var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, _sizeOfTValue,
            out var keyOffset, out var nextOffset, out var bucketOffset);

        var oldBuffer = _buffer;
        var oldKeys = _keys;
        var oldNext = _next;
        var oldBuckets = _buckets;
        var oldBucketCapacity = _bucketCapacity;
#if MHP_ENABLE_SAFETY_CHECKS
        var oldMemoryHandle = _memoryHandle;
#endif

        AllocateBuffer(totalSize, keyOffset, nextOffset, bucketOffset, AllocationOption.None);
        _capacity = newCapacity;
        _bucketCapacity = newBucketCapacity;

        Clear();

        for (int i = 0, num = oldBucketCapacity; i < num; ++i)
        {
            for (var idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
            {
                var newIdx = Add(oldKeys[idx]);
                MemCpy(_buffer + _sizeOfTValue * newIdx, oldBuffer + _sizeOfTValue * idx, (nuint)_sizeOfTValue);
            }
        }

        if (_allocationHandle.Free != null)
        {
            _allocationHandle.Free(_allocationHandle.State, oldBuffer
#if MHP_ENABLE_SAFETY_CHECKS
                , oldMemoryHandle
#endif
                );
        }
    }

    public void Resize(int newCapacity)
    {
        ThrowIfNotCreated();

        newCapacity = Math.Max(newCapacity, _count);
        var newBucketCapacity = CeilPow2(newCapacity * 2);

        if (_capacity == newCapacity && _bucketCapacity == newBucketCapacity)
        {
            return;
        }

        ResizeExact(newCapacity, newBucketCapacity);
    }

    public void TrimExcess()
    {
        ThrowIfNotCreated();

        var capacity = CalcCapacityCeilPow2(_count);
        ResizeExact(capacity, capacity * 2);
    }

    public int Find(scoped in TKey key)
    {
        ThrowIfNotCreated();

        if (_allocatedIndex <= 0)
        {
            return -1;
        }

        // First find the slot based on the hash
        var bucket = GetBucket(key);
        var entryIdx = _buckets[bucket];

        if ((uint)entryIdx < (uint)_capacity)
        {
            var nextPtrs = _next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(_keys, entryIdx).Equals(key))
            {
                entryIdx = nextPtrs[entryIdx];
                if ((uint)entryIdx >= (uint)_capacity)
                {
                    return -1;
                }
            }

            return entryIdx;
        }

        return -1;
    }

    public int TryAdd(scoped in TKey key)
    {
        ThrowIfNotCreated();

        if (Find(in key) != -1)
        {
            return -1;
        }

        return AllocateEntry(key);
    }

    public int Add(scoped in TKey key)
    {
        ThrowIfNotCreated();

        return AllocateEntry(key);
    }

    public int TryRemove(scoped in TKey key)
    {
        ThrowIfNotCreated();

        if (_capacity == 0)
        {
            return -1;
        }

        var removed = 0;

        // First find the slot based on the hash
        var bucket = GetBucket(key);

        var prevEntry = -1;
        var entryIdx = _buckets[bucket];

        while (entryIdx >= 0 && entryIdx < _capacity)
        {
            if (UnsafeUtility.ReadArrayElement<TKey>(_keys, entryIdx).Equals(key))
            {
                removed++;

                // Found matching element, remove it
                if (prevEntry < 0)
                {
                    _buckets[bucket] = _next[entryIdx];
                }
                else
                {
                    _next[prevEntry] = _next[entryIdx];
                }

                // And free the index
                var nextIdx = _next[entryIdx];
                _next[entryIdx] = _firstFreeIndex;
                _firstFreeIndex = entryIdx;
                entryIdx = nextIdx;

                break;
            }
            else
            {
                prevEntry = entryIdx;
                entryIdx = _next[entryIdx];
            }
        }

        _count -= removed;
        return 0 != removed ? removed : -1;
    }

    public int RemoveAll(scoped in TKey key)
    {
        ThrowIfNotCreated();

        if (_capacity == 0)
        {
            return 0;
        }

        var removed = 0;
        var bucket = GetBucket(key);
        var prevEntry = -1;
        var entryIdx = _buckets[bucket];

        while (entryIdx >= 0 && entryIdx < _capacity)
        {
            if (UnsafeUtility.ReadArrayElement<TKey>(_keys, entryIdx).Equals(key))
            {
                removed++;

                var nextIdx = _next[entryIdx];
                if (prevEntry < 0)
                {
                    _buckets[bucket] = nextIdx;
                }
                else
                {
                    _next[prevEntry] = nextIdx;
                }

                _next[entryIdx] = _firstFreeIndex;
                _firstFreeIndex = entryIdx;
                entryIdx = nextIdx;
                continue;
            }

            prevEntry = entryIdx;
            entryIdx = _next[entryIdx];
        }

        _count -= removed;
        return removed;
    }

    public bool TryGetValue<TValue>(scoped in TKey key, out TValue item)
        where TValue : unmanaged
    {
        ThrowIfNotCreated();

        var idx = Find(key);

        if (idx != -1)
        {
            item = UnsafeUtility.ReadArrayElement<TValue>(_buffer, idx);
            return true;
        }

        item = default;
        return false;
    }

    public int FindNext(int entryIdx, scoped in TKey key)
    {
        ThrowIfNotCreated();

        if ((uint)entryIdx >= (uint)_capacity)
        {
            return -1;
        }

        var nextIndex = _next[entryIdx];
        while ((uint)nextIndex < (uint)_capacity)
        {
            if (UnsafeUtility.ReadArrayElement<TKey>(_keys, nextIndex).Equals(key))
            {
                return nextIndex;
            }

            nextIndex = _next[nextIndex];
        }

        return -1;
    }

    public int CountValuesForKey(scoped in TKey key)
    {
        ThrowIfNotCreated();

        var count = 0;
        for (var idx = Find(key); idx != -1; idx = FindNext(idx, key))
        {
            count++;
        }

        return count;
    }

    public ref TValue GetValueRef<TValue>(scoped in TKey key, out bool exists)
        where TValue : unmanaged
    {
        ThrowIfNotCreated();

        var idx = Find(key);
        if (idx != -1)
        {
            exists = true;
            return ref UnsafeUtility.ReadArrayElementRef<TValue>(_buffer, idx);
        }

        exists = false;
        return ref Unsafe.NullRef<TValue>();
    }

    public ref TValue GetValueRefOrAddDefault<TValue>(scoped in TKey key, out bool exists)
        where TValue : unmanaged
    {
        ThrowIfNotCreated();

        var idx = -1;
        var bucket = -1;
        var hash = key.GetHashCode();

        if (_allocatedIndex > 0)
        {
            // First find the slot based on the hash
            bucket = GetBucket(hash);
            var entryIdx = _buckets[bucket];

            if ((uint)entryIdx < (uint)_capacity)
            {
                var nextPtrs = _next;
                while (!UnsafeUtility.ReadArrayElement<TKey>(_keys, entryIdx).Equals(key))
                {
                    entryIdx = nextPtrs[entryIdx];
                    if ((uint)entryIdx >= (uint)_capacity)
                    {
                        goto Found;
                    }
                }

                idx = entryIdx;
                goto Found;
            }
        }

    Found:

        if (idx != -1)
        {
            exists = true;
            return ref UnsafeUtility.ReadArrayElementRef<TValue>(_buffer, idx);
        }

        idx = AllocateEntry(key);

        UnsafeUtility.WriteArrayElement(_buffer, idx, default(TValue));

        exists = false;
        return ref UnsafeUtility.ReadArrayElementRef<TValue>(_buffer, idx);
    }

    public bool MoveNextSearch(ref int bucketIndex, ref int nextIndex, out int index)
    {
        ThrowIfNotCreated();

        for (int i = bucketIndex, num = _bucketCapacity; i < num; ++i)
        {
            var idx = _buckets[i];

            if (idx != -1)
            {
                index = idx;
                bucketIndex = i + 1;
                nextIndex = _next[idx];

                return true;
            }
        }

        index = -1;
        bucketIndex = _bucketCapacity;
        nextIndex = -1;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext(ref int bucketIndex, ref int nextIndex, out int index)
    {
        ThrowIfNotCreated();

        if (nextIndex != -1)
        {
            index = nextIndex;
            nextIndex = _next[nextIndex];
            return true;
        }

        return MoveNextSearch(ref bucketIndex, ref nextIndex, out index);
    }

    internal UnsafeArray<TKey> GetKeyArray(Allocator allocator)
    {
        ThrowIfNotCreated();

        var result = new UnsafeArray<TKey>(_count, allocator);

        for (int i = 0, count = 0, max = result.Count, capacity = _bucketCapacity; i < capacity && count < max; i++)
        {
            var bucket = _buckets[i];

            while (bucket != -1)
            {
                result[count++] = UnsafeUtility.ReadArrayElement<TKey>(_keys, bucket);
                bucket = _next[bucket];
            }
        }

        return result;
    }

    internal UnsafeArray<TValue> GetValueArray<TValue>(Allocator allocator)
        where TValue : unmanaged
    {
        ThrowIfNotCreated();

        var result = new UnsafeArray<TValue>(_count, allocator);

        for (int i = 0, count = 0, max = result.Count, capacity = _bucketCapacity; i < capacity && count < max; ++i)
        {
            var bucket = _buckets[i];

            while (bucket != -1)
            {
                result[count++] = UnsafeUtility.ReadArrayElement<TValue>(_buffer, bucket);
                bucket = _next[bucket];
            }
        }

        return result;
    }

    public UnsafeArray<KeyValuePair<TKey, TValue>> GetKeyValueArrays<TValue>(Allocator allocator)
        where TValue : unmanaged
    {
        ThrowIfNotCreated();

        var result = new UnsafeArray<KeyValuePair<TKey, TValue>>(_count, allocator);

        for (int i = 0, count = 0, max = result.Count, capacity = _bucketCapacity; i < capacity && count < max; i++)
        {
            var bucket = _buckets[i];

            while (bucket != -1)
            {
                result[count] = new(UnsafeUtility.ReadArrayElement<TKey>(_keys, bucket),
                    UnsafeUtility.ReadArrayElement<TValue>(_buffer, bucket));

                count++;
                bucket = _next[bucket];
            }
        }

        return result;
    }

    public void Clear()
    {
        ThrowIfNotCreated();

        MemSet(_buckets, 0xff, (nuint)_bucketCapacity * sizeof(int));
        MemSet(_next, 0xff, (nuint)_capacity * sizeof(int));

        _count = 0;
        _firstFreeIndex = -1;
        _allocatedIndex = 0;
    }

    public void Dispose()
    {
        if (!IsCreated)
        {
            var message = "The HashMapHelper is not created or already disposed.";
#if MHP_ENABLE_STACKTRACE
            var stackTrace = new StackTrace(1, true);
            var sb = new System.Text.StringBuilder();
            foreach (var frame in stackTrace.GetFrames())
            {
                var fileName = frame?.GetFileName();
                if (frame != null)
                {
                    var methodInfo = DiagnosticMethodInfo.Create(frame);
                    sb.AppendLine($"File: {fileName}, Type: {methodInfo?.DeclaringTypeName}, Method: {methodInfo?.Name}, Line: {frame.GetFileLineNumber()}");
                }
            }

            message += Environment.NewLine + sb.ToString();
#endif
            Debug.WriteLine(message);
            return;
        }

        if (_allocationHandle.Free != null)
        {
            _allocationHandle.Free(_allocationHandle.State, _buffer
#if MHP_ENABLE_SAFETY_CHECKS
                , _memoryHandle
#endif
                );
        }

        _buffer = null;
        _keys = null;
        _next = null;
        _buckets = null;

        _count = 0;
        _capacity = 0;
        _bucketCapacity = 0;
    }
}
