using System.Runtime.Versioning;

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

[Obsolete("Use AllocationHandle instead.")]
public enum Allocator : byte
{
    // Make the first allocator as invalid because we don't want to user create a default collection without passing any parameters
    /// <summary>
    /// The invalid allocator. This value is reserved and should not be used for actual memory allocations. It can be used to indicate an uninitialized or invalid state in allocation scenarios.
    /// </summary>
    Invalid,
    /// <summary>
    /// Allocator for temporary allocations. Allocations are automatically released after use automatically.
    /// </summary>
    Temp,
    /// <summary>
    /// Allocator for persistent allocations using a free list. Allocations are not automatically released after use, but can be reused to reduce fragmentation, system call and improve performance.
    /// </summary>
    FreeList,
    /// <summary>
    /// Allocator for persistent allocations. Allocations are not automatically released after use.
    /// </summary>
    Persistent,
}
