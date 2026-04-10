using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeHashSet
{
    private UnsafeHashSet<int> _set;

    [TestInitialize]
    public void Initialize()
    {
        _set = new UnsafeHashSet<int>(4, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _set.Dispose();
    }

    [TestMethod]
    public void TestAddContains()
    {
        Assert.IsTrue(_set.Add(1));
        Assert.IsTrue(_set.Add(2));
        Assert.IsFalse(_set.Add(1)); // Duplicate
        Assert.AreEqual(2, _set.Count);
        Assert.IsTrue(_set.Contains(1));
        Assert.IsTrue(_set.Contains(2));
        Assert.IsFalse(_set.Contains(3));
    }

    [TestMethod]
    public void TestRemove()
    {
        _set.Add(1);
        _set.Add(2);
        Assert.IsTrue(_set.Remove(1));
        Assert.IsFalse(_set.Contains(1));
        Assert.AreEqual(1, _set.Count);
        Assert.IsFalse(_set.Remove(3)); // Not present
    }

    [TestMethod]
    public void TestClear()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Clear();
        Assert.AreEqual(0, _set.Count);
        Assert.IsFalse(_set.Contains(1));
    }

    [TestMethod]
    public void TestResize()
    {
        for (var i = 0; i < 100; i++)
        {
            _set.Add(i);
        }
        Assert.AreEqual(100, _set.Count);
        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(_set.Contains(i));
        }
    }

    [TestMethod]
    public void TestEnumerator()
    {
        _set.Add(10);
        _set.Add(20);
        var sum = 0;
        var count = 0;
        foreach (var item in _set)
        {
            sum += item;
            count++;
        }
        Assert.AreEqual(30, sum);
        Assert.AreEqual(2, count);
    }
}
