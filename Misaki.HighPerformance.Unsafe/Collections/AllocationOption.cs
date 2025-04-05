namespace Misaki.HighPerformance.Unsafe.Collections;

[Flags]
public enum AllocationOption : byte
{
    None = 0,
    /// <summary>
    /// Allocator for initialized memory.
    /// </summary>
    Clear = 1 << 0,
    /// <summary>
    /// Allocator for untracked memory. It always allocates memory without using the allocation manager.
    /// Always free it manually even if you use the <see cref="Allocator.Temp"/> allocator.
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
    /// Allocator for temporary allocations. Allocations are cleared after use.
    /// </summary>
    Temp,
    /// <summary>
    /// Allocator for persistent allocations. Allocations are not cleared after use.
    /// </summary>
    Persistent,
    /// <summary>
    /// Allocator for external memory. Allocations are not cleared after use.
    /// </summary>
    External
}