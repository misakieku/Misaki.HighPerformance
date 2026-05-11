#if MHP_ENABLE_SAFETY_CHECKS
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public class UnsafeMemoryDiagnosticTest
{
    [TestMethod]
    public unsafe void NoLeak_DoesNotThrow()
    {

        using (var diagnostic = new UnsafeMemoryDiagnostic())
        {
            var memory = new MemoryBlock(1024, 16, AllocationHandle.Persistent);
            memory.Dispose();
        }
        // Should not throw
    }

    [TestMethod]
    public unsafe void MemoryLeak_ThrowsException()
    {
        MemoryBlock memory = default;
        Assert.ThrowsExactly<MemoryLeakException>(() =>
        {
            using (var diagnostic = new UnsafeMemoryDiagnostic())
            {
                memory = new MemoryBlock(1024, 16, AllocationHandle.Persistent);
                // Intentionally not freeing to cause leak
            }
        });

        memory.Dispose(); // Call this so we won't get error about leaked memory during global cleanup.
    }

    [TestMethod]
    public unsafe void OutOfOrderFrees_DoesNotThrow()
    {
        using (var diagnostic = new UnsafeMemoryDiagnostic())
        {
            var memory1 = new MemoryBlock(1024, 16, AllocationHandle.Persistent);
            var memory2 = new MemoryBlock(1024, 16, AllocationHandle.Persistent);
            var memory3 = new MemoryBlock(1024, 16, AllocationHandle.Persistent);

            // Free inner, then next, then last
            memory2.Dispose();
            memory3.Dispose();
            memory1.Dispose();
        }
        // Should not throw
    }
}
#endif