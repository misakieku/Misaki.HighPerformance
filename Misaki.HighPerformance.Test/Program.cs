using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

//BenchmarkRunner.Run<SPMDBenchmark>();
//var hashMap = new UnsafeHashMap<int, int>(10, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
//hashMap[0] = 5;
//hashMap[1] = 6;

//Console.WriteLine(hashMap[1]);

//ref var v = ref hashMap.GetValueRefOrAddDefault(1, out var exists);

//Console.WriteLine(exists);

//v = 10;
//Console.WriteLine(hashMap[1]);

//hashMap.Dispose();

//class TestClass
//{
//    private UnsafeHashMap<int, int> _map;
//    ref int Test()
//    {
//        ref var x = ref _map.GetValueRef(9, out var exists);
//        if (exists)
//        {
//            return ref x;
//        }

//        return ref Unsafe.NullRef<int>();
//    }
//}

var opts = new AllocationManagerInitOpts
{
    ArenaCapacity = 1024 * 1024,
    StackCapacity = 1024 * 1024,
    FreeListConcurrencyLevel = 1
};

AllocationManager.Initialize(opts);

using var arr = new UnsafeArray<int>(10, Allocator.FreeList);
var marr = new int[10];
arr.CopyTo(marr);

AllocationManager.Dispose();
