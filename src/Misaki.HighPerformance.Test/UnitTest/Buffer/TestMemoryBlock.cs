using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public unsafe class TestMemoryBlock
{
    [TestMethod]
    public void AllocationTest()
    {
        var block = new MemoryBlock(1024, 8, AllocationHandle.Persistent);
        Assert.IsTrue(block.IsCreated);
        block.Dispose();
        Assert.IsFalse(block.IsCreated);
    }

    [TestMethod]
    public void MemoryWriteReadTest()
    {
        var block = new MemoryBlock(1024, 8, AllocationHandle.Persistent);
        Assert.IsTrue(block.IsCreated);
        var span = block.AsSpan<byte>();

        for (var i = 0; i < span.Length; i++)
        {
            span[i] = (byte)(i % 256);
        }

        for (var i = 0; i < span.Length; i++)
        {
            Assert.AreEqual((byte)(i % 256), span[i]);
        }

        block.Dispose();
    }

    [TestMethod]
    public void ExternalMemoryTest()
    {
        var externalArray = new byte[1024];
        fixed (byte* ptr = externalArray)
        {
            var block = new MemoryBlock(ptr, (nuint)externalArray.Length);
            Assert.IsTrue(block.IsCreated);
            var span = block.AsSpan<byte>();
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = (byte)(i % 256);
            }
            
            for (var i = 0; i < span.Length; i++)
            {
                Assert.AreEqual((byte)(i % 256), externalArray[i]);
            }

            block.Dispose();
        }
    }
}
