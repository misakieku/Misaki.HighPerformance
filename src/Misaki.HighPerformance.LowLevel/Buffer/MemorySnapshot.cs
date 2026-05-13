namespace Misaki.HighPerformance.LowLevel.Buffer;

public readonly struct MemorySnapshot
{
#if MHP_ENABLE_SAFETY_CHECKS
    private readonly AllocationInfo[] _allocations;
    private readonly nuint _totalAllocatedMemory;
#endif

    public ReadOnlySpan<AllocationInfo> Allocations =>
#if MHP_ENABLE_SAFETY_CHECKS
        _allocations;
#else
        ReadOnlySpan<AllocationInfo>.Empty;
#endif

    public nuint TotalAllocatedMemory =>
#if MHP_ENABLE_SAFETY_CHECKS
        _totalAllocatedMemory;
#else
        0;
#endif

    public MemorySnapshot()
    {
#if MHP_ENABLE_SAFETY_CHECKS
        _allocations = AllocationManager.s_allocations.ToArray();
        _totalAllocatedMemory = (nuint)AllocationManager.s_totalAllocatedMemory;
#endif
    }
}