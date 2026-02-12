using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestBool2
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new bool2();
        Assert.AreNotEqual(math.TRUE, v1.x);
        Assert.AreNotEqual(math.TRUE, v1.y);

        // Single value constructor
        var v2 = new bool2(true);
        Assert.AreEqual(math.TRUE, v2.x);
        Assert.AreEqual(math.TRUE, v2.y);

        // Component constructor
        var v3 = new bool2(true, false);
        Assert.AreEqual(math.TRUE, v3.x);
        Assert.AreNotEqual(math.TRUE, v3.y);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new bool2(true, false);
        var b = new bool2(true, false);
        var c = new bool2(false, true);

        // For bool vectors, we typically use math.all for equality comparison
        var isEqual = math.all(a == b);
        Assert.IsTrue(isEqual);

        var isNotEqual = math.any(a != c);
        Assert.IsTrue(isNotEqual);
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new bool2(true, false);

        Assert.AreEqual(math.TRUE, v.x);
        Assert.AreNotEqual(math.TRUE, v.y);

        var xy = v.xy;
        Assert.AreEqual(math.TRUE, xy.x);
        Assert.AreNotEqual(math.TRUE, xy.y);
    }

    [TestMethod]
    public void TestMathFunctions()
    {
        var v = new bool2(true, false);

        // Test any function
        var anyResult = math.any(v);
        Assert.IsTrue(anyResult);

        var allFalse = new bool2(false, false);
        var anyFalse = math.any(allFalse);
        Assert.IsFalse(anyFalse);

        // Test all function
        var allResult = math.all(v);
        Assert.IsFalse(allResult);

        var allTrue = new bool2(true, true);
        var allTrueResult = math.all(allTrue);
        Assert.IsTrue(allTrueResult);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var v = new bool2(true, false);

        Assert.AreEqual(math.TRUE, v[0]);
        Assert.AreNotEqual(math.TRUE, v[1]);
    }
}