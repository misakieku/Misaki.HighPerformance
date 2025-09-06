using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Exceptions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Holds information about a memory allocation.
/// </summary>
public readonly unsafe struct AllocationInfo
{
    /// <summary>
    /// Get the size of the allocation in bytes.
    /// </summary>
    public nuint Size
    {
        get; init;
    }

    /// <summary>
    /// Get the allocator used for the allocation.
    /// </summary>
    public void* Allocator
    {
        get; init;
    }

    /// <summary>
    /// Get the function pointer used to free the allocated memory.
    /// </summary>
    public FreeFunc FreeHandler
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

    private unsafe struct ArenaAllocator : IAllocator, IDisposable
    {
        private DynamicArena _arena;
        private AllocationHandle _handle;

        public readonly ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

        public void Init(uint initialSize)
        {
            _arena = new DynamicArena(initialSize);
            _handle = new AllocationHandle(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
        }

        [UnmanagedCallersOnly]
        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            var selfPtr = (ArenaAllocator*)instance;
            var ptr = selfPtr->_arena.Allocate(size, alignment, allocationOption);

            return ptr;
        }

        [UnmanagedCallersOnly]
        private static void* Reallocate(void* instance, void* ptr, nuint size, nuint alignment)
        {
            var selfPtr = (ArenaAllocator*)instance;
            var newPtr = selfPtr->_arena.Allocate(size, alignment, AllocationOption.None);
            MemCpy(newPtr, ptr, size);
            // We do not free the old pointer here, as it is managed by the arena.
            return newPtr;
        }

        [UnmanagedCallersOnly]
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
            _handle = new AllocationHandle(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
        }

        [UnmanagedCallersOnly]
        private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
        {
            var ptr = AlignedAlloc(size, alignment);

            if (!allocationOption.HasFlag(AllocationOption.UnTracked))
            {
                TrackAllocation(ptr, size, instance, &FreeBlock);
            }

            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(ptr, size);
            }

            return ptr;
        }

        [UnmanagedCallersOnly]
        private static void* Reallocate(void* instance, void* ptr, nuint size, nuint alignment)
        {
            var newPtr = AlignedRealloc(ptr, size, alignment);
            UpdateAllocation(ptr, newPtr, size, instance, &FreeBlock);
            return newPtr;
        }

        [UnmanagedCallersOnly]
        private static void FreeBlock(void* instance, void* ptr)
        {
            AlignedFree(ptr);
            UntrackAllocation(ptr);
        }
    }

    private const uint _DEFAULT_ARENA_SIZE = 512 * 1024;

    private static readonly ArenaAllocator* s_arenaAllocator;
    private static readonly HeapAllocator* s_persistentAllocator;

    private static bool s_debugLayer;
    private static ConcurrentDictionary<nint, AllocationInfo>? s_allocated;

    /// <summary>
    /// Gets a reference to the allocation handle for temporary allocations.
    /// </summary>
    public static ref AllocationHandle TempHandle => ref s_arenaAllocator->Handle;

    /// <summary>
    /// Gets a reference to the persistent allocation handle.
    /// </summary>
    public static ref AllocationHandle PersistentHandle => ref s_persistentAllocator->Handle;

    static AllocationManager()
    {
        s_arenaAllocator = (ArenaAllocator*)NativeMemory.Alloc((nuint)sizeof(ArenaAllocator));
        s_persistentAllocator = (HeapAllocator*)NativeMemory.Alloc((nuint)sizeof(HeapAllocator));

        s_arenaAllocator->Init(_DEFAULT_ARENA_SIZE);
        s_persistentAllocator->Init();
    }

    /// <summary>
    /// Enables the debug layer, allowing additional diagnostic information to be collected.
    /// </summary>
    public static void EnableDebugLayer()
    {
        s_debugLayer = true;
        s_allocated ??= new(-1, 64);
    }

    /// <summary>
    /// Gets a reference to the allocation handle for the specified allocator type.
    /// </summary>
    /// <param name="allocator">The allocator type for which to retrieve the allocation handle.</param>
    /// <returns>A reference to the allocation handle associated with the specified allocator type.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static ref AllocationHandle GetAllocationHandle(Allocator allocator)
    {
        switch (allocator)
        {
            case Allocator.Temp:
                return ref TempHandle;
            case Allocator.Persistent:
                return ref PersistentHandle;
            default:
                throw new ArgumentException("Target allocator type does not support custom allocation.", nameof(allocator));
        }
    }

    /// <summary>
    /// Tracks a memory allocation in the allocation manager.
    /// </summary>
    /// <param name="ptr">The pointer to the allocated memory.</param>
    /// <param name="allocationSize">The size of the allocation in bytes.</param>
    /// <param name="allocator">The allocator used for the allocation.</param>
    /// <param name="freeFunc">The function pointer used to free the allocated memory.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TrackAllocation(void* ptr, nuint allocationSize, void* allocator, FreeFunc freeFunc)
    {
        if (!s_debugLayer || s_allocated == null || ptr == null)
        {
            return;
        }

        s_allocated[(nint)ptr] = new AllocationInfo
        {
            Size = allocationSize,
            Allocator = allocator,
            FreeHandler = freeFunc,
            StackTrace = new StackTrace(true)
        };
    }

    /// <summary>
    /// Updates the allocation tracking information by replacing the old pointer with a new pointer.
    /// </summary>
    /// <param name="oldPtr">A pointer to the previously allocated memory. If <paramref name="oldPtr"/> is not tracked, the method does nothing.</param>
    /// <param name="newPtr">A pointer to the newly allocated memory. This pointer will replace <paramref name="oldPtr"/> in the allocation tracking.</param>
    /// <param name="allocationSize">The size, in bytes, of the new allocation.</param>
    /// <param name="allocator">A pointer to the allocator responsible for the new allocation.</param>
    /// <param name="freeFunc">A delegate or function pointer used to free the memory associated with the allocation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateAllocation(void* oldPtr, void* newPtr, nuint allocationSize, void* allocator, FreeFunc freeFunc)
    {
        if (!s_debugLayer || s_allocated == null || oldPtr == null || newPtr == null)
        {
            return;
        }

        // If we don't find the allocation info, that means the oldPtr was not tracked
        if (s_allocated.Remove((nint)oldPtr, out var info))
        {
            s_allocated[(nint)newPtr] = new AllocationInfo
            {
                Size = allocationSize,
                Allocator = allocator,
                FreeHandler = freeFunc,
                StackTrace = info.StackTrace
            };
        }
    }

    /// <summary>
    /// Removes the specified memory allocation from the tracking system.
    /// </summary>
    /// <param name="ptr">A pointer to the memory allocation to untrack.</param>
    public static void UntrackAllocation(void* ptr)
    {
        if (s_allocated == null)
        {
            return;
        }

        s_allocated.Remove((nint)ptr, out _);
    }

    /// <summary>
    /// Disposes of the AllocationManager, freeing all allocated memory and resources.
    /// </summary>
    public static void Dispose()
    {
        if (s_allocated != null)
        {
            nuint unfreeBytes = 0u;
            foreach (var pair in s_allocated)
            {
                unfreeBytes += pair.Value.Size;
                pair.Value.FreeHandler(pair.Value.Allocator, (void*)pair.Key);
            }

            if (unfreeBytes > 0u)
            {
                throw new MemoryLeakException([.. s_allocated.Values]);
            }

            s_allocated.Clear();
        }

        if (s_arenaAllocator != null)
        {
            s_arenaAllocator->Dispose();
            NativeMemory.Free(s_arenaAllocator);
        }

        if (s_persistentAllocator != null)
        {
            NativeMemory.Free(s_persistentAllocator);
        }
    }
}