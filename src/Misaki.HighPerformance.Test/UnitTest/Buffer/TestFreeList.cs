using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest.Buffer;

[TestClass]
public unsafe class TestFreeList
{
    [TestMethod]
    public void SingleThreadedAllocFreeTest()
    {
        using var freeList = new FreeList(8, 1024);

        // Allocate various sizes
        var p1 = freeList.Allocate(16, 8);
        var p2 = freeList.Allocate(32, 8);
        var p3 = freeList.Allocate(64, 8);

        Assert.IsNotNull(p1);
        Assert.IsNotNull(p2);
        Assert.IsNotNull(p3);

        // Free them
        freeList.Free(p1);
        freeList.Free(p2);
        freeList.Free(p3);

        // Allocate again - should reuse from buckets (or at least succeed)
        var p4 = freeList.Allocate(16, 8);
        var p5 = freeList.Allocate(32, 8);

        Assert.IsNotNull(p4);
        Assert.IsNotNull(p5);

        freeList.Free(p4);
        freeList.Free(p5);
    }

    [TestMethod]
    public void MultiThreadedAllocSameThreadFreeTest()
    {
        const int threadCount = 8;
        const int iterations = 1000;
        using var freeList = new FreeList(8, 64 * 1024);

        var threads = new Thread[threadCount];
        for (var i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (var j = 0; j < iterations; j++)
                {
                    var ptr = freeList.Allocate(16, 8);
                    Assert.IsNotNull(ptr);
                    freeList.Free(ptr);
                }
            });
        }

        foreach (var t in threads)
            t.Start();
        foreach (var t in threads)
            t.Join();
    }

    [TestMethod]
    public void MultiThreadedCrossThreadFreeTest()
    {
        const int producerCount = 4;
        const int consumerCount = 4;
        const int iterations = 5000;
        using var freeList = new FreeList(8, 64 * 1024);

        var queue = new System.Collections.Concurrent.ConcurrentQueue<IntPtr>();
        var producers = new Thread[producerCount];
        var consumers = new Thread[consumerCount];

        var producing = true;

        for (var i = 0; i < producerCount; i++)
        {
            producers[i] = new Thread(() =>
            {
                for (var j = 0; j < iterations; j++)
                {
                    var ptr = freeList.Allocate(32, 8);
                    Assert.IsNotNull(ptr);
                    queue.Enqueue((IntPtr)ptr);
                }
            });
        }

        for (var i = 0; i < consumerCount; i++)
        {
            consumers[i] = new Thread(() =>
            {
                while (Volatile.Read(ref producing) || !queue.IsEmpty)
                {
                    if (queue.TryDequeue(out var ptr))
                    {
                        freeList.Free((void*)ptr);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            });
        }

        foreach (var t in producers)
            t.Start();
        foreach (var t in consumers)
            t.Start();

        foreach (var t in producers)
            t.Join();
        Volatile.Write(ref producing, false);
        foreach (var t in consumers)
            t.Join();
    }

    [TestMethod]
    public void OverflowCacheTest()
    {
        // Set maxConcurrencyLevel to 1, but use more threads
        const int threadCount = 5;
        using var freeList = new FreeList(8, 1024);

        var threads = new Thread[threadCount];
        for (var i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                var ptr = freeList.Allocate(16, 8);
                Assert.IsNotNull(ptr);
                freeList.Free(ptr);
            });
        }

        foreach (var t in threads)
            t.Start();
        foreach (var t in threads)
            t.Join();
    }

    [TestMethod]
    public void LargeAllocationTest()
    {
        using var freeList = new FreeList(8, 1024);

        // Allocate larger than default chunk size
        nuint largeSize = 2048;
        var ptr = freeList.Allocate(largeSize, 8);
        Assert.IsNotNull(ptr);

        freeList.Free(ptr);
    }

    [TestMethod]
    public void ZeroSizeAllocation_ReturnsNull()
    {
        using var freeList = new FreeList(8, 1024);
        Assert.IsNull(freeList.Allocate(0, 8));
    }

    [TestMethod]
    public void DoubleDispose_IsSafe()
    {
        var freeList = new FreeList(8, 1024);
        freeList.Dispose();
        freeList.Dispose(); // Should not throw
    }

    [TestMethod]
    public void CollectLocal()
    {
        const int threadCount = 8;
        const int iterations = 1000;
        using var freeList = new FreeList(8, 64 * 1024);

        var threads = new Thread[threadCount];
        for (var i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (var j = 0; j < iterations; j++)
                {
                    var ptr = freeList.Allocate(128, 8);
                    Assert.IsNotNull(ptr);
                    freeList.Free(ptr);
                }

                freeList.CollectLocal();
            });
        }

        foreach (var t in threads)
            t.Start();
        foreach (var t in threads)
            t.Join();
    }
}
