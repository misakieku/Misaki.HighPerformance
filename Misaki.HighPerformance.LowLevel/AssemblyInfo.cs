global using static Misaki.HighPerformance.LowLevel.Utilities.MemoryUtility;

global using unsafe AllocFunc = delegate*<void*, nuint, nuint, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption, Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle*, void*>;
global using unsafe ReallocFunc = delegate*<void*, void*, nuint, nuint, nuint, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption, Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle*, void*>;
global using unsafe FreeFunc = delegate*<void*, void*, Misaki.HighPerformance.LowLevel.Buffer.MemoryHandle, void>;
