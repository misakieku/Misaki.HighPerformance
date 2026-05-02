using Misaki.HighPerformance.LowLevel.Buffer;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DisposablePtr<UnsafeParallelQueue<T>> Allocate(int capacityPerChunk, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        var pQueue = (UnsafeParallelQueue<T>*)handle.Alloc(handle.State, SizeOf<DisposablePtr<UnsafeParallelQueue<T>>>(), AlignOf<DisposablePtr<UnsafeParallelQueue<T>>>(), AllocationOption.None);
        *pQueue = new UnsafeParallelQueue<T>(capacityPerChunk, handle, allocationOption);
        return new DisposablePtr<UnsafeParallelQueue<T>>(pQueue);
    }

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

    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeParallelQueue(int capacityPerChunk, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(capacityPerChunk, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Try to enqueue an item. Expands automatically if the current chunk is full.
    /// </summary>
    public void Enqueue(scoped in T item)
    {
        SpinWait spin = new SpinWait();

        while (true)
        {
            ChunkHeader* tail = (ChunkHeader*)_tail;

            // Reserve our slot
            int tailIdx = Interlocked.Increment(ref tail->tail) - 1;

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
    public bool TryDequeue(out T item)
    {
        SpinWait spin = new SpinWait();

        while (true)
        {
            ChunkHeader* head = (ChunkHeader*)Volatile.Read(ref _head);
            if (head == null)
            {
                item = default;
                return false;
            }

            int currentHead = Volatile.Read(ref head->head);
            int currentTail = Volatile.Read(ref head->tail);

            if (currentHead >= head->capacity)
            {
                // Current chunk exhausted. Advance _head to Next chunk.
                ChunkHeader* next = (ChunkHeader*)Volatile.Read(ref *(nint*)&head->next);
                if (next != null)
                {
                    if (Interlocked.CompareExchange(ref _head, (nint)next, (nint)head) == (nint)head)
                    {
                        // Successfully unlinked this chunk from _head.
                        // If all slots have already been read, recycle it safely now!
                        if (Volatile.Read(ref head->consumedSlots) >= head->capacity)
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
                SpinWait innerWait = new SpinWait();
                while (Volatile.Read(ref slot->state) == 0)
                {
                    innerWait.SpinOnce();
                }

                item = slot->value;

                // Track how many values have been permanently read
                int consumed = Interlocked.Increment(ref head->consumedSlots);

                // We recycle only if all readers are done AND this chunk is already detached from _head 
                // (prevents ABA object reuse crashes where _head still points to a recycled memory block).
                if (consumed >= head->capacity && Volatile.Read(ref _head) != (nint)head)
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
            ChunkHeader* free = (ChunkHeader*)Volatile.Read(ref _freeList);
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
                MemClear(slots, (uint)(_chunkCapacity * sizeof(ChunkSlot)));
                return free;
            }
        }

        return AllocateNewChunk();
    }

    private readonly ChunkHeader* AllocateNewChunk()
    {
        nuint byteSize = (nuint)sizeof(ChunkHeader) + (nuint)(_chunkCapacity * sizeof(ChunkSlot));
        ChunkHeader* block = (ChunkHeader*)_allocHandle.Alloc(_allocHandle.State, byteSize, AlignOf<int>(), _allocOption);

        block->next = null;
        block->nextFree = null;
        block->capacity = _chunkCapacity;
        block->head = 0;
        block->tail = 0;
        block->consumedSlots = 0;

        var slots = (ChunkSlot*)(block + 1);
        MemClear(slots, (uint)(_chunkCapacity * sizeof(ChunkSlot)));

        return block;
    }

    private void RecycleChunk(ChunkHeader* chunk)
    {
        // Push lock-free to the free list
        while (true)
        {
            ChunkHeader* free = (ChunkHeader*)Volatile.Read(ref _freeList);
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

    public void Dispose()
    {
        if (!IsCreated)
        {
            return;
        }

        // Dispose Active Chunks
        ChunkHeader* curr = (ChunkHeader*)_head;
        while (curr != null)
        {
            var next = curr->next;
            _allocHandle.Free(_allocHandle.State, curr);
            curr = next;
        }

        // Dispose FreeList cache Chunks
        ChunkHeader* free = (ChunkHeader*)_freeList;
        while (free != null)
        {
            var next = free->nextFree;
            _allocHandle.Free(_allocHandle.State, free);
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