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
}