using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeBitSet
{
    private UnsafeBitSet _set1;
    private UnsafeBitSet _set2;

    [TestInitialize]
    public void Initialize()
    {
        _set1 = new UnsafeBitSet(16, Allocator.Persistent, AllocationOption.Clear);
        _set2 = new UnsafeBitSet(16, Allocator.Persistent, AllocationOption.Clear);
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
        Assert.AreEqual(256, _set1.BitCount);
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
        for (int i = 0; i < _set1.BitCount; i++)
        {
            _set1.SetBit(i);
        }

        _set1.ClearAll();
        for (int i = 0; i < _set1.BitCount; i++)
        {
            Assert.IsFalse(_set1.IsSet(i));
        }
    }

    [TestMethod]
    public void TestAndOperation()
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
}