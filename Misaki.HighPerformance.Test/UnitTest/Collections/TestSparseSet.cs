using Misaki.HighPerformance.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestSparseSet
{
    private SparseSet<int> _sparseSet = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sparseSet = new SparseSet<int>(16);
    }

    [TestMethod]
    public void InvalidID()
    {
        Assert.IsFalse(_sparseSet.Contains(0, 0));
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
}
