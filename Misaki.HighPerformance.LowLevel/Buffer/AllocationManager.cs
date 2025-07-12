using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Contracts;
using Misaki.HighPerformance.LowLevel.Exceptions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

using unsafe FreeFunc = delegate* unmanaged<void*, void*, void>;

public unsafe struct ArenaAllocator : IAllocator, IDisposable
{
    private DynamicArena _arena;
    private AllocationHandle _handle;

    public readonly ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

    public ArenaAllocator(uint initialSize)
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
        // NOTE: We do not free the old pointer here, as it is managed by the arena.
        return newPtr;
    }

    [UnmanagedCallersOnly]
    private static void FreeBlock(void* instance, void* ptr)
    {
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

public unsafe struct DefaultAllocator : IAllocator
{
    private AllocationHandle _handle;

    public ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

    public DefaultAllocator()
    {
        _handle = new AllocationHandle(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
    }

    [UnmanagedCallersOnly]
    private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
    {
        var ptr = AlignedAlloc(size, alignment);
        AllocationManager.TrackAllocation(ptr, size, instance, &FreeBlock);
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
        AllocationManager.UpdateAllocation(ptr, newPtr, size, instance, &FreeBlock);
        return newPtr;
    }

    [UnmanagedCallersOnly]
    private static void FreeBlock(void* instance, void* ptr)
    {
        AlignedFree(ptr);
        AllocationManager.RemoveAllocation(ptr);
    }
}

public unsafe struct EmptyAllocator : IAllocator
{
    private AllocationHandle _handle;

    public ref AllocationHandle Handle => ref Unsafe.AsRef(in _handle);

    public EmptyAllocator()
    {
        _handle = new AllocationHandle(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &FreeBlock);
    }

    [UnmanagedCallersOnly]
    private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
    {
        return null;
    }

    [UnmanagedCallersOnly]
    private static void* Reallocate(void* instance, void* ptr, nuint size, nuint alignment)
    {
        return ptr;
    }

    [UnmanagedCallersOnly]
    private static void FreeBlock(void* instance, void* ptr)
    {
    }
}

public static unsafe class AllocationManager
{
    public readonly struct AllocationInfo
    {
        public nuint Size
        {
            get; init;
        }

        public void* Allocator
        {
            get; init;
        }

        public FreeFunc FreeHandler
        {
            get; init;
        }

        public StackTrace StackTrace
        {
            get; init;
        }
    }

    private const uint _DEFAULT_ARENA_SIZE = 512 * 1024;

    private static ArenaAllocator s_arenaAllocator = new(_DEFAULT_ARENA_SIZE);
    private static DefaultAllocator s_persistentAllocator = new();
    private static EmptyAllocator s_emptyAllocator = new();

    private static bool s_debugLayer;
    private static Dictionary<nint, AllocationInfo>? s_allocated;

    public static ArenaAllocator TempAllocator => s_arenaAllocator;
    public static DefaultAllocator PersistentAllocator => s_persistentAllocator;
    public static EmptyAllocator EmptyAllocator => s_emptyAllocator;

    public static void EnableDebugLayer()
    {
        s_debugLayer = true;
        s_allocated ??= new Dictionary<nint, AllocationInfo>(64);
    }

    public static ref AllocationHandle GetAllocationHandle(Allocator allocator)
    {
        switch (allocator)
        {
            case Allocator.Temp:
                return ref s_arenaAllocator.Handle;
            case Allocator.Persistent:
                return ref s_persistentAllocator.Handle;
            case Allocator.External:
                return ref s_emptyAllocator.Handle;
            default:
                throw new ArgumentException("Invalid allocator type.", nameof(allocator));
        }
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateAllocation(void* oldPtr, void* newPtr, nuint allocationSize, void* allocator, FreeFunc freeFunc)
    {
        if (!s_debugLayer || s_allocated == null || oldPtr == null || newPtr == null)
        {
            return;
        }

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
        else
        {
            TrackAllocation(newPtr, allocationSize, allocator, freeFunc);
        }
    }

    public static void RemoveAllocation(void* ptr)
    {
        if (s_allocated == null)
        {
            return;
        }

        s_allocated.Remove((nint)ptr);
    }

    /// <summary>
    /// Disposes of the AllocationManager, freeing all allocated memory and resources.
    /// </summary>
    public static void Dispose()
    {
        s_arenaAllocator.Dispose();

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
    }
}