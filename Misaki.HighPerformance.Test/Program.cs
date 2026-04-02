using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

//BenchmarkRunner.Run<SPMDBenchmark>();

var opts = new AllocationManagerInitOpts
{
    ArenaCapacity = 1024 * 1024,
    StackCapacity = 1024 * 1024,
    FreeListConcurrencyLevel = 1
};

var pool = new MemoryPool<VirtualStack, VirtualStack.CreationOpts>(new VirtualStack.CreationOpts
{
    reserveCapacity = 1024 * 1024
});

var arr = new UnsafeArray<int>(1000, pool.AllocationHandle);

Test(in pool);
arr.Dispose();

Console.WriteLine("Done.");

void Test(ref readonly MemoryPool<VirtualStack, VirtualStack.CreationOpts> pool)
{
    using var arr = new UnsafeArray<int>(1000, pool.AllocationHandle);
    if (true)
    {
        using var arr1 = new UnsafeArray<int>(1000, pool.AllocationHandle);
    }

    using var arr3 = Test2(in pool);
    using var arr2 = new UnsafeArray<int>(1000, pool.AllocationHandle);
}

UnsafeArray<int> Test2(ref readonly MemoryPool<VirtualStack, VirtualStack.CreationOpts> pool)
{
    using var arr = new UnsafeArray<int>(1000, pool.AllocationHandle);
    var arr1 = new UnsafeArray<int>(1000, pool.AllocationHandle);
    return arr1;
}
