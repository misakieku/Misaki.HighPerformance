using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Helpers;

public unsafe struct HashMapHelper<TKey> : IDisposable
    where TKey : unmanaged, IEquatable<TKey>
{
    internal unsafe struct Enumerator
    {
        public HashMapHelper<TKey>* buffer;
        public int index;
        public int bucketIndex;
        public int nextIndex;

        public unsafe Enumerator(HashMapHelper<TKey>* data)
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
            return new KeyValuePair<TKey, TValue>(buffer->_keys[index], UnsafeUtilities.ReadArrayElement<TValue>(buffer->_buffer, index));
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

    private readonly int _sizeOfTValue;
    private readonly int _log2MinGrowth;

    private AllocationHandle* _handle;

    public const int MINIMAL_CAPACITY = 64;

    public readonly byte* Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer;
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    public readonly int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _capacity;
    }

    public readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer != null;
    }

    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !IsCreated || _count == 0;
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

    public HashMapHelper(int capacity, int sizeOfTValue, uint minGrowth, ref AllocationHandle handle, AllocationOption allocationOption)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        if (sizeOfTValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeOfTValue), "Size of TValue must be greater than zero.");
        }

        _capacity = CalcCapacityCeilPow2(capacity);
        _bucketCapacity = _capacity * 2;

        _sizeOfTValue = sizeOfTValue;
        _log2MinGrowth = BitOperations.Log2(minGrowth);

        _handle = (AllocationHandle*)Unsafe.AsPointer(ref handle);

        var totalSize = CalculateDataSize(_capacity, _bucketCapacity, sizeOfTValue,
            out var keyOffset, out var nextOffset, out var bucketOffset);

        AllocateBuffer(totalSize, keyOffset, nextOffset, bucketOffset, allocationOption);
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int CalcCapacityCeilPow2(int capacity)
    {
        capacity = Math.Max(Math.Max(1, _count), capacity);
        var newCapacity = Math.Max(capacity, 1 << _log2MinGrowth);
        var result = MathUtilities.CeilPow2(newCapacity);

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
    private void AllocateBuffer(int totalSize, int keyOffset, int nextOffset, int bucketOffset, AllocationOption allocationOption)
    {
        var alignSize = sizeof(TKey) > sizeof(int) ? AlignOf<TKey>() : AlignOf<int>();

        _buffer = (byte*)_handle->Alloc(_handle->Allocator, (uint)totalSize, (uint)alignSize, allocationOption);
        _keys = (TKey*)(_buffer + keyOffset);
        _next = (int*)(_buffer + nextOffset);
        _buckets = (int*)(_buffer + bucketOffset);
    }

    internal void ResizeExact(int newCapacity, int newBucketCapacity)
    {
        var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, _sizeOfTValue,
            out var keyOffset, out var nextOffset, out var bucketOffset);

        var oldBuffer = _buffer;
        var oldKeys = _keys;
        var oldNext = _next;
        var oldBuckets = _buckets;
        var oldBucketCapacity = _bucketCapacity;

        AllocateBuffer(totalSize, keyOffset, nextOffset, bucketOffset, AllocationOption.None);
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

    internal void Resize(int newCapacity)
    {
        newCapacity = Math.Max(newCapacity, _count);
        var newBucketCapacity = MathUtilities.CeilPow2(newCapacity * 2);

        if (_capacity == newCapacity && _bucketCapacity == newBucketCapacity)
        {
            return;
        }

        ResizeExact(newCapacity, newBucketCapacity);
    }

    public void TrimExcess()
    {
        var capacity = CalcCapacityCeilPow2(_count);
        ResizeExact(capacity, capacity * 2);
    }

    public int Find(in TKey key)
    {
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
            while (!UnsafeUtilities.ReadArrayElement<TKey>(_keys, entryIdx).Equals(key))
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

        UnsafeUtilities.WriteArrayElement(_keys, idx, key);
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
            if (UnsafeUtilities.ReadArrayElement<TKey>(_keys, entryIdx).Equals(key))
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
        var idx = Find(key);

        if (idx != -1)
        {
            item = UnsafeUtilities.ReadArrayElement<TValue>(_buffer, idx);
            return true;
        }

        item = default;
        return false;
    }

    public bool MoveNextSearch(ref int bucketIndex, ref int nextIndex, out int index)
    {
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
        var result = new UnsafeArray<TKey>(_count, allocator);

        for (int i = 0, count = 0, max = result.Count, capacity = _bucketCapacity; i < capacity && count < max; i++)
        {
            var bucket = _buckets[i];

            while (bucket != -1)
            {
                result[count++] = UnsafeUtilities.ReadArrayElement<TKey>(_keys, bucket);
                bucket = _next[bucket];
            }
        }

        return result;
    }

    internal UnsafeArray<TValue> GetValueArray<TValue>(Allocator allocator)
        where TValue : unmanaged
    {
        var result = new UnsafeArray<TValue>(_count, allocator);

        for (int i = 0, count = 0, max = result.Count, capacity = _bucketCapacity; i < capacity && count < max; ++i)
        {
            var bucket = _buckets[i];

            while (bucket != -1)
            {
                result[count++] = UnsafeUtilities.ReadArrayElement<TValue>(_buffer, bucket);
                bucket = _next[bucket];
            }
        }

        return result;
    }

    public UnsafeArray<KeyValuePair<TKey, TValue>> GetKeyValueArrays<TValue>(Allocator allocator)
        where TValue : unmanaged
    {
        var result = new UnsafeArray<KeyValuePair<TKey, TValue>>(_count, allocator);

        for (int i = 0, count = 0, max = result.Count, capacity = _bucketCapacity; i < capacity && count < max; i++)
        {
            var bucket = _buckets[i];

            while (bucket != -1)
            {
                result[count] = new(UnsafeUtilities.ReadArrayElement<TKey>(_keys, bucket),
                    UnsafeUtilities.ReadArrayElement<TValue>(_buffer, bucket));

                count++;
                bucket = _next[bucket];
            }
        }

        return result;
    }

    public void Clear()
    {
        MemSet(_buckets, 0xff, (nuint)_bucketCapacity * sizeof(int));
        MemSet(_next, 0xff, (nuint)_capacity * sizeof(int));

        _count = 0;
        _firstFreeIndex = -1;
        _allocatedIndex = 0;
    }

    public void Dispose()
    {
        if (IsCreated)
        {
            _handle->Free(_handle->Allocator, _buffer);

            _buffer = null;
            _keys = null;
            _next = null;
            _buckets = null;

            _count = 0;
            _capacity = 0;
            _bucketCapacity = 0;
        }
    }
}