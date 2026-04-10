using Misaki.HighPerformance.LowLevel.Buffer;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public unsafe class TestArena
{
    [TestMethod]
    public void VirtualArena_AllocateReallocateResetDispose_Works()
    {
        var arena = new VirtualArena(100_000);
        try
        {
            Assert.IsGreaterThanOrEqualTo(100_000u, arena.Reserved);
            Assert.AreEqual(0u, (uint)arena.Allocated);

            var p1 = (byte*)arena.Allocate(64, 16, AllocationOption.Clear);
            Assert.IsTrue(p1 != null);
            Assert.AreEqual((nuint)0u, ((nuint)p1) & 15u);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(0, p1[i]);
                p1[i] = 0x5A;
            }

            var sameOnShrink = arena.Reallocate(p1, 64, 32, 16, AllocationOption.None);
            Assert.AreEqual((nint)p1, (nint)sameOnShrink);

            var p2 = (byte*)arena.Allocate(32, 8, AllocationOption.None);
            Assert.IsTrue(p2 != null);

            for (var i = 0; i < 64; i++)
            {
                p1[i] = (byte)(i + 1);
            }

            var moved = (byte*)arena.Reallocate(p1, 64, 128, 16, AllocationOption.None);
            Assert.IsNotNull(moved);
            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual((byte)(i + 1), moved[i]);
            }

            arena.Reset();
            Assert.AreEqual(0u, (uint)arena.Allocated);

            var pAfterReset = (byte*)arena.Allocate(32, 8, AllocationOption.None);
            Assert.AreEqual((nint)arena.Buffer, (nint)pAfterReset);
        }
        finally
        {
            arena.Dispose();
        }

        Assert.IsTrue(arena.Buffer == null);
        Assert.AreEqual(0u, (uint)arena.Reserved);
        Assert.IsNull(arena.Allocate(16, 8, AllocationOption.None));

        // Double dispose should be safe
        arena.Dispose();
    }

    [TestMethod]
    public void VirtualArena_ReallocateTailExtendsInPlace_AndClearsAdditionalMemory()
    {
        var arena = new VirtualArena(128 * 1024);
        try
        {
            var ptr = (byte*)arena.Allocate(32, 8, AllocationOption.None);
            Assert.IsNotNull(ptr);
            for (var i = 0; i < 32; i++)
                ptr[i] = 0x33;

            var grown = (byte*)arena.Reallocate(ptr, 32, 96, 8, AllocationOption.Clear);
            Assert.AreEqual((nint)ptr, (nint)grown);

            for (var i = 0; i < 32; i++)
                Assert.AreEqual(0x33, grown[i]);
            for (var i = 32; i < 96; i++)
                Assert.AreEqual(0, grown[i]);
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void VirtualArena_MultiThreadedAllocation_IsThreadSafe()
    {
        var mem = (VirtualArena*)NativeMemory.AllocZeroed((nuint)sizeof(VirtualArena));
        *mem = new VirtualArena(8 * 1024 * 1024);

        try
        {
            const int threadCount = 8;
            const int iterations = 2000;
            var ptrs = new ConcurrentDictionary<nint, byte>();
            var errors = new ConcurrentQueue<Exception>();

            var shared = (IntPtr)mem;
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        var arena = (VirtualArena*)shared;
                        for (var j = 0; j < iterations; j++)
                        {
                            var p = (byte*)arena->Allocate(16, 8, AllocationOption.None);
                            Assert.IsNotNull(p);
                            Assert.AreEqual((nuint)0, ((nuint)p) & 7);
                            Assert.IsTrue(ptrs.TryAdd((nint)p, 0));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                });
            }

            foreach (var t in threads)
                t.Start();
            foreach (var t in threads)
                t.Join();

            if (errors.TryDequeue(out var err))
            {
                throw err;
            }

            Assert.AreEqual(threadCount * iterations, ptrs.Count);
        }
        finally
        {
            mem->Dispose();
            NativeMemory.Free(mem);
        }
    }

    [TestMethod]
    public void Arena_AllocateReallocateResetDispose_Works()
    {
        var arena = new Arena(1024);
        try
        {
            var p1 = (byte*)arena.Allocate(32, 8, AllocationOption.Clear);
            Assert.IsNotNull(p1);
            for (var i = 0; i < 32; i++)
                Assert.AreEqual(0, p1[i]);

            for (var i = 0; i < 32; i++)
                p1[i] = 0x42;
            var same = (byte*)arena.Reallocate(p1, 32, 64, 8, AllocationOption.Clear);
            Assert.AreEqual((nint)p1, (nint)same);
            for (var i = 0; i < 32; i++)
                Assert.AreEqual(0x42, same[i]);
            for (var i = 32; i < 64; i++)
                Assert.AreEqual(0, same[i]);

            var p2 = (byte*)arena.Allocate(16, 8, AllocationOption.None);
            Assert.IsNotNull(p2);

            for (var i = 0; i < 64; i++)
                same[i] = (byte)(255 - i);
            var moved = (byte*)arena.Reallocate(same, 64, 96, 8, AllocationOption.None);
            Assert.IsNotNull(moved);
            for (var i = 0; i < 64; i++)
                Assert.AreEqual((byte)(255 - i), moved[i]);

            arena.Reset();
            Assert.AreEqual(0u, (uint)arena.Offset);
            var firstAfterReset = (byte*)arena.Allocate(8, 8, AllocationOption.None);
            Assert.AreEqual((nint)arena.Buffer, (nint)firstAfterReset);
        }
        finally
        {
            arena.Dispose();
        }

        Assert.IsTrue(arena.Buffer == null);
        Assert.IsNull(arena.Allocate(8, 8, AllocationOption.None));
        arena.Dispose();
    }

    [TestMethod]
    public void Arena_InvalidAlignment_Throws()
    {
        var arena = new Arena(256);
        try
        {
            Assert.ThrowsExactly<ArgumentException>(() => arena.Allocate(8, 3, AllocationOption.None));
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void Arena_MultiThreadedAllocation_IsThreadSafe()
    {
        var mem = (Arena*)NativeMemory.AllocZeroed((nuint)sizeof(Arena));
        *mem = new Arena(4 * 1024 * 1024);

        try
        {
            const int threadCount = 8;
            const int iterations = 2000;

            var ptrs = new ConcurrentDictionary<nint, byte>();
            var errors = new ConcurrentQueue<Exception>();
            var shared = (IntPtr)mem;

            var threads = new Thread[threadCount];
            for (var i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        var arena = (Arena*)shared;
                        for (var j = 0; j < iterations; j++)
                        {
                            var p = (byte*)arena->Allocate(16, 8, AllocationOption.None);
                            Assert.IsNotNull(p);
                            Assert.AreEqual((nuint)0, ((nuint)p) & 7);
                            Assert.IsTrue(ptrs.TryAdd((nint)p, 0));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                });
            }

            foreach (var t in threads)
                t.Start();
            foreach (var t in threads)
                t.Join();

            if (errors.TryDequeue(out var err))
            {
                throw err;
            }

            Assert.AreEqual(threadCount * iterations, ptrs.Count);
        }
        finally
        {
            mem->Dispose();
            NativeMemory.Free(mem);
        }
    }

    [TestMethod]
    public void DynamicArena_GrowthReallocateResetDispose_Works()
    {
        var arena = new DynamicArena(64);
        try
        {
            var p1 = (byte*)arena.Allocate(48, 8, AllocationOption.None);
            Assert.IsNotNull(p1);
            for (var i = 0; i < 48; i++)
                p1[i] = (byte)(i + 1);

            // Force growth to another node
            var p2 = (byte*)arena.Allocate(128, 8, AllocationOption.Clear);
            Assert.IsNotNull(p2);
            Assert.AreNotEqual((nint)p1, (nint)p2);
            for (var i = 0; i < 128; i++)
                Assert.AreEqual(0, p2[i]);

            var moved = (byte*)arena.Reallocate(p1, 48, 96, 8, AllocationOption.None);
            Assert.IsNotNull(moved);
            for (var i = 0; i < 48; i++)
                Assert.AreEqual((byte)(i + 1), moved[i]);

            arena.Reset();
            var firstAfterReset = (byte*)arena.Allocate(16, 8, AllocationOption.None);
            Assert.IsNotNull(firstAfterReset);
            Assert.AreEqual((nint)p1, (nint)firstAfterReset);
        }
        finally
        {
            arena.Dispose();
        }

        Assert.IsNull(arena.Allocate(8, 8, AllocationOption.None));
        arena.Dispose();
    }

    [TestMethod]
    public void DynamicArena_MultiThreadedAllocation_IsThreadSafe()
    {
        var mem = (DynamicArena*)NativeMemory.AllocZeroed((nuint)sizeof(DynamicArena));
        *mem = new DynamicArena(256);

        try
        {
            const int threadCount = 8;
            const int iterations = 1000;

            var ptrs = new ConcurrentDictionary<nint, byte>();
            var errors = new ConcurrentQueue<Exception>();
            var shared = (IntPtr)mem;
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        var arena = (DynamicArena*)shared;
                        for (var j = 0; j < iterations; j++)
                        {
                            var p = (byte*)arena->Allocate(24, 8, AllocationOption.None);
                            Assert.IsNotNull(p);
                            Assert.AreEqual((nuint)0, ((nuint)p) & 7);
                            Assert.IsTrue(ptrs.TryAdd((nint)p, 0));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                });
            }

            foreach (var t in threads)
                t.Start();
            foreach (var t in threads)
                t.Join();

            if (errors.TryDequeue(out var err))
            {
                throw err;
            }

            Assert.AreEqual(threadCount * iterations, ptrs.Count);
        }
        finally
        {
            mem->Dispose();
            NativeMemory.Free(mem);
        }
    }
}