using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeParallelQueue
{
    private UnsafeParallelQueue<int> _queue;

    [TestInitialize]
    public void Setup()
    {
        _queue = new UnsafeParallelQueue<int>(32, LowLevel.Buffer.AllocationHandle.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _queue.Dispose();
    }

    [TestMethod]
    public void TestIsEmpty()
    {
        Assert.IsTrue(_queue.IsEmpty);

        _queue.Enqueue(1);
        _queue.Enqueue(2);
        Assert.IsFalse(_queue.IsEmpty);

        Assert.IsTrue(_queue.TryDequeue(out _));
        Assert.IsTrue(_queue.TryDequeue(out _));
        Assert.IsTrue(_queue.IsEmpty);
    }

    [TestMethod]
    public void TestIsEmptyAcrossChunks()
    {
        for (var i = 0; i < 100; i++)
        {
            _queue.Enqueue(i);
        }

        Assert.IsFalse(_queue.IsEmpty);

        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(_queue.TryDequeue(out _));
        }

        // Partially-consumed final chunk with no successor must read as empty
        Assert.IsTrue(_queue.IsEmpty);
        Assert.AreEqual(0, _queue.Count);
    }

    [TestMethod]
    public void TestIsEmptyAfterClear()
    {
        for (var i = 0; i < 100; i++)
        {
            _queue.Enqueue(i);
        }

        _queue.Clear();
        Assert.IsTrue(_queue.IsEmpty);
    }

    [TestMethod]
    public void TestCountSingleChunk()
    {
        Assert.AreEqual(0, _queue.Count);

        for (var i = 0; i < 32; i++)
        {
            _queue.Enqueue(i);
        }

        Assert.AreEqual(32, _queue.Count);

        Assert.IsTrue(_queue.TryDequeue(out _));
        Assert.AreEqual(31, _queue.Count);

        _queue.Enqueue(100);
        Assert.AreEqual(32, _queue.Count);
    }

    [TestMethod]
    public void TestCountAcrossChunks()
    {
        for (var i = 0; i < 100; i++)
        {
            _queue.Enqueue(i);
        }

        Assert.AreEqual(100, _queue.Count);

        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(_queue.TryDequeue(out _));
        }

        Assert.AreEqual(0, _queue.Count);
        Assert.IsFalse(_queue.TryDequeue(out _));
    }

    [TestMethod]
    public void TestClear()
    {
        for (var i = 0; i < 100; i++)
        {
            _queue.Enqueue(i);
        }

        _queue.Clear();

        Assert.AreEqual(0, _queue.Count);
        Assert.IsFalse(_queue.TryDequeue(out _));

        // Queue must remain fully usable after clearing
        _queue.Enqueue(42);
        Assert.AreEqual(1, _queue.Count);
        Assert.IsTrue(_queue.TryDequeue(out var value));
        Assert.AreEqual(42, value);
        Assert.AreEqual(0, _queue.Count);
    }

    [TestMethod]
    public void TestClearEmptyQueue()
    {
        _queue.Clear();
        Assert.AreEqual(0, _queue.Count);

        _queue.Enqueue(7);
        Assert.AreEqual(1, _queue.Count);
        Assert.IsTrue(_queue.TryDequeue(out var value));
        Assert.AreEqual(7, value);
    }

    [TestMethod]
    public void TestClearAfterPartialDrain()
    {
        for (var i = 0; i < 200; i++)
        {
            _queue.Enqueue(i);
        }

        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(_queue.TryDequeue(out _));
        }

        _queue.Clear();
        Assert.AreEqual(0, _queue.Count);

        // Reuse after clear; chunks come back from the recycled pool
        for (var i = 0; i < 100; i++)
        {
            _queue.Enqueue(i);
        }

        Assert.AreEqual(100, _queue.Count);
        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(_queue.TryDequeue(out var value));
            Assert.AreEqual(i, value);
        }
    }

    [TestMethod]
    public void TestParallelEnqueueThenCount()
    {
        const int total = 5000;
        var producer = _queue.AsParallelProducer();

        Parallel.For(0, total, i =>
        {
            producer.Enqueue(i);
        });

        // Quiescent after Parallel.For: Count must be exact
        Assert.AreEqual(total, _queue.Count);
        Assert.IsFalse(_queue.IsEmpty);
    }

    [TestMethod]
    public void TestParallelDrainThenIsEmpty()
    {
        const int total = 5000;
        var producer = _queue.AsParallelProducer();
        var consumer = _queue.AsParallelConsumer();

        Parallel.For(0, total, i =>
        {
            producer.Enqueue(i);
        });

        Parallel.For(0, total, i =>
        {
            while (consumer.TryDequeue(out _))
            {
            }
        });

        Assert.AreEqual(0, _queue.Count);
        Assert.IsTrue(_queue.IsEmpty);
    }

    [TestMethod]
    public void TestParallelEnqueueDequeue()
    {
        const int total = 5000;
        var producer = _queue.AsParallelProducer();
        var consumer = _queue.AsParallelConsumer();

        Parallel.For(0, total, i =>
        {
            producer.Enqueue(i);
        });

        Parallel.For(0, total, i =>
        {
            while (consumer.TryDequeue(out _))
            {
            }
        });

        Assert.AreEqual(0, _queue.Count);
        Assert.IsTrue(_queue.IsEmpty);
    }
}
