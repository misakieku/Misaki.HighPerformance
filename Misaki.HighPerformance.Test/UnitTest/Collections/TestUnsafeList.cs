using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeList
{
    private UnsafeList<int> _list;

    [TestInitialize]
    public void Initialize()
    {
        _list = new UnsafeList<int>(4, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_list.IsCreated)
        {
            _list.Dispose();
        }
    }

    [TestMethod]
    public void TestAdd()
    {
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);
        Assert.AreEqual(3, _list.Count);
        Assert.AreEqual(1, _list[0]);
        Assert.AreEqual(2, _list[1]);
        Assert.AreEqual(3, _list[2]);
    }

    [TestMethod]
    public void TestResize()
    {
        for (var i = 0; i < 10; i++)
        {
            _list.Add(i);
        }
        Assert.AreEqual(10, _list.Count);
        Assert.IsTrue(_list.Capacity >= 10);
        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(i, _list[i]);
        }
    }

    [TestMethod]
    public void TestRemoveAt()
    {
        _list.Add(0);
        _list.Add(1);
        _list.Add(2);
        _list.RemoveAt(1);
        Assert.AreEqual(2, _list.Count);
        Assert.AreEqual(0, _list[0]);
        Assert.AreEqual(2, _list[1]);
    }

    [TestMethod]
    public void TestRemoveAtSwapBack()
    {
        _list.Add(10);
        _list.Add(11);
        _list.Add(12);
        _list.Add(13);

        // Before: [10, 11, 12, 13]
        _list.RemoveAtSwapBack(1);
        // After: [10, 13, 12]

        Assert.AreEqual(3, _list.Count, "Count should be 3");
        Assert.AreEqual(10, _list[0], "Index 0 should be 10");
        Assert.AreEqual(13, _list[1], "Index 1 should be 13");
        Assert.AreEqual(12, _list[2], "Index 2 should be 12");
    }

    [TestMethod]
    public void TestClear()
    {
        _list.Add(1);
        _list.Add(2);
        _list.Clear();
        Assert.AreEqual(0, _list.Count);
    }

    [TestMethod]
    public void TestAddRange()
    {
        int[] values = { 10, 20, 30 };
        _list.AddRange(values);
        Assert.AreEqual(3, _list.Count);
        Assert.AreEqual(10, _list[0]);
        Assert.AreEqual(20, _list[1]);
        Assert.AreEqual(30, _list[2]);
    }

    [TestMethod]
    public void TestAsSpan()
    {
        _list.Add(1);
        _list.Add(2);
        var span = _list.AsSpan();
        Assert.AreEqual(2, span.Length);
        Assert.AreEqual(1, span[0]);
        Assert.AreEqual(2, span[1]);
    }

    [TestMethod]
    public void TestEnumerator()
    {
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);
        var sum = 0;
        foreach (var item in _list)
        {
            sum += item;
        }
        Assert.AreEqual(6, sum);
    }
}
