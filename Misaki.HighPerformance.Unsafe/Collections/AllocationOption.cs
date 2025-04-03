namespace Misaki.HighPerformance.Unsafe.Collections;

public enum AllocationOption : byte
{
    /// <summary>
    /// Allocator for uninitialized memory
    /// </summary>
    UnInitialized,
    /// <summary>
    /// Allocator for initialized memory.
    /// </summary>
    Clear,
    /// <summary>
    /// Allocator for untracked memory.
    /// Use this option carefully, as the allocation manager will not track the memory.
    /// No warning will be given if the memory is not freed.
    /// </summary>
    UnTracked
}

public enum Allocator : byte
{
    // Make the first allocator as invalid because we don't want to user create a defualt collection without passing any parameters
    Invalid,
    /// <summary>
    /// Allocator for temporary allocations. Allocations are cleared after use.
    /// </summary>
    Temp,
    /// <summary>
    /// Allocator for persistent allocations. Allocations are not cleared after use.
    /// </summary>
    Persistent,
}