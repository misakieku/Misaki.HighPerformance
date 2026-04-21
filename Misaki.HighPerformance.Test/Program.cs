using BenchmarkDotNet.Running;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Test.Benchmark;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

BenchmarkRunner.Run<ParallelNoiseBenchmark>();

//var bench = new ParallelNoiseBenchmark();
//bench.Setup();

//for (int i = 0; i < 4096 * 5; i++)
//{
//    bench.JobSystem();
//}

//bench.Cleanup();

//bench.Setup();

//for (int i = 0; i < 4096 * 5; i++)
//{
//    bench.JobSystem();
//}

//bench.Cleanup();

//bench.Setup();

//for (int i = 0; i < 4096 * 5; i++)
//{
//    bench.JobSystem();
//}

//bench.Cleanup();

//AllocationManager.Initialize(AllocationManagerInitOpts.Default);
//var set = new UnsafeBitSet(100, AllocationHandle.Persistent, AllocationOption.Clear);
//set.SetBit(0);
//Console.WriteLine(set.NextSetBit(0));

//set.Dispose();
//AllocationManager.Dispose();

//unsafe
//{
//    var testC = new TestC();
//    testC.BindToVtables();
//    testC.MyValue = 42;

//    // 1. Casting to IB (Direct cast, offset 0)
//    var pB = (IB.Interface*)&testC;
//    pB->MethodA();
//    pB->MethodB();

//    // 2. Casting to IC (Pointer adjustment needed!)
//    // We must offset the pointer by the size of one void* so it points to lpVtbl_IC
//    var pC = (IC.Interface*)((byte*)&testC + sizeof(void*));
//    pC->MethodC();
//}

//public interface IA
//{
//    [StructLayout(LayoutKind.Sequential)]
//    unsafe struct Interface
//    {
//        public void** lpVtbl;
//        public void MethodA() => ((delegate*<Interface*, void>)lpVtbl[0])((Interface*)Unsafe.AsPointer(ref this));
//    }
//}

//public interface IB : IA
//{
//    [StructLayout(LayoutKind.Sequential)]
//    new unsafe struct Interface
//    {
//        public void** lpVtbl;
//        // IA Methods
//        public void MethodA() => ((delegate*<Interface*, void>)lpVtbl[0])((Interface*)Unsafe.AsPointer(ref this));
//        // IB Methods
//        public void MethodB() => ((delegate*<Interface*, void>)lpVtbl[1])((Interface*)Unsafe.AsPointer(ref this));
//    }
//}

//public interface IC
//{
//    [StructLayout(LayoutKind.Sequential)]
//    unsafe struct Interface
//    {
//        public void** lpVtbl;
//        public void MethodC() => ((delegate*<Interface*, void>)lpVtbl[0])((Interface*)Unsafe.AsPointer(ref this));
//    }
//}

//[StructLayout(LayoutKind.Sequential)]
//public unsafe partial struct TestC
//{
//    // Offset 0: Primary VTable (Covers IB and IA)
//    public void** lpVtbl_IB;

//    // Offset 8 (on 64-bit): Secondary VTable (Covers IC)
//    public void** lpVtbl_IC;

//    // Offset 16: Fields
//    public int MyValue;
//}

//public unsafe partial struct TestC
//{
//    public struct Vtbl_IB
//    {
//        public delegate*<IB.Interface*, void> MethodA;
//        public delegate*<IB.Interface*, void> MethodB;
//    }

//    public struct Vtbl_IC
//    {
//        public delegate*<IC.Interface*, void> MethodC;
//    }

//    [FixedAddressValueType]
//    public static readonly Vtbl_IB Table_IB = new Vtbl_IB
//    {
//        MethodA = &Native_MethodA,
//        MethodB = &Native_MethodB
//    };

//    [FixedAddressValueType]
//    public static readonly Vtbl_IC Table_IC = new Vtbl_IC
//    {
//        MethodC = &Native_MethodC
//    };

//    public void BindToVtables()
//    {
//        fixed (Vtbl_IB* pIB = &Table_IB)
//            lpVtbl_IB = (void**)pIB;
//        fixed (Vtbl_IC* pIC = &Table_IC)
//            lpVtbl_IC = (void**)pIC;
//    }

//    // --- IB & IA Implementations ---
//    public static void Native_MethodA(IB.Interface* self)
//    {
//        // For IB/IA, no adjustment is needed because lpVtbl_IB is at offset 0
//        var pThis = (TestC*)self;
//        Console.WriteLine($"MethodA called. MyValue: {pThis->MyValue}");
//    }

//    public static void Native_MethodB(IB.Interface* self)
//    {
//        var pThis = (TestC*)self;
//        Console.WriteLine($"MethodB called. MyValue: {pThis->MyValue}");
//    }

//    // --- IC Implementations ---
//    public static void Native_MethodC(IC.Interface* self)
//    {
//        // WARNING: 'self' points to the lpVtbl_IC field, NOT the start of TestC!
//        // We must shift the pointer backward by the size of lpVtbl_IB (sizeof(void*))
//        // to reconstruct the original TestC pointer, otherwise we read garbage memory.
//        var pThis = (TestC*)((byte*)self - sizeof(void*));
//        Console.WriteLine($"MethodC called. MyValue: {pThis->MyValue}");
//    }
//}