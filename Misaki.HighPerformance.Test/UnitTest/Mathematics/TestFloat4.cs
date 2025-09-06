using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestFloat4
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new float4();
        Assert.AreEqual(0f, v1.x, 1e-6f);
        Assert.AreEqual(0f, v1.y, 1e-6f);
        Assert.AreEqual(0f, v1.z, 1e-6f);
        Assert.AreEqual(0f, v1.w, 1e-6f);

        // Single value constructor
        var v2 = new float4(5.5f);
        Assert.AreEqual(5.5f, v2.x, 1e-6f);
        Assert.AreEqual(5.5f, v2.y, 1e-6f);
        Assert.AreEqual(5.5f, v2.z, 1e-6f);
        Assert.AreEqual(5.5f, v2.w, 1e-6f);

        // Component constructor
        var v3 = new float4(1.5f, 2.5f, 3.5f, 4.5f);
        Assert.AreEqual(1.5f, v3.x, 1e-6f);
        Assert.AreEqual(2.5f, v3.y, 1e-6f);
        Assert.AreEqual(3.5f, v3.z, 1e-6f);
        Assert.AreEqual(4.5f, v3.w, 1e-6f);

        // Mixed constructors
        var v4 = new float4(new float2(1.5f, 2.5f), 3.5f, 4.5f);
        Assert.AreEqual(1.5f, v4.x, 1e-6f);
        Assert.AreEqual(2.5f, v4.y, 1e-6f);
        Assert.AreEqual(3.5f, v4.z, 1e-6f);
        Assert.AreEqual(4.5f, v4.w, 1e-6f);

        var v5 = new float4(new float3(1.5f, 2.5f, 3.5f), 4.5f);
        Assert.AreEqual(1.5f, v5.x, 1e-6f);
        Assert.AreEqual(2.5f, v5.y, 1e-6f);
        Assert.AreEqual(3.5f, v5.z, 1e-6f);
        Assert.AreEqual(4.5f, v5.w, 1e-6f);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new float4(10.5f, 20.5f, 30.5f, 40.5f);
        var b = new float4(5.5f, 4.5f, 6.5f, 8.5f);

        // Addition
        var add = a + b;
        Assert.AreEqual(16f, add.x, 1e-6f);
        Assert.AreEqual(25f, add.y, 1e-6f);
        Assert.AreEqual(37f, add.z, 1e-6f);
        Assert.AreEqual(49f, add.w, 1e-6f);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5f, sub.x, 1e-6f);
        Assert.AreEqual(16f, sub.y, 1e-6f);
        Assert.AreEqual(24f, sub.z, 1e-6f);
        Assert.AreEqual(32f, sub.w, 1e-6f);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(57.75f, mul.x, 1e-6f);
        Assert.AreEqual(92.25f, mul.y, 1e-6f);
        Assert.AreEqual(198.25f, mul.z, 1e-6f);
        Assert.AreEqual(344.25f, mul.w, 1e-6f);

        // Scalar operations
        var scalarMul = a * 2f;
        Assert.AreEqual(21f, scalarMul.x, 1e-6f);
        Assert.AreEqual(41f, scalarMul.y, 1e-6f);
        Assert.AreEqual(61f, scalarMul.z, 1e-6f);
        Assert.AreEqual(81f, scalarMul.w, 1e-6f);
    }

    [TestMethod]
    public void TestVectorOperations()
    {
        var a = new float4(1f, 2f, 3f, 4f);
        var b = new float4(5f, 6f, 7f, 8f);

        // Dot product
        var dot = math.dot(a, b);
        Assert.AreEqual(70f, dot, 1e-6f); // 1*5 + 2*6 + 3*7 + 4*8 = 5+12+21+32 = 70
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new float4(10.5f, 20.5f, 30.5f, 40.5f);
        var b = new float4(10.5f, 20.5f, 30.5f, 40.5f);
        var c = new float4(5.5f, 30.5f, 25.5f, 45.5f);

        // Equality (approximate for floating point)
        Assert.IsTrue(math.all(math.abs(a - b) < 1e-6f));
        Assert.IsFalse(math.all(math.abs(a - c) < 1e-6f));
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new float4(1.5f, 2.5f, 3.5f, 4.5f);

        Assert.AreEqual(1.5f, v.x, 1e-6f);
        Assert.AreEqual(2.5f, v.y, 1e-6f);
        Assert.AreEqual(3.5f, v.z, 1e-6f);
        Assert.AreEqual(4.5f, v.w, 1e-6f);

        var xy = v.xy;
        Assert.AreEqual(1.5f, xy.x, 1e-6f);
        Assert.AreEqual(2.5f, xy.y, 1e-6f);

        var xyz = v.xyz;
        Assert.AreEqual(1.5f, xyz.x, 1e-6f);
        Assert.AreEqual(2.5f, xyz.y, 1e-6f);
        Assert.AreEqual(3.5f, xyz.z, 1e-6f);

        var xyzw = v.xyzw;
        Assert.AreEqual(1.5f, xyzw.x, 1e-6f);
        Assert.AreEqual(2.5f, xyzw.y, 1e-6f);
        Assert.AreEqual(3.5f, xyzw.z, 1e-6f);
        Assert.AreEqual(4.5f, xyzw.w, 1e-6f);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new float4(5.5f, -3.5f, 7.5f, -9.5f);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5.5f, neg.x, 1e-6f);
        Assert.AreEqual(3.5f, neg.y, 1e-6f);
        Assert.AreEqual(-7.5f, neg.z, 1e-6f);
        Assert.AreEqual(9.5f, neg.w, 1e-6f);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5.5f, pos.x, 1e-6f);
        Assert.AreEqual(-3.5f, pos.y, 1e-6f);
        Assert.AreEqual(7.5f, pos.z, 1e-6f);
        Assert.AreEqual(-9.5f, pos.w, 1e-6f);
    }

    [TestMethod]
    public void TestMathFunctions()
    {
        var v = new float4(1f, 2f, 2f, 0f);

        // Test length
        var length = math.length(v);
        Assert.AreEqual(3f, length, 1e-6f); // sqrt(1+4+4+0) = sqrt(9) = 3

        // Test normalize
        var normalized = math.normalize(v);
        var expectedLength = math.length(normalized);
        Assert.AreEqual(1f, expectedLength, 1e-6f);
        Assert.AreEqual(1f / 3f, normalized.x, 1e-6f);
        Assert.AreEqual(2f / 3f, normalized.y, 1e-6f);
        Assert.AreEqual(2f / 3f, normalized.z, 1e-6f);
        Assert.AreEqual(0f, normalized.w, 1e-6f);
    }

    [TestMethod]
    public void TestStaticValues()
    {
        var zero = float4.zero;
        Assert.AreEqual(0f, zero.x, 1e-6f);
        Assert.AreEqual(0f, zero.y, 1e-6f);
        Assert.AreEqual(0f, zero.z, 1e-6f);
        Assert.AreEqual(0f, zero.w, 1e-6f);

        // Test unit vectors
        var unitX = new float4(1f, 0f, 0f, 0f);
        var unitY = new float4(0f, 1f, 0f, 0f);
        var unitZ = new float4(0f, 0f, 1f, 0f);
        var unitW = new float4(0f, 0f, 0f, 1f);

        Assert.AreEqual(1f, unitX.x, 1e-6f);
        Assert.AreEqual(1f, unitY.y, 1e-6f);
        Assert.AreEqual(1f, unitZ.z, 1e-6f);
        Assert.AreEqual(1f, unitW.w, 1e-6f);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var v = new float4(10.5f, 20.5f, 30.5f, 40.5f);

        Assert.AreEqual(10.5f, v[0], 1e-6f);
        Assert.AreEqual(20.5f, v[1], 1e-6f);
        Assert.AreEqual(30.5f, v[2], 1e-6f);
        Assert.AreEqual(40.5f, v[3], 1e-6f);
    }
}
