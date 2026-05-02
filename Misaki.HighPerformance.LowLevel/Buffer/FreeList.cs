#if true
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A variable-size allocator that uses per-thread caches for the hot path and a remote-free queue for cross-thread deallocation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FreeList : IMemoryAllocator<FreeList, FreeList.CreationOptions>
{
    public struct CreationOptions
    {
        public nuint alignment;
        public nuint chunkSize;
        public int maxConcurrencyLevel;
    }

    public static FreeList Create(in CreationOptions opts)
    {
        return new FreeList(opts.alignment, opts.chunkSize);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FreeNode
    {
        public FreeNode* next;
        public MemoryChunk* ownerChunk;
        public byte bucketIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryChunk
    {
        public MemoryChunk* next;
        public byte* memory;
        public nuint size;
        public nuint used;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct SizeBucket
    {
        [FieldOffset(0)]
        public long freeCount;
        [FieldOffset(8)]
        public nint freeHead;
        [FieldOffset(16)]
        public nuint blockSize;
        [FieldOffset(24)]
        public int creationLock;
    }

    [StructLayout(LayoutKind.Explicit, Size = 648)]
    private struct ThreadCache
    {
        [FieldOffset(0)]
        public fixed byte buckets[_MAX_BUCKETS * 32];
        [FieldOffset(512)]
        public int threadId;
        [FieldOffset(516)]
        public int active;

        // Padding to prevent false sharing on remoteFreeHead
        [FieldOffset(576)]
        public nint remoteFreeHead;
        [FieldOffset(584)]
        public ThreadCache* next;
        [FieldOffset(592)]
        public ThreadCache* inactiveNext;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct BlockHeader
    {
        [FieldOffset(0)]
        public MemoryChunk* ownerChunk;
        [FieldOffset(8)]
        public ThreadCache* ownerCache;
        [FieldOffset(16)]
        public uint magicNumber;
        [FieldOffset(20)]
        public byte bucketIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SharedState
    {
        public int isDisposed;
        public ThreadCache* headCache;
        public ThreadCache* inactiveCacheHead;
    }

    private class CacheReclaimer
    {
        private readonly ThreadCache* _cache;
        private readonly SharedState* _state;

        public CacheReclaimer(ThreadCache* cache, SharedState* state)
        {
            _cache = cache;
            _state = state;
        }

        ~CacheReclaimer()
        {
            if (_cache != null && Volatile.Read(ref _state->isDisposed) == 0)
            {
                Volatile.Write(ref _cache->active, 0);
                ThreadCache* current;
                do
                {
                    current = (ThreadCache*)Volatile.Read(ref *(nint*)&_state->inactiveCacheHead);
                    _cache->inactiveNext = current;
                }
                while (Interlocked.CompareExchange(ref *(nint*)&_state->inactiveCacheHead, (nint)_cache, (nint)current) != (nint)current);
            }
        }
    }

    private const byte _MAX_BUCKETS = 16;
    private const int _DEFAULT_MAX_CONCURRENCY_LEVEL = 1;
    private const int _OVERFLOW_CACHE_INDEX = 0;
    private const nuint _MIN_BLOCK_SIZE = 16;
    private const nuint _DEFAULT_CHUNK_SIZE = 64 * 1024;
    private const uint _MAGIC_NUMBER = 0xDEADBEEF;

    [ThreadStatic]
    private static ThreadCache* t_localCache;

    [ThreadStatic]
    private static void* t_ownerId;

    [ThreadStatic]
    private static CacheReclaimer? t_cacheReclaimer;

    private void* _instanceId;
    private DynamicArena _chunkArena;
    private MemoryChunk* _chunks;
    private readonly nuint _chunkSize;
    private readonly nuint _alignment;
    private volatile int _disposed;
    private volatile int _chunkCreationLock;
    private volatile int _cacheRegistrationLock;

    /// <summary>
    /// Gets the alignment requirement for allocations.
    /// </summary>
    public readonly nuint Alignment => _alignment;

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

        try
        {
            var state = (SharedState*)Malloc((nuint)sizeof(SharedState));
            state->isDisposed = 0;
            state->headCache = null;
            state->inactiveCacheHead = null;

            _instanceId = state;

            _chunks = null;
            _disposed = 0;
            _chunkCreationLock = 0;
            _cacheRegistrationLock = 0;
            _chunkArena = new DynamicArena(1024);
        }
        catch
        {
            if (_instanceId != null)
            {
                MemoryUtility.Free(_instanceId);
                _instanceId = null;
            }

            _chunkArena.Dispose();

            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SizeBucket* GetBuckets(ThreadCache* cache)
    {
        return (SizeBucket*)cache->buckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeBuckets(ThreadCache* cache)
    {
        var buckets = GetBuckets(cache);
        var size = _MIN_BLOCK_SIZE;

        for (var i = 0; i < _MAX_BUCKETS; i++)
        {
            buckets[i].blockSize = size;
            buckets[i].freeHead = 0;
            buckets[i].freeCount = 0;
            buckets[i].creationLock = 0;
            size *= 2;
        }

        cache->remoteFreeHead = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte FindBucket(nuint size)
    {
        var blockSize = _MIN_BLOCK_SIZE;
        for (byte i = 0; i < _MAX_BUCKETS; i++)
        {
            if (size <= blockSize)
            {
                return i;
            }

            blockSize <<= 1;
        }

        return byte.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ThreadCache* CreateCacheForThread(int threadId)
    {
        var cache = (ThreadCache*)_chunkArena.Allocate(SizeOf<ThreadCache>(), AlignOf<ThreadCache>(), AllocationOption.Clear);
        if (cache == null)
        {
            return null;
        }

        InitializeBuckets(cache);
        cache->threadId = threadId;
        cache->active = 1;
        return cache;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void DrainRemoteFrees(ThreadCache* cache)
    {
        if (Volatile.Read(ref cache->remoteFreeHead) == 0)
        {
            return;
        }

        var head = (FreeNode*)Interlocked.Exchange(ref cache->remoteFreeHead, 0);
        while (head != null)
        {
            var next = head->next;
            PushToBucket(cache, head->bucketIndex, head, head->ownerChunk);
            head = next;
        }
    }

    private ThreadCache* RegisterThreadCache()
    {
        if (_instanceId == null || _disposed != 0)
        {
            return null;
        }

        var state = (SharedState*)_instanceId;

        if (Volatile.Read(ref state->isDisposed) != 0)
        {
            return null;
        }

        var threadId = Environment.CurrentManagedThreadId;
        ThreadCache* cacheToUse = null;

        while (true)
        {
            cacheToUse = (ThreadCache*)Volatile.Read(ref *(nint*)&state->inactiveCacheHead);
            if (cacheToUse == null)
            {
                break;
            }

            var nextInactive = cacheToUse->inactiveNext;
            if (Interlocked.CompareExchange(ref *(nint*)&state->inactiveCacheHead, (nint)nextInactive, (nint)cacheToUse) == (nint)cacheToUse)
            {
                cacheToUse->threadId = threadId;
                Volatile.Write(ref cacheToUse->active, 1);
                break;
            }
        }

        if (cacheToUse == null)
        {
            var spinWait = new SpinWait();
            while (Interlocked.CompareExchange(ref _cacheRegistrationLock, 1, 0) != 0)
            {
                spinWait.SpinOnce();
            }

            try
            {
                cacheToUse = CreateCacheForThread(threadId);
                if (cacheToUse != null)
                {
                    cacheToUse->next = state->headCache;
                    state->headCache = cacheToUse;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _cacheRegistrationLock, 0);
            }
        }

        if (cacheToUse != null)
        {
            t_ownerId = _instanceId;
            t_localCache = cacheToUse;
            t_cacheReclaimer = new CacheReclaimer(cacheToUse, state);
        }

        return cacheToUse;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ThreadCache* GetCurrentCache()
    {
        if (t_ownerId == _instanceId && t_localCache != null)
        {
            return t_localCache;
        }

        return RegisterThreadCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void* TryPopFromBucket(ThreadCache* cache, byte bucketIndex)
    {
        var buckets = GetBuckets(cache);
        var bucket = &buckets[bucketIndex];
        var head = (FreeNode*)bucket->freeHead;
        if (head == null)
        {
            return null;
        }

        bucket->freeHead = (nint)head->next;
        bucket->freeCount--;

        AssignBlockHeader((BlockHeader*)head, head->ownerChunk, head->bucketIndex, cache);
        return head;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void PushToBucket(ThreadCache* cache, byte bucketIndex, void* ptr, MemoryChunk* ownerChunk)
    {
        var buckets = GetBuckets(cache);
        var bucket = &buckets[bucketIndex];
        var node = (FreeNode*)ptr;
        node->ownerChunk = ownerChunk;
        node->bucketIndex = bucketIndex;
        node->next = (FreeNode*)bucket->freeHead;
        bucket->freeHead = (nint)node;
        bucket->freeCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssignBlockHeader(BlockHeader* header, MemoryChunk* ownerChunk, byte bucketIndex, ThreadCache* ownerCache)
    {
        header->ownerChunk = ownerChunk;
        header->bucketIndex = bucketIndex;
        header->magicNumber = _MAGIC_NUMBER;
        header->ownerCache = ownerCache;
    }

    private bool TryCreateBlocksForBucket(ThreadCache* cache, byte bucketIndex)
    {
        var buckets = GetBuckets(cache);
        var bucket = &buckets[bucketIndex];

        var spinWait = new SpinWait();
        while (Interlocked.CompareExchange(ref bucket->creationLock, 1, 0) != 0)
        {
            spinWait.SpinOnce();
        }

        try
        {
            DrainRemoteFrees(cache);
            if (bucket->freeHead != 0)
            {
                return true;
            }

            var blockSize = bucket->blockSize;
            var blocksToCreate = Math.Max(1u, _chunkSize / blockSize);
            blocksToCreate = Math.Min(blocksToCreate, 256);

            if (blocksToCreate == 0)
            {
                return false;
            }

            var totalSize = blocksToCreate * blockSize;
            var memory = AllocateFromChunk(totalSize, _alignment, out var chunk);
            if (memory == null)
            {
                return false;
            }

            for (nuint i = 0; i < blocksToCreate; i++)
            {
                var blockStartPtr = memory + (i * blockSize);
                PushToBucket(cache, bucketIndex, blockStartPtr, chunk);
            }

            return true;
        }
        finally
        {
            Interlocked.Exchange(ref bucket->creationLock, 0);
        }
    }

    private byte* AllocateFromChunk(nuint size, nuint alignment, out MemoryChunk* ownerChunk)
    {
        var spinWait = new SpinWait();
        while (Interlocked.CompareExchange(ref _chunkCreationLock, 1, 0) != 0)
        {
            spinWait.SpinOnce();
        }

        try
        {
            var chunk = _chunks;
            while (chunk != null)
            {
                var alignedOffset = (chunk->used + alignment - 1) & ~(alignment - 1);
                var totalNeeded = alignedOffset - chunk->used + size;
                var available = chunk->size - chunk->used;

                if (totalNeeded <= available)
                {
                    var memory = chunk->memory + alignedOffset;
                    chunk->used = alignedOffset + size;
                    ownerChunk = chunk;
                    return memory;
                }
                chunk = chunk->next;
            }

            var newChunkSize = Math.Max(_chunkSize, size); // 默认保底 64KB
            var newMemory = (byte*)AlignedAlloc(newChunkSize, alignment);
            if (newMemory == null)
            {
                ownerChunk = null;
                return null;
            }

            var newChunk = (MemoryChunk*)_chunkArena.Allocate(SizeOf<MemoryChunk>(), AlignOf<MemoryChunk>(), AllocationOption.None);
            newChunk->memory = newMemory;
            newChunk->size = newChunkSize;
            newChunk->used = size;
            newChunk->next = _chunks;
            _chunks = newChunk;

            ownerChunk = newChunk;
            return newMemory;
        }
        finally
        {
            Interlocked.Exchange(ref _chunkCreationLock, 0);
        }
    }

    /// <summary>
    /// Allocates a memory block of the specified size.
    /// </summary>
    /// <remarks>
    /// This is thread safe.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (_disposed != 0)
        {
            return null;
        }

        if (size == 0)
        {
            return null;
        }

        if (alignment == 0)
        {
            alignment = _alignment;
        }

        if ((alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        }

        var alignedSize = (size + alignment - 1) & ~(alignment - 1);
        alignedSize = Math.Max(alignedSize, _MIN_BLOCK_SIZE);

        var totalSize = alignedSize + (nuint)sizeof(BlockHeader);
        var bucketIndex = FindBucket(totalSize);
        var cache = GetCurrentCache();

        try
        {
            void* ptr = null;
            if (bucketIndex != byte.MaxValue)
            {
                ptr = TryPopFromBucket(cache, bucketIndex);
                if (ptr == null)
                {
                    DrainRemoteFrees(cache);

                    ptr = TryPopFromBucket(cache, bucketIndex);
                    if (ptr == null && TryCreateBlocksForBucket(cache, bucketIndex))
                    {
                        ptr = TryPopFromBucket(cache, bucketIndex);
                    }
                }
            }
            else
            {
                // Oversized block: Bypass chunk linking entirely and go straight to the OS
                ptr = AlignedAlloc(totalSize, alignment);
                if (ptr != null)
                {
                    // Pass null for ownerChunk so 'Free' knows this is a standalone allocation
                    AssignBlockHeader((BlockHeader*)ptr, null, bucketIndex, cache);
                }
            }

            if (ptr == null)
            {
                return null;
            }

            var header = (BlockHeader*)ptr;
            header->ownerCache = cache;

            var userPtr = (byte*)ptr + sizeof(BlockHeader);
            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(userPtr, alignedSize);
            }

            return userPtr;
        }
        finally
        {

        }
    }

    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (_disposed != 0)
        {
            return null;
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr != null && ptr != null)
        {
            var copySize = Math.Min(oldSize, newSize);
            MemCpy(newPtr, ptr, copySize);
            Free(ptr);
        }

        return newPtr;
    }

    /// <summary>
    /// Frees a previously allocated memory block.
    /// </summary>
    /// <remarks>
    /// This is thread safe.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Free(void* ptr)
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

        var chunk = header->ownerChunk;
        if (chunk == null)
        {
            return;
        }

        var targetCache = header->ownerCache;
        var bucketIndex = header->bucketIndex;

        if (bucketIndex == byte.MaxValue)
        {
            // This is an oversized allocation. It doesn't belong to a bucket or a chunk.
            // Erase the magic number for safety and instantly yield it back to the OS.
            header->magicNumber = 0;
            AlignedFree(blockStartPtr);
            return;
        }

        var sameThread = t_ownerId == _instanceId && t_localCache == targetCache;

        if (sameThread)
        {
            PushToBucket(targetCache, bucketIndex, blockStartPtr, chunk);
            return;
        }

        var remoteNode = (FreeNode*)blockStartPtr;
        remoteNode->ownerChunk = chunk;
        remoteNode->bucketIndex = bucketIndex;

        nint head;
        do
        {
            head = targetCache->remoteFreeHead;
            remoteNode->next = (FreeNode*)head;
        } while (Interlocked.CompareExchange(ref targetCache->remoteFreeHead, (nint)remoteNode, head) != head);
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        if (_instanceId != null)
        {
            var state = (SharedState*)_instanceId;
            Volatile.Write(ref state->isDisposed, 1);

            var current = state->headCache;
            while (current != null)
            {
                DrainRemoteFrees(current);
                current->active = 0;
                current = current->next;
            }

            MemoryUtility.Free(_instanceId);
            _instanceId = null;
        }

        var arena = _chunkArena;
        var chunk = _chunks;
        _chunks = null;

        while (chunk != null)
        {
            var next = chunk->next;
            AlignedFree(chunk->memory);
            chunk = next;
        }

        arena.Dispose();
    }
}
#else
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A variable-size allocator that uses per-thread caches for the hot path and a remote-free queue for cross-thread deallocation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FreeList : IMemoryAllocator<FreeList, FreeList.CreationOptions>
{
    public struct CreationOptions
    {
        public nuint alignment;
        public nuint chunkSize;
        public int maxConcurrencyLevel;
    }

    public static FreeList Create(in CreationOptions opts)
    {
        return new FreeList(opts.alignment, opts.chunkSize, opts.maxConcurrencyLevel);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FreeNode
    {
        public FreeNode* next;
        public MemoryChunk* ownerChunk;
        public nuint blockSize;
        public int bucketIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryChunk
    {
        public MemoryChunk* next;
        public byte* memory;
        public nuint size;
        public nuint used;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct SizeBucket
    {
        [FieldOffset(0)]
        public long freeCount;
        [FieldOffset(8)]
        public nint freeHead;
        [FieldOffset(16)]
        public nuint blockSize;
        [FieldOffset(24)]
        public int creationLock;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ThreadCache
    {
        public fixed byte buckets[_MAX_BUCKETS * 32];
        public nint remoteFreeHead;
        public int threadId;
        public int active;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct BlockHeader
    {
        [FieldOffset(0)]
        public MemoryChunk* ownerChunk;
        [FieldOffset(8)]
        public nuint blockSize;
        [FieldOffset(16)]
        public ulong magicNumber;
        [FieldOffset(24)]
        public int ownerCacheIndex;
    }

    private const int _MAX_BUCKETS = 16;
    private const int _DEFAULT_MAX_CONCURRENCY_LEVEL = 1;
    private const int _OVERFLOW_CACHE_INDEX = 0;
    private const nuint _MIN_BLOCK_SIZE = 16;
    private const nuint _DEFAULT_CHUNK_SIZE = 64 * 1024;
    private const ulong _MAGIC_NUMBER = 0xDEADBEEFDEADBEEF;

    [ThreadStatic]
    private static int t_cacheIndex;

    [ThreadStatic]
    private static void* t_ownerId;

    private void* _instanceId;
    private ThreadCache** _caches;
    private DynamicArena _chunkArena;
    private MemoryChunk* _chunks;
    private readonly nuint _chunkSize;
    private readonly nuint _alignment;
    private readonly int _maxConcurrencyLevel;
    private int _cacheCount;
    private volatile int _disposed;
    private volatile int _chunkCreationLock;
    private volatile int _cacheRegistrationLock;
    private volatile int _overflowLock;

    /// <summary>
    /// Gets the alignment requirement for allocations.
    /// </summary>
    public readonly nuint Alignment => _alignment;

    /// <summary>
    /// Gets the chunk size used by this allocator.
    /// </summary>
    public readonly nuint ChunkSize => _chunkSize;

    /// <summary>
    /// Gets the maximum number of dedicated thread caches.
    /// </summary>
    public readonly int MaxConcurrencyLevel => _maxConcurrencyLevel;

    /// <summary>
    /// Initializes a new variable-size FreeList allocator with the specified parameters.
    /// </summary>
    /// <param name="alignment">Alignment requirement for blocks (must be power of 2).</param>
    /// <param name="chunkSize">Size of memory chunks to allocate (default: 64KB).</param>
    /// <param name="maxConcurrencyLevel">Maximum number of dedicated thread caches.</param>
    public FreeList(nuint alignment, nuint chunkSize = _DEFAULT_CHUNK_SIZE, int maxConcurrencyLevel = _DEFAULT_MAX_CONCURRENCY_LEVEL)
    {
        if (alignment == 0 || (alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of 2", nameof(alignment));
        }

        if (chunkSize < 1024)
        {
            throw new ArgumentException("Chunk size must be at least 1KB", nameof(chunkSize));
        }

        if (maxConcurrencyLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrencyLevel), "Max concurrency level must be greater than zero.");
        }

        _alignment = alignment;
        _chunkSize = chunkSize;
        _maxConcurrencyLevel = maxConcurrencyLevel;

        try
        {
            _instanceId = Malloc((nuint)sizeof(nint));

            _chunks = null;
            _cacheCount = 0;
            _disposed = 0;
            _chunkCreationLock = 0;
            _cacheRegistrationLock = 0;
            _overflowLock = 0;
            _chunkArena = new DynamicArena(1024);
            _caches = (ThreadCache**)Malloc((nuint)sizeof(ThreadCache*) * (nuint)(maxConcurrencyLevel + 1));

            for (var i = 0; i <= maxConcurrencyLevel; i++)
            {
                _caches[i] = null;
            }

            var overflowCache = CreateCacheForThread(0);
            if (overflowCache == null)
            {
                throw new OutOfMemoryException("Failed to initialize free list overflow cache.");
            }

            _caches[_OVERFLOW_CACHE_INDEX] = overflowCache;

        }
        catch
        {
            if (_instanceId != null)
            {
                Free(_instanceId);
                _instanceId = null;
            }

            if (_caches != null)
            {
                Free(_caches);
                _caches = null;
            }

            _chunkArena.Dispose();

            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SizeBucket* GetBuckets(ThreadCache* cache)
    {
        return (SizeBucket*)cache->buckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeBuckets(ThreadCache* cache)
    {
        var buckets = GetBuckets(cache);
        var size = _MIN_BLOCK_SIZE;

        for (var i = 0; i < _MAX_BUCKETS; i++)
        {
            buckets[i].blockSize = size;
            buckets[i].freeHead = 0;
            buckets[i].freeCount = 0;
            buckets[i].creationLock = 0;
            size *= 2;
        }

        cache->remoteFreeHead = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindBucket(nuint size)
    {
        var blockSize = _MIN_BLOCK_SIZE;
        for (var i = 0; i < _MAX_BUCKETS; i++)
        {
            if (size <= blockSize)
            {
                return i;
            }

            blockSize <<= 1;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ThreadCache* CreateCacheForThread(int threadId)
    {
        var cache = (ThreadCache*)_chunkArena.Allocate(SizeOf<ThreadCache>(), AlignOf<ThreadCache>(), AllocationOption.Clear);
        if (cache == null)
        {
            return null;
        }

        InitializeBuckets(cache);
        cache->threadId = threadId;
        cache->active = 1;
        return cache;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void DrainRemoteFrees(ThreadCache* cache)
    {
        var head = (FreeNode*)Interlocked.Exchange(ref cache->remoteFreeHead, 0);
        while (head != null)
        {
            var next = head->next;
            PushToBucket(cache, head->bucketIndex, head, head->ownerChunk, head->blockSize);
            head = next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ThreadCache* GetOverflowCache()
    {
        return _caches[_OVERFLOW_CACHE_INDEX];
    }

    private ThreadCache* RegisterThreadCache()
    {
        while (Interlocked.CompareExchange(ref _cacheRegistrationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            if (t_ownerId == _instanceId && t_cacheIndex > 0 && t_cacheIndex <= _cacheCount)
            {
                return _caches[t_cacheIndex];
            }

            if (_cacheCount >= _maxConcurrencyLevel)
            {
                t_ownerId = _instanceId;
                t_cacheIndex = _OVERFLOW_CACHE_INDEX;
                return GetOverflowCache();
            }

            var threadId = Environment.CurrentManagedThreadId;
            var cache = CreateCacheForThread(threadId);
            if (cache == null)
            {
                t_ownerId = _instanceId;
                t_cacheIndex = _OVERFLOW_CACHE_INDEX;
                return GetOverflowCache();
            }

            _cacheCount++;
            _caches[_cacheCount] = cache;

            t_ownerId = _instanceId;
            t_cacheIndex = _cacheCount;
            return cache;
        }
        finally
        {
            Interlocked.Exchange(ref _cacheRegistrationLock, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ThreadCache* GetCurrentCache()
    {
        if (t_ownerId == _instanceId)
        {
            var index = t_cacheIndex;
            if ((uint)index <= (uint)_cacheCount)
            {
                var cache = _caches[index];
                if (cache != null)
                {
                    return cache;
                }
            }
        }

        return RegisterThreadCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void* TryPopFromBucket(ThreadCache* cache, int cacheIndex, int bucketIndex)
    {
        var buckets = GetBuckets(cache);
        var bucket = &buckets[bucketIndex];
        var head = (FreeNode*)bucket->freeHead;
        if (head == null)
        {
            return null;
        }

        bucket->freeHead = (nint)head->next;
        bucket->freeCount--;

        AssignBlockHeader((BlockHeader*)head, head->ownerChunk, head->blockSize, cacheIndex);
        return head;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void PushToBucket(ThreadCache* cache, int bucketIndex, void* ptr, MemoryChunk* ownerChunk, nuint blockSize)
    {
        var buckets = GetBuckets(cache);
        var bucket = &buckets[bucketIndex];
        var node = (FreeNode*)ptr;
        node->ownerChunk = ownerChunk;
        node->blockSize = blockSize;
        node->bucketIndex = bucketIndex;
        node->next = (FreeNode*)bucket->freeHead;
        bucket->freeHead = (nint)node;
        bucket->freeCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssignBlockHeader(BlockHeader* header, MemoryChunk* ownerChunk, nuint blockSize, int ownerCacheIndex)
    {
        header->ownerChunk = ownerChunk;
        header->blockSize = blockSize;
        header->magicNumber = _MAGIC_NUMBER;
        header->ownerCacheIndex = ownerCacheIndex;
    }

    private bool TryCreateBlocksForBucket(ThreadCache* cache, int cacheIndex, int bucketIndex)
    {
        var buckets = GetBuckets(cache);
        var bucket = &buckets[bucketIndex];

        while (Interlocked.CompareExchange(ref bucket->creationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            DrainRemoteFrees(cache);
            if (bucket->freeHead != 0)
            {
                return true;
            }

            var blockSize = bucket->blockSize;
            var blocksToCreate = Math.Max(1u, _chunkSize / blockSize);
            blocksToCreate = Math.Min(blocksToCreate, 256);

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

            while (Interlocked.CompareExchange(ref _chunkCreationLock, 1, 0) != 0)
            {
                Thread.SpinWait(1);
            }

            try
            {
                chunk->memory = memory;
                chunk->size = totalSize;
                chunk->used = totalSize;
                chunk->next = _chunks;
                _chunks = chunk;
            }
            finally
            {
                Interlocked.Exchange(ref _chunkCreationLock, 0);
            }

            for (nuint i = 0; i < blocksToCreate; i++)
            {
                var blockStartPtr = memory + (i * blockSize);
                PushToBucket(cache, bucketIndex, blockStartPtr, chunk, blockSize);
            }

            return true;
        }
        finally
        {
            Interlocked.Exchange(ref bucket->creationLock, 0);
        }
    }

    private void* AllocateFromChunk(int cacheIndex, nuint size, nuint alignment)
    {
        while (Interlocked.CompareExchange(ref _chunkCreationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            var chunk = _chunks;
            while (chunk != null)
            {
                var alignedOffset = (chunk->used + alignment - 1) & ~(alignment - 1);
                var totalNeeded = alignedOffset - chunk->used + size;
                var available = chunk->size - chunk->used;

                if (totalNeeded <= available)
                {
                    var blockStartPtr = chunk->memory + alignedOffset;
                    chunk->used = alignedOffset + size;
                    AssignBlockHeader((BlockHeader*)blockStartPtr, chunk, size, cacheIndex);
                    return blockStartPtr;
                }

                chunk = chunk->next;
            }

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

            AssignBlockHeader((BlockHeader*)newMemory, newChunk, size, cacheIndex);
            return newMemory;
        }
        finally
        {
            Interlocked.Exchange(ref _chunkCreationLock, 0);
        }
    }

    /// <summary>
    /// Allocates a memory block of the specified size.
    /// </summary>
    /// <remarks>
    /// This is thread safe.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (_disposed != 0)
        {
            return null;
        }

        if (size == 0)
        {
            return null;
        }

        if (alignment == 0)
        {
            alignment = _alignment;
        }

        if ((alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        }

        var alignedSize = (size + alignment - 1) & ~(alignment - 1);
        alignedSize = Math.Max(alignedSize, _MIN_BLOCK_SIZE);

        var totalSize = alignedSize + (nuint)sizeof(BlockHeader);
        var bucketIndex = FindBucket(totalSize);
        var cache = GetCurrentCache();
        var cacheIndex = t_cacheIndex;
        var requiresOverflowLock = cacheIndex == _OVERFLOW_CACHE_INDEX;

        if (requiresOverflowLock)
        {
            while (Interlocked.CompareExchange(ref _overflowLock, 1, 0) != 0)
            {
                Thread.SpinWait(1);
            }
        }

        try
        {
            DrainRemoteFrees(cache);

            void* ptr = null;
            if (bucketIndex >= 0)
            {
                ptr = TryPopFromBucket(cache, cacheIndex, bucketIndex);
                if (ptr == null && TryCreateBlocksForBucket(cache, cacheIndex, bucketIndex))
                {
                    ptr = TryPopFromBucket(cache, cacheIndex, bucketIndex);
                }
            }
            else
            {
                // Oversized block: Bypass chunk linking entirely and go straight to the OS
                ptr = AlignedAlloc(totalSize, alignment);
                if (ptr != null)
                {
                    // Pass null for ownerChunk so 'Free' knows this is a standalone allocation
                    AssignBlockHeader((BlockHeader*)ptr, null, totalSize, cacheIndex);
                }
            }

            if (ptr == null)
            {
                return null;
            }

            var header = (BlockHeader*)ptr;
            header->ownerCacheIndex = cacheIndex;

            var userPtr = (byte*)ptr + sizeof(BlockHeader);
            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(userPtr, alignedSize);
            }

            return userPtr;
        }
        finally
        {
            if (requiresOverflowLock)
            {
                Interlocked.Exchange(ref _overflowLock, 0);
            }
        }
    }

    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (_disposed != 0)
        {
            return null;
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr != null && ptr != null)
        {
            var copySize = Math.Min(oldSize, newSize);
            MemCpy(newPtr, ptr, copySize);
            Free(ptr);
        }

        return newPtr;
    }

    /// <summary>
    /// Frees a previously allocated memory block.
    /// </summary>
    /// <remarks>
    /// This is thread safe.
    /// </remarks>
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

        var chunk = header->ownerChunk;
        if (chunk == null)
        {
            return;
        }

        var blockSize = header->blockSize;
        var ownerCacheIndex = header->ownerCacheIndex;
        var bucketIndex = FindBucket(blockSize);

        if (bucketIndex < 0)
        {
            // This is an oversized allocation. It doesn't belong to a bucket or a chunk.
            // Erase the magic number for safety and instantly yield it back to the OS.
            header->magicNumber = 0;
            AlignedFree(blockStartPtr);
            return;
        }

        var sameThread = t_ownerId == _instanceId && t_cacheIndex == ownerCacheIndex;
        var targetCache = ownerCacheIndex >= 0 && ownerCacheIndex <= _cacheCount ? _caches[ownerCacheIndex] : null;
        if (targetCache == null)
        {
            targetCache = GetOverflowCache();
            ownerCacheIndex = _OVERFLOW_CACHE_INDEX;
            sameThread = t_ownerId == _instanceId && t_cacheIndex == ownerCacheIndex;
        }

        if (sameThread)
        {
            if (ownerCacheIndex == _OVERFLOW_CACHE_INDEX)
            {
                while (Interlocked.CompareExchange(ref _overflowLock, 1, 0) != 0)
                {
                    Thread.SpinWait(1);
                }

                try
                {
                    PushToBucket(targetCache, bucketIndex, blockStartPtr, chunk, blockSize);
                }
                finally
                {
                    Interlocked.Exchange(ref _overflowLock, 0);
                }
            }
            else
            {
                PushToBucket(targetCache, bucketIndex, blockStartPtr, chunk, blockSize);
            }

            return;
        }

        var remoteNode = (FreeNode*)blockStartPtr;
        remoteNode->ownerChunk = chunk;
        remoteNode->blockSize = blockSize;
        remoteNode->bucketIndex = bucketIndex;

        nint head;
        do
        {
            head = targetCache->remoteFreeHead;
            remoteNode->next = (FreeNode*)head;
        } while (Interlocked.CompareExchange(ref targetCache->remoteFreeHead, (nint)remoteNode, head) != head);
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        if (_caches != null)
        {
            for (var i = 0; i <= _cacheCount; i++)
            {
                var cache = _caches[i];
                if (cache != null)
                {
                    DrainRemoteFrees(cache);
                    cache->active = 0;
                }
            }
        }

        if (_caches != null)
        {
            MemoryUtility.Free(_caches);
            _caches = null;
        }

        if (_instanceId != null)
        {
            MemoryUtility.Free(_instanceId);
            _instanceId = null;
        }

        var arena = _chunkArena;
        var chunk = _chunks;
        _chunks = null;

        while (chunk != null)
        {
            var next = chunk->next;
            AlignedFree(chunk->memory);
            chunk = next;
        }

        arena.Dispose();
    }
}
#endif