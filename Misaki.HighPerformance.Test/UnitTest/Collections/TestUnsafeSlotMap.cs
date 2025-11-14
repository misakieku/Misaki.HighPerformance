using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeSlotMap
{
    private UnsafeSlotMap<int> _slotMap;

    [TestInitialize]
    public void Initialize()
    {
        _slotMap = new UnsafeSlotMap<int>(16, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _slotMap.Dispose();
    }

    [TestMethod]
    public void Add()
    {
        var id = _slotMap.Add(10, out var gen);
        Assert.IsTrue(_slotMap.Contains(id, gen));
    }

    [TestMethod]
    public void Remove()
    {
        var id = _slotMap.Add(20, out var gen);
        Assert.IsTrue(_slotMap.Contains(id, gen));

        _slotMap.Remove(id, gen);
        Assert.IsFalse(_slotMap.Contains(id, gen));
    }

    [TestMethod]
    public void IndexReuse()
    {
        var id = _slotMap.Add(20, out var gen);
        Assert.IsTrue(_slotMap.Contains(id, gen));

        _slotMap.Remove(id, gen);
        Assert.IsFalse(_slotMap.Contains(id, gen));

        var newId = _slotMap.Add(30, out var newGen);
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
            indices[i] = _slotMap.Add(i, out generations[i]);
        }

        Assert.AreEqual(count, _slotMap.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.IsTrue(_slotMap.Contains(indices[i], generations[i]));
        }
    }

    [TestMethod]
    public void Clear()
    {
        var id1 = _slotMap.Add(10, out var gen1);
        var id2 = _slotMap.Add(20, out var gen2);

        Assert.AreEqual(2, _slotMap.Count);

        _slotMap.Clear();

        Assert.AreEqual(0, _slotMap.Count);
        Assert.IsFalse(_slotMap.Contains(id1, gen1));
        Assert.IsFalse(_slotMap.Contains(id2, gen2));
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
            ids[i] = _slotMap.Add(values[i], out gens[i]);
        }

        var index = 0;
        foreach (var value in _slotMap)
        {
            Assert.AreEqual(values[index], value);
            index++;
        }

        Assert.AreEqual(count, index);
    }
}