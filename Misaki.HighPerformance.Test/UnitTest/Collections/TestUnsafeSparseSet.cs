using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeSparseSet
{
    private UnsafeSparseSet<int> _sparseSet;

    [TestInitialize]
    public void Initialize()
    {
        _sparseSet = new UnsafeSparseSet<int>(16, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _sparseSet.Dispose();
    }

    [TestMethod]
    public void Add()
    {
        var id = _sparseSet.Add(10, out var gen);
        Assert.IsTrue(_sparseSet.Contains(id, gen));
    }

    [TestMethod]
    public void Remove()
    {
        var id = _sparseSet.Add(20, out var gen);
        Assert.IsTrue(_sparseSet.Contains(id, gen));

        _sparseSet.Remove(id, gen);
        Assert.IsFalse(_sparseSet.Contains(id, gen));
    }

    [TestMethod]
    public void IndexReuse()
    {
        var id = _sparseSet.Add(20, out var gen);
        Assert.IsTrue(_sparseSet.Contains(id, gen));

        _sparseSet.Remove(id, gen);
        Assert.IsFalse(_sparseSet.Contains(id, gen));

        var newId = _sparseSet.Add(30, out var newGen);
        Assert.AreEqual(id, newId);
        Assert.AreNotEqual(gen, newGen);
    }

    [TestMethod]
    public unsafe void Resize()
    {
        const int count = 20;

        var indices = stackalloc int[count];
        var generations = stackalloc int[count];

        for (var i = 0; i < count; i++)
        {
            indices[i] = _sparseSet.Add(i, out generations[i]);
        }

        Assert.AreEqual(count, _sparseSet.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.IsTrue(_sparseSet.Contains(indices[i], generations[i]));
        }
    }

    [TestMethod]
    public void Clear()
    {
        var id1 = _sparseSet.Add(10, out var gen1);
        var id2 = _sparseSet.Add(20, out var gen2);

        Assert.AreEqual(2, _sparseSet.Count);

        _sparseSet.Clear();

        Assert.AreEqual(0, _sparseSet.Count);
        Assert.IsFalse(_sparseSet.Contains(id1, gen1));
        Assert.IsFalse(_sparseSet.Contains(id2, gen2));
    }

    [TestMethod]
    public unsafe void Enumerate()
    {
        const int count = 3;

        var values = stackalloc int[count] { 10, 20, 30 };
        var ids = stackalloc int[count];
        var gens = stackalloc int[count];

        for (var i = 0; i < count; i++)
        {
            ids[i] = _sparseSet.Add(values[i], out gens[i]);
        }

        var index = 0;
        foreach (var value in _sparseSet)
        {
            Assert.AreEqual(values[index], value);
            index++;
        }

        Assert.AreEqual(count, index);
    }

    [TestMethod]
    public unsafe void MemoryCompact()
    {
        const int count = 3;

        var values = stackalloc int[count] { 10, 20, 30 };
        var ids = stackalloc int[count];
        var gens = stackalloc int[count];

        for (var i = 0; i < count; i++)
        {
            ids[i] = _sparseSet.Add(values[i], out gens[i]);
        }

        _sparseSet.Remove(ids[1], gens[1]); // Remove the second element (20)

        var ptr = (int*)_sparseSet.GetUnsafePtr();

        Assert.AreEqual(2, _sparseSet.Count);

        var index = 0;
        foreach (var value in _sparseSet)
        {
            Assert.AreEqual(ptr[index], value);
            index++;
        }

        Assert.AreEqual(2, index);
    }
}