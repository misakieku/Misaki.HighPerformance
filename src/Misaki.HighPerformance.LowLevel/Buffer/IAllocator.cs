using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A structure that represents a handle to a memory allocation for tracking and management purposes.
/// </summary>
public readonly struct MemoryHandle : IDisposable, IEquatable<MemoryHandle>
{
    /// <summary>
    /// The id of the allocation.
    /// </summary>
    public readonly int ID
    {
        get;
    }

    /// <summary>
    /// The generation of the allocation.
    /// </summary>
    public readonly int Generation
    {
        get;
    }

    public static readonly MemoryHandle Invalid = default;

    public bool IsValid => AllocationManager.ContainsAllocation(this);
    public bool IsInvalid => !IsValid;

    /// <summary>
    /// Creates a new instance of the MemoryHandle struct with the specified id and generation.
    /// </summary>
    /// <param name="id">The id of the allocation.</param>
    /// <param name="generation">The generation of the allocation.</param>
    public MemoryHandle(int id, int generation)
    {
        ID = id;
        Generation = generation;
    }

    /// <summary>
    /// Creates a new memory handle for the specified memory allocation.
    /// </summary>
    /// <param name="address">The address of the memory allocation.</param>
    /// <param name="size">The size of the memory allocation.</param>
    /// <returns>The created memory handle.</returns>
    public static unsafe MemoryHandle Create(void* address, nuint size)
    {
        return AllocationManager.AddAllocation(address, size);
    }

    /// <summary>
    /// Updates the memory handle with a new address and size.
    /// </summary>
    /// <param name="newAddress">The new address of the memory allocation.</param>
    /// <param name="newSize">The new size of the memory allocation.</param>
    public unsafe void Update(void* newAddress, nuint newSize)
    {
        AllocationManager.UpdateAllocation(this, newAddress, newSize);
    }

    public bool Equals(MemoryHandle other)
    {
        return ID == other.ID && Generation == other.Generation;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is MemoryHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ID ^ Generation;
    }

    public override string? ToString()
    {
        return $"MemoryHandle(Id: {ID}, generation: {Generation})";
    }

    /// <summary>
    /// Removes the handle from the tracking system.
    /// </summary>
    /// <remarks>
    /// This does not free the associated memory allocation.
    /// </remarks>
    public void Dispose()
    {
        if (IsInvalid)
        {
            return;
        }

        AllocationManager.RemoveAllocation(this);
    }

    public static bool operator ==(MemoryHandle left, MemoryHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MemoryHandle left, MemoryHandle right)
    {
        return !(left == right);
    }
}

/// <summary>
/// A structure that encapsulates function pointers for memory allocation operations.
/// </summary>
public readonly unsafe struct AllocationHandle
{
    /// <summary>
    /// The invalid allocator. This value is reserved and should not be used for actual memory allocations. It can be used to indicate an uninitialized or invalid state in allocation scenarios.
    /// </summary>
    public static readonly AllocationHandle Invalid = default;

    /// <summary>
    /// Allocator for temporary allocations. Allocations are automatically released after use automatically.
    /// </summary>
    public static AllocationHandle Temp => AllocationManager.s_arenaAllocator.AllocationHandle;

    /// <summary>
    /// Allocator for persistent allocations. Allocations are not automatically released after use.
    /// </summary>
    public static AllocationHandle FreeList => AllocationManager.s_freeListAllocator.AllocationHandle;

    /// <summary>
    /// Allocator for persistent allocations using a free list. Allocations are not automatically released after use, but can be reused to reduce fragmentation, system call and improve performance.
    /// </summary>
    public static AllocationHandle Persistent => AllocationManager.s_pHeapAllocator->Handle;

    /// <summary>
    /// Allocator for persistent allocations using a Two-Level Segregated Fit (TLSF) algorithm. Allocations are not automatically released after use, but can be reused to reduce fragmentation, system call and improve performance.
    /// </summary>
    public static AllocationHandle TLSF => AllocationManager.s_pTLSFAllocator->Handle;

    private readonly void* _state;
    private readonly AllocFunc _alloc;
    private readonly ReallocFunc _realloc;
    private readonly FreeFunc _free;

    public AllocationHandle(void* state, AllocFunc alloc, ReallocFunc realloc, FreeFunc free)
    {
        _state = state;
        _alloc = alloc;
        _realloc = realloc;
        _free = free;
    }

    /// <summary>
    /// Allocates a block of memory with the specified size, alignment, and allocation options.
    /// </summary>
    /// <param name="size">The size of the memory block to allocate.</param>
    /// <param name="alignment">The alignment of the memory block.</param>
    /// <param name="option">The allocation options.</param>
    /// <returns>A pointer to the allocated memory block. null if allocation fails.</returns>
    public void* Alloc(nuint size, nuint alignment, AllocationOption option = AllocationOption.None)
    {
        Debug.Assert(_alloc != null);
        return _alloc(_state, size, alignment, option);
    }

    /// <summary>
    /// Reallocates a block of memory to a new size and alignment.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to reallocate.</param>
    /// <param name="oldSize">The size of the existing memory block.</param>
    /// <param name="newSize">The new size for the memory block.</param>
    /// <param name="alignment">The alignment of the memory block.</param>
    /// <param name="allocationOption">The allocation options.</param>
    /// <returns>A pointer to the reallocated memory block. null if reallocation fails.</returns>
    public void* Realloc(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        Debug.Assert(_realloc != null);
        return _realloc(_state, ptr, oldSize, newSize, alignment, allocationOption);
    }

    /// <summary>
    /// Frees a previously allocated block of memory.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to free.</param>
    public void Free(void* ptr)
    {
        Debug.Assert(_free != null);
        _free(_state, ptr);
    }
}

/// <summary>
/// Represents an state interface for managing memory allocations.
/// </summary>
public interface IAllocator
{
    /// <summary>
    /// Gets a reference to the allocation handle associated with this state.
    /// </summary>
    AllocationHandle Handle
    {
        get;
    }
}

public unsafe interface IMemoryAllocator<TSelf, TOpts> : IDisposable
    where TSelf : unmanaged, IMemoryAllocator<TSelf, TOpts>
{
    /// <summary>
    /// Creates a new instance of the allocator with the specified options.
    /// </summary>
    /// <param name="opts">The options for creating the allocator.</param>
    /// <returns>The created allocator instance.</returns>
    static abstract TSelf Create(in TOpts opts);

    /// <summary>
    /// Allocates a block of memory with the specified size, alignment, and allocation options.
    /// </summary>
    /// <remarks>
    /// The returned pointer must be freed using the <see cref="Free(void*)"/> method to avoid memory leaks.
    /// </remarks>
    /// <param name="size">The size of the memory block to allocate.</param>
    /// <param name="alignment">The alignment of the memory block.</param>
    /// <param name="option">The allocation options.</param>
    /// <returns>A pointer to the allocated memory block. null if allocation fails.</returns>
    void* Allocate(nuint size, nuint alignment, AllocationOption option = AllocationOption.None);
    /// <summary>
    /// Reallocates a block of memory to a new size and alignment.
    /// </summary>
    /// <remarks>
    /// The returned pointer must be freed using the <see cref="Free(void*)"/> method to avoid memory leaks. If the reallocation fails, the original pointer remains valid and must be freed by the caller.
    /// </remarks>
    /// <param name="ptr">A pointer to the memory block to reallocate.</param>
    /// <param name="oldSize">The size of the original memory block.</param>
    /// <param name="newSize">The size of the new memory block.</param>
    /// <param name="alignment">The alignment of the new memory block.</param>
    /// <param name="allocationOption">The allocation options.</param>
    /// <returns>A pointer to the reallocated memory block. null if reallocation fails.</returns>
    void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None);
    /// <summary>
    /// Frees a previously allocated block of memory.
    /// </summary>
    /// <remarks>
    /// The pointer must have been returned by a previous call to the <see cref="Allocate(nuint, nuint, AllocationOption)"/> or <see cref="Reallocate(void*, nuint, nuint, nuint, AllocationOption)"/> method. After calling this method, the pointer is no longer valid and must not be used.
    /// </remarks>
    /// <param name="ptr">A pointer to the memory block to free.</param>
    void Free(void* ptr);
}