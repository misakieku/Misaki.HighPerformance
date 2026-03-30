using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeArray
{
    private UnsafeArray<int> _arr;

    [TestInitialize]
    public void Initialize()
    {
        _arr = new UnsafeArray<int>(16, Allocator.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _arr.Dispose();
    }

    [GlobalTestCleanup]
    public static void GlobalCleanup(TestContext ctx)
    {
        AllocationManager.Dispose();
    }

    [TestMethod]
    public void TestIndexAccess()
    {
        for (var i = 0; i < _arr.Count; i++)
        {
            _arr[i] = i * 10;
        }

        for (var i = 0; i < _arr.Count; i++)
        {
            Assert.AreEqual(i * 10, _arr[i]);
        }
    }

    [TestMethod]
    public void TestEnumeration()
    {
        _arr.Clear();

        var expectedValue = 0;
        foreach (var item in _arr)
        {
            Assert.AreEqual(expectedValue, item);
        }
    }

    [TestMethod]
    public void TestIsCreated()
    {
        Assert.IsTrue(_arr.IsCreated);
        _arr.Dispose();
        Assert.IsFalse(_arr.IsCreated);
    }
}