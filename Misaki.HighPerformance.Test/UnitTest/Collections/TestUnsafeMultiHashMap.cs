using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeMultiHashMap
{
    private UnsafeMultiHashMap<int, int> _multiHashMap;

    [TestInitialize]
    public void Initialize()
    {
        _multiHashMap = new UnsafeMultiHashMap<int, int>(4, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _multiHashMap.Dispose();
    }

    [TestMethod]
    public void Add_AllowsDuplicateKeys()
    {
        _multiHashMap.Add(1, 10);
        _multiHashMap.Add(1, 20);
        _multiHashMap.Add(2, 30);
        _multiHashMap.Add(1, 40);

        Assert.AreEqual(4, _multiHashMap.Count);
        Assert.IsTrue(_multiHashMap.ContainsKey(1));
        Assert.AreEqual(3, _multiHashMap.CountValuesForKey(1));

        var values = new int[3];
        var count = 0;

        Assert.IsTrue(_multiHashMap.TryGetFirstValue(1, out values[count++], out var iterator));

        while (_multiHashMap.TryGetNextValue(out var value, ref iterator))
        {
            values[count++] = value;
        }

        Assert.AreEqual(3, count);
        Array.Sort(values);
        CollectionAssert.AreEqual(new[] { 10, 20, 40 }, values);
    }

    [TestMethod]
    public void GetValuesForKey_EnumeratesAllMatchingValues()
    {
        _multiHashMap.Add(7, 1);
        _multiHashMap.Add(7, 2);
        _multiHashMap.Add(3, 99);
        _multiHashMap.Add(7, 3);

        var values = new int[3];
        var index = 0;

        foreach (var value in _multiHashMap.GetValuesForKey(7))
        {
            values[index++] = value;
        }

        Assert.AreEqual(3, index);
        Array.Sort(values);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
    }

    [TestMethod]
    public void Remove_RemovesAllValuesForKey()
    {
        _multiHashMap.Add(5, 10);
        _multiHashMap.Add(5, 20);
        _multiHashMap.Add(6, 30);

        Assert.IsTrue(_multiHashMap.Remove(5));

        Assert.AreEqual(1, _multiHashMap.Count);
        Assert.IsFalse(_multiHashMap.ContainsKey(5));
        Assert.AreEqual(0, _multiHashMap.CountValuesForKey(5));
        Assert.IsFalse(_multiHashMap.TryGetFirstValue(5, out _, out _));
        Assert.IsTrue(_multiHashMap.TryGetValue(6, out var remainingValue));
        Assert.AreEqual(30, remainingValue);
    }

    [TestMethod]
    public void Clear_RemovesAllEntries()
    {
        _multiHashMap.Add(1, 10);
        _multiHashMap.Add(1, 20);
        _multiHashMap.Add(2, 30);

        _multiHashMap.Clear();

        Assert.AreEqual(0, _multiHashMap.Count);
        Assert.IsFalse(_multiHashMap.ContainsKey(1));
        Assert.IsFalse(_multiHashMap.ContainsKey(2));
        Assert.IsFalse(_multiHashMap.TryGetFirstValue(1, out _, out _));
    }

    [TestMethod]
    public void Resize_PreservesDuplicateValues()
    {
        const int keyCount = 4;
        const int valuesPerKey = 8;

        for (var key = 0; key < keyCount; key++)
        {
            for (var value = 0; value < valuesPerKey; value++)
            {
                _multiHashMap.Add(key, key * 100 + value);
            }
        }

        Assert.AreEqual(keyCount * valuesPerKey, _multiHashMap.Count);

        for (var key = 0; key < keyCount; key++)
        {
            Assert.AreEqual(valuesPerKey, _multiHashMap.CountValuesForKey(key));

            var values = new int[valuesPerKey];
            var index = 0;

            Assert.IsTrue(_multiHashMap.TryGetFirstValue(key, out values[index++], out var iterator));
            while (_multiHashMap.TryGetNextValue(out var value, ref iterator))
            {
                values[index++] = value;
            }

            Assert.AreEqual(valuesPerKey, index);
            Array.Sort(values);

            var expected = new int[valuesPerKey];
            for (var value = 0; value < valuesPerKey; value++)
            {
                expected[value] = key * 100 + value;
            }

            CollectionAssert.AreEqual(expected, values);
        }
    }

    [TestMethod]
    public void Enumerate_ReturnsEveryPair()
    {
        _multiHashMap.Add(1, 10);
        _multiHashMap.Add(1, 20);
        _multiHashMap.Add(2, 30);

        var pairs = new List<KeyValuePair<int, int>>();
        foreach (var pair in _multiHashMap)
        {
            pairs.Add(pair);
        }

        Assert.AreEqual(3, pairs.Count);
        CollectionAssert.AreEquivalent(
            new List<KeyValuePair<int, int>>
            {
                new(1, 10),
                new(1, 20),
                new(2, 30),
            },
            pairs);
    }
}
