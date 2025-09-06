using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestUint2
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var v1 = new uint2();
        Assert.AreEqual(0u, v1.x);
        Assert.AreEqual(0u, v1.y);

        // Single value constructor
        var v2 = new uint2(5u);
        Assert.AreEqual(5u, v2.x);
        Assert.AreEqual(5u, v2.y);

        // Component constructor
        var v3 = new uint2(1u, 2u);
        Assert.AreEqual(1u, v3.x);
        Assert.AreEqual(2u, v3.y);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new uint2(10u, 20u);
        var b = new uint2(5u, 4u);

        // Addition
        var add = a + b;
        Assert.AreEqual(15u, add.x);
        Assert.AreEqual(24u, add.y);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(5u, sub.x);
        Assert.AreEqual(16u, sub.y);

        // Multiplication
        var mul = a * b;
        Assert.AreEqual(50u, mul.x);
        Assert.AreEqual(80u, mul.y);

        // Division
        var div = a / b;
        Assert.AreEqual(2u, div.x);
        Assert.AreEqual(5u, div.y);

        // Modulus
        var mod = a % b;
        Assert.AreEqual(0u, mod.x);
        Assert.AreEqual(0u, mod.y);

        // Scalar operations
        var scalarMul = a * 2u;
        Assert.AreEqual(20u, scalarMul.x);
        Assert.AreEqual(40u, scalarMul.y);
    }

    [TestMethod]
    public void TestBitwiseOperators()
    {
        var a = new uint2(0b1010u, 0b1100u);
        var b = new uint2(0b1100u, 0b1010u);

        // Bitwise AND
        var and = a & b;
        Assert.AreEqual(0b1000u, and.x);
        Assert.AreEqual(0b1000u, and.y);

        // Bitwise OR
        var or = a | b;
        Assert.AreEqual(0b1110u, or.x);
        Assert.AreEqual(0b1110u, or.y);

        // Bitwise XOR
        var xor = a ^ b;
        Assert.AreEqual(0b0110u, xor.x);
        Assert.AreEqual(0b0110u, xor.y);

        // Bitwise NOT
        var not = ~a;
        Assert.AreEqual(~0b1010u, not.x);
        Assert.AreEqual(~0b1100u, not.y);
    }

    [TestMethod]
    public void TestShiftOperators()
    {
        var a = new uint2(8u, 16u);

        // Left shift
        var leftShift = a << 1;
        Assert.AreEqual(16u, leftShift.x);
        Assert.AreEqual(32u, leftShift.y);

        // Right shift
        var rightShift = a >> 1;
        Assert.AreEqual(4u, rightShift.x);
        Assert.AreEqual(8u, rightShift.y);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new uint2(10u, 20u);
        var b = new uint2(10u, 20u);
        var c = new uint2(5u, 30u);

        // Equality
        var isEqual = math.all(a == b);
        Assert.IsTrue(isEqual);

        var isNotEqual = math.any(a != c);
        Assert.IsTrue(isNotEqual);
    }

    [TestMethod]
    public void TestUnaryOperators()
    {
        var a = new uint2(5u, 3u);

        // Unary plus
        var pos = +a;
        Assert.AreEqual(5u, pos.x);
        Assert.AreEqual(3u, pos.y);

        // Note: unary minus doesn't make sense for unsigned types
    }

    [TestMethod]
    public void TestSwizzleProperties()
    {
        var v = new uint2(1u, 2u);

        Assert.AreEqual(1u, v.x);
        Assert.AreEqual(2u, v.y);

        var xy = v.xy;
        Assert.AreEqual(1u, xy.x);
        Assert.AreEqual(2u, xy.y);
    }
}
