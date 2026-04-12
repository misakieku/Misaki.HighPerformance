using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public readonly struct MemoryHandle : IDisposable, IEquatable<MemoryHandle>
{
    public readonly int ID
    {
        get => field - 1;
    }

    public readonly int Generation
    {
        get => field - 1;
    }

    public static readonly MemoryHandle Invalid = default;

    public bool IsValid => AllocationManager.ContainsAllocation(this);
    public bool IsInvalid => !IsValid;

    public MemoryHandle(int id, int generation)
    {
        ID = id + 1;
        Generation = generation + 1;
    }

    public unsafe static MemoryHandle Create(void* address, nuint size)
    {
        return AllocationManager.AddAllocation(address, size);
    }

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
        return $"MemoryHandle(Id: {ID}, Generation: {Generation})";
    }

    public void Dispose()
    {
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
    /// Gets a pointer to the state instance associated with this allocation handle.
    /// </summary>
    public required void* State
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for allocating memory.
    /// </summary>
    public required AllocFunc Alloc
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for reallocating memory.
    /// </summary>
    public required ReallocFunc Realloc
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for freeing allocated memory.
    /// </summary>
    public required FreeFunc Free
    {
        get; init;
    }
}

/// <summary>
/// Represents an state interface for managing memory allocations.
/// </summary>
/// <remarks>
/// The state must be pined to a specific memory region.
/// Otherwise the reference of the <see cref="AllocationHandle.State"/>, may become invalid and lead to undefined behavior.
/// </remarks>
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
    static abstract TSelf Create(in TOpts opts);

    void* Allocate(nuint size, nuint alignment, AllocationOption option = AllocationOption.None);
    void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None);
    void Free(void* ptr);
}