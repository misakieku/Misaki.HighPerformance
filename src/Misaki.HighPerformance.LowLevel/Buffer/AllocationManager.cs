#if MHP_ENABLE_SAFETY_CHECKS
using Misaki.HighPerformance.Collections;
#endif
using Misaki.HighPerformance.LowLevel.Utilities;
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
    /// Gets the address of the allocated memory block.
    /// </summary>
    public IntPtr Address
    {
        get; init;
    }

    /// <summary>
    /// Gets the size of the allocation in bytes.
    /// </summary>
    public nuint Size
    {
        get; init;
    }

#if MHP_ENABLE_SAFETY_CHECKS
    /// <summary>
    /// Tracks the index in the thread-local allocation list for diagnostic checks.
    /// </summary>
    public int ThreadLocalIndex
    {
        get; init;
    }
#endif

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

public struct AllocationManagerDesc
{
    public nuint ArenaCapacity
    {
        get; set;
    }

    public nuint StackCapacity
    {
        get; set;
    }

    public nuint FreeListChunkSize
    {
        get; set;
    }

    public nuint FreeListDefaultAlignment
    {
        get; set;
    }

    public nuint TLSFAlignment
    {
        get; set;
    }

    public nuint TLSFInitialChunkSize
    {
        get; set;
    }

    public static AllocationManagerDesc Default => new AllocationManagerDesc
    {
        ArenaCapacity = 1024 * 1024 * 1024, // 1 GB
        StackCapacity = 32 * 1024 * 1024, // 32 MB per thread
        FreeListDefaultAlignment = 16,
        FreeListChunkSize = 64 * 1024,
        TLSFAlignment = 16,
        TLSFInitialChunkSize = 64 * 1024 * 1024, // 64 MB
    };
}

/// <summary>
/// Provides memory allocation management for native memory allocations, with support for tracking,
/// debugging, and custom allocation strategies.
/// </summary>
public static unsafe class AllocationManager
{
    internal struct HeapAllocator : IAllocator
    {
        private AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;

        public void Init()
        {
            _handle = new AllocationHandle(null, &Allocate, &Reallocate, &Free);
        }

        private static void* Allocate(void* _, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            var ptr = MemoryUtility.AlignedAlloc(size, alignment);
            if (ptr == null)
            {
                return null;
            }

            if (allocationOption.HasOption(AllocationOption.Clear))
            {
                MemoryUtility.MemClear(ptr, size);
            }

            return ptr;
        }

        private static void* Reallocate(void* _, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
        {
            var newPtr = MemoryUtility.AlignedRealloc(ptr, newSize, alignment);
            if (newPtr == null)
            {
                return null;
            }

            if (allocationOption.HasOption(AllocationOption.Clear) && newSize > oldSize)
            {
                var offset = (byte*)newPtr + oldSize;
                var clearSize = newSize - oldSize;
                MemoryUtility.MemClear(offset, clearSize);
            }

            return newPtr;
        }

        private static void Free(void* _, void* ptr)
        {
            MemoryUtility.AlignedFree(ptr);
        }
    }

    // TODO: Lock-free implementation
    internal struct TLSFAllocator : IAllocator
    {
        private static readonly Lock s_lock = new Lock();

        private TLSF _tlsf;
        private AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;

        public void Init(nuint alignment, nuint initialChunkSize)
        {
            _tlsf = new TLSF(alignment, initialChunkSize);
            _handle = new AllocationHandle(Unsafe.AsPointer(in this), &Allocate, &Reallocate, &Free);
        }

        private static void* Allocate(void* state, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            var allocator = (TLSFAllocator*)state;

            lock (s_lock)
            {
                return allocator->_tlsf.Allocate(size, alignment, allocationOption);
            }
        }

        private static void* Reallocate(void* state, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
        {
            var allocator = (TLSFAllocator*)state;

            lock (s_lock)
            {
                return allocator->_tlsf.Reallocate(ptr, oldSize, newSize, alignment, allocationOption);
            }
        }

        private static void Free(void* state, void* ptr)
        {
            var allocator = (TLSFAllocator*)state;

            lock (s_lock)
            {
                allocator->_tlsf.Free(ptr);
            }
        }
    }

    private class ThreadLocalStackPool
    {
        public MemoryPool<VirtualStack, VirtualStack.CreationOptions> pool;

        public ThreadLocalStackPool(nuint stackCapacity)
        {
            pool = new MemoryPool<VirtualStack, VirtualStack.CreationOptions>(new VirtualStack.CreationOptions
            {
                reserveCapacity = stackCapacity
            });
        }

        ~ThreadLocalStackPool()
        {
            pool.Dispose();
        }
    }

    internal static MemoryPool<VirtualArena, VirtualArena.CreationOptions> s_arenaAllocator;
    internal static MemoryPool<FreeList, FreeList.CreationOptions> s_freeListAllocator;
    internal static HeapAllocator* s_pHeapAllocator;
    internal static TLSFAllocator* s_pTLSFAllocator;

    [ThreadStatic]
    private static ThreadLocalStackPool? t_stackAllocator;

#if MHP_ENABLE_SAFETY_CHECKS
    internal static ConcurrentSlotMap<AllocationInfo> s_allocations = null!;
    internal static long s_totalAllocatedMemory;
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

    public static nuint TotalAllocatedMemory =>
#if MHP_ENABLE_SAFETY_CHECKS
        (nuint)s_totalAllocatedMemory;
#else
        0;
#endif

    private static volatile bool s_initialized;

    private static nuint s_threadLocalStackSize;

    private static void ReplaceIfZero(ref AllocationManagerDesc desc, AllocationManagerDesc defaultDesc)
    {
        desc.ArenaCapacity = desc.ArenaCapacity != 0
            ? desc.ArenaCapacity
            : defaultDesc.ArenaCapacity;
        desc.StackCapacity = desc.StackCapacity != 0
            ? desc.StackCapacity
            : defaultDesc.StackCapacity;
        desc.FreeListChunkSize = desc.FreeListChunkSize != 0
            ? desc.FreeListChunkSize
            : defaultDesc.FreeListChunkSize;
        desc.FreeListDefaultAlignment = desc.FreeListDefaultAlignment != 0
            ? desc.FreeListDefaultAlignment
            : defaultDesc.FreeListDefaultAlignment;
        desc.TLSFAlignment = desc.TLSFAlignment != 0
            ? desc.TLSFAlignment
            : defaultDesc.TLSFAlignment;
        desc.TLSFInitialChunkSize = desc.TLSFInitialChunkSize != 0
            ? desc.TLSFInitialChunkSize
            : defaultDesc.TLSFInitialChunkSize;
    }

    public static void Initialize(AllocationManagerDesc desc = default)
    {
        if (s_initialized)
        {
            return;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        s_allocations = new ConcurrentSlotMap<AllocationInfo>(256);
#endif

        var defaultDesc = AllocationManagerDesc.Default;
        ReplaceIfZero(ref desc, defaultDesc);

        s_arenaAllocator = new MemoryPool<VirtualArena, VirtualArena.CreationOptions>(new VirtualArena.CreationOptions
        {
            reserveCapacity = desc.ArenaCapacity
        });

        s_freeListAllocator = new MemoryPool<FreeList, FreeList.CreationOptions>(new FreeList.CreationOptions
        {
            alignment = desc.FreeListDefaultAlignment,
            chunkSize = desc.FreeListChunkSize
        });

        s_pHeapAllocator = (HeapAllocator*)NativeMemory.Alloc((nuint)sizeof(HeapAllocator));
        s_pHeapAllocator->Init();

        s_pTLSFAllocator = (TLSFAllocator*)NativeMemory.Alloc((nuint)sizeof(TLSFAllocator));
        s_pTLSFAllocator->Init(desc.TLSFAlignment, desc.TLSFInitialChunkSize);

        s_threadLocalStackSize = desc.StackCapacity;

        s_initialized = true;
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

        t_stackAllocator ??= new ThreadLocalStackPool(s_threadLocalStackSize);
        return t_stackAllocator.pool.Allocator.CreateScope(t_stackAllocator.pool.AllocationHandle);
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

        var threadLocalIndex = MemoryDiagnostic.ReserveLocalAllocation();

        var info = new AllocationInfo
        {
            Address = (IntPtr)ptr,
            Size = size,
            ThreadLocalIndex = threadLocalIndex,
#if MHP_ENABLE_STACKTRACE
            StackTrace = new StackTrace(1, true)
#endif
        };

        Interlocked.Add(ref s_totalAllocatedMemory, (long)size);

        var id = s_allocations.Add(info, out var generation);
        var handle = new MemoryHandle(id, generation);

        MemoryDiagnostic.SetLocalAllocation(threadLocalIndex, handle);

        return handle;
#else
        return MemoryHandle.Invalid;
#endif
    }

    public static void UpdateAllocation(MemoryHandle handle, void* newPtr, nuint newSize)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        if (newPtr == null)
        {
            s_allocations.Remove(handle.ID, handle.Generation, out var info);
            Interlocked.Add(ref s_totalAllocatedMemory, -(long)info.Size);
        }
        else
        {

            if (s_allocations.TryGetElement(handle.ID, handle.Generation, out var oldInfo))
            {
                var diff = (long)newSize - (long)oldInfo.Size;
                Interlocked.Add(ref s_totalAllocatedMemory, diff);

                var newInfo = oldInfo with
                {
                    Address = (IntPtr)newPtr,
                    Size = newSize
                };

                s_allocations.UpdateElement(handle.ID, handle.Generation, newInfo);
            }
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

        if (s_allocations.Remove(handle.ID, handle.Generation, out var info))
        {
            Interlocked.Add(ref s_totalAllocatedMemory, -(long)info.Size);
            MemoryDiagnostic.RemoveLocalAllocation(info.ThreadLocalIndex);
            return true;
        }

        return false;
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
    /// This only validates the memory when you added the allocation via <see cref="AddAllocation(void*, nuint)"/>.
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

        if (s_pHeapAllocator != null)
        {
            NativeMemory.Free(s_pHeapAllocator);
            s_pHeapAllocator = null;
        }

        if (s_pTLSFAllocator != null)
        {
            NativeMemory.Free(s_pTLSFAllocator);
            s_pTLSFAllocator = null;
        }
    }
}
