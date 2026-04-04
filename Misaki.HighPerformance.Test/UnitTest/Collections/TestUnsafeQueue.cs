using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeQueue
{
    private UnsafeQueue<int> _queue;

    [TestInitialize]
    public void Setup()
    {
        _queue = new UnsafeQueue<int>(4, LowLevel.Buffer.Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _queue.Dispose();
    }

    [TestMethod]
    public void TestEnqueueDequeue()
    {
        _queue.Enqueue(1);
        _queue.Enqueue(2);
        _queue.Enqueue(3);
        Assert.AreEqual(3, _queue.Count);
        Assert.AreEqual(1, _queue.Dequeue());
        Assert.AreEqual(2, _queue.Dequeue());
        Assert.AreEqual(3, _queue.Dequeue());
        Assert.AreEqual(0, _queue.Count);
    }

    [TestMethod]
    public void TestPeek()
    {
        _queue.Enqueue(10);
        _queue.Enqueue(20);
        Assert.AreEqual(10, _queue.Peek());
        Assert.AreEqual(10, _queue.Dequeue());
        Assert.AreEqual(20, _queue.Peek());
    }

    [TestMethod]
    public void TestTryPeek()
    {
        Assert.IsFalse(_queue.TryPeek(out _));
        _queue.Enqueue(5);
        Assert.IsTrue(_queue.TryPeek(out var value));
        Assert.AreEqual(5, value);
    }

    [TestMethod]
    public void TestResize()
    {
        for (var i = 0; i < 10; i++)
        {
            _queue.Enqueue(i);
        }

        Assert.AreEqual(10, _queue.Count);

        _queue.Dequeue();
        _queue.Dequeue();

        for (var i = 10; i < 20; i++)
        {
            _queue.Enqueue(i);
        }

        for (var i = 2; i < 20; i++)
        {
            Assert.AreEqual(i, _queue.Dequeue());
        }
    }
}
