using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
[DoNotParallelize]
public class TestAllocationManager
{
    [TestMethod]
    public void PersistentAllocationTest()
    {
        var ptr1 = new MemoryBlock(1024, 8, Allocator.Persistent);
        var ptr2 = new MemoryBlock(2048, 8, Allocator.Persistent);

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
        var ptr1 = new MemoryBlock(1024, 8, Allocator.Temp);
        var ptr2 = new MemoryBlock(2048, 8, Allocator.Temp);

        Assert.IsTrue(ptr1.IsCreated);
        Assert.IsTrue(ptr2.IsCreated);

        ptr1.Dispose();
        ptr2.Dispose();

        Assert.IsFalse(ptr1.IsCreated);
        Assert.IsFalse(ptr2.IsCreated);
    }

    [TestMethod]
    public void FreeListAllocationTest()
    {
        var ptr1 = new MemoryBlock(1024, 8, Allocator.FreeList);
        var ptr2 = new MemoryBlock(2048, 8, Allocator.FreeList);

        Assert.IsTrue(ptr1.IsCreated);
        Assert.IsTrue(ptr2.IsCreated);

        ptr1.Dispose();
        ptr2.Dispose();

        Assert.IsFalse(ptr1.IsCreated);
        Assert.IsFalse(ptr2.IsCreated);
    }

    [TestMethod]
    public unsafe void StackAllocationTest()
    {
        var thread = new Thread(() =>
        {
            var scope = AllocationManager.CreateStackScope();
            var ptr1 = new MemoryBlock(1024, 8, scope.AllocationHandle);

            Assert.IsTrue(ptr1.IsCreated);
            Assert.AreEqual(1024u, ((VirtualStack*)scope.AllocationHandle.State)->Allocated);

            ptr1.Dispose();
            scope.Dispose();
            Assert.AreEqual(0u, ((VirtualStack*)scope.AllocationHandle.State)->Allocated);
        });

        thread.Start();

        var scope = AllocationManager.CreateStackScope();
        var ptr2 = new MemoryBlock(1024, 8, scope.AllocationHandle);

        Assert.IsTrue(ptr2.IsCreated);
        Assert.AreEqual(1024u, ((VirtualStack*)scope.AllocationHandle.State)->Allocated);

        ptr2.Dispose();
        scope.Dispose();
        Assert.AreEqual(0u, ((VirtualStack*)scope.AllocationHandle.State)->Allocated);
    }
}
