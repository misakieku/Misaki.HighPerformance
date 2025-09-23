global using static Misaki.HighPerformance.LowLevel.Utilities.MemoryUtilities;


global using unsafe AllocFunc = delegate*<void*, nuint, nuint, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption, void*>;
global using unsafe FreeFunc = delegate*<void*, void*, void>;
global using unsafe ReallocFunc = delegate*<void*, void*, nuint, nuint, void*>;