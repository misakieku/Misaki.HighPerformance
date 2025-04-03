namespace Misaki.HighPerformance.Unsafe.Collections;

public enum AllocationOption : byte
{
    UnInitialized,
    Clear
}

public enum Allocator: byte
{
    // Make the first allocator as invalid because we don't want to user create a defualt collection without passing any parameters
    Invalid = 0,
    Temp = 1,
    Persistent = 2,
}