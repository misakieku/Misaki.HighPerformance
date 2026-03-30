global using static Misaki.HighPerformance.LowLevel.Utilities.MemoryUtility;

global using unsafe AllocFunc = delegate*<void*, nuint, nuint, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption
#if MHP_ENABLE_SAFETY_CHECKS
    , Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle*
#endif
    , void*>;
global using unsafe ReallocFunc = delegate*<void*, void*, nuint, nuint, nuint, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption
#if MHP_ENABLE_SAFETY_CHECKS
    , Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle*
#endif
    , void*>;
global using unsafe FreeFunc = delegate*<void*, void*
#if MHP_ENABLE_SAFETY_CHECKS
    , Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle
#endif
    , void>;
global using unsafe IsValidFunc = delegate*<void*
#if MHP_ENABLE_SAFETY_CHECKS
    , Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle
#endif
    , bool>;
