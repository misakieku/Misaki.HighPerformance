using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public unsafe class TestStack
{
    [TestMethod]
    public void Stack_ScopeAllocateReallocateResetDispose_Works()
    {
        var stack = new Stack(256 * 1024);
        try
        {
            var handle = default(AllocationHandle);
            using (stack.CreateScope(handle))
            {
                var p1 = (byte*)stack.Allocate(32, 8, AllocationOption.Clear);
                Assert.IsNotNull(p1);
                for (var i = 0; i < 32; i++)
                {
                    Assert.AreEqual(0, p1[i]);
                }

                for (var i = 0; i < 32; i++)
                {
                    p1[i] = 0x11;
                }

                var grown = (byte*)stack.Reallocate(p1, 32, 96, 8, AllocationOption.Clear);
                Assert.AreEqual((nint)p1, (nint)grown);
                for (var i = 0; i < 32; i++)
                {
                    Assert.AreEqual(0x11, grown[i]);
                }

                for (var i = 32; i < 96; i++)
                {
                    Assert.AreEqual(0, grown[i]);
                }
            }

            Assert.AreEqual(0u, (uint)stack.Offset);

            stack.Reset();
            Assert.AreEqual(0u, (uint)stack.Offset);
        }
        finally
        {
            stack.Dispose();
        }

        Assert.IsNull(stack.Buffer);
    }

    [TestMethod]
    public void Stack_NestedScopes_RewindToPreviousOffsets()
    {
        var stack = new Stack(128 * 1024);
        try
        {
            var handle = default(AllocationHandle);
            using (stack.CreateScope(handle))
            {
                var pOuter = stack.Allocate(32, 8, AllocationOption.None);
                Assert.IsNotNull(pOuter);
                var outerOffset = stack.Offset;

                using (stack.CreateScope(handle))
                {
                    var pInner = stack.Allocate(128, 8, AllocationOption.None);
                    Assert.IsNotNull(pInner);
                    Assert.IsTrue(stack.Offset > outerOffset);
                }

                Assert.AreEqual(outerOffset, stack.Offset);
            }

            Assert.AreEqual(0u, (uint)stack.Offset);
        }
        finally
        {
            stack.Dispose();
        }
    }

    [TestMethod]
    public void Stack_MultipleAllocations_Works()
    {
        var stack = new Stack(1024 * 1024);
        try
        {
            using (stack.CreateScope(default))
            {
                var p1 = (byte*)stack.Allocate(32, 8, AllocationOption.Clear);
                Assert.IsNotNull(p1);
                for (var i = 0; i < 32; i++)
                {
                    Assert.AreEqual(0, p1[i]);
                }

                for (var i = 0; i < 32; i++)
                {
                    p1[i] = 0x42;
                }

                var same = (byte*)stack.Reallocate(p1, 32, 64, 8, AllocationOption.Clear);
                Assert.AreEqual((nint)p1, (nint)same);
                for (var i = 0; i < 32; i++)
                {
                    Assert.AreEqual(0x42, same[i]);
                }

                for (var i = 32; i < 64; i++)
                {
                    Assert.AreEqual(0, same[i]);
                }

                var p2 = (byte*)stack.Allocate(16, 8, AllocationOption.None);
                Assert.IsNotNull(p2);

                for (var i = 0; i < 64; i++)
                {
                    same[i] = (byte)(255 - i);
                }

                var moved = (byte*)stack.Reallocate(same, 64, 96, 8, AllocationOption.None);
                Assert.IsNotNull(moved);
                for (var i = 0; i < 64; i++)
                {
                    Assert.AreEqual((byte)(255 - i), moved[i]);
                }

                stack.Reset();
                Assert.AreEqual(0u, (uint)stack.Offset);
                var firstAfterReset = (byte*)stack.Allocate(8, 8, AllocationOption.None);
                Assert.AreEqual((nint)stack.Buffer, (nint)firstAfterReset);
            }
        }
        finally
        {
            stack.Dispose();
        }

        Assert.IsNull(stack.Buffer);
    }

    [TestMethod]
    public void Stack_InvalidAlignment_Throws()
    {
        var stack = new Stack(128 * 1024);
        try
        {
            using (stack.CreateScope(default))
            {
                Assert.ThrowsExactly<ArgumentException>(() => stack.Allocate(32, 3, AllocationOption.None));
            }
        }
        finally
        {
            stack.Dispose();
        }
    }

    [TestMethod]
    public void Stack_ZeroSizeAllocation_ReturnsNull()
    {
        var stack = new Stack(128 * 1024);
        try
        {
            using (stack.CreateScope(default))
            {
                Assert.IsNull(stack.Allocate(0, 8, AllocationOption.None));
            }
        }
        finally
        {
            stack.Dispose();
        }
    }

    [TestMethod]
    public void Stack_AllocationExceedsCapacity_Throws()
    {
        var stack = new Stack(1024);
        try
        {
            using (stack.CreateScope(default))
            {
                Assert.ThrowsExactly<OutOfMemoryException>(() => stack.Allocate(2048, 8, AllocationOption.None));
            }
        }
        finally
        {
            stack.Dispose();
        }
    }

    [TestMethod]
    public void Stack_ReallocateSmallerSize_ReturnsSamePointer()
    {
        var stack = new Stack(128 * 1024);
        try
        {
            using (stack.CreateScope(default))
            {
                var p = (byte*)stack.Allocate(64, 8);
                Assert.IsNotNull(p);

                for (var i = 0; i < 32; i++)
                {
                    p[i] = (byte)(i + 1);
                }

                var shrunk = (byte*)stack.Reallocate(p, 64, 32, 8, AllocationOption.None);
                Assert.AreEqual((nint)p, (nint)shrunk);
                for (var i = 0; i < 32; i++)
                {
                    Assert.AreEqual((byte)(i + 1), shrunk[i]);
                }
            }
        }
        finally
        {
            stack.Dispose();
        }
    }

    [TestMethod]
    public void Stack_DoubleDispose_IsSafe()
    {
        var stack = new Stack(128 * 1024);
        stack.Dispose();
        stack.Dispose(); // Should not throw
    }
}
