using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeChunkedQueue
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestMethod]
    public void BasicEnqueueDequeueTest()
    {
        using var queue = new UnsafeChunkedQueue<int>(32, Allocator.Persistent);

        Assert.IsTrue(queue.IsCreated);

        queue.Enqueue(10);
        queue.Enqueue(20);

        Assert.IsTrue(queue.TryDequeue(out var val1));
        Assert.AreEqual(10, val1);

        Assert.IsTrue(queue.TryDequeue(out var val2));
        Assert.AreEqual(20, val2);

        Assert.IsFalse(queue.TryDequeue(out _));
    }

    [TestMethod]
    public void ChunkExpansionTest()
    {
        // Force chunk expansions by enqueuing more than the chunk capacity
        using var queue = new UnsafeChunkedQueue<int>(16, Allocator.Persistent);

        var totalItems = 100;

        for (var i = 0; i < totalItems; i++)
        {
            queue.Enqueue(i);
        }

        for (var i = 0; i < totalItems; i++)
        {
            Assert.IsTrue(queue.TryDequeue(out var val));
            Assert.AreEqual(i, val);
        }

        Assert.IsFalse(queue.TryDequeue(out _));
    }

    [TestMethod]
    public void ConcurrentEnqueueDequeueTest()
    {
        // Multi-threaded stress test to verify lock-free safety and chunk caching
        using var queue = new UnsafeChunkedQueue<int>(64, Allocator.Persistent);
        var totalElements = 100_000;

        var enqueueTask = Task.Run(() =>
        {
            Parallel.For(0, totalElements, i =>
            {
                queue.Enqueue(i);
            });
        }, TestContext.CancellationToken);

        long sum = 0;
        var count = 0;

        var dequeueTask = Task.Run(() =>
        {
            while (Volatile.Read(ref count) < totalElements)
            {
                if (queue.TryDequeue(out var val))
                {
                    Interlocked.Add(ref sum, val);
                    Interlocked.Increment(ref count);
                }
            }
        }, TestContext.CancellationToken);

        Task.WaitAll(enqueueTask, dequeueTask);

        Assert.AreEqual(totalElements, count);

        // Sum of 0..N-1 is N * (N - 1) / 2
        var expectedSum = (long)totalElements * (totalElements - 1) / 2;
        Assert.AreEqual(expectedSum, sum);
    }
}
