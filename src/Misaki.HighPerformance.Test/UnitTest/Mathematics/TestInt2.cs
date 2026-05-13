using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestInt2
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new int2();
        Assert.AreEqual(0, v1.x);
        Assert.AreEqual(0, v1.y);

        // Single value constructor
        var v2 = new int2(5);
        Assert.AreEqual(5, v2.x);
        Assert.AreEqual(5, v2.y);

        // Component constructor
        var v3 = new int2(1, 2);
        Assert.AreEqual(1, v3.x);
        Assert.AreEqual(2, v3.y);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new int2(10, 20);
        var b = new int2(5, 4);

        // Addition
        var add = a + b;
        Assert.AreEqual(15, add.x);
        Assert.AreEqual(24, add.y);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5, sub.x);
        Assert.AreEqual(16, sub.y);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(50, mul.x);
        Assert.AreEqual(80, mul.y);

        // Division
        var div = a / b;
        Assert.AreEqual(2, div.x);
        Assert.AreEqual(5, div.y);

        // Scalar operations
        var scalarMul = a * 2;
        Assert.AreEqual(20, scalarMul.x);
        Assert.AreEqual(40, scalarMul.y);

        var scalarDiv = a / 2;
        Assert.AreEqual(5, scalarDiv.x);
        Assert.AreEqual(10, scalarDiv.y);
    }

    [TestMethod]
    public void TestBitwiseOperators()
    {
        var a = new int2(0b1010, 0b1100);
        var b = new int2(0b1100, 0b1010);

        // Bitwise AND
        var and = a & b;
        Assert.AreEqual(0b1000, and.x);
        Assert.AreEqual(0b1000, and.y);

        // Bitwise OR
        var or = a | b;
        Assert.AreEqual(0b1110, or.x);
        Assert.AreEqual(0b1110, or.y);

        // Bitwise XOR
        var xor = a ^ b;
        Assert.AreEqual(0b0110, xor.x);
        Assert.AreEqual(0b0110, xor.y);

        // Bitwise NOT
        var not = ~a;
        Assert.AreEqual(~0b1010, not.x);
        Assert.AreEqual(~0b1100, not.y);
    }

    [TestMethod]
    public void TestShiftOperators()
    {
        var a = new int2(8, 16);

        // Left shift
        var leftShift = a << 1;
        Assert.AreEqual(16, leftShift.x);
        Assert.AreEqual(32, leftShift.y);

        // Right shift
        var rightShift = a >> 1;
        Assert.AreEqual(4, rightShift.x);
        Assert.AreEqual(8, rightShift.y);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new int2(10, 20);
        var b = new int2(10, 20);
        var c = new int2(5, 30);

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
        var v = new int2(1, 2);

        // Test common swizzles if they exist
        Assert.AreEqual(1, v.x);
        Assert.AreEqual(2, v.y);

        var xy = v.xy;
        Assert.AreEqual(1, xy.x);
        Assert.AreEqual(2, xy.y);
    }

    [TestMethod]
    public void TestStaticProperties()
    {
        var zero = int2.zero;
        Assert.AreEqual(0, zero.x);
        Assert.AreEqual(0, zero.y);
        var one = int2.one;
        Assert.AreEqual(1, one.x);
        Assert.AreEqual(1, one.y);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new int2(5, -3);

        // Unary minus
        var neg = -a;
        Assert.AreEqual(-5, neg.x);
        Assert.AreEqual(3, neg.y);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5, pos.x);
        Assert.AreEqual(-3, pos.y);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var v = new int2(10, 20);

        Assert.AreEqual(10, v[0]);
        Assert.AreEqual(20, v[1]);
    }
}
