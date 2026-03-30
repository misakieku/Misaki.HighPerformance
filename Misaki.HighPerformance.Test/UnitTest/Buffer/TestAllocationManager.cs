using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public class TestAllocationManager
{
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
#if MHP_ENABLE_SAFETY_CHECKS
            var leaks = AllocationManager.LiveAllocationCount;
            Assert.AreEqual(0, leaks);
#endif
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
#if MHP_ENABLE_SAFETY_CHECKS
            var leaks = AllocationManager.LiveAllocationCount;
            Assert.AreEqual(2, leaks);
#endif

            return;
        }
        finally
        {
            array.Dispose();
            array2.Dispose();
        }

#if ENABLE_SAFETY_CHECKS
        Assert.Fail("Expected MemoryLeakException was not thrown.");
#endif
    }
}