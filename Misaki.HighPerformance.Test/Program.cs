using BenchmarkDotNet.Running;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.Benchmark;
using Misaki.HighPerformance.Test.Jobs;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

BenchmarkRunner.Run<SPMDBenchmark>();
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