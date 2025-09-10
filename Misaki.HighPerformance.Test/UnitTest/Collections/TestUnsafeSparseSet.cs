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

    [TestMethod]
    public void Add()
    {
        var id = _sparseSet.Add(10);
        Assert.IsTrue(_sparseSet.Contains(id));
    }

    [TestMethod]
    public void Remove()
    {
        var id = _sparseSet.Add(20);
        Assert.IsTrue(_sparseSet.Contains(id));

        _sparseSet.Remove(id);
        Assert.IsFalse(_sparseSet.Contains(id));
    }

    [TestMethod]
    public void IndexReuse()
    {
        var id = _sparseSet.Add(20);
        Assert.IsTrue(_sparseSet.Contains(id));

        _sparseSet.Remove(id);
        Assert.IsFalse(_sparseSet.Contains(id));

        var newId = _sparseSet.Add(30);
        Assert.AreEqual(id, newId);
    }
}