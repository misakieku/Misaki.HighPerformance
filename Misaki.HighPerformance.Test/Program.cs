using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Numerics;

unsafe
{
    Console.WriteLine(sizeof(UnsafeHashMap<int, float>));
    Console.WriteLine(MemoryUtilities.AlignOf<UnsafeHashMap<int, float>>());
    Console.WriteLine(1 << Math.Min(3, BitOperations.TrailingZeroCount(sizeof(UnsafeHashMap<int, float>))));
}