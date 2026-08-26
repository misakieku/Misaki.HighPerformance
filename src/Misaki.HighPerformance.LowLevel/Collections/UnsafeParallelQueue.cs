using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// A dynamically resizing, parallel, lock-free queue using unmanaged chunks.
/// Uses a very brief spin lock only during chunk allocation, alongside a lock-free segment cache.
/// </summary>
public unsafe struct UnsafeParallelQueue<T> : IDisposable
    where T : unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ChunkSlot
    {
        // 0 = Empty, 1 = Ready (Writer has finished writing)
        public int state;
        public T value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ChunkHeader
    {
        public ChunkHeader* next;
        public ChunkHeader* nextFree;
        public int capacity;

        // Cache line padding to avoid false sharing between atomic counters
        private readonly long _pad1, _pad2, _pad3;
        public int head;

        private readonly long _pad4, _pad5, _pad6;
        public int tail;

        private readonly long _pad7, _pad8, _pad9;
        public int consumedSlots;
    }

    public readonly unsafe struct ParallelProducer
    {
        private readonly UnsafeParallelQueue<T>* _queue;

        internal ParallelProducer(UnsafeParallelQueue<T>* queue)
        {
            _queue = queue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(scoped in T item)
        {
            _queue->Enqueue(item);
        }
    }

    public readonly unsafe struct ParallelConsumer
    {
        private readonly UnsafeParallelQueue<T>* _queue;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _queue->Count;
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _queue->IsEmpty;
        }

        internal ParallelConsumer(UnsafeParallelQueue<T>* queue)
        {
            _queue = queue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            return _queue->TryDequeue(out item);
        }
    }

    // Pointer representations (nint utilized for straightforward Interlocked compatibility)
    private nint _head;
    private nint _tail;
    private nint _freeList;

    private int _expandLock;

#if MHP_ENABLE_SAFETY_CHECKS
    private readonly MemoryHandle _memoryHandle;
#endif
    private readonly AllocationHandle _allocHandle;
    private readonly AllocationOption _allocOption;
    private readonly int _chunkCapacity;

    public readonly bool IsCreated => _head != 0;

    /// <summary>
    /// Gets a value indicating whether the queue contains no items.
    /// </summary>
    /// <remarks>
    /// O(1): inspects only the head chunk. Every chunk beyond the head is always non-empty
    /// (expansion writes its first slot before linking), so the queue is empty exactly when the
    /// head chunk has no unconsumed slots and no successor. Like <see cref="Count"/>, this is a
    /// best-effort snapshot while producers or consumers are running concurrently; exact when
    /// quiescent.
    /// </remarks>
    public bool IsEmpty
    {
        get
        {
            if (!IsCreated)
            {
                return true;
            }

            var head = (ChunkHeader*)Volatile.Read(ref _head);
            return Volatile.Read(ref head->head) >= Volatile.Read(ref head->tail)
                && Volatile.Read(ref *(nint*)&head->next) == 0;
        }
    }

    /// <summary>
    /// Gets the approximate number of items currently in the queue.
    /// </summary>
    /// <remarks>
    /// Walks the active chunk chain without locking. While producers or consumers are running
    /// concurrently the value is a best-effort snapshot (items mid-enqueue or mid-dequeue may be
    /// transiently miscounted); it is exact when the queue is quiescent.
    /// </remarks>
    public int Count
    {
        get
        {
            if (!IsCreated)
            {
                return 0;
            }

            var count = 0;
            var chunk = (ChunkHeader*)Volatile.Read(ref _head);
            while (chunk != null)
            {
                // tail may exceed capacity transiently due to failed reservations, and consumedSlots
                // may briefly exceed a stale tail read while a chunk is being drained, so clamp.
                var written = Math.Min(Volatile.Read(ref chunk->tail), chunk->capacity);
                var consumed = Volatile.Read(ref chunk->consumedSlots);
                count += Math.Max(0, written - consumed);

                chunk = (ChunkHeader*)Volatile.Read(ref *(nint*)&chunk->next);
            }

            return count;
        }
    }

    /// <summary>
    /// Allocates a new UnsafeParallelQueue on the heap using the provided allocation handle and returns a DisposablePtr to it.
    /// </summary>
    /// <param name="capacityPerChunk">The capacity per chunk.</param>
    /// <param name="handle">The allocation handle.</param>
    /// <param name="allocationOption">The allocation option.</param>
    /// <returns>A DisposablePtr to the allocated UnsafeParallelQueue.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DisposablePtr<UnsafeParallelQueue<T>> Allocate(int capacityPerChunk, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        var pQueue = (UnsafeParallelQueue<T>*)handle.Alloc(MemoryUtility.SizeOf<DisposablePtr<UnsafeParallelQueue<T>>>(), MemoryUtility.AlignOf<DisposablePtr<UnsafeParallelQueue<T>>>(), AllocationOption.None);
        *pQueue = new UnsafeParallelQueue<T>(capacityPerChunk, handle, allocationOption);
        return new DisposablePtr<UnsafeParallelQueue<T>>(pQueue);
    }

    /// <summary>
    /// Initializes a new instance of the UnsafeParallelQueue with the specified capacity per chunk and allocation handle.
    /// </summary>
    /// <param name="capacityPerChunk">The capacity per chunk.</param>
    /// <param name="handle">The allocation handle.</param>
    /// <param name="allocationOption">The allocation option.</param>
    public UnsafeParallelQueue(int capacityPerChunk, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        _chunkCapacity = Math.Max(32, capacityPerChunk);
        _allocHandle = handle;
        _allocOption = allocationOption;
        _freeList = 0;
        _expandLock = 0;

        // Preallocate the first chunk
        var initialChunk = AllocateNewChunk();
        _head = (nint)initialChunk;
        _tail = (nint)initialChunk;

#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle = MemoryHandle.Create(initialChunk, (nuint)(_chunkCapacity * sizeof(ChunkSlot)));
#endif
    }

    /// <summary>
    /// Try to enqueue an item. Expands automatically if the current chunk is full.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    public void Enqueue(scoped in T item)
    {
        var spin = new SpinWait();

        while (true)
        {
            var tail = (ChunkHeader*)_tail;

            // Reserve our slot
            var tailIdx = Interlocked.Increment(ref tail->tail) - 1;

            if (tailIdx < tail->capacity)
            {
                // Slot secured. Let's write.
                var slot = (ChunkSlot*)(tail + 1) + tailIdx;
                slot->value = item;
                Volatile.Write(ref slot->state, 1); // Mark as readable
                return;
            }

            // Chunk is full. Expand the queue.
            if (Interlocked.CompareExchange(ref _expandLock, 1, 0) == 0)
            {
                // Verify no other thread already expanded
                if (tail == (ChunkHeader*)_tail)
                {
                    var newChunk = GetChunkFromPoolOrAllocate();

                    // Pre-write our object onto the new chunk's first spot safely
                    newChunk->tail = 1;
                    var slot = (ChunkSlot*)(newChunk + 1) + 0;
                    slot->value = item;
                    Volatile.Write(ref slot->state, 1);

                    // Attach new chunk
                    Volatile.Write(ref *(nint*)&tail->next, (nint)newChunk);
                    Volatile.Write(ref _tail, (nint)newChunk);
                    Volatile.Write(ref _expandLock, 0);
                    return;
                }

                Volatile.Write(ref _expandLock, 0); // Release if another thread expanded
            }

            // Another thread is allocating the chunk. Spin and retry.
            spin.SpinOnce();
        }
    }

    /// <summary>
    /// Attempts to dequeue an item. 
    /// </summary>
    /// <param name="item">The dequeued item if successful; otherwise, the default value of T.</param>
    /// <returns>True if an item was dequeued successfully; otherwise, false.</returns>
    public bool TryDequeue(out T item)
    {
        var spin = new SpinWait();

        while (true)
        {
            var head = (ChunkHeader*)Volatile.Read(ref _head);
            if (head == null)
            {
                item = default;
                return false;
            }

            var currentHead = Volatile.Read(ref head->head);
            var currentTail = Volatile.Read(ref head->tail);

            if (currentHead >= head->capacity)
            {
                // Current chunk exhausted. Advance _head to Next chunk.
                var next = (ChunkHeader*)Volatile.Read(ref *(nint*)&head->next);
                if (next != null)
                {
                    if (Interlocked.CompareExchange(ref _head, (nint)next, (nint)head) == (nint)head)
                    {
                        // Successfully unlinked this chunk from _head.
                        // If all slots have already been read, claim the recycle. The claim
                        // (consumedSlots: capacity -> capacity + 1) is atomic so that a
                        // concurrent final reader cannot push the chunk to the pool twice.
                        if (Interlocked.CompareExchange(ref head->consumedSlots, head->capacity + 1, head->capacity) == head->capacity)
                        {
                            RecycleChunk(head);
                        }
                    }
                    continue;
                }
                else
                {
                    // We reached the end of the chunks, but a writer might be locking to expand right now.
                    if (Volatile.Read(ref _expandLock) == 1)
                    {
                        spin.SpinOnce();
                        continue;
                    }

                    item = default;
                    return false;
                }
            }

            // Prevent infinite loop: if head has caught up to tail, the queue chunk is empty.
            if (currentHead >= currentTail)
            {
                item = default;
                return false;
            }

            // Try to acquire the slot at currentHead lock-free
            if (Interlocked.CompareExchange(ref head->head, currentHead + 1, currentHead) == currentHead)
            {
                var slot = (ChunkSlot*)(head + 1) + currentHead;

                // Wait until the Enqueuing thread has finished writing (usually 0 spins)
                var innerWait = new SpinWait();
                while (Volatile.Read(ref slot->state) == 0)
                {
                    innerWait.SpinOnce();
                }

                item = slot->value;

                // Track how many values have been permanently read
                var consumed = Interlocked.Increment(ref head->consumedSlots);

                // We recycle only if all readers are done AND this chunk is already detached from _head
                // (prevents ABA object reuse crashes where _head still points to a recycled memory block).
                // Exactly one thread ever observes consumed == capacity; it must still win the
                // recycle claim (consumedSlots: capacity -> capacity + 1) against a concurrent
                // detacher that may already have claimed it, so the chunk is never pushed twice.
                if (consumed == head->capacity && Volatile.Read(ref _head) != (nint)head
                    && Interlocked.CompareExchange(ref head->consumedSlots, head->capacity + 1, head->capacity) == head->capacity)
                {
                    RecycleChunk(head);
                }

                return true;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ChunkHeader* GetChunkFromPoolOrAllocate()
    {
        // Pop lock-free from the free list
        while (true)
        {
            var free = (ChunkHeader*)Volatile.Read(ref _freeList);
            if (free == null)
            {
                break;
            }

            var nextFree = free->nextFree;
            if (Interlocked.CompareExchange(ref _freeList, (nint)nextFree, (nint)free) == (nint)free)
            {
                // Reset chunk
                free->next = null;
                free->nextFree = null;
                free->head = 0;
                free->tail = 0;
                free->consumedSlots = 0;

                var slots = (ChunkSlot*)(free + 1);
                MemoryUtility.MemClear(slots, (uint)(_chunkCapacity * sizeof(ChunkSlot)));
                return free;
            }
        }

        return AllocateNewChunk();
    }

    private readonly ChunkHeader* AllocateNewChunk()
    {
        var byteSize = (nuint)sizeof(ChunkHeader) + (nuint)(_chunkCapacity * sizeof(ChunkSlot));
        var block = (ChunkHeader*)_allocHandle.Alloc(byteSize, MemoryUtility.AlignOf<int>(), _allocOption);

        block->next = null;
        block->nextFree = null;
        block->capacity = _chunkCapacity;
        block->head = 0;
        block->tail = 0;
        block->consumedSlots = 0;

        var slots = (ChunkSlot*)(block + 1);
        MemoryUtility.MemClear(slots, (uint)(_chunkCapacity * sizeof(ChunkSlot)));

        return block;
    }

    private void RecycleChunk(ChunkHeader* chunk)
    {
        // Push lock-free to the free list
        while (true)
        {
            var free = (ChunkHeader*)Volatile.Read(ref _freeList);
            chunk->nextFree = free;
            if (Interlocked.CompareExchange(ref _freeList, (nint)chunk, (nint)free) == (nint)free)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Returns a parallel producer for this queue. The returned struct contains a raw pointer
    /// to the queue and can be used from multiple threads as long as the queue struct itself
    /// remains alive and its address stable.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParallelProducer AsParallelProducer()
    {
        return new ParallelProducer((UnsafeParallelQueue<T>*)Unsafe.AsPointer(ref this));
    }

    /// <summary>
    /// Returns a parallel consumer for this queue. The returned struct contains a raw pointer
    /// to the queue and can be used from multiple threads as long as the queue struct itself
    /// remains alive and its address stable.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParallelConsumer AsParallelConsumer()
    {
        return new ParallelConsumer((UnsafeParallelQueue<T>*)Unsafe.AsPointer(ref this));
    }

    /// <summary>
    /// Removes all items from the queue and resets it to its initial empty state.
    /// Active chunks are recycled into the internal chunk pool; no memory is released.
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe. It must only be called while the queue is quiescent,
    /// i.e. when no other thread is currently enqueuing or dequeuing.
    /// </remarks>
    public void Clear()
    {
        if (!IsCreated)
        {
            return;
        }

        // Recycle every active chunk back into the pool, then reclaim one as the fresh
        // head/tail chunk so the queue matches its post-construction state.
        var curr = (ChunkHeader*)_head;
        while (curr != null)
        {
            var next = curr->next;
            RecycleChunk(curr);
            curr = next;
        }

        _head = 0;
        _tail = 0;
        _expandLock = 0;

        var initialChunk = GetChunkFromPoolOrAllocate();
        _head = (nint)initialChunk;
        _tail = (nint)initialChunk;
    }

    public void Dispose()
    {
        if (!IsCreated)
        {
            return;
        }

        // Dispose Active Chunks
        var curr = (ChunkHeader*)_head;
        while (curr != null)
        {
            var next = curr->next;
            _allocHandle.Free(curr);
            curr = next;
        }

        // Dispose FreeList cache Chunks
        var free = (ChunkHeader*)_freeList;
        while (free != null)
        {
            var next = free->nextFree;
            _allocHandle.Free(free);
            free = next;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle.Dispose();
#endif

        _head = 0;
        _tail = 0;
        _freeList = 0;
    }
}