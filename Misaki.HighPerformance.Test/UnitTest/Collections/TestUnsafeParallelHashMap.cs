using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeParallelHashMap
{
    [TestMethod]
    public void TestParallelWrite()
    {
        using var map = new UnsafeParallelHashMap<int, int>(10000, 1, AllocationHandle.Temp, AllocationOption.None);
        var writer = map.AsParallelWriter();

        Parallel.For(0, 10000, i =>
        {
            writer.TryAdd(i, i * 2);
        });

        Assert.AreEqual(10000, map.Count);

        for (var i = 0; i < 10000; i++)
        {
            Assert.IsTrue(map.TryGetValue(i, out var val));
            Assert.AreEqual(i * 2, val);
        }
    }

    [TestMethod]
    public void TestBasicOperations()
    {
        using var map = new UnsafeParallelHashMap<int, int>(16, 1, AllocationHandle.Temp, AllocationOption.None);

        Assert.IsTrue(map.IsEmpty);
        Assert.AreEqual(0, map.Count);

        // Add
        map.Add(1, 10);
        map.Add(2, 20);
        map.Add(3, 30);

        Assert.IsFalse(map.IsEmpty);
        Assert.AreEqual(3, map.Count);

        // TryGetValue existing
        Assert.IsTrue(map.TryGetValue(2, out var val));
        Assert.AreEqual(20, val);

        // TryGetValue non-existing
        Assert.IsFalse(map.TryGetValue(4, out _));

        // Remove existing
        Assert.IsTrue(map.Remove(2));
        Assert.AreEqual(2, map.Count);
        Assert.IsFalse(map.TryGetValue(2, out _));

        // Remove non-existing
        Assert.IsFalse(map.Remove(4));

        // Clear
        map.Clear();
        Assert.AreEqual(0, map.Count);
        Assert.IsTrue(map.IsEmpty);
    }

    [TestMethod]
    public void TestResize()
    {
        using var map = new UnsafeParallelHashMap<int, int>(16, 1, AllocationHandle.Temp, AllocationOption.None);

        // Single thread adds causing resize
        for (var i = 0; i < 1000; i++)
        {
            map.Add(i, i * 10);
        }

        Assert.AreEqual(1000, map.Count);
        Assert.IsTrue(map.Capacity >= 1000);

        for (var i = 0; i < 1000; i++)
        {
            Assert.IsTrue(map.TryGetValue(i, out var val));
            Assert.AreEqual(i * 10, val);
        }
    }

    private struct BadKey : IEquatable<BadKey>
    {
        public int Id;
        public BadKey(int id) => Id = id;
        public bool Equals(BadKey other) => Id == other.Id;
        public override int GetHashCode() => 1; // Force collision
    }

    [TestMethod]
    public void TestHashCollisions()
    {
        using var map = new UnsafeParallelHashMap<BadKey, int>(16, 1, AllocationHandle.Temp, AllocationOption.None);

        for (var i = 0; i < 10; i++)
        {
            map.Add(new BadKey(i), i * 5);
        }

        Assert.AreEqual(10, map.Count);

        // Verify we can retrieve them all out of the same bucket
        for (var i = 0; i < 10; i++)
        {
            Assert.IsTrue(map.TryGetValue(new BadKey(i), out var val));
            Assert.AreEqual(i * 5, val);
        }

        // Remove from the middle of the linked list
        Assert.IsTrue(map.Remove(new BadKey(5)));
        Assert.IsFalse(map.TryGetValue(new BadKey(5), out _));
        Assert.AreEqual(9, map.Count);

        // Make sure everything else is intact
        Assert.IsTrue(map.TryGetValue(new BadKey(6), out var val6));
        Assert.AreEqual(6 * 5, val6);
    }

    [TestMethod]
    public void TestAddDuplicate()
    {
        using var map = new UnsafeParallelHashMap<int, int>(16, 1, AllocationHandle.Temp, AllocationOption.None);

        Assert.AreEqual(0, map.Add(1, 100));

        // Adding again should return -1
        Assert.AreEqual(-1, map.Add(1, 200));
        Assert.AreEqual(1, map.Count);

        var writer = map.AsParallelWriter();
        Assert.IsFalse(writer.TryAdd(1, 300));
    }

    [TestMethod]
    public void TestParallelWriteExceedsCapacity()
    {
        using var map = new UnsafeParallelHashMap<int, int>(50, 1, AllocationHandle.Temp, AllocationOption.None);
        var writer = map.AsParallelWriter();

        // The exact exception will be wrapped in AggregateException by Parallel.For
        Assert.ThrowsExactly<AggregateException>(() =>
        {
            Parallel.For(0, 100, i =>
            {
                writer.TryAdd(i, i);
            });
        });
    }
}
