using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestUnsafeStack
{
    private UnsafeStack<int> _stack;

    [TestInitialize]
    public void Initialize()
    {
        _stack = new UnsafeStack<int>(16, AllocationHandle.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _stack.Dispose();
    }

    [TestMethod]
    public void TestPushPop()
    {
        for (var i = 0; i < 10; i++)
        {
            _stack.Push(i);
        }
        Assert.AreEqual(10, _stack.Count);
        for (var i = 9; i >= 0; i--)
        {
            var value = _stack.Pop();
            Assert.AreEqual(i, value);
        }
        Assert.AreEqual(0, _stack.Count);
    }

    [TestMethod]
    public void TestPeek()
    {
        _stack.Push(42);
        var value = _stack.Peek();
        Assert.AreEqual(42, value);
        Assert.AreEqual(1, _stack.Count);
    }

    [TestMethod]
    public void TestEnumeration()
    {
        for (var i = 0; i < 5; i++)
        {
            _stack.Push(i);
        }

        var expected = 4;
        foreach (var item in _stack)
        {
            Assert.AreEqual(expected, item);
            expected--;
        }
    }
}
