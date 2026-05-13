using BenchmarkDotNet.Attributes;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.Benchmark;

[MemoryDiagnoser]
public unsafe class AllocationBenchmark
{
    private FreeList _frreList;

    private void* _nativePtr;
    private void* _freeListPtr;
    private byte[]? _managedArray;
    private IntPtr _marshalPtr;

    [Params(8, 64, 512, 1024, 4096, 8192)]
    public nuint Size = 1024;

    [GlobalSetup]
    public void Setup()
    {
        _frreList = new FreeList(8);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _frreList.Dispose();
    }

    [Benchmark]
    public void NativeMemoryAlloc()
    {
        _nativePtr = NativeMemory.Alloc(Size);
        NativeMemory.Free(_nativePtr);
    }

    [Benchmark]
    public void FreeListAlloc()
    {
        _freeListPtr = _frreList.Allocate(Size, 8);
        _frreList.Free(_freeListPtr);
    }

    [Benchmark]
    public void ManagedAlloca()
    {
        _managedArray = new byte[Size];
    }

    [Benchmark]
    public void GCAlloc()
    {
        _marshalPtr = Marshal.AllocHGlobal((nint)Size);
        Marshal.FreeHGlobal(_marshalPtr);
    }
}