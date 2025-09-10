using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A lock-free, thread-safe variable-size allocator that manages memory blocks of different sizes.
/// Optimized for high-performance scenarios with frequent allocations and deallocations.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 256)] // Cache line aligned to prevent false sharing
public unsafe struct FreeList : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FreeNode
    {
        public FreeNode* next;
        public nuint size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryChunk
    {
        public MemoryChunk* next;
        public byte* memory;
        public nuint size;
        public nuint used; // Amount of memory used in this chunk
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct SizeBucket
    {
        [FieldOffset(0)]
        public long freeCount; // Number of free blocks
        [FieldOffset(8)]
        public nint freeHead; // Free list head for this size
        [FieldOffset(16)]
        public nuint blockSize; // Fixed size for this bucket
        [FieldOffset(24)]
        public int creationLock;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct BlockHeader
    {
        // Ensure the size is fixed across x86 and x64
        [FieldOffset(0)]
        public MemoryChunk* ownerChunk;
        [FieldOffset(8)]
        public nuint blockSize;
        [FieldOffset(16)]
        public ulong magicNumber;
    }

    private const int _MAX_BUCKETS = 16; // Number of size buckets
    private const nuint _MIN_BLOCK_SIZE = 16; // Minimum block size
    private const nuint _DEFAULT_CHUNK_SIZE = 64 * 1024; // 64KB chunks
    private const ulong _MAGIC_NUMBER = 0xDEADBEEFDEADBEEF; // For validating blocks

    [FieldOffset(0)]
    private fixed byte _buckets[_MAX_BUCKETS * 32]; // SizeBucket array (32 bytes per bucket)

    [FieldOffset(512)]
    private DynamicArena _chunkArena; // 128

    [FieldOffset(640)]
    private MemoryChunk* _chunks; // 8

    [FieldOffset(648)]
    private readonly nuint _chunkSize; // 8

    [FieldOffset(656)]
    private readonly nuint _alignment; // 8

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
    public FreeList(nuint alignment, nuint chunkSize = _DEFAULT_CHUNK_SIZE)
    {
        if (alignment == 0 || (alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of 2", nameof(alignment));
        }

        if (chunkSize < 1024)
        {
            throw new ArgumentException("Chunk size must be at least 1KB", nameof(chunkSize));
        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly SizeBucket* GetBuckets()
    {
        fixed (byte* ptr = _buckets)
        {
            return (SizeBucket*)ptr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int FindBucket(nuint size)
    {
        var buckets = GetBuckets();

        for (var i = 0; i < _MAX_BUCKETS; i++)
        {
            if (size <= buckets[i].blockSize)
            {
                return i;
            }
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
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (_disposed != 0 || size == 0)
        {
            return null;
        }

        if (alignment == 0)
        {
            alignment = _alignment;
        }

        // Align size to alignment boundary
        var alignedSize = (size + alignment - 1) & ~(alignment - 1);
        alignedSize = Math.Max(alignedSize, _MIN_BLOCK_SIZE);

        var totalSize = alignedSize + (nuint)sizeof(BlockHeader);

        var bucketIndex = FindBucket(totalSize);
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
            ptr = AllocateFromChunk(totalSize, alignment);
        }

        if (ptr != null)
        {
            var header = (BlockHeader*)ptr;
            Interlocked.Add(ref _totalAllocatedBytes, (long)header->blockSize);

            var pUserData = (byte*)ptr + sizeof(BlockHeader);
            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(pUserData, alignedSize);
            }

            return pUserData;
        }

        return null;
    }

    /// <summary>
    /// Frees a previously allocated memory block. Thread-safe using lock-free algorithms.
    /// </summary>
    /// <param name="block">MemoryBlock to free.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free(void* ptr)
    {
        if (_disposed != 0 || ptr == null)
        {
            return;
        }

        var blockStartPtr = (byte*)ptr - sizeof(BlockHeader);
        var header = (BlockHeader*)blockStartPtr;

        if (header->magicNumber != _MAGIC_NUMBER)
        {
            return;
        }

        var chuck = header->ownerChunk;
        if (chuck == null)
        {
            return;
        }

        var bucketIndex = FindBucket(header->blockSize);
        if (bucketIndex >= 0)
        {
            PushToBucket(bucketIndex, blockStartPtr, header->blockSize);
        }

        Interlocked.Add(ref _totalAllocatedBytes, -(long)header->blockSize);
        header->ownerChunk = null;
        header->blockSize = 0;
        header->magicNumber = 0;
    }

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
            {
                return null;
            }

            headPtr = (FreeNode*)head;
            newHead = (nint)headPtr->next;

        } while (Interlocked.CompareExchange(ref bucket->freeHead, newHead, head) != head);

        Interlocked.Decrement(ref bucket->freeCount);
        return (void*)head;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssignBlockHeader(BlockHeader* header, MemoryChunk* ownerChunk, nuint blockSize)
    {
        header->ownerChunk = ownerChunk;
        header->blockSize = blockSize;
        header->magicNumber = _MAGIC_NUMBER;
    }

    private bool TryCreateBlocksForBucket(int bucketIndex)
    {
        var buckets = GetBuckets();
        var bucket = &buckets[bucketIndex];

        while (Interlocked.CompareExchange(ref bucket->creationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            if (bucket->freeHead != 0)
            {
                return true; // Another thread did the work for us!
            }

            var blockSize = bucket->blockSize;
            var blocksToCreate = Math.Min(_chunkSize / blockSize, 256); // Limit number of blocks

            if (blocksToCreate == 0)
            {
                return false;
            }

            var totalSize = blocksToCreate * blockSize;
            var memory = (byte*)AlignedAlloc(totalSize, _alignment);
            if (memory == null)
            {
                return false;
            }

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
                var blockStartPtr = memory + (i * blockSize);

                AssignBlockHeader((BlockHeader*)blockStartPtr, chunk, blockSize);
                PushToBucket(bucketIndex, blockStartPtr, blockSize);
            }

            return true;
        }
        finally
        {
            Interlocked.Exchange(ref bucket->creationLock, 0);
        }
    }

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
                    var blockStartPtr = chunk->memory + alignedOffset;

                    // Write the header and return the pointer WITH the header
                    AssignBlockHeader((BlockHeader*)blockStartPtr, chunk, size);
                    return blockStartPtr;
                }

                chunk = chunk->next;
            }

            // Create new chunk
            var newChunkSize = Math.Max(_chunkSize, size + alignment);
            var newMemory = (byte*)AlignedAlloc(newChunkSize, alignment);
            if (newMemory == null)
            {
                return null;
            }

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

            // Write the header and return the pointer WITH the header
            AssignBlockHeader((BlockHeader*)newMemory, newChunk, size);
            return newMemory;
        }
        finally
        {
            Interlocked.Exchange(ref _chunkCreationLock, 0);
        }
    }

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
                chunk = next;
            }

            _chunkArena.Dispose();

            _chunks = null;
            _totalAllocatedBytes = 0;
            _totalFreeBytes = 0;
        }
    }
}