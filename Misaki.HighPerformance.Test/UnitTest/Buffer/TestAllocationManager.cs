using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public class TestAllocationManager
{
    [TestInitialize]
    public void Initialize()
    {
        AllocationManager.EnableDebugLayer();
    }

    [TestMethod]
    public void ShouldNotLeakTest()
    {
        try
        {
            var array = new UnsafeArray<int>(10, Allocator.Persistent);
            var array2 = new UnsafeArray<int>(10, Allocator.Persistent);

            array.Dispose();
            array2.Dispose();

            AllocationManager.Dispose();
        }
        finally
        {
            var leaks = AllocationManager.LiveHeapAllocationCount;
            Assert.AreEqual(0, leaks);
        }
    }

    [TestMethod]
    public void ShouldLeakTest()
    {
        var array = new UnsafeArray<int>(10, Allocator.Persistent);
        var array2 = new UnsafeArray<int>(10, Allocator.Persistent);

        try
        {
            AllocationManager.Dispose();
        }
        catch (MemoryLeakException)
        {
            var leaks = AllocationManager.LiveHeapAllocationCount;
            Assert.AreEqual(2, leaks);

            return;
        }
        finally
        {
            array.Dispose();
            array2.Dispose();
        }

        Assert.Fail("Expected MemoryLeakException was not thrown.");
    }
}