using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

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

using var pool = new MemoryPool<VirtualStack, VirtualStack.CreationOpts>(new VirtualStack.CreationOpts() { reserveCapacity = 1024 * 1024 });
using var scope = pool.Allocator.CreateScope(pool.AllocationHandle);

var arr = new UnsafeArray<int>(1000, scope.AllocationHandle);
for (var i = 0; i < arr.Length; i++)
{
    Console.WriteLine(arr[i]);
}

arr.Dispose();
