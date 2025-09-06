using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestInt4
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new int4();
        Assert.AreEqual(0, v1.x);
        Assert.AreEqual(0, v1.y);
        Assert.AreEqual(0, v1.z);
        Assert.AreEqual(0, v1.w);

        // Single value constructor
        var v2 = new int4(5);
        Assert.AreEqual(5, v2.x);
        Assert.AreEqual(5, v2.y);
        Assert.AreEqual(5, v2.z);
        Assert.AreEqual(5, v2.w);

        // Component constructor
        var v3 = new int4(1, 2, 3, 4);
        Assert.AreEqual(1, v3.x);
        Assert.AreEqual(2, v3.y);
        Assert.AreEqual(3, v3.z);
        Assert.AreEqual(4, v3.w);

        // Mixed constructors
        var v4 = new int4(new int2(1, 2), 3, 4);
        Assert.AreEqual(1, v4.x);
        Assert.AreEqual(2, v4.y);
        Assert.AreEqual(3, v4.z);
        Assert.AreEqual(4, v4.w);

        var v5 = new int4(new int3(1, 2, 3), 4);
        Assert.AreEqual(1, v5.x);
        Assert.AreEqual(2, v5.y);
        Assert.AreEqual(3, v5.z);
        Assert.AreEqual(4, v5.w);

        var v6 = new int4(new int2(1, 2), new int2(3, 4));
        Assert.AreEqual(1, v6.x);
        Assert.AreEqual(2, v6.y);
        Assert.AreEqual(3, v6.z);
        Assert.AreEqual(4, v6.w);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new int4(10, 20, 30, 40);
        var b = new int4(5, 4, 6, 8);

        // Addition
        var add = a + b;
        Assert.AreEqual(15, add.x);
        Assert.AreEqual(24, add.y);
        Assert.AreEqual(36, add.z);
        Assert.AreEqual(48, add.w);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5, sub.x);
        Assert.AreEqual(16, sub.y);
        Assert.AreEqual(24, sub.z);
        Assert.AreEqual(32, sub.w);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(50, mul.x);
        Assert.AreEqual(80, mul.y);
        Assert.AreEqual(180, mul.z);
        Assert.AreEqual(320, mul.w);

        // Division
        var div = a / b;
        Assert.AreEqual(2, div.x);
        Assert.AreEqual(5, div.y);
        Assert.AreEqual(5, div.z);
        Assert.AreEqual(5, div.w);

        // Scalar operations
        var scalarMul = a * 2;
        Assert.AreEqual(20, scalarMul.x);
        Assert.AreEqual(40, scalarMul.y);
        Assert.AreEqual(60, scalarMul.z);
        Assert.AreEqual(80, scalarMul.w);
    }

    [TestMethod]
    public void TestBitwiseOperators()
    {
        var a = new int4(0b1010, 0b1100, 0b1001, 0b0110);
        var b = new int4(0b1100, 0b1010, 0b1011, 0b1001);

        // Bitwise AND
        var and = a & b;
        Assert.AreEqual(0b1000, and.x);
        Assert.AreEqual(0b1000, and.y);
        Assert.AreEqual(0b1001, and.z);
        Assert.AreEqual(0b0000, and.w);

        // Bitwise OR
        var or = a | b;
        Assert.AreEqual(0b1110, or.x);
        Assert.AreEqual(0b1110, or.y);
        Assert.AreEqual(0b1011, or.z);
        Assert.AreEqual(0b1111, or.w);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new int4(10, 20, 30, 40);
        var b = new int4(10, 20, 30, 40);
        var c = new int4(5, 30, 25, 45);

        // Equality
        Assert.IsTrue(math.all(a == b));
        Assert.IsFalse(math.all(a == c));

        // Inequality
        Assert.IsFalse(math.all(a != b));
        Assert.IsTrue(math.all(a != c));
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new int4(1, 2, 3, 4);

        Assert.AreEqual(1, v.x);
        Assert.AreEqual(2, v.y);
        Assert.AreEqual(3, v.z);
        Assert.AreEqual(4, v.w);

        var xy = v.xy;
        Assert.AreEqual(1, xy.x);
        Assert.AreEqual(2, xy.y);

        var xyz = v.xyz;
        Assert.AreEqual(1, xyz.x);
        Assert.AreEqual(2, xyz.y);
        Assert.AreEqual(3, xyz.z);

        var xyzw = v.xyzw;
        Assert.AreEqual(1, xyzw.x);
        Assert.AreEqual(2, xyzw.y);
        Assert.AreEqual(3, xyzw.z);
        Assert.AreEqual(4, xyzw.w);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new int4(5, -3, 7, -9);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5, neg.x);
        Assert.AreEqual(3, neg.y);
        Assert.AreEqual(-7, neg.z);
        Assert.AreEqual(9, neg.w);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5, pos.x);
        Assert.AreEqual(-3, pos.y);
        Assert.AreEqual(7, pos.z);
        Assert.AreEqual(-9, pos.w);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var v = new int4(10, 20, 30, 40);

        Assert.AreEqual(10, v[0]);
        Assert.AreEqual(20, v[1]);
        Assert.AreEqual(30, v[2]);
        Assert.AreEqual(40, v[3]);
    }
}
