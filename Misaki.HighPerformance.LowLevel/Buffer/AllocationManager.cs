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

    public int FreeListConcurrencyLevel
    {
        get; init;
    }

    public static AllocationManagerInitOpts Default => new AllocationManagerInitOpts
    {
        ArenaCapacity = 1024 * 1024 * 1024, // 1 GB
        StackCapacity = 16 * 1024 * 1024, // 16 MB per thread
        FreeListConcurrencyLevel = Environment.ProcessorCount
    };
}

/// <summary>
/// Provides memory allocation management for native memory allocations, with support for tracking,
/// debugging, and custom allocation strategies.
/// </summary>
public static unsafe class AllocationManager
{
    private struct ArenaAllocator : IAllocator, IDisposable
    {
        private const int _ARENA_MAGIC_ID = -3941029;

        private VirtualArena _arena;
        private int _currentTick;
        private AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;
        public readonly int CurrentTick => _currentTick;

        public void Init(nuint capacity)
        {
            _arena = new VirtualArena(capacity);
            _handle = new AllocationHandle
            {
                State = Unsafe.AsPointer(ref this),
                Alloc = &Allocate,
                Realloc = &Reallocate,
                Free = null,
#if MHP_ENABLE_SAFETY_CHECKS
                IsValid = &IsValid
#else
                IsValid = null
#endif
            };

            _currentTick = 0;
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            var selfPtr = (ArenaAllocator*)instance;
            var ptr = selfPtr->_arena.Allocate(size, alignment, allocationOption);
            if (ptr == null)
            {
#if MHP_ENABLE_SAFETY_CHECKS
                *pHandle = MemoryHandle.Invalid;
#endif
                return null;
            }

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = new MemoryHandle(_ARENA_MAGIC_ID, selfPtr->_currentTick);
#endif
            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            if (ptr == null)
            {
                return Allocate(instance, newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                    , pHandle
#endif
                    );
            }

            var selfPtr = (ArenaAllocator*)instance;
            var newPtr = selfPtr->_arena.Allocate(newSize, alignment, allocationOption);
            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = new MemoryHandle(_ARENA_MAGIC_ID, selfPtr->_currentTick);
#endif
            return newPtr;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        private static bool IsValid(void* instance, MemoryHandle handle)
        {
            var selfPtr = (ArenaAllocator*)instance;
            return handle.ID == _ARENA_MAGIC_ID && handle.Generation == selfPtr->_currentTick;
        }
#endif

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
            return HeapAlloc(size, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                , pHandle
#endif
                );
        }

        private static void* Reallocate(void* _, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            if (ptr == null)
            {
                return Allocate(null, newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                    , pHandle
#endif
                    );
            }

#if MHP_ENABLE_SAFETY_CHECKS
            MemoryHandle newHandle;
#endif
            var newPtr = HeapAlloc(newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                , &newHandle
#endif
                );

            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));
            HeapFree(ptr
#if MHP_ENABLE_SAFETY_CHECKS
                , *pHandle
#endif
                );

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = newHandle;
#endif
            return newPtr;
        }

        private static void Free(void* _, void* ptr
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle handle
#endif
            )
        {
            HeapFree(ptr
#if MHP_ENABLE_SAFETY_CHECKS
                , handle
#endif
                );
        }

#if MHP_ENABLE_SAFETY_CHECKS
        private static bool IsValid(void* _, MemoryHandle handle)
        {
            return ContainsAllocation(handle);
        }
#endif
    }

    private struct StackAllocator : IAllocator, IDisposable
    {
        private const int _STACK_MAGIC_ID = -6843541;

        private static void** s_pStackBuffers = null;
        private static int s_stackCount = 0;
        private static int s_stackCapacity = 0;
        private static readonly SpinLock s_locker = new SpinLock(false);

        [ThreadStatic]
        private static VirtualStack s_stack;
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
#if MHP_ENABLE_SAFETY_CHECKS
                IsValid = &IsValid
#else
                IsValid = null
#endif
            };
        }

        private static void EnsureInitialize()
        {
            if (s_stack.Buffer == null)
            {
                s_stack = new VirtualStack(s_threadLocalStackDefaultSize);

                var token = false;
                try
                {
                    s_locker.Enter(ref token);
                    if (s_pStackBuffers == null)
                    {
                        s_pStackBuffers = (void**)Malloc((nuint)(sizeof(void*) * Environment.ProcessorCount));
                        s_stackCapacity = Environment.ProcessorCount;
                    }

                    if (s_stackCount >= s_stackCapacity)
                    {
                        var pOld = s_pStackBuffers;
                        var newCapacity = s_stackCapacity * 2;
                        var pNew = (void**)Realloc(pOld, (nuint)(sizeof(void*) * newCapacity));

                        s_pStackBuffers = pNew;
                        s_stackCapacity = newCapacity;
                    }

                    s_pStackBuffers[s_stackCount] = s_stack.Buffer;
                    s_stackCount++;
                }
                finally
                {
                    if (token)
                    {
                        s_locker.Exit();
                    }
                }
            }
        }

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            EnsureInitialize();

            var ptr = s_stack.Allocate(size, alignment, allocationOption);
            if (ptr == null)
            {
#if MHP_ENABLE_SAFETY_CHECKS
                *pHandle = MemoryHandle.Invalid;
#endif
                return null;
            }

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = new MemoryHandle(_STACK_MAGIC_ID, (int)s_stack.Offset);
#endif
            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            if (ptr == null)
            {
                return Allocate(instance, newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                    , pHandle
#endif
                    );
            }

            EnsureInitialize();

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

#if MHP_ENABLE_SAFETY_CHECKS
                *pHandle = new MemoryHandle(_STACK_MAGIC_ID, (int)s_stack.Offset);
#endif
                return ptr;
            }

            var newPtr = s_stack.Allocate(newSize, alignment, allocationOption);
            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = new MemoryHandle(_STACK_MAGIC_ID, (int)s_stack.Offset);
#endif
            return newPtr;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        private static bool IsValid(void* instance, MemoryHandle handle)
        {
            return handle.ID == _STACK_MAGIC_ID && handle.Generation <= (int)s_stack.Offset;
        }
#endif

        public static VirtualStack.Scope CreateScope(StackAllocator* pSelf)
        {
            EnsureInitialize();
            return s_stack.CreateScope(pSelf->_handle);
        }

        public readonly void Dispose()
        {
            if (s_pStackBuffers == null)
            {
                return;
            }

            for (var i = 0; i < s_stackCount; i++)
            {
                Munmap(s_pStackBuffers[i], s_threadLocalStackDefaultSize);
            }
        }
    }

    private struct FreeListAllocator : IAllocator, IDisposable
    {
        private FreeList _freeList;
        private AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;

        public void Init(int concurrencyLevel)
        {
            _freeList = new FreeList(8, 64 * 1024, concurrencyLevel);
            _handle = new AllocationHandle
            {
                State = Unsafe.AsPointer(ref this),
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

        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            var selfPtr = (FreeListAllocator*)instance;
            var ptr = selfPtr->_freeList.Allocate(size, alignment, allocationOption);
            if (ptr == null)
            {
                return null;
            }

#if MHP_ENABLE_SAFETY_CHECKS
            *pHandle = AddAllocation(ptr, size);
#endif
            return ptr;
        }

        private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle* pHandle
#endif
            )
        {
            if (ptr == null)
            {
                return Allocate(instance, newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                    , pHandle
#endif
                    );
            }

            var selfPtr = (FreeListAllocator*)instance;
            var newPtr = selfPtr->_freeList.Allocate(newSize, alignment, allocationOption);
            if (newPtr == null)
            {
                return null;
            }

            MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

            selfPtr->_freeList.Free(ptr);

#if MHP_ENABLE_SAFETY_CHECKS
            RemoveAllocation(*pHandle);
            *pHandle = AddAllocation(newPtr, newSize);
#endif

            return newPtr;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        private static bool IsValid(void* instance, MemoryHandle handle)
        {
            return ContainsAllocation(handle);
        }
#endif

        private static void Free(void* instance, void* ptr
#if MHP_ENABLE_SAFETY_CHECKS
            , MemoryHandle handle
#endif
            )
        {
            var selfPtr = (FreeListAllocator*)instance;
            selfPtr->_freeList.Free(ptr);
#if MHP_ENABLE_SAFETY_CHECKS
            RemoveAllocation(handle);
#endif
        }

        public void Dispose()
        {
            _freeList.Dispose();
        }
    }

    private static ArenaAllocator* s_pArenaAllocator;
    private static HeapAllocator* s_pHeapAllocator;
    private static StackAllocator* s_pStackAllocator;
    private static FreeListAllocator* s_pFreeListAllocator;

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
    private static nuint s_threadLocalStackDefaultSize;

    public static void Initialize(AllocationManagerInitOpts opts)
    {
        if (s_initialized)
        {
            return;
        }

#if MHP_ENABLE_SAFETY_CHECKS
        s_allocations = new ConcurrentSlotMap<AllocationInfo>(256);
#endif

        var ptr = (byte*)Malloc((nuint)(sizeof(ArenaAllocator) + sizeof(HeapAllocator) + sizeof(StackAllocator) + sizeof(FreeListAllocator)));

        s_pArenaAllocator = (ArenaAllocator*)ptr;
        s_pHeapAllocator = (HeapAllocator*)(ptr + sizeof(ArenaAllocator));
        s_pStackAllocator = (StackAllocator*)(ptr + sizeof(ArenaAllocator) + sizeof(HeapAllocator));
        s_pFreeListAllocator = (FreeListAllocator*)(ptr + sizeof(ArenaAllocator) + sizeof(HeapAllocator) + sizeof(StackAllocator));

        s_pArenaAllocator->Init(opts.ArenaCapacity);
        s_pHeapAllocator->Init();
        s_pStackAllocator->Init();
        s_pFreeListAllocator->Init(opts.FreeListConcurrencyLevel);

        s_threadLocalStackDefaultSize = opts.StackCapacity;

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
            Allocator.Temp => s_pArenaAllocator->Handle,
            Allocator.Persistent => s_pHeapAllocator->Handle,
            Allocator.FreeList => s_pFreeListAllocator->Handle,
            _ => throw new ArgumentException("Target allocator type does not support custom allocation.", nameof(allocator)),
        };
    }

    /// <summary>
    /// Allocates a block of memory from the heap with the specified newSize and alignment, using the given allocation options.
    /// </summary>
    /// <param name="size">The number of bytes to allocate. Must be greater than zero.</param>
    /// <param name="alignment">The alignment, in bytes, for the allocated memory block. Must be a power of two.</param>
    /// <param name="allocationOption">An optional set of flags that control allocation behavior, such as whether the memory should be cleared or
    /// tracked. The default is <see cref="AllocationOption.None"/>.</param>
    /// <returns>A pointer to the beginning of the allocated memory block.</returns>
    /// <exception cref="OutOfMemoryException">Thrown if the allocation fails.</exception>
    public static void* HeapAlloc(nuint size, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle* pHandle
#endif
        )
    {
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

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

    /// <summary>
    /// Releases a block of unmanaged memory previously allocated by the heap allocator.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to be freed. The pointer must have been returned by a compatible heap allocation
    ///     method and must not be null.</param>
    /// <param name="handle">The handle representing the memory allocation to free. The handle must be valid and previously allocated.</param>
    public static void HeapFree(void* ptr
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle handle
#endif
        )
    {
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        AlignedFree(ptr);

#if MHP_ENABLE_SAFETY_CHECKS
        RemoveAllocation(handle);
#endif
    }

    /// <summary>
    /// Releases a block of unmanaged memory previously allocated by the heap allocator.
    /// </summary>
    /// <remarks>
    /// No ops when MHP_ENABLE_SAFETY_CHECKS is disabled, as we cannot fetch the allocation info from the handle to get the pointer to free.
    /// </remarks>
    /// <param name="handle">The handle representing the memory allocation to free. The handle must be valid and previously allocated.</param>
    public static void HeapFree(MemoryHandle handle)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");

        if (TryGetAllocation(handle, out var info))
        {
            HeapFree((void*)info.Address, handle);
        }
#endif
        // No-op when safety checks are disabled, as we cannot fetch the allocation info from the handle.
    }

    /// <summary>
    /// Resets the temporary memory allocator, clearing all allocated memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetTempAllocator()
    {
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        s_pArenaAllocator->Reset();
    }

    /// <summary>
    /// Creates a new thread local stack scope for managing temporary allocations within the current context.
    /// </summary>
    /// <returns>A <see cref="Stack.Scope"/> instance representing the newly created stack scope. The scope must be disposed when no longer needed to release allocated resources.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VirtualStack.Scope CreateStackScope()
    {
        Debug.Assert(s_initialized, "AllocationManager is not initialized.");
        return StackAllocator.CreateScope(s_pStackAllocator);
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

        s_pArenaAllocator->Dispose();
        s_pStackAllocator->Dispose();
        s_pFreeListAllocator->Dispose();

        Free(s_pArenaAllocator);
    }
}
