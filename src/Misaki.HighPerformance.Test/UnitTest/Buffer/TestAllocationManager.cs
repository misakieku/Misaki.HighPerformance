using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
[DoNotParallelize]
public class TestAllocationManager
{
    [TestMethod]
    public void PersistentAllocationTest()
    {
        using var ptr1 = new MemoryBlock(1024, 8, AllocationHandle.Persistent);
        using var ptr2 = new MemoryBlock(2048, 8, AllocationHandle.Persistent);

        Assert.IsTrue(ptr1.IsCreated);
        Assert.IsTrue(ptr2.IsCreated);

        ptr1.Dispose();
        ptr2.Dispose();

        Assert.IsFalse(ptr1.IsCreated);
        Assert.IsFalse(ptr2.IsCreated);
    }

    [TestMethod]
    public void TempAllocationTest()
    {
        using var ptr1 = new MemoryBlock(1024, 8, AllocationHandle.Temp);
        using var ptr2 = new MemoryBlock(2048, 8, AllocationHandle.Temp);

        Assert.IsTrue(ptr1.IsCreated);
        Assert.IsTrue(ptr2.IsCreated);

        ptr1.Dispose();
        ptr2.Dispose();

        Assert.IsFalse(ptr1.IsCreated);
        Assert.IsFalse(ptr2.IsCreated);

        AllocationManager.ResetTempAllocator();
    }

    [TestMethod]
    public void FreeListAllocationTest()
    {
        var ptr1 = new MemoryBlock(1024, 8, AllocationHandle.FreeList);
        var ptr2 = new MemoryBlock(2048, 8, AllocationHandle.FreeList);

        Assert.IsTrue(ptr1.IsCreated);
        Assert.IsTrue(ptr2.IsCreated);

        ptr1.Dispose();
        ptr2.Dispose();

        Assert.IsFalse(ptr1.IsCreated);
        Assert.IsFalse(ptr2.IsCreated);
    }

    [TestMethod]
    public void StackAllocationTest()
    {
        var thread = new Thread(() =>
        {
            using var scope = AllocationManager.CreateStackScope();
            using var ptr1 = new MemoryBlock(1024, 8, scope.AllocationHandle);

            Assert.IsTrue(ptr1.IsCreated);

            Thread.Sleep(100); // Simulate some work

            ptr1.Dispose();
            scope.Dispose();
        });

        thread.Start();

        using var scope = AllocationManager.CreateStackScope();
        Assert.AreEqual(0u, scope.OriginalOffset);

        using var ptr2 = new MemoryBlock(1024, 8, scope.AllocationHandle);

        Assert.IsTrue(ptr2.IsCreated);

        ptr2.Dispose();
        scope.Dispose();

        thread.Join();
    }
}
