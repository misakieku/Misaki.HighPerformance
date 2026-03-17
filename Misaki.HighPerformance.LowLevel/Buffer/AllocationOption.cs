namespace Misaki.HighPerformance.LowLevel.Buffer;

[Flags]
public enum AllocationOption : byte
{
    /// <summary>
    /// Default allocation option. Values are uninitialized.
    /// </summary>
    None = 0,
    /// <summary>
    /// Clear the memory to zero upon allocation.
    /// </summary>
    Clear = 1 << 0
}

public enum Allocator : byte
{
    // Make the first allocator as invalid because we don't want to user create a default collection without passing any parameters
    Invalid,
    /// <summary>
    /// Allocator for temporary allocations. Allocations are automatically released after use automatically.
    /// </summary>
    Temp,
    /// <summary>
    /// Allocator for persistent allocations. Allocations are not automatically released after use.
    /// </summary>
    Persistent,
}
