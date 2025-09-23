namespace Misaki.HighPerformance.LowLevel.Buffer;

[Flags]
public enum AllocationOption : byte
{
    None = 0,
    /// <summary>
    /// Allocator for initialized memory.
    /// </summary>
    Clear = 1 << 0,
    /// <summary>
    /// Allocator for untracked memory.
    /// </summary>
    /// <remarks>
    /// Use this option carefully, as the allocation manager will not track the memory.
    /// No warning will be given if the memory is not freed.
    /// </remarks>
    UnTracked = 1 << 1,
}

public enum Allocator : byte
{
    // Make the first allocator as invalid because we don't want to user create a default collection without passing any parameters
    Invalid,
    /// <summary>
    /// Allocator for temporary allocations. Allocations are released after use automatically.
    /// </summary>
    Temp,
    /// <summary>
    /// Allocator for persistent allocations. Allocations are not released after use.
    /// </summary>
    Persistent,
    /// <summary>
    /// Allocator for stack allocations. Must have at least one active stack scope. Allocations are released when the stack scope is exited.
    /// </summary>
    Stack
}