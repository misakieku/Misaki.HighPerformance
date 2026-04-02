using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

//BenchmarkRunner.Run<SPMDBenchmark>();

var opts = new AllocationManagerInitOpts
{
    ArenaCapacity = 1024 * 1024,
    StackCapacity = 1024 * 1024,
    FreeListConcurrencyLevel = 1
};

AllocationManager.Initialize(opts);

var arr = new UnsafeArray<int>(10, Allocator.Persistent);
var arrcpy = arr;
arr.Dispose();
arrcpy.Dispose();

Console.WriteLine(arr.IsCreated);
Console.WriteLine(arrcpy.IsCreated);

AllocationManager.Dispose();
