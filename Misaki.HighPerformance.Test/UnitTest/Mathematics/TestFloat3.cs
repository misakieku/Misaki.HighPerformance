using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestFloat3
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new float3();
        Assert.AreEqual(0f, v1.x, 1e-6f);
        Assert.AreEqual(0f, v1.y, 1e-6f);
        Assert.AreEqual(0f, v1.z, 1e-6f);

        // Single value constructor
        var v2 = new float3(5.5f);
        Assert.AreEqual(5.5f, v2.x, 1e-6f);
        Assert.AreEqual(5.5f, v2.y, 1e-6f);
        Assert.AreEqual(5.5f, v2.z, 1e-6f);

        // Component constructor
        var v3 = new float3(1.5f, 2.5f, 3.5f);
        Assert.AreEqual(1.5f, v3.x, 1e-6f);
        Assert.AreEqual(2.5f, v3.y, 1e-6f);
        Assert.AreEqual(3.5f, v3.z, 1e-6f);

        // Mixed constructors
        var v4 = new float3(new float2(1.5f, 2.5f), 3.5f);
        Assert.AreEqual(1.5f, v4.x, 1e-6f);
        Assert.AreEqual(2.5f, v4.y, 1e-6f);
        Assert.AreEqual(3.5f, v4.z, 1e-6f);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new float3(10.5f, 20.5f, 30.5f);
        var b = new float3(5.5f, 4.5f, 6.5f);

        // Addition
        var add = a + b;
        Assert.AreEqual(16f, add.x, 1e-6f);
        Assert.AreEqual(25f, add.y, 1e-6f);
        Assert.AreEqual(37f, add.z, 1e-6f);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5f, sub.x, 1e-6f);
        Assert.AreEqual(16f, sub.y, 1e-6f);
        Assert.AreEqual(24f, sub.z, 1e-6f);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(57.75f, mul.x, 1e-6f);
        Assert.AreEqual(92.25f, mul.y, 1e-6f);
        Assert.AreEqual(198.25f, mul.z, 1e-6f);

        // Scalar operations
        var scalarMul = a * 2f;
        Assert.AreEqual(21f, scalarMul.x, 1e-6f);
        Assert.AreEqual(41f, scalarMul.y, 1e-6f);
        Assert.AreEqual(61f, scalarMul.z, 1e-6f);
    }

    [TestMethod]
    public void TestVectorOperations()
    {
        var a = new float3(1f, 0f, 0f);
        var b = new float3(0f, 1f, 0f);

        // Cross product
        var cross = math.cross(a, b);
        Assert.AreEqual(0f, cross.x, 1e-6f);
        Assert.AreEqual(0f, cross.y, 1e-6f);
        Assert.AreEqual(1f, cross.z, 1e-6f);

        // Dot product
        var dot = math.dot(a, b);
        Assert.AreEqual(0f, dot, 1e-6f);

        // Test with non-orthogonal vectors
        var c = new float3(3f, 4f, 0f);
        var d = new float3(1f, 1f, 1f);

        var dotCD = math.dot(c, d);
        Assert.AreEqual(7f, dotCD, 1e-6f); // 3*1 + 4*1 + 0*1 = 7
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new float3(10.5f, 20.5f, 30.5f);
        var b = new float3(10.5f, 20.5f, 30.5f);
        var c = new float3(5.5f, 30.5f, 25.5f);

        // Equality (approximate for floating point)
        Assert.IsTrue(math.all(math.abs(a - b) < 1e-6f));
        Assert.IsFalse(math.all(math.abs(a - c) < 1e-6f));
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new float3(1.5f, 2.5f, 3.5f);

        Assert.AreEqual(1.5f, v.x, 1e-6f);
        Assert.AreEqual(2.5f, v.y, 1e-6f);
        Assert.AreEqual(3.5f, v.z, 1e-6f);


        var xy = v.xy;
        Assert.AreEqual(1.5f, xy.x, 1e-6f);
        Assert.AreEqual(2.5f, xy.y, 1e-6f);

        var xyz = v.xyz;
        Assert.AreEqual(1.5f, xyz.x, 1e-6f);
        Assert.AreEqual(2.5f, xyz.y, 1e-6f);
        Assert.AreEqual(3.5f, xyz.z, 1e-6f);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new float3(5.5f, -3.5f, 7.5f);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5.5f, neg.x, 1e-6f);
        Assert.AreEqual(3.5f, neg.y, 1e-6f);
        Assert.AreEqual(-7.5f, neg.z, 1e-6f);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5.5f, pos.x, 1e-6f);
        Assert.AreEqual(-3.5f, pos.y, 1e-6f);
        Assert.AreEqual(7.5f, pos.z, 1e-6f);
    }

    [TestMethod]
    public void TestMathFunctions()
    {
        var v = new float3(3f, 4f, 0f);

        // Test length
        var length = math.length(v);
        Assert.AreEqual(5f, length, 1e-6f);

        // Test normalize
        var normalized = math.normalize(v);
        var expectedLength = math.length(normalized);
        Assert.AreEqual(1f, expectedLength, 1e-6f);
        Assert.AreEqual(0.6f, normalized.x, 1e-6f);
        Assert.AreEqual(0.8f, normalized.y, 1e-6f);
        Assert.AreEqual(0f, normalized.z, 1e-6f);
    }

    [TestMethod]
    public void TestStaticValues()
    {
        var zero = float3.zero;
        Assert.AreEqual(0f, zero.x, 1e-6f);
        Assert.AreEqual(0f, zero.y, 1e-6f);
        Assert.AreEqual(0f, zero.z, 1e-6f);


        var right = new float3(1f, 0f, 0f);
        var up = new float3(0f, 1f, 0f);
        var forward = new float3(0f, 0f, 1f);

        // These are common vector directions
        Assert.AreEqual(1f, right.x, 1e-6f);
        Assert.AreEqual(1f, up.y, 1e-6f);
        Assert.AreEqual(1f, forward.z, 1e-6f);
    }
}
