using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.LowLevel.Contracts;

using unsafe AllocFunc = delegate* unmanaged<void*, nuint, nuint, AllocationOption, void*>;
using unsafe FreeFunc = delegate* unmanaged<void*, void*, void>;
using unsafe ReallocFunc = delegate* unmanaged<void*, void*, nuint, nuint, void*>;

public unsafe readonly struct AllocationHandle
{
    public void* Allocator
    {
        get;
    }

    public AllocFunc Alloc
    {
        get;
    }

    public ReallocFunc Realloc
    {
        get;
    }

    public FreeFunc Free
    {
        get;
    }

    public AllocationHandle(void* allocator, AllocFunc alloc, ReallocFunc realloc, FreeFunc free)
    {
        Allocator = allocator;
        Alloc = alloc;
        Realloc = realloc;
        Free = free;
    }
}

/// <summary>
/// Represents an allocator interface for managing memory allocations.
/// </summary>
/// <remarks>
/// The allocator must be static or pined to a specific memory region.
/// </remarks>
public unsafe interface IAllocator
{
    public ref AllocationHandle Handle
    {
        get;
    }
}