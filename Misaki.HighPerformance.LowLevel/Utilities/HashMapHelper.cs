using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Utilities;

public unsafe struct HashMapHelper<TKey> : IDisposable
    where TKey : unmanaged, IEquatable<TKey>
{
    internal unsafe struct Enumerator
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

    private AllocationHandle* _handle;

    public const int MINIMAL_CAPACITY = 64;

    public readonly byte* Buffer => _buffer;

    public readonly int Count => _count;

    public readonly int Capacity => _capacity;

    public readonly bool IsEmpty => !IsCreated || _count == 0;

    public readonly bool IsCreated
    {
        get
        {
            var handle = SafeHandle.GetSafeHandle(_buffer, (nuint)_alignment);
            return handle != null && Volatile.Read(ref handle->valid) == 1;
        }
    }

    private static int CalculateDataSize(int capacity, int bucketCapacity, int sizeOfTValue, int alignment, out int outValueOffset, out int outKeyOffset, out int outNextOffset, out int outBucketOffset)
    {
        static int AlignUp(int size, int align)
        {
            return (size + (align - 1)) & ~(align - 1);
        }

        var headerSize = SafeHandle.GetPaddedHeaderSize((nuint)alignment);

        var valuesSize = (sizeOfTValue * capacity);
        var valuesSizePadded = AlignUp(valuesSize, alignment);

        var keysSize = (sizeof(TKey) * capacity);
        var keysSizePadded = AlignUp(keysSize, alignment);

        var nextSize = (sizeof(int) * capacity);
        var nextSizePadded = AlignUp(nextSize, alignment);

        // Buckets are the last item, doesn't need padding after it
        var bucketSize = (sizeof(int) * bucketCapacity);

        outValueOffset = (int)headerSize;
        outKeyOffset = outValueOffset + valuesSizePadded;
        outNextOffset = outKeyOffset + keysSizePadded;
        outBucketOffset = outNextOffset + nextSizePadded;

        // Total size is header + all buffers
        var totalSize = (int)headerSize + outKeyOffset + keysSizePadded + nextSizePadded + bucketSize;

        return totalSize;
    }

    public HashMapHelper(int capacity, int sizeOfTValue, int alignOfTValue, uint minGrowth, ref AllocationHandle handle, AllocationOption allocationOption)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        if (sizeOfTValue < 0 || alignOfTValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeOfTValue), "Size or alignment of TValue can not be less than zero.");
        }

        _capacity = CalcCapacityCeilPow2(capacity);
        _bucketCapacity = _capacity * 2;

        var alignOfKey = (int)AlignOf<TKey>();
        var alignOfInt = (int)AlignOf<int>();
        var maxDataAlign = Math.Max(Math.Max(alignOfTValue, alignOfKey), alignOfInt);

        _alignment = (int)SafeHandle.GetAlignWithHeader((nuint)maxDataAlign);
        _sizeOfTValue = sizeOfTValue;
        _log2MinGrowth = BitOperations.Log2(minGrowth);

        _handle = (AllocationHandle*)Unsafe.AsPointer(ref handle);

        var totalSize = CalculateDataSize(_capacity, _bucketCapacity, sizeOfTValue, _alignment,
            out var valueOffset, out var keyOffset, out var nextOffset, out var bucketOffset);

        AllocateBuffer(totalSize, valueOffset, keyOffset, nextOffset, bucketOffset, _alignment, allocationOption);
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    private readonly int GetBucket(in TKey key)
    {
        var h = (uint)key.GetHashCode();
        return (int)(h & (uint)(_bucketCapacity - 1));
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
    private void AllocateBuffer(int totalSize, int valueOffset, int keyOffset, int nextOffset, int bucketOffset, int alignment, AllocationOption allocationOption)
    {
        var buf = (byte*)_handle->Alloc(_handle->Allocator, (uint)totalSize, (nuint)alignment, allocationOption);

        _buffer = buf + valueOffset;
        _keys = (TKey*)(_buffer + keyOffset);
        _next = (int*)(_buffer + nextOffset);
        _buckets = (int*)(_buffer + bucketOffset);
    }

    private void ResizeExact(int newCapacity, int newBucketCapacity)
    {
        var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, _sizeOfTValue, _alignment,
            out var valueOffset, out var keyOffset, out var nextOffset, out var bucketOffset);

        var oldBuffer = _buffer;
        var oldKeys = _keys;
        var oldNext = _next;
        var oldBuckets = _buckets;
        var oldBucketCapacity = _bucketCapacity;

        AllocateBuffer(totalSize, valueOffset, keyOffset, nextOffset, bucketOffset, _alignment, AllocationOption.None);
        _capacity = newCapacity;
        _bucketCapacity = newBucketCapacity;

        Clear();

        for (int i = 0, num = oldBucketCapacity; i < num; ++i)
        {
            for (var idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
            {
                var newIdx = TryAdd(oldKeys[idx]);
                MemCpy(_buffer + _sizeOfTValue * newIdx, oldBuffer + _sizeOfTValue * idx, (nuint)_sizeOfTValue);
            }
        }

        _handle->Free(_handle->Allocator, oldBuffer);
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

    public int Find(in TKey key)
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

    public int TryAdd(in TKey key)
    {
        ThrowIfNotCreated();

        var k = key;
        if (Find(in key) != -1)
        {
            return -1;
        }

        // Allocate an entry from the free list
        int idx;
        int* next;

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

        // Add the index to the hash-map
        next = _next;
        next[idx] = _buckets[bucket];
        _buckets[bucket] = idx;
        _count++;

        return idx;
    }

    public int TryRemove(in TKey key)
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

    public bool TryGetValue<TValue>(in TKey key, out TValue item)
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
            return;
        }

        if (_handle != null)
        {
            _handle->Free(_handle->Allocator, _buffer);
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