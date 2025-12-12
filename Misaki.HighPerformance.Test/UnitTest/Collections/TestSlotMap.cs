using Misaki.HighPerformance.Collections;
using System.Collections.Concurrent;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestConcurrentSlotMap
{
    private ConcurrentSlotMap<int> _slotMap = null!;

    public TestContext TestContext
    {
        get;
        set;
    }

    [TestInitialize]
    public void Initialize()
    {
        _slotMap = new ConcurrentSlotMap<int>();
    }

    [TestMethod]
    public void TestDefaultIndex()
    {
        Assert.IsFalse(_slotMap.Contains(0, 0));
    }

    [TestMethod]
    public void TestAddAndContains()
    {
        var slotIndex = _slotMap.Add(42, out var generation);
        Assert.IsTrue(_slotMap.Contains(slotIndex, generation));
        Assert.AreEqual(42, _slotMap.GetElementAt(slotIndex, generation));
    }

    [TestMethod]
    public void TestRemove()
    {
        var slotIndex = _slotMap.Add(100, out var generation);
        Assert.IsTrue(_slotMap.Contains(slotIndex, generation));
        var removed = _slotMap.Remove(slotIndex, generation);
        Assert.IsTrue(removed);
        Assert.IsFalse(_slotMap.Contains(slotIndex, generation));
    }

    [TestMethod]
    public void TestRemoveInvalid()
    {
        var slotIndex = _slotMap.Add(200, out var generation);
        Assert.IsTrue(_slotMap.Contains(slotIndex, generation));
        var removed = _slotMap.Remove(slotIndex, generation + 1); // Wrong generation
        Assert.IsFalse(removed);
        Assert.IsTrue(_slotMap.Contains(slotIndex, generation));
    }

    [TestMethod]
    public void TestIndexReuse()
    {
        var slotIndex1 = _slotMap.Add(300, out var generation1);
        _slotMap.Remove(slotIndex1, generation1);
        var slotIndex2 = _slotMap.Add(400, out var generation2);
        Assert.AreEqual(slotIndex1, slotIndex2);
        Assert.AreNotEqual(generation1, generation2);
    }

    [TestMethod]
    public void TestConcurrentAdditions()
    {
        const int threadCount = 8;
        const int itemsPerThread = 1000;
        var tasks = new List<Task>();

        for (int t = 0; t < threadCount; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < itemsPerThread; i++)
                {
                    _slotMap.Add(i, out _);
                }
            }, TestContext.CancellationTokenSource.Token));
        }

        Task.WaitAll(tasks, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(threadCount * itemsPerThread, _slotMap.Count);
    }

    [TestMethod]
    public void TestConcurrentRandomAddRemove()
    {
        const int threadCount = 8;
        const int operationsPerThread = 1000;

        var tasks = new List<Task>();
        var rand = new Random();
        var addedItems = new ConcurrentBag<(int slotIndex, int generation)>();

        var count = 0;

        for (int t = 0; t < threadCount; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    if (rand.NextDouble() < 0.5)
                    {
                        var slotIndex = _slotMap.Add(i, out var generation);
                        addedItems.Add((slotIndex, generation));

                        Interlocked.Increment(ref count);
                    }
                    else if (addedItems.TryTake(out var item))
                    {
                        _slotMap.Remove(item.slotIndex, item.generation);
                        Interlocked.Decrement(ref count);
                    }
                }
            }, TestContext.CancellationTokenSource.Token));
        }

        Task.WaitAll(tasks, TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(count, _slotMap.Count);
    }
}