using Misaki.HighPerformance.Collections;
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
        public GCHandle stackHandle;   // GCHandle to managed StackTrace
    }

    private struct ArenaAllocator : IAllocator, IDisposable
    {
        private const int _ARENA_MAGIC_ID = -3941029;

        private DynamicArena _arena;
        private AllocationHandle _handle;
        private int _currentTick;

        public readonly AllocationHandle Handle => _handle;
        public readonly int CurrentTick => _currentTick;

        public void Init(uint initialSize)
        {
            _arena = new DynamicArena(initialSize);
            _handle = new AllocationHandle
            {
                State = Unsafe.AsPointer(ref this),
                Alloc = &Allocate,
                Realloc = &Reallocate,
                Free = null,
                IsValid = &IsValid
            };
            _currentTick = 0;
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
        {
            var selfPtr = (ArenaAllocator*)instance;
            var ptr = selfPtr->_arena.Allocate(size, alignment, allocationOption);
            if (ptr == null)
            {
                *pHandle = MemoryHandle.Invalid;
                return null;
            }

            *pHandle = new MemoryHandle(_ARENA_MAGIC_ID, selfPtr->_currentTick);
            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
        {
            if (ptr == null)
            {
                return Allocate(instance, newSize, alignment, allocationOption, pHandle);
            }

            var selfPtr = (ArenaAllocator*)instance;
            var newPtr = selfPtr->_arena.Allocate(newSize, alignment, allocationOption);
            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

            *pHandle = new MemoryHandle(_ARENA_MAGIC_ID, selfPtr->_currentTick);
            return newPtr;
        }

        private static bool IsValid(void* instance, MemoryHandle handle)
        {
            var selfPtr = (ArenaAllocator*)instance;
            return handle.id == _ARENA_MAGIC_ID && handle.generation == selfPtr->_currentTick;
        }

        public void Reset()
        {
            _arena.Reset();
            _currentTick++;
        }

        public void Dispose()
        {
            _arena.Dispose();
        }
    }

    private struct HeapAllocator : IAllocator
    {
        private AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;

        public void Init()
        {
            _handle = new AllocationHandle
            {
                State = null,
                Alloc = &Allocate,
                Realloc = &Reallocate,
                Free = &Free,
                IsValid = &IsValid
            };
        }

        private static void* Allocate(void* _, nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
        {
            return HeapAlloc(size, alignment, allocationOption, pHandle);
        }

        private static void* Reallocate(void* _, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
        {
            if (ptr == null)
            {
                return Allocate(null, newSize, alignment, allocationOption, pHandle);
            }

            MemoryHandle newHandle;
            var newPtr = HeapAlloc(newSize, alignment, allocationOption, &newHandle);
            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));
            HeapFree(ptr, *pHandle);

            *pHandle = newHandle;
            return newPtr;
        }

        private static void Free(void* _, void* ptr, MemoryHandle handle)
        {
            HeapFree(ptr, handle);
        }

        private static bool IsValid(void* _, MemoryHandle handle)
        {
            return ContainsAllocation(handle);
        }
    }

    private struct StackAllocator : IAllocator
    {
        private const int _STACK_MAGIC_ID = -6843541;

        [ThreadStatic]
        private static Stack s_stack;
        private AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;

        public void Init()
        {
            _handle = new AllocationHandle
            {
                State = Unsafe.AsPointer(ref this),
                Alloc = &Allocate,
                Realloc = &Reallocate,
                Free = null,
                IsValid = &IsValid
            };
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
        {
            var ptr = s_stack.Allocate(size, alignment, allocationOption);
            if (ptr == null)
            {
                *pHandle = MemoryHandle.Invalid;
                return null;
            }

            *pHandle = new MemoryHandle(_STACK_MAGIC_ID, (int)s_stack.Offset);
            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
        {
            if (ptr == null)
            {
                return Allocate(instance, newSize, alignment, allocationOption, pHandle);
            }

            // Optimize for last allocation. Set offset directly.
            var oldBase = s_stack.Buffer + s_stack.Offset - oldSize;
            if (ptr == oldBase)
            {
                if (newSize > oldSize)
                {
                    var diff = newSize - oldSize;
                    s_stack.Offset += diff;
                    if (allocationOption.HasFlag(AllocationOption.Clear))
                    {
                        MemClear(s_stack.Buffer + s_stack.Offset - diff, diff);
                    }
                }

                *pHandle = new MemoryHandle(_STACK_MAGIC_ID, (int)s_stack.Offset);
                return ptr;
            }

            var newPtr = s_stack.Allocate(newSize, alignment, allocationOption);
            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

            *pHandle = new MemoryHandle(_STACK_MAGIC_ID, (int)s_stack.Offset);
            return newPtr;
        }

        private static bool IsValid(void* instance, MemoryHandle handle)
        {
            return handle.id == _STACK_MAGIC_ID && handle.generation <= (int)s_stack.Offset;
        }

        public static Stack.Scope CreateScope(StackAllocator* pSelf)
        {
            return s_stack.CreateScope(pSelf->_handle);
        }
    }

    private const uint _DEFAULT_MEMORY_POOL_SIZE = 1024 * 1024; // 1 MB

    private static readonly ArenaAllocator* s_pArenaAllocator;
    private static readonly HeapAllocator* s_pHeapAllocator;
    private static readonly StackAllocator* s_pStackAllocator;

    private static bool s_debugLayer;
    private static bool s_disposed;

    private static AllocationHeader* s_pLiveHead;
    private static SpinLock s_liveLock;

    private static readonly ConcurrentSlotMap<IntPtr> s_allocations;

    public static readonly MemoryHandle MagicHandle = new MemoryHandle(int.MinValue, int.MinValue);

    /// <summary>
    /// Gets the number of live persistent heap allocations when the debug layer is disabled.
    /// </summary>
    public static int LiveAllocationCount => s_allocations.Count;

    /// <summary>
    /// Gets a value indicating whether the debug layer is currently enabled.
    /// </summary>
    public static bool IsDebugLayerEnabled => s_debugLayer;


    static AllocationManager()
    {
        var allocatorTotalSize = (nuint)(sizeof(ArenaAllocator) + sizeof(HeapAllocator) + sizeof(StackAllocator));
        var basePtr = NativeMemory.Alloc(allocatorTotalSize);
        s_pArenaAllocator = (ArenaAllocator*)basePtr;
        s_pHeapAllocator = (HeapAllocator*)((byte*)basePtr + (nuint)sizeof(ArenaAllocator));
        s_pStackAllocator = (StackAllocator*)((byte*)basePtr + (nuint)(sizeof(ArenaAllocator) + sizeof(HeapAllocator)));

        s_liveLock = new SpinLock(false);

        s_allocations = new ConcurrentSlotMap<IntPtr>(256);

        s_pArenaAllocator->Init(_DEFAULT_MEMORY_POOL_SIZE);
        s_pHeapAllocator->Init();
        s_pStackAllocator->Init();

        s_pLiveHead = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte* AlignUp(byte* p, nuint alignment)
    {
        var a = alignment == 0 ? (nuint)IntPtr.Size : alignment;
        return (byte*)(((nuint)p + (a - 1)) & ~(a - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GCHandle HeaderGetHandle(AllocationHeader* header) => header->stackHandle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HeaderSetHandle(AllocationHeader* header, GCHandle handle) => header->stackHandle = handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HeaderFreeHandle(AllocationHeader* header)
    {
        if (header->stackHandle.IsAllocated)
        {
            header->stackHandle.Free();
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
            header->next = s_pLiveHead;
            if (s_pLiveHead != null)
            {
                s_pLiveHead->prev = header;
            }
            s_pLiveHead = header;
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
            {
                prev->next = next;
            }
            else
            {
                s_pLiveHead = next;
            }

            if (next != null)
            {
                next->prev = prev;
            }

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
            MemClear(newUser + oldSize, newSize - oldSize);
        }

        // Unlink and free the old block (without freeing the StackTrace pHandle again)
        //oldHeader->stackHandle = GCHandle.FromIntPtr(0);
        UnlinkHeader(oldHeader);
        AlignedFree(oldHeader->basePtr);

        return newUser;
    }

    /// <summary>
    /// Enables the debug layer, allowing additional diagnostic information to be collected.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnableDebugLayer()
    {
        // To avoid ambiguity between pointers allocated before/after enabling, this must be called
        // before any heap allocations are live.
        if (s_allocations.Count != 0)
        {
            throw new InvalidOperationException("EnableDebugLayer must be called before any allocations are active.");
        }

        s_debugLayer = true;
    }

    /// <summary>
    /// Gets a reference to the allocation pHandle for the specified allocator type.
    /// </summary>
    /// <param name="allocator">The allocator type for which to retrieve the allocation pHandle.</param>
    /// <returns>A reference to the allocation pHandle associated with the specified allocator type.</returns>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllocationHandle GetAllocationHandle(Allocator allocator)
    {
        return allocator switch
        {
            Allocator.Temp => s_pArenaAllocator->Handle,
            Allocator.Persistent => s_pHeapAllocator->Handle,
            _ => throw new ArgumentException("Target allocator type does not support custom allocation.", nameof(allocator)),
        };
    }

    /// <summary>
    /// Allocates a block of memory from the heap with the specified size and alignment, using the given allocation options.
    /// </summary>
    /// <remarks>
    /// This will allocate memory from the heap. If the debug layer is enabled, additional tracking information will be recorded.
    /// The memory handle is always tracked unless the <see cref="AllocationOption.Untrack"/> flag is specified.
    /// </remarks>
    /// <param name="size">The number of bytes to allocate. Must be greater than zero.</param>
    /// <param name="alignment">The alignment, in bytes, for the allocated memory block. Must be a power of two.</param>
    /// <param name="allocationOption">An optional set of flags that control allocation behavior, such as whether the memory should be cleared or
    /// tracked. The default is <see cref="AllocationOption.None"/>.</param>
    /// <returns>A pointer to the beginning of the allocated memory block.</returns>
    /// <exception cref="OutOfMemoryException">Thrown if the allocation fails.</exception>
    public static void* HeapAlloc(nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        var isUntrack = allocationOption.HasFlag(AllocationOption.Untrack);

        void* ptr;
        if (s_debugLayer && !isUntrack)
        {
            ptr = DebugAllocate(size, alignment);
        }
        else
        {
            ptr = AlignedAlloc(size, alignment);
        }

        if (ptr == null)
        {
            *pHandle = MemoryHandle.Invalid;
            return null;
        }

        if (allocationOption.HasFlag(AllocationOption.Clear))
        {
            MemClear(ptr, size);
        }

        if (isUntrack)
        {
            *pHandle = MagicHandle;
        }
        else
        {
            *pHandle = AddAllocation((IntPtr)ptr);
        }

        return ptr;
    }

    /// <summary>
    /// Releases a block of unmanaged memory previously allocated by the heap allocator.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to be freed. The pointer must have been returned by a compatible heap allocation
    ///     method and must not be null.</param>
    /// <param name="handle">The handle representing the memory allocation to free. The handle must be valid and previously allocated.</param>
    public static void HeapFree(void* ptr, MemoryHandle handle)
    {
        if (s_debugLayer && handle != MagicHandle)
        {
            DebugFree(ptr);
        }
        else
        {
            AlignedFree(ptr);
        }

        RemoveAllocation(handle);
    }

    /// <summary>
    /// Releases a block of unmanaged memory previously allocated by the heap allocator.
    /// </summary>
    /// <param name="handle">The handle representing the memory allocation to free. The handle must be valid and previously allocated.</param>
    public static void HeapFree(MemoryHandle handle)
    {
        if (TryGetAllocation(handle, out var ptr))
        {
            HeapFree((void*)ptr, handle);
        }
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
        return StackAllocator.CreateScope(s_pStackAllocator);
    }

    /// <summary>
    /// Registers a memory allocation and returns a handle that can be used to manage or reference the allocated memory.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to be registered. The pointer must reference a valid, allocated memory region.</param>
    /// <returns>A MemoryHandle representing the registered allocation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryHandle AddAllocation(IntPtr ptr)
    {
        var id = s_allocations.Add(ptr, out var generation);
        return new MemoryHandle(id, generation);
    }

    /// <summary>
    /// Removes the memory allocation associated with the specified handle.
    /// </summary>
    /// <param name="handle">The handle representing the memory allocation to remove. The handle must be valid and previously allocated.</param>
    /// <returns>true if the allocation was successfully removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RemoveAllocation(MemoryHandle handle)
    {
        return s_allocations.Remove(handle.id, handle.generation);
    }

    /// <summary>
    /// Attempts to retrieve the memory allocation pointer associated with the specified handle.
    /// </summary>
    /// <param name="handle">The memory handle identifying the allocation to retrieve allocation.</param>
    /// <param name="ptr">When this method returns, contains the pointer to the memory allocation if found; otherwise, <see cref="IntPtr.Zero"/>.</param>
    /// <returns>true if the allocation was found and <paramref name="ptr"/> contains a valid pointer; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetAllocation(MemoryHandle handle, out IntPtr ptr)
    {
        return s_allocations.TryGetElement(handle.id, handle.generation, out ptr);
    }

    /// <summary>
    /// Determines whether the specified memory handle refers to a currently tracked allocation.
    /// </summary>
    /// <remarks>
    /// This only validates the memory when you added the allocation via <see cref="AddAllocation(IntPtr)"/>.
    /// For validating memory from <see cref="AllocationHandle"/>, use <see cref="AllocationHandle.IsValid"/> instead.
    /// </remarks>
    /// <param name="handle">The memory handle to check for an associated allocation.</param>
    /// <returns>true if the allocation corresponding to the handle exists; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAllocation(MemoryHandle handle)
    {
        if (handle == MagicHandle)
        {
            return true;
        }

        return s_allocations.Contains(handle.id, handle.generation);
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
                if (s_pLiveHead != null)
                {
                    snapshot.Capacity = 128;
                    for (var p = s_pLiveHead; p != null; p = p->next)
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

        if (LiveAllocationCount != 0)
        {
            throw new MemoryLeakException($"Found {LiveAllocationCount} memory lakes! Please enable debug layer for more informations.");
        }

        // NOTE: Arena allocator holds the base ptr for all allocators, heap and stack allocators do not own any memory themselves.
        if (s_pArenaAllocator != null)
        {
            s_pArenaAllocator->Dispose();

            Stack.DisposeAll();

            NativeMemory.Free(s_pArenaAllocator);
        }

        s_disposed = true;
    }
}
