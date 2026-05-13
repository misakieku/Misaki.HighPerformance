using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeBitSet
{
    private UnsafeBitSet _set1;
    private UnsafeBitSet _set2;

    [TestInitialize]
    public void Initialize()
    {
        _set1 = new UnsafeBitSet(16, AllocationHandle.Persistent, AllocationOption.Clear);
        _set2 = new UnsafeBitSet(16, AllocationHandle.Persistent, AllocationOption.Clear);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _set1.Dispose();
        _set2.Dispose();
    }

    [TestMethod]
    public void TestBitCount()
    {
        Assert.AreEqual(256, _set1.Count);
    }

    [TestMethod]
    public void TestSetAndGet()
    {
        Assert.IsFalse(_set1.IsSet(0));
        _set1.SetBit(0);
        Assert.IsTrue(_set1.IsSet(0));
        _set1.ClearBit(0);
        Assert.IsFalse(_set1.IsSet(0));
    }

    [TestMethod]
    public void TestClearAll()
    {
        for (var i = 0; i < _set1.Count; i++)
        {
            _set1.SetBit(i);
        }

        _set1.ClearAll();
        for (var i = 0; i < _set1.Count; i++)
        {
            Assert.IsFalse(_set1.IsSet(i));
        }
    }

    [TestMethod]
    public void TestAnd()
    {
        _set1.SetBit(0);
        _set1.SetBit(1);

        _set2.SetBit(1);
        _set2.SetBit(2);

        _set1.And(_set2);

        Assert.IsFalse(_set1.IsSet(0));
        Assert.IsTrue(_set1.IsSet(1));
        Assert.IsFalse(_set1.IsSet(2));
    }

    [TestMethod]
    public void TestNot()
    {
        _set1.SetBit(0);
        _set1.SetBit(1);

        _set2.SetBit(1);
        _set2.SetBit(2);

        _set1.Not();
        _set2.Not();

        Assert.IsFalse(_set1.IsSet(0));
        Assert.IsFalse(_set1.IsSet(1));
        Assert.IsTrue(_set1.IsSet(2));
        Assert.IsTrue(_set1.IsSet(3));
        Assert.IsTrue(_set2.IsSet(0));
        Assert.IsFalse(_set2.IsSet(1));
    }

    [TestMethod]
    public void TestANDC()
    {
        _set1.SetBit(0);
        _set1.SetBit(1);

        _set2.SetBit(1);
        _set2.SetBit(2);

        _set1.ANDC(_set2);

        Assert.IsTrue(_set1.IsSet(0));
        Assert.IsFalse(_set1.IsSet(1));
        Assert.IsFalse(_set1.IsSet(2));
    }

}