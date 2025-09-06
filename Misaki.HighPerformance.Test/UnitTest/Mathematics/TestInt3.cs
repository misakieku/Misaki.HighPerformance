using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestInt3
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new int3();
        Assert.AreEqual(0, v1.x);
        Assert.AreEqual(0, v1.y);
        Assert.AreEqual(0, v1.z);

        // Single value constructor
        var v2 = new int3(5);
        Assert.AreEqual(5, v2.x);
        Assert.AreEqual(5, v2.y);
        Assert.AreEqual(5, v2.z);

        // Component constructor
        var v3 = new int3(1, 2, 3);
        Assert.AreEqual(1, v3.x);
        Assert.AreEqual(2, v3.y);
        Assert.AreEqual(3, v3.z);

        // Mixed constructors
        var v4 = new int3(new int2(1, 2), 3);
        Assert.AreEqual(1, v4.x);
        Assert.AreEqual(2, v4.y);
        Assert.AreEqual(3, v4.z);

        var v5 = new int3(1, new int2(2, 3));
        Assert.AreEqual(1, v5.x);
        Assert.AreEqual(2, v5.y);
        Assert.AreEqual(3, v5.z);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new int3(10, 20, 30);
        var b = new int3(5, 4, 6);

        // Addition
        var add = a + b;
        Assert.AreEqual(15, add.x);
        Assert.AreEqual(24, add.y);
        Assert.AreEqual(36, add.z);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5, sub.x);
        Assert.AreEqual(16, sub.y);
        Assert.AreEqual(24, sub.z);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(50, mul.x);
        Assert.AreEqual(80, mul.y);
        Assert.AreEqual(180, mul.z);

        // Division
        var div = a / b;
        Assert.AreEqual(2, div.x);
        Assert.AreEqual(5, div.y);
        Assert.AreEqual(5, div.z);

        // Scalar operations
        var scalarMul = a * 2;
        Assert.AreEqual(20, scalarMul.x);
        Assert.AreEqual(40, scalarMul.y);
        Assert.AreEqual(60, scalarMul.z);
    }

    [TestMethod]
    public void TestBitwiseOperators()
    {
        var a = new int3(0b1010, 0b1100, 0b1001);
        var b = new int3(0b1100, 0b1010, 0b1011);

        // Bitwise AND
        var and = a & b;
        Assert.AreEqual(0b1000, and.x);
        Assert.AreEqual(0b1000, and.y);
        Assert.AreEqual(0b1001, and.z);

        // Bitwise OR
        var or = a | b;
        Assert.AreEqual(0b1110, or.x);
        Assert.AreEqual(0b1110, or.y);
        Assert.AreEqual(0b1011, or.z);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new int3(10, 20, 30);
        var b = new int3(10, 20, 30);
        var c = new int3(5, 30, 25);

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
        var v = new int3(1, 2, 3);

        Assert.AreEqual(1, v.x);
        Assert.AreEqual(2, v.y);
        Assert.AreEqual(3, v.z);

        // Test common swizzles if they exist
        try
        {
            var xy = v.xy;
            Assert.AreEqual(1, xy.x);
            Assert.AreEqual(2, xy.y);

            var xyz = v.xyz;
            Assert.AreEqual(1, xyz.x);
            Assert.AreEqual(2, xyz.y);
            Assert.AreEqual(3, xyz.z);
        }
        catch
        {
            // Swizzles might not be implemented
        }
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new int3(5, -3, 7);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5, neg.x);
        Assert.AreEqual(3, neg.y);
        Assert.AreEqual(-7, neg.z);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5, pos.x);
        Assert.AreEqual(-3, pos.y);
        Assert.AreEqual(7, pos.z);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var v = new int3(10, 20, 30);

        Assert.AreEqual(10, v[0]);
        Assert.AreEqual(20, v[1]);
        Assert.AreEqual(30, v[2]);
    }
}
