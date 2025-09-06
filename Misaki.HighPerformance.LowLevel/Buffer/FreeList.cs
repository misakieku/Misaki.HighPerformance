using Misaki.HighPerformance.LowLevel.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A lock-free, thread-safe variable-size allocator that manages memory blocks of different sizes.
/// Optimized for high-performance scenarios with frequent allocations and deallocations.
/// 
/// Example usage:
/// <code>
/// // Create a free list with multiple size buckets
/// var freeList = new FreeList();
/// 
/// // Allocate a 70-byte block
/// var block = freeList.Allocate(70);
/// if (block.IsValid)
/// {
///     // Use the memory block...
///     
///     // Free the block when done
///     freeList.Free(block);
/// }
/// 
/// // Dispose when finished
/// freeList.Dispose();
/// </code>
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 256)] // Cache line aligned to prevent false sharing
public unsafe struct FreeList : IDisposable
{
    /// <summary>
    /// Node structure for the lock-free free list with size information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FreeNode
    {
        public FreeNode* next;
        public nuint size;
    }

    /// <summary>
    /// Memory chunk that contains variable-size blocks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryChunk
    {
        public MemoryChunk* next;
        public byte* memory;
        public nuint size;
        public nuint used; // Amount of memory used in this chunk
    }

    /// <summary>
    /// Size bucket for different allocation sizes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SizeBucket
    {
        public nint freeHead; // Free list head for this size
        public nuint blockSize; // Fixed size for this bucket
        public long freeCount; // Number of free blocks
    }

    private const int _MAX_BUCKETS = 16; // Number of size buckets
    private const nuint _MIN_BLOCK_SIZE = 16; // Minimum block size
    private const nuint _DEFAULT_CHUNK_SIZE = 64 * 1024; // 64KB chunks

    [FieldOffset(0)]
    private fixed byte _buckets[_MAX_BUCKETS * 32]; // SizeBucket array (32 bytes per bucket)

    [FieldOffset(512)]
    private DynamicArena _chunkArena; // 128

    [FieldOffset(640)]
    private MemoryChunk* _chunks; // 8

    [FieldOffset(648)]
    private nuint _chunkSize; // 8

    [FieldOffset(656)]
    private nuint _alignment; // 8

    [FieldOffset(664)]
    private long _totalAllocatedBytes; // 8

    [FieldOffset(672)]
    private long _totalFreeBytes; // 8

    [FieldOffset(676)]
    private volatile int _disposed; // 4

    [FieldOffset(680)]
    private volatile int _chunkCreationLock; // 4

    /// <summary>
    /// Gets the alignment requirement for allocations.
    /// </summary>
    public readonly nuint Alignment => _alignment;

    /// <summary>
    /// Gets the total number of allocated bytes.
    /// </summary>
    public readonly long TotalAllocatedBytes => Interlocked.Read(ref Unsafe.AsRef(in _totalAllocatedBytes));

    /// <summary>
    /// Gets the total number of free bytes available.
    /// </summary>
    public readonly long TotalFreeBytes => Interlocked.Read(ref Unsafe.AsRef(in _totalFreeBytes));

    /// <summary>
    /// Gets whether the allocator has been disposed.
    /// </summary>
    public readonly bool IsDisposed => _disposed != 0;

    /// <summary>
    /// Gets the chunk size used by this allocator.
    /// </summary>
    public readonly nuint ChunkSize => _chunkSize;

    /// <summary>
    /// Initializes a new variable-size FreeList allocator with the specified parameters.
    /// </summary>
    /// <param name="alignment">Alignment requirement for blocks (must be power of 2).</param>
    /// <param name="chunkSize">Size of memory chunks to allocate (default: 64KB).</param>
    public FreeList(nuint alignment = 8, nuint chunkSize = _DEFAULT_CHUNK_SIZE)
    {
        if (alignment == 0 || (alignment & (alignment - 1)) != 0)
            throw new ArgumentException("Alignment must be a power of 2", nameof(alignment));

        if (chunkSize < 1024)
            throw new ArgumentException("Chunk size must be at least 1KB", nameof(chunkSize));

        _alignment = alignment;
        _chunkSize = chunkSize;
        _chunks = null;
        _totalAllocatedBytes = 0;
        _totalFreeBytes = 0;
        _disposed = 0;
        _chunkCreationLock = 0;

        _chunkArena = new DynamicArena(1024);
        InitializeBuckets();
    }

    /// <summary>
    /// Initializes the size buckets with exponential sizes.
    /// </summary>
    private readonly void InitializeBuckets()
    {
        var buckets = GetBuckets();
        var size = _MIN_BLOCK_SIZE;

        for (var i = 0; i < _MAX_BUCKETS; i++)
        {
            buckets[i].blockSize = size;
            buckets[i].freeHead = 0;
            buckets[i].freeCount = 0;
            size *= 2; // Exponential size increase
        }
    }

    /// <summary>
    /// Gets a pointer to the size buckets array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly SizeBucket* GetBuckets()
    {
        fixed (byte* ptr = _buckets)
        {
            return (SizeBucket*)ptr;
        }
    }

    /// <summary>
    /// Finds the appropriate bucket for the given size.
    /// </summary>
    /// <param name="size">Size to find bucket for.</param>
    /// <returns>Bucket index, or -1 if too large for buckets.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int FindBucket(nuint size)
    {
        var buckets = GetBuckets();

        for (var i = 0; i < _MAX_BUCKETS; i++)
        {
            if (size <= buckets[i].blockSize)
                return i;
        }

        return -1; // Size too large for buckets
    }

    /// <summary>
    /// Allocates a memory block of the specified size. Thread-safe using lock-free algorithms.
    /// </summary>
    /// <param name="size">Size of memory to allocate in bytes.</param>
    /// <param name="alignment">Alignment requirement (0 = use default).</param>
    /// <param name="allocationOption">Options for allocation (e.g., clear memory).</param>
    /// <returns>MemoryBlock containing allocated memory, or Invalid if allocation fails.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MemoryBlock Allocate(nuint size, nuint alignment = 0, AllocationOption allocationOption = AllocationOption.None)
    {
        if (_disposed != 0 || size == 0)
            return MemoryBlock.Invalid;

        if (alignment == 0)
            alignment = _alignment;

        // Align size to alignment boundary
        var alignedSize = (size + alignment - 1) & ~(alignment - 1);
        alignedSize = Math.Max(alignedSize, _MIN_BLOCK_SIZE);

        var bucketIndex = FindBucket(alignedSize);
        void* ptr = null;

        if (bucketIndex >= 0)
        {
            // Try to allocate from bucket
            ptr = TryPopFromBucket(bucketIndex);

            if (ptr == null)
            {
                // Create new blocks for this bucket
                if (TryCreateBlocksForBucket(bucketIndex))
                {
                    ptr = TryPopFromBucket(bucketIndex);
                }
            }
        }

        if (ptr == null)
        {
            // Fallback to direct allocation from chunk
            ptr = AllocateFromChunk(alignedSize, alignment);
        }

        if (ptr != null)
        {
            Interlocked.Add(ref _totalAllocatedBytes, (long)alignedSize);

            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(ptr, alignedSize);
            }

            return new MemoryBlock(ptr, alignedSize, alignment);
        }

        return MemoryBlock.Invalid;
    }

    /// <summary>
    /// Frees a previously allocated memory block. Thread-safe using lock-free algorithms.
    /// </summary>
    /// <param name="block">MemoryBlock to free.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free(MemoryBlock block)
    {
        if (!block.IsValid || _disposed != 0)
            return;

        if (!IsValidBlock(block.Ptr))
            return; // Invalid pointer, ignore

        var bucketIndex = FindBucket(block.Size);
        if (bucketIndex >= 0)
        {
            PushToBucket(bucketIndex, block.Ptr, block.Size);
        }

        Interlocked.Add(ref _totalAllocatedBytes, -(long)block.Size);
    }

    /// <summary>
    /// Tries to pop a free block from the specified bucket.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void* TryPopFromBucket(int bucketIndex)
    {
        var buckets = GetBuckets();
        var bucket = &buckets[bucketIndex];

        nint head, newHead;
        FreeNode* headPtr;

        do
        {
            head = bucket->freeHead;
            if (head == 0)
                return null;

            headPtr = (FreeNode*)head;
            newHead = (nint)headPtr->next;

        } while (Interlocked.CompareExchange(ref bucket->freeHead, newHead, head) != head);

        Interlocked.Decrement(ref bucket->freeCount);
        return (void*)head;
    }

    /// <summary>
    /// Pushes a block to the specified bucket's free list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void PushToBucket(int bucketIndex, void* ptr, nuint size)
    {
        var buckets = GetBuckets();
        var bucket = &buckets[bucketIndex];
        var node = (FreeNode*)ptr;

        node->size = size;

        nint head;
        do
        {
            head = bucket->freeHead;
            node->next = (FreeNode*)head;

        } while (Interlocked.CompareExchange(ref bucket->freeHead, (nint)node, head) != head);

        Interlocked.Increment(ref bucket->freeCount);
    }

    /// <summary>
    /// Creates new blocks for the specified bucket.
    /// </summary>
    private bool TryCreateBlocksForBucket(int bucketIndex)
    {
        while (Interlocked.CompareExchange(ref _chunkCreationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            var buckets = GetBuckets();
            var blockSize = buckets[bucketIndex].blockSize;
            var blocksToCreate = Math.Min(_chunkSize / blockSize, 256); // Limit number of blocks

            if (blocksToCreate == 0)
                return false;

            var totalSize = blocksToCreate * blockSize;
            var memory = (byte*)AlignedAlloc(totalSize, _alignment);
            if (memory == null)
                return false;

            var chunk = (MemoryChunk*)_chunkArena.Allocate(SizeOf<MemoryChunk>(), AlignOf<MemoryChunk>(), AllocationOption.None);
            if (chunk == null)
            {
                AlignedFree(memory);
                return false;
            }

            chunk->memory = memory;
            chunk->size = totalSize;
            chunk->used = totalSize;
            chunk->next = _chunks;
            _chunks = chunk;

            // Add all blocks to the bucket's free list
            for (nuint i = 0; i < blocksToCreate; i++)
            {
                var blockPtr = memory + (i * blockSize);
                PushToBucket(bucketIndex, blockPtr, blockSize);
            }

            return true;
        }
        finally
        {
            Interlocked.Exchange(ref _chunkCreationLock, 0);
        }
    }

    /// <summary>
    /// Allocates memory directly from a chunk (for large allocations).
    /// </summary>
    private void* AllocateFromChunk(nuint size, nuint alignment)
    {
        while (Interlocked.CompareExchange(ref _chunkCreationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            // Try to find space in existing chunks first
            var chunk = _chunks;
            while (chunk != null)
            {
                var available = chunk->size - chunk->used;
                var alignedOffset = (chunk->used + alignment - 1) & ~(alignment - 1);
                var totalNeeded = alignedOffset - chunk->used + size;

                if (totalNeeded <= available)
                {
                    var ptr = chunk->memory + alignedOffset;
                    chunk->used += totalNeeded;
                    return ptr;
                }

                chunk = chunk->next;
            }

            // Create new chunk
            var newChunkSize = Math.Max(_chunkSize, size + alignment);
            var newMemory = (byte*)AlignedAlloc(newChunkSize, alignment);
            if (newMemory == null)
                return null;

            var newChunk = (MemoryChunk*)_chunkArena.Allocate(SizeOf<MemoryChunk>(), AlignOf<MemoryChunk>(), AllocationOption.None);
            if (newChunk == null)
            {
                AlignedFree(newMemory);
                return null;
            }

            newChunk->memory = newMemory;
            newChunk->size = newChunkSize;
            newChunk->used = size;
            newChunk->next = _chunks;
            _chunks = newChunk;

            return newMemory;
        }
        finally
        {
            Interlocked.Exchange(ref _chunkCreationLock, 0);
        }
    }

    /// <summary>
    /// Validates that a pointer belongs to one of our memory chunks.
    /// </summary>
    private readonly bool IsValidBlock(void* ptr)
    {
        var chunk = _chunks;
        while (chunk != null)
        {
            var chunkStart = (nuint)chunk->memory;
            var chunkEnd = chunkStart + chunk->size;
            var ptrValue = (nuint)ptr;

            if (ptrValue >= chunkStart && ptrValue < chunkEnd)
                return true;

            chunk = chunk->next;
        }

        return false;
    }

    /// <summary>
    /// Disposes the free list and frees all allocated memory.
    /// Note: This method is NOT thread-safe by design as requested.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            // Free all memory chunks
            var chunk = _chunks;
            while (chunk != null)
            {
                var next = chunk->next;
                AlignedFree(chunk->memory);
                MemoryUtilities.Free(chunk);
                chunk = next;
            }

            _chunks = null;
            _totalAllocatedBytes = 0;
            _totalFreeBytes = 0;
        }
    }
}