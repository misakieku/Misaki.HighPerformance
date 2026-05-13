using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestFloat2
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new float2();
        Assert.AreEqual(0f, v1.x, 1e-6f);
        Assert.AreEqual(0f, v1.y, 1e-6f);

        // Single value constructor
        var v2 = new float2(5.5f);
        Assert.AreEqual(5.5f, v2.x, 1e-6f);
        Assert.AreEqual(5.5f, v2.y, 1e-6f);

        // Component constructor
        var v3 = new float2(1.5f, 2.5f);
        Assert.AreEqual(1.5f, v3.x, 1e-6f);
        Assert.AreEqual(2.5f, v3.y, 1e-6f);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new float2(10.5f, 20.5f);
        var b = new float2(5.5f, 4.5f);

        // Addition
        var add = a + b;
        Assert.AreEqual(16f, add.x, 1e-6f);
        Assert.AreEqual(25f, add.y, 1e-6f);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5f, sub.x, 1e-6f);
        Assert.AreEqual(16f, sub.y, 1e-6f);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(57.75f, mul.x, 1e-6f);
        Assert.AreEqual(92.25f, mul.y, 1e-6f);

        // Division
        var div = a / b;
        Assert.AreEqual(10.5f / 5.5f, div.x, 1e-6f);
        Assert.AreEqual(20.5f / 4.5f, div.y, 1e-6f);

        // Scalar operations
        var scalarMul = a * 2f;
        Assert.AreEqual(21f, scalarMul.x, 1e-6f);
        Assert.AreEqual(41f, scalarMul.y, 1e-6f);

        var scalarDiv = a / 2f;
        Assert.AreEqual(5.25f, scalarDiv.x, 1e-6f);
        Assert.AreEqual(10.25f, scalarDiv.y, 1e-6f);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new float2(10.5f, 20.5f);
        var b = new float2(10.5f, 20.5f);
        var c = new float2(5.5f, 30.5f);

        // Equality (approximate for floating point)
        Assert.IsTrue(math.all(math.abs(a - b) < 1e-6f));
        Assert.IsFalse(math.all(math.abs(a - c) < 1e-6f));
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new float2(1.5f, 2.5f);

        Assert.AreEqual(1.5f, v.x, 1e-6f);
        Assert.AreEqual(2.5f, v.y, 1e-6f);

        var xy = v.xy;
        Assert.AreEqual(1.5f, xy.x, 1e-6f);
        Assert.AreEqual(2.5f, xy.y, 1e-6f);
    }

    [TestMethod]
    public void TestStaticProperties()
    {
        var zero = float2.zero;
        Assert.AreEqual(0f, zero.x, 1e-6f);
        Assert.AreEqual(0f, zero.y, 1e-6f);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new float2(5.5f, -3.5f);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5.5f, neg.x, 1e-6f);
        Assert.AreEqual(3.5f, neg.y, 1e-6f);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5.5f, pos.x, 1e-6f);
        Assert.AreEqual(-3.5f, pos.y, 1e-6f);
    }

    [TestMethod]
    public void TestMathFunctions()
    {
        var v = new float2(3f, 4f);

        // Test dot product
        var dot = math.dot(v, v);
        Assert.AreEqual(25f, dot, 1e-6f);

        // Test length
        var length = math.length(v);
        Assert.AreEqual(5f, length, 1e-6f);

        // Test normalize
        var normalized = math.normalize(v);
        var expectedLength = math.length(normalized);
        Assert.AreEqual(1f, expectedLength, 1e-6f);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var v = new float2(10.5f, 20.5f);

        Assert.AreEqual(10.5f, v[0], 1e-6f);
        Assert.AreEqual(20.5f, v[1], 1e-6f);
    }
}
