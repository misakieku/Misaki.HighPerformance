using Misaki.HighPerformance;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics.SPMD;
using Misaki.HighPerformance.Test.UnitTest.Jobs;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

BenchmarkDotNet.Running.BenchmarkRunner.Run<Misaki.HighPerformance.Test.Benchmark.SPMDBenchmark>();
//return;
//using Misaki.HighPerformance.Collections;
//using Misaki.HighPerformance.LowLevel.Buffer;
//using Misaki.HighPerformance.LowLevel.Collections;

//AllocationManager.EnableDebugLayer();
//using var csm = new UnsafeSlotMap<int>(4, Allocator.Persistent);
//AllocationManager.Dispose();

//using Misaki.HighPerformance.Mathematics;
//using System.Numerics;

//var a = new Misaki.HighPerformance.Mathematics.float4x4(
//    1, 2, 3, 4,
//    5, 6, 7, 8,
//    9, 10, 11, 12,
//    13, 14, 15, 16);
//var b = new Misaki.HighPerformance.Mathematics.float4x4(
//    16, 15, 14, 13,
//    12, 11, 10, 9,
//    8, 7, 6, 5,
//    4, 3, 2, 1);

//Console.WriteLine(math.mul(a, b));

//var ma = new Matrix4x4(
//        1, 2, 3, 4,
//        5, 6, 7, 8,
//        9, 10, 11, 12,
//        13, 14, 15, 16);
//var mb = new Matrix4x4(
//        16, 15, 14, 13,
//        12, 11, 10, 9,
//        8, 7, 6, 5,
//        4, 3, 2, 1);
//Console.WriteLine(Matrix4x4.Multiply(ma, mb));

//int[] arr1 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
//int[] arr2 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

//unsafe 
//{
//    fixed (int* p1 = arr1)
//    fixed (int* p2 = arr2)
//    {
//        Console.WriteLine(MemoryUtility.MemCmp(p1, p2, (nuint)(arr1.Length * sizeof(int))));
//    }
//}