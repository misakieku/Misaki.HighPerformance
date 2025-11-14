using Misaki.HighPerformance.LowLevel.Contracts;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Holds information about a memory allocation.
/// </summary>
public readonly struct AllocationInfo
{
    /// <summary>
    /// Get the size of the allocation in bytes.
    /// </summary>
    public nuint Size
    {
        get; init;
    }

    /// <summary>
    /// Get the stack trace at the time of allocation for debugging purposes.
    /// </summary>
    public StackTrace StackTrace
    {
        get; init;
    }
}

/// <summary>
/// Provides memory allocation management for native memory allocations, with support for tracking,
/// debugging, and custom allocation strategies.
/// </summary>
public static unsafe class AllocationManager
{
    // === Intrusive allocation tracking (enabled when debug layer is on) ===
    [StructLayout(LayoutKind.Sequential)]
    private struct AllocationHeader
    {
        public AllocationHeader* prev;
        public AllocationHeader* next;
        public void* basePtr;      // pointer returned by underlying allocator
        public nuint userSize;     // requested size from the user
        public nint stackHandle;   // GCHandle to managed StackTrace (stored as IntPtr)
    }

    private unsafe struct ArenaAllocator : IAllocator, IDisposable
    {
        private DynamicArena _arena;
        private AllocationHandle _handle;

        public readonly ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

        public void Init(uint initialSize)
        {
            _arena = new(initialSize);
            _handle = new(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            var selfPtr = (ArenaAllocator*)instance;
            var ptr = selfPtr->_arena.Allocate(size, alignment, allocationOption);

            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
        {
            var selfPtr = (ArenaAllocator*)instance;
            var newPtr = selfPtr->_arena.Allocate(newSize, alignment, allocationOption);
            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                if (newSize > oldSize)
                {
                    MemClear((byte*)newPtr + oldSize, newSize - oldSize);
                }
            }

            // We do not free the old pointer here, as it is managed by the arena.
            return newPtr;
        }

        private static void FreeBlock(void* instance, void* ptr)
        {
            // The arena allocator does not free individual blocks, as it manages memory in chunks.
        }

        public void Reset()
        {
            _arena.Reset();
        }

        public void Dispose()
        {
            _arena.Dispose();
        }
    }

    private unsafe struct HeapAllocator : IAllocator
    {
        private AllocationHandle _handle;

        public readonly ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

        public void Init()
        {
            _handle = new(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            return HeapAlloc(size, alignment, allocationOption);
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
        {
            return HeapRealloc(ptr, oldSize, newSize, alignment, allocationOption);
        }

        private static void FreeBlock(void* instance, void* ptr)
        {
            HeapFree(ptr);
        }
    }

    private unsafe struct StackAllocator : IAllocator
    {
        [ThreadStatic]
        private static Stack s_stack;
        private AllocationHandle _handle;

        public readonly ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

        public void Init()
        {
            _handle = new(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            var ptr = s_stack.Allocate(size, alignment, allocationOption);

            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
        {
            var newPtr = s_stack.Allocate(newSize, alignment, AllocationOption.None);
            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                if (newSize > oldSize)
                {
                    MemClear((byte*)newPtr + oldSize, newSize - oldSize);
                }
            }

            // We do not free the old pointer here, as it is managed by the stack.
            return newPtr;
        }

        private static void FreeBlock(void* instance, void* ptr)
        {
            // The stack allocator does not free individual blocks, as it manages memory in a stack-like manner.
        }

        public static Stack.Scope CreateScope()
        {
            return s_stack.CreateScope();
        }
    }

    private const uint _DEFAULT_MEMORY_POOL_SIZE = 512 * 1024; // 512 KB

    private static readonly ArenaAllocator* s_pArenaAllocator;
    private static readonly HeapAllocator* s_pHeapAllocator;
    private static readonly StackAllocator* s_pStackAllocator;

    private static bool s_debugLayer;
    private static bool s_disposed;

    private static AllocationHeader* s_liveHead;
    private static SpinLock s_liveLock;

    // Lightweight allocation counter for non-debug layer (no sizes, just count of live heap blocks)
    private static long s_activeHeapAllocations;

    static AllocationManager()
    {
        s_pArenaAllocator = (ArenaAllocator*)NativeMemory.Alloc((nuint)sizeof(ArenaAllocator));
        s_pHeapAllocator = (HeapAllocator*)NativeMemory.Alloc((nuint)sizeof(HeapAllocator));
        s_pStackAllocator = (StackAllocator*)NativeMemory.Alloc((nuint)sizeof(StackAllocator));

        s_liveLock = new SpinLock(false);

        s_pArenaAllocator->Init(_DEFAULT_MEMORY_POOL_SIZE);
        s_pHeapAllocator->Init();
        s_pStackAllocator->Init();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte* AlignUp(byte* p, nuint alignment)
    {
        var a = alignment == 0 ? (nuint)IntPtr.Size : alignment;
        return (byte*)(((nuint)p + (a - 1)) & ~(a - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GCHandle HeaderGetHandle(AllocationHeader* header) => GCHandle.FromIntPtr(header->stackHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HeaderSetHandle(AllocationHeader* header, GCHandle handle) => header->stackHandle = GCHandle.ToIntPtr(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HeaderFreeHandle(AllocationHeader* header)
    {
        if (header->stackHandle != 0)
        {
            GCHandle.FromIntPtr(header->stackHandle).Free();
            header->stackHandle = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LinkHeader(AllocationHeader* header)
    {
        var taken = false;
        try
        {
            s_liveLock.Enter(ref taken);
            header->prev = null;
            header->next = s_liveHead;
            if (s_liveHead != null)
            {
                s_liveHead->prev = header;
            }
            s_liveHead = header;
        }
        finally
        {
            if (taken)
            {
                s_liveLock.Exit();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnlinkHeader(AllocationHeader* header)
    {
        var taken = false;
        try
        {
            s_liveLock.Enter(ref taken);
            var prev = header->prev;
            var next = header->next;

            if (prev != null)
                prev->next = next;
            else
                s_liveHead = next;

            if (next != null)
                next->prev = prev;

            header->prev = header->next = null;
        }
        finally
        {
            if (taken)
            {
                s_liveLock.Exit();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void* DebugAllocate(nuint size, nuint alignment)
    {
        // Over-allocate to fit header + alignment padding; we align the user pointer, header is placed just before it.
        var pad = alignment == 0 ? (nuint)IntPtr.Size : alignment;
        var total = size + (nuint)sizeof(AllocationHeader) + (pad - 1);

        var basePtr = AlignedAlloc(total, pad);
        var user = AlignUp((byte*)basePtr + (nuint)sizeof(AllocationHeader), pad);
        var header = (AllocationHeader*)(user - (nuint)sizeof(AllocationHeader));

        header->basePtr = basePtr;
        header->userSize = size;
        HeaderSetHandle(header, GCHandle.Alloc(new StackTrace(2, true)));

        LinkHeader(header);
        return user;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DebugFree(void* userPtr)
    {
        var header = (AllocationHeader*)((byte*)userPtr - (nuint)sizeof(AllocationHeader));
        UnlinkHeader(header);
        HeaderFreeHandle(header);
        AlignedFree(header->basePtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void* DebugReallocate(void* userPtr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
    {
        if (userPtr == null)
        {
            return DebugAllocate(newSize, alignment);
        }

        var oldHeader = (AllocationHeader*)((byte*)userPtr - (nuint)sizeof(AllocationHeader));
        var handle = HeaderGetHandle(oldHeader); // preserve original allocation StackTrace

        var pad = alignment == 0 ? (nuint)IntPtr.Size : alignment;
        var total = newSize + (nuint)sizeof(AllocationHeader) + (pad - 1);
        var newBase = AlignedAlloc(total, pad);
        var newUser = AlignUp((byte*)newBase + (nuint)sizeof(AllocationHeader), pad);
        var newHeader = (AllocationHeader*)(newUser - (nuint)sizeof(AllocationHeader));

        newHeader->basePtr = newBase;
        newHeader->userSize = newSize;
        HeaderSetHandle(newHeader, handle); // transfer ownership to the new header

        LinkHeader(newHeader);

        // Mirror original behavior: copy newSize bytes
        MemCpy(newUser, userPtr, newSize);
        if (allocationOption.HasFlag(AllocationOption.Clear) && newSize > oldSize)
        {
            MemClear((byte*)newUser + oldSize, newSize - oldSize);
        }

        // Unlink and free the old block (without freeing the StackTrace handle again)
        oldHeader->stackHandle = 0;
        UnlinkHeader(oldHeader);
        AlignedFree(oldHeader->basePtr);

        return newUser;
    }

    /// <summary>
    /// Gets the number of live persistent heap allocations when the debug layer is disabled.
    /// </summary>
    public static long LiveHeapAllocationCount => Interlocked.Read(ref s_activeHeapAllocations);

    /// <summary>
    /// Enables the debug layer, allowing additional diagnostic information to be collected.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnableDebugLayer()
    {
        // To avoid ambiguity between pointers allocated before/after enabling, this must be called
        // before any heap allocations are live.
        if (Interlocked.Read(ref s_activeHeapAllocations) != 0)
        {
            throw new InvalidOperationException("EnableDebugLayer must be called before any heap allocations are active.");
        }

        s_debugLayer = true;
    }

    /// <summary>
    /// Gets a reference to the allocation handle for the specified allocator type.
    /// </summary>
    /// <param name="allocator">The allocator type for which to retrieve the allocation handle.</param>
    /// <returns>A reference to the allocation handle associated with the specified allocator type.</returns>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref AllocationHandle GetAllocationHandle(Allocator allocator)
    {
        switch (allocator)
        {
            case Allocator.Temp:
                return ref s_pArenaAllocator->Handle;
            case Allocator.Persistent:
                return ref s_pHeapAllocator->Handle;
            case Allocator.Stack:
                return ref s_pStackAllocator->Handle;
            default:
                throw new ArgumentException("Target allocator type does not support custom allocation.", nameof(allocator));
        }
    }

    /// <summary>
    /// Allocates a block of memory from the heap with the specified size and alignment, using the given allocation
    /// options.
    /// </summary>
    /// <param name="size">The number of bytes to allocate. Must be greater than zero.</param>
    /// <param name="alignment">The alignment, in bytes, for the allocated memory block. Must be a power of two.</param>
    /// <param name="allocationOption">An optional set of flags that control allocation behavior, such as whether the memory should be cleared or
    /// tracked. The default is <see cref="AllocationOption.None"/>.</param>
    /// <returns>A pointer to the beginning of the allocated memory block.</returns>
    /// <exception cref="OutOfMemoryException">Thrown if the allocation fails.</exception>
    public static void* HeapAlloc(nuint size, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        void* ptr;
        if (s_debugLayer)
        {
            ptr = DebugAllocate(size, alignment);
        }
        else
        {
            ptr = AlignedAlloc(size, alignment);
        }

        if (allocationOption.HasFlag(AllocationOption.Clear))
        {
            MemClear(ptr, size);
        }

        Interlocked.Increment(ref s_activeHeapAllocations);
        return ptr;
    }

    /// <summary>
    /// Reallocates a block of memory to a new size and alignment, optionally clearing newly allocated memory and
    /// applying allocation options.
    /// </summary>\
    /// <param name="ptr">A pointer to the previously allocated memory block to be reallocated. Can be <see langword="null"/> to allocate new memory.</param>
    /// <param name="oldSize">The size, in bytes, of the memory block currently pointed to by <paramref name="ptr"/>.</param>
    /// <param name="newSize">The desired size, in bytes, for the reallocated memory block.</param>
    /// <param name="alignment">The required alignment, in bytes, for the reallocated memory block. Must be a power of two.</param>
    /// <param name="allocationOption">An optional set of flags that control allocation behavior, such as whether to clear newly allocated memory or
    ///     track the allocation. The default is <see cref="AllocationOption.None"/>.</param>
    /// <returns>A pointer to the reallocated memory block with the specified size and alignment. Returns <see langword="null"/>
    ///     if the allocation fails.</returns>
    public static void* HeapRealloc(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (s_debugLayer)
        {
            return DebugReallocate(ptr, oldSize, newSize, alignment, allocationOption);
        }

        var newPtr = AlignedRealloc(ptr, newSize, alignment);
        if (allocationOption.HasFlag(AllocationOption.Clear)
            && newSize > oldSize)
        {
            MemClear((byte*)newPtr + oldSize, newSize - oldSize);
        }

        return newPtr;
    }

    /// <summary>
    /// Releases a block of unmanaged memory previously allocated by the heap allocator.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to be freed. The pointer must have been returned by a compatible heap allocation
    ///     method and must not be null.</param>
    public static void HeapFree(void* ptr)
    {
        if (s_debugLayer)
        {
            DebugFree(ptr);
        }
        else
        {
            AlignedFree(ptr);
        }

        Interlocked.Decrement(ref s_activeHeapAllocations);
    }

    /// <summary>
    /// Resets the temporary memory allocator, clearing all allocated memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetTempAllocator()
    {
        s_pArenaAllocator->Reset();
    }

    /// <summary>
    /// Creates a new thread local stack scope for managing temporary allocations within the current context.
    /// </summary>
    /// <returns>A <see cref="Stack.Scope"/> instance representing the newly created stack scope. The scope must be disposed when no longer needed to release allocated resources.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stack.Scope CreateStackScope()
    {
        return StackAllocator.CreateScope();
    }

    /// <summary>
    /// Disposes of the AllocationManager, freeing all allocated memory and resources.
    /// </summary>
    public static void Dispose()
    {
        if (s_disposed)
        {
            return;
        }

        // In debug mode, walk the intrusive list to surface any leaks.
        if (s_debugLayer)
        {
            var snapshot = new List<AllocationInfo>();
            var taken = false;
            try
            {
                s_liveLock.Enter(ref taken);
                if (s_liveHead != null)
                {
                    snapshot.Capacity = 128;
                    for (var p = s_liveHead; p != null; p = p->next)
                    {
                        var trace = (StackTrace)HeaderGetHandle(p).Target!;
                        snapshot.Add(new AllocationInfo
                        {
                            Size = p->userSize,
                            StackTrace = trace
                        });
                    }
                }
            }
            finally
            {
                if (taken)
                {
                    s_liveLock.Exit();
                }
            }

            nuint unfreeBytes = 0u;
            foreach (var info in snapshot)
            {
                unfreeBytes += info.Size;
            }

            if (unfreeBytes > 0u)
            {
                throw new MemoryLeakException(CollectionsMarshal.AsSpan(snapshot));
            }
        }
        else if (s_activeHeapAllocations != 0)
        {
            throw new MemoryLeakException($"Found {s_activeHeapAllocations} memory lakes! Please enable debug layer for more informations.");
        }

        if (s_pArenaAllocator != null)
        {
            s_pArenaAllocator->Dispose();
            NativeMemory.Free(s_pArenaAllocator);
        }

        if (s_pHeapAllocator != null)
        {
            NativeMemory.Free(s_pHeapAllocator);
        }

        if (s_pStackAllocator != null)
        {
            NativeMemory.Free(s_pStackAllocator);
        }

        s_activeHeapAllocations = 0;
        s_disposed = true;
    }
}