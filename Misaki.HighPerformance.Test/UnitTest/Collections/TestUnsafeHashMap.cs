using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeHashMap
{
    private UnsafeHashMap<int, int> _map;

    [TestInitialize]
    public void Initialize()
    {
        _map = new UnsafeHashMap<int, int>(4, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_map.IsCreated)
        {
            _map.Dispose();
        }
    }

    [TestMethod]
    public void TestAddGet()
    {
        _map.Add(1, 100);
        _map.Add(2, 200);
        Assert.AreEqual(2, _map.Count);
        Assert.AreEqual(100, _map[1]);
        Assert.AreEqual(200, _map[2]);
    }

    [TestMethod]
    public void TestTryAdd()
    {
        Assert.IsTrue(_map.TryAdd(1, 100));
        Assert.IsFalse(_map.TryAdd(1, 200)); // Duplicate key
        Assert.AreEqual(100, _map[1]);
    }

    [TestMethod]
    public void TestRemove()
    {
        _map.Add(1, 100);
        Assert.IsTrue(_map.Remove(1));
        Assert.IsFalse(_map.ContainsKey(1));
        Assert.AreEqual(0, _map.Count);
    }

    [TestMethod]
    public void TestTryGetValue()
    {
        _map.Add(1, 100);
        Assert.IsTrue(_map.TryGetValue(1, out var value));
        Assert.AreEqual(100, value);
        Assert.IsFalse(_map.TryGetValue(2, out _));
    }

    [TestMethod]
    public void TestIndexerUpdate()
    {
        _map[1] = 100;
        _map[1] = 200;
        Assert.AreEqual(200, _map[1]);
        Assert.AreEqual(1, _map.Count);
    }

    [TestMethod]
    public void TestClear()
    {
        _map.Add(1, 100);
        _map.Clear();
        Assert.AreEqual(0, _map.Count);
        Assert.IsFalse(_map.ContainsKey(1));
    }

    [TestMethod]
    public void TestEnumerator()
    {
        _map.Add(1, 10);
        _map.Add(2, 20);
        var keySum = 0;
        var valueSum = 0;
        foreach (var kvp in _map)
        {
            keySum += kvp.Key;
            valueSum += kvp.Value;
        }
        Assert.AreEqual(3, keySum);
        Assert.AreEqual(30, valueSum);
    }
}
