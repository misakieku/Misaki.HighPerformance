using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public class TestTlsfAllocator
{
    [TestMethod]
    public unsafe void TestBasicAllocationAndFree()
    {
        using var allocator = TLSF.Create(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 4096 });

        var ptr1 = allocator.Allocate(100, 16);
        Assert.IsTrue(ptr1 != null);

        var ptr2 = allocator.Allocate(200, 16);
        Assert.IsTrue(ptr2 != null);

        allocator.Free(ptr1);
        allocator.Free(ptr2);
    }

    [TestMethod]
    public unsafe void TestReallocate()
    {
        using var allocator = TLSF.Create(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 4096 });

        var ptr = allocator.Allocate(100, 16);
        Assert.IsTrue(ptr != null);

        var newPtr = allocator.Reallocate(ptr, 100, 200, 16);
        Assert.IsTrue(newPtr != null);

        allocator.Free(newPtr);
    }

    [TestMethod]
    public unsafe void TestMemoryPoolIntegration()
    {
        using var pool = new MemoryPool<TLSF, TLSF.CreationOptions>(new TLSF.CreationOptions { alignment = 16 });

        var ptr2 = pool.Allocator.Allocate(50, 16);
        Assert.IsTrue(ptr2 != null);
        pool.Allocator.Free(ptr2);
    }

    [TestMethod]
    public unsafe void TestLargeAllocation()
    {
        using var allocator = TLSF.Create(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 4096 });

        // This should force the allocator to allocate a new chunk
        var ptr = allocator.Allocate(1024 * 1024, 16);
        Assert.IsTrue(ptr != null);
        allocator.Free(ptr);
    }

    [TestMethod]
    public unsafe void TestMultipleAllocations()
    {
        using var allocator = TLSF.Create(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 4096 });

        var ptrs = stackalloc void*[100];

        for (var i = 0; i < 100; i++)
        {
            ptrs[i] = allocator.Allocate((nuint)(i * 10 + 16), 16);
            Assert.IsTrue(ptrs[i] != null);
        }

        for (var i = 0; i < 100; i++)
        {
            allocator.Free(ptrs[i]);
        }
    }
}
