using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestDouble2
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new double2();
        Assert.AreEqual(0.0, v1.x, 1e-15);
        Assert.AreEqual(0.0, v1.y, 1e-15);

        // Single value constructor
        var v2 = new double2(5.5);
        Assert.AreEqual(5.5, v2.x, 1e-15);
        Assert.AreEqual(5.5, v2.y, 1e-15);

        // Component constructor
        var v3 = new double2(1.5, 2.5);
        Assert.AreEqual(1.5, v3.x, 1e-15);
        Assert.AreEqual(2.5, v3.y, 1e-15);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new double2(10.5, 20.5);
        var b = new double2(5.5, 4.5);

        // Addition
        var add = a + b;
        Assert.AreEqual(16.0, add.x, 1e-15);
        Assert.AreEqual(25.0, add.y, 1e-15);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5.0, sub.x, 1e-15);
        Assert.AreEqual(16.0, sub.y, 1e-15);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(57.75, mul.x, 1e-15);
        Assert.AreEqual(92.25, mul.y, 1e-15);

        // Division
        var div = a / b;
        Assert.AreEqual(10.5 / 5.5, div.x, 1e-15);
        Assert.AreEqual(20.5 / 4.5, div.y, 1e-15);

        // Scalar operations
        var scalarMul = a * 2.0;
        Assert.AreEqual(21.0, scalarMul.x, 1e-15);
        Assert.AreEqual(41.0, scalarMul.y, 1e-15);

        var scalarDiv = a / 2.0;
        Assert.AreEqual(5.25, scalarDiv.x, 1e-15);
        Assert.AreEqual(10.25, scalarDiv.y, 1e-15);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new double2(10.5, 20.5);
        var b = new double2(10.5, 20.5);
        var c = new double2(5.5, 30.5);

        // Equality (approximate for floating point)
        Assert.IsTrue(math.all(math.abs(a - b) < 1e-15));
        Assert.IsFalse(math.all(math.abs(a - c) < 1e-15));
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new double2(1.5, 2.5);

        Assert.AreEqual(1.5, v.x, 1e-15);
        Assert.AreEqual(2.5, v.y, 1e-15);

        // Test common swizzles
        var xy = v.xy;
        Assert.AreEqual(1.5, xy.x, 1e-15);
        Assert.AreEqual(2.5, xy.y, 1e-15);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new double2(5.5, -3.5);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5.5, neg.x, 1e-15);
        Assert.AreEqual(3.5, neg.y, 1e-15);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5.5, pos.x, 1e-15);
        Assert.AreEqual(-3.5, pos.y, 1e-15);
    }

    [TestMethod]
    public void TestMathFunctions()
    {
        var v = new double2(3.0, 4.0);

        // Test dot product
        var dot = math.dot(v, v);
        Assert.AreEqual(25.0, dot, 1e-15);

        // Test length
        var length = math.length(v);
        Assert.AreEqual(5.0, length, 1e-15);

        // Test normalize
        var normalized = math.normalize(v);
        var expectedLength = math.length(normalized);
        Assert.AreEqual(1.0, expectedLength, 1e-15);
    }
}
