#if MHP_ENABLE_SAFETY_CHECKS
using Misaki.HighPerformance.Collections;
#endif
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Holds information about a memory allocation.
/// </summary>
public readonly struct AllocationInfo
{
    /// <summary>
    /// Gets the address of the allocated memory block.
    /// </summary>
    public IntPtr Address
    {
        get; init;
    }

    /// <summary>
    /// Gets the newSize of the allocation in bytes.
    /// </summary>
    public nuint Size
    {
        get; init;
    }

#if MHP_ENABLE_STACKTRACE
    /// <summary>
    /// Gets the stack trace at the time of allocation for debugging purposes.
    /// </summary>
    public StackTrace? StackTrace
    {
        get; init;
    }
#endif
}

public readonly struct AllocationManagerInitOpts
{
    public nuint ArenaCapacity
    {
        get; init;
    }

    public nuint StackCapacity
    {
        get; init;
    }

    public nuint FreeListChunkSize
    {
        get; init;
    }

    public nuint FreeListDefaultAlignment
    {
        get; init;
    }

    public int FreeListConcurrencyLevel
    {
        get; init;
    }

    public static AllocationManagerInitOpts Default => new AllocationManagerInitOpts
    {
        ArenaCapacity = 1024 * 1024 * 1024, // 1 GB
        StackCapacity = 16 * 1024 * 1024, // 16 MB per thread
        FreeListChunkSize = 64 * 1024 * 1024,
        FreeListDefaultAlignment = 8,
        FreeListConcurrencyLevel = Environment.ProcessorCount
    };
}

/// <summary>
/// Provides memory allocation management for native memory allocations, with support for tracking,
/// debugging, and custom allocation strategies.
/// </summary>
public static unsafe class AllocationManager
{
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
#if MHP_ENABLE_SAFETY_CHECKS
                IsValid = &IsValid
#else
                IsValid = null
#endif
            };
        }

        private static void* Allocate(void* _, nuint size, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            var ptr = AlignedAlloc(size, alignment);
            if (ptr == null)
            {
#if MHP_ENABLE_SAFETY_CHECKS
                *pHandle = MemoryHandle.Invalid;
#endif
                return null;
            }

            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(ptr, size);
            }

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = AddAllocation(ptr, size);
#endif
            return ptr;
        }

        private static void* Reallocate(void* _, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            var newPtr = AlignedRealloc(ptr, newSize, alignment);

#if MHP_ENABLE_SAFETY_CHECKS
            if (ptr == null && newPtr != null)
            {
                AddAllocation(newPtr, newSize);
            }
            else
            {
                if (newPtr == null)
                {
                    RemoveAllocation(*pHandle);
                }
                else
                {
                    UpdateAllocation(*pHandle, newPtr, newSize);
                }
            }
#endif

            if (newPtr == null)
            {
                return null;
            }

            if (allocationOption.HasFlag(AllocationOption.Clear) && newSize > oldSize)
            {
                var offset = (byte*)newPtr + oldSize;
                var clearSize = newSize - oldSize;
                MemClear(offset, clearSize);
            }

            return newPtr;
        }

        private static void Free(void* _, void* ptr
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle handle
#endif
            )
        {
            AlignedFree(ptr);
#if MHP_ENABLE_SAFETY_CHECKS
            RemoveAllocation(handle);
#endif
        }

#if MHP_ENABLE_SAFETY_CHECKS
        private static bool IsValid(void* _, MemoryHandle handle)
        {
            return ContainsAllocation(handle);
        }
#endif
    }

    private static MemoryPool<VirtualArena, VirtualArena.CreationOptions> s_arenaAllocator;
    private static MemoryPool<FreeList, FreeList.CreationOptions> s_freeListAllocator;

    [ThreadStatic]
    private static MemoryPool<VirtualStack, VirtualStack.CreationOptions> t_stackAllocator;

    private static HeapAllocator* s_pHeapAllocator;

#if MHP_ENABLE_SAFETY_CHECKS
    private static ConcurrentSlotMap<AllocationInfo> s_allocations = null!;
#endif

    /// <summary>
    /// Gets the number of live tracked heap allocations. Always returns 0 if MHP_ENABLE_SAFETY_CHECKS is disabled.
    /// </summary>
    public static int LiveAllocationCount =>
#if MHP_ENABLE_SAFETY_CHECKS
        s_allocations.Count;
#else
        0;
#endif

    private static volatile bool s_initialized;

    private static nuint s_threadLocalStackSize;
    private static readonly SpinLock s_stackLocker = new SpinLock(false);
    private static VirtualStack** s_ppStack;
    private static int s_ppStackCount;
    private static int s_ppStackCapacity;

    private static void EnsureThreadLocalStackInitialize()
    {
        if (Unsafe.IsNullRef(ref t_stackAllocator.Allocator))
        {
            t_stackAllocator = new MemoryPool<VirtualStack, VirtualStack.CreationOptions>(new VirtualStack.CreationOptions
            {
                reserveCapacity = s_threadLocalStackSize
            });

            var token = false;
            try
            {
                s_stackLocker.Enter(ref token);
                if (s_ppStack == null)
                {
                    s_ppStack = (VirtualStack**)Malloc((nuint)(sizeof(VirtualStack*) * Environment.ProcessorCount));
                    s_ppStackCapacity = Environment.ProcessorCount;
                }

                if (s_ppStackCount >= s_ppStackCapacity)
                {
                    var pOld = s_ppStack;
                    var newCapacity = s_ppStackCapacity * 2;
                    var pNew = (VirtualStack**)Realloc(pOld, (nuint)(sizeof(VirtualStack*) * newCapacity));

                    s_ppStack = pNew;
                    s_ppStackCapacity = newCapacity;
                }

                s_ppStack[s_ppStackCount] = (VirtualStack*)Unsafe.AsPointer(ref t_stackAllocator.Allocator);
                var test = s_ppStack[s_ppStackCount];
                s_ppStackCount++;
            }
            finally
            {
                if (token)
                {
                    s_stackLocker.Exit();
                }
            }
        }
    }

    public static void Initialize(AllocationManagerInitOpts opts)
    {
        if (s_initialized)
        {
            return;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        s_allocations = new ConcurrentSlotMap<AllocationInfo>(256);
#endif

        s_arenaAllocator = new MemoryPool<VirtualArena, VirtualArena.CreationOptions>(new VirtualArena.CreationOptions
        {
            reserveCapacity = opts.ArenaCapacity
        });

        s_freeListAllocator = new MemoryPool<FreeList, FreeList.CreationOptions>(new FreeList.CreationOptions
        {
            alignment = opts.FreeListDefaultAlignment,
            chunkSize = opts.FreeListChunkSize,
            maxConcurrencyLevel = opts.FreeListConcurrencyLevel
        });

        s_pHeapAllocator = (HeapAllocator*)Malloc((nuint)(sizeof(HeapAllocator)));
        s_pHeapAllocator->Init();

        s_threadLocalStackSize = opts.StackCapacity;

        s_initialized = true;
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
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        return allocator switch
        {
            Allocator.Temp => s_arenaAllocator.AllocationHandle,
            Allocator.Persistent => s_pHeapAllocator->Handle,
            Allocator.FreeList => s_freeListAllocator.AllocationHandle,
            _ => throw new ArgumentException("Target allocator type does not support custom allocation.", nameof(allocator)),
        };
    }

    /// <summary>
    /// Resets the temporary memory allocator, clearing all allocated memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetTempAllocator()
    {
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        s_arenaAllocator.Allocator.Reset();
    }

    /// <summary>
    /// Creates a new thread local stack scope for managing temporary allocations within the current context.
    /// </summary>
    /// <returns>A <see cref="Stack.Scope"/> instance representing the newly created stack scope. The scope must be disposed when no longer needed to release allocated resources.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VirtualStack.Scope CreateStackScope()
    {
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        
        EnsureThreadLocalStackInitialize();
        return t_stackAllocator.Allocator.CreateScope(t_stackAllocator.AllocationHandle);
    }

    /// <summary>
    /// Registers a memory allocation and returns a handle that can be used to manage or reference the allocated memory.
    /// </summary>
    /// <remarks>
    /// Always returns an invalid handle if MHP_ENABLE_SAFETY_CHECKS is disabled.
    /// </remarks>
    /// <param name="ptr">A pointer to the memory block to be registered. The pointer must reference a valid, allocated memory region.</param>
    /// <param name="size">The newSize of the memory block to be registered.</param>
    /// <returns>A MemoryHandle representing the registered allocation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryHandle AddAllocation(void* ptr, nuint size)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        var info = new AllocationInfo
        {
            Address = (IntPtr)ptr,
            Size = size,
#if MHP_ENABLE_STACKTRACE
            StackTrace = new StackTrace(1, true)
#endif
        };

        var id = s_allocations.Add(info, out var generation);
        return new MemoryHandle(id, generation);
#else
        return MemoryHandle.Invalid;
#endif
    }

    public static void UpdateAllocation(MemoryHandle handle, void* newPtr, nuint newSize)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        if (s_allocations.TryGetElement(handle.ID, handle.Generation, out var oldInfo))
        {
            var newInfo = oldInfo with
            {
                Address = (IntPtr)newPtr,
                Size = newSize
            };

            s_allocations.UpdateElement(handle.ID, handle.Generation, newInfo);
        }
#endif
    }

    /// <summary>
    /// Removes the memory allocation associated with the specified handle.
    /// </summary>
    /// <remarks>
    /// Always returns false if debug layer is disabled.
    /// </remarks>
    /// <param name="handle">The handle representing the memory allocation to remove. The handle must be valid and previously allocated.</param>
    /// <returns>true if the allocation was successfully removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RemoveAllocation(MemoryHandle handle)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        return s_allocations.Remove(handle.ID, handle.Generation, out var info);
#else
        return false;
#endif
    }

    /// <summary>
    /// Attempts to retrieve the memory allocation pointer associated with the specified handle.
    /// </summary>
    /// <remarks>
    /// Always returns false if debug layer is disabled, and the output pointer will be set to <see cref="IntPtr.Zero"/>.
    /// </remarks>
    /// <param name="handle">The memory handle identifying the allocation to retrieve allocation.</param>
    /// <param name="info">When this method returns, contains the allocation information associated with the specified handle, if the allocation was found; otherwise, an uninitialized value.</param>
    /// <returns>true if the allocation was found and <paramref name="info"/> contains valid allocation information; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetAllocation(MemoryHandle handle, out AllocationInfo info)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        return s_allocations.TryGetElement(handle.ID, handle.Generation, out info);
#else
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Determines whether the specified memory handle refers to a currently tracked allocation.
    /// </summary>
    /// <remarks>
    /// This only validates the memory when you added the allocation via <see cref="AddAllocation(IntPtr)"/>.
    /// For validating memory from <see cref="AllocationHandle"/>, use <see cref="AllocationHandle.IsValid"/> instead.
    /// Always returns false if debug layer is disabled.
    /// </remarks>
    /// <param name="handle">The memory handle to check for an associated allocation.</param>
    /// <returns>true if the allocation corresponding to the handle exists; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAllocation(MemoryHandle handle)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        return s_allocations.Contains(handle.ID, handle.Generation);
#else
        return false;
#endif
    }

    /// <summary>
    /// Gets the total newSize of all currently tracked allocations.
    /// </summary>
    /// <remarks>
    /// Always returns 0 if MHP_ENABLE_SAFETY_CHECKS is disabled.
    /// </remarks>
    /// <returns>The total newSize of all currently tracked allocations.</returns>
    public static nuint GetTotalAllocatedMemory()
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        nuint total = 0;
        foreach (var allocation in s_allocations)
        {
            total += allocation.Size;
        }

        return total;
#else
        return 0;
#endif
    }

    /// <summary>
    /// Disposes of the AllocationManager, freeing all allocated memory and resources.
    /// </summary>
    public static void Dispose()
    {
        if (!s_initialized)
        {
            return;
        }

        s_initialized = false;

#if MHP_ENABLE_SAFETY_CHECKS
        if (s_allocations.Count > 0)
        {
            throw new MemoryLeakException(s_allocations);
        }
#endif

        s_arenaAllocator.Dispose();
        s_freeListAllocator.Dispose();

        if (s_ppStack != null)
        {
            for (var i = 0; i < s_ppStackCount; i++)
            {
                var pStack = s_ppStack[i];
                if (pStack != null)
                {
                    pStack->Dispose();
                    Free(pStack);
                }
            }

            Free(s_ppStack);
            s_ppStack = null;
        }

        if (s_pHeapAllocator != null)
        {
            Free(s_pHeapAllocator);
            s_pHeapAllocator = null;
        }
    }
}
