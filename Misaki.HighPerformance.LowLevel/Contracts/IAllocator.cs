namespace Misaki.HighPerformance.LowLevel.Contracts;

/// <summary>
/// A structure that encapsulates function pointers for memory allocation operations.
/// </summary>
public readonly unsafe struct AllocationHandle
{
    /// <summary>
    /// Gets a pointer to the allocator instance associated with this allocation handle.
    /// </summary>
    public void* Allocator
    {
        get;
    }

    /// <summary>
    /// Gets a function pointer for allocating memory.
    /// </summary>
    public AllocFunc Alloc
    {
        get;
    }

    /// <summary>
    /// Gets a function pointer for reallocating memory.
    /// </summary>
    public ReallocFunc Realloc
    {
        get;
    }

    /// <summary>
    /// Gets a function pointer for freeing allocated memory.
    /// </summary>
    public FreeFunc Free
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocationHandle"/> struct with the specified allocator and memory
    /// management functions.
    /// </summary>
    /// <param name="allocator">A pointer to the allocator instance used for memory management.</param>
    /// <param name="alloc">The function used to allocate memory.</param>
    /// <param name="realloc">The function used to reallocate memory.</param>
    /// <param name="free">The function used to free allocated memory.</param>
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
/// The allocator must be pined to a specific memory region.
/// Otherwise the reference of the <see cref="AllocationHandle.Allocator"/>, may become invalid and lead to undefined behavior.
/// </remarks>
public interface IAllocator
{
    /// <summary>
    /// Gets a reference to the allocation handle associated with this allocator.
    /// </summary>
    ref AllocationHandle Handle
    {
        get;
    }
}