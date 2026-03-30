using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public readonly struct MemoryHandle : IEquatable<MemoryHandle>
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

    public MemoryHandle(int id, int generation)
    {
        ID = id + 1;
        Generation = generation + 1;
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
    /// Gets a pointer to the state instance associated with this allocation handle.
    /// </summary>
    public void* State
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for allocating memory.
    /// </summary>
    public AllocFunc Alloc
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for reallocating memory.
    /// </summary>
    public ReallocFunc Realloc
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for freeing allocated memory.
    /// </summary>
    public FreeFunc Free
    {
        get; init;
    }

    /// <summary>
    /// Gets a function pointer for validating a memory handle.
    /// </summary>
    public IsValidFunc IsValid
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
    void Free(void* ptr);
}