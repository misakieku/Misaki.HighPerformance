global using static Misaki.HighPerformance.LowLevel.Helpers.MemoryUtilities;


global using unsafe AllocFunc = delegate* unmanaged<void*, nuint, nuint, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption, void*>;
global using unsafe FreeFunc = delegate* unmanaged<void*, void*, void>;
global using unsafe ReallocFunc = delegate* unmanaged<void*, void*, nuint, nuint, void*>;