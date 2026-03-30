using Misaki.HighPerformance.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestSlotMap
{
    private SlotMap<int> _slotMap = null!;

    [TestInitialize]
    public void Initialize()
    {
        _slotMap = new SlotMap<int>();
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
        var removed = _slotMap.Remove(slotIndex, generation + 1); // Wrong Generation
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
}