using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestInt2x2
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var m1 = new int2x2();

        // Column constructor
        var c0 = new int2(1, 2);
        var c1 = new int2(3, 4);
        var m2 = new int2x2(c0, c1);

        Assert.AreEqual(1, m2.c0.x);
        Assert.AreEqual(2, m2.c0.y);
        Assert.AreEqual(3, m2.c1.x);
        Assert.AreEqual(4, m2.c1.y);

        var m3 = new int2x2(1, 2, 3, 4);
        Assert.AreEqual(1, m3.c0.x);
        Assert.AreEqual(2, m3.c1.x);
        Assert.AreEqual(3, m3.c0.y);
        Assert.AreEqual(4, m3.c1.y);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var b = new int2x2(
            new int2(5, 6),
            new int2(7, 8)
        );

        // Addition
        var add = a + b;
        Assert.AreEqual(6, add.c0.x);
        Assert.AreEqual(8, add.c0.y);
        Assert.AreEqual(10, add.c1.x);
        Assert.AreEqual(12, add.c1.y);

        // Subtraction
        var sub = b - a;
        Assert.AreEqual(4, sub.c0.x);
        Assert.AreEqual(4, sub.c0.y);
        Assert.AreEqual(4, sub.c1.x);
        Assert.AreEqual(4, sub.c1.y);

        // Scalar multiplication
        var scalarMul = a * 2;
        Assert.AreEqual(2, scalarMul.c0.x);
        Assert.AreEqual(4, scalarMul.c0.y);
        Assert.AreEqual(6, scalarMul.c1.x);
        Assert.AreEqual(8, scalarMul.c1.y);
    }

    [TestMethod]
    public void TestMatrixMultiplication()
    {
        var a = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var b = new int2x2(
            new int2(5, 6),
            new int2(7, 8)
        );

        // Matrix multiplication
        var mul = math.mul(a, b);

        // Expected result: [1 3] * [5 7] = [1*5+3*6  1*7+3*8] = [23 31]
        //                  [2 4]   [6 8]   [2*5+4*6  2*7+4*8]   [34 46]
        Assert.AreEqual(23, mul.c0.x);
        Assert.AreEqual(34, mul.c0.y);
        Assert.AreEqual(31, mul.c1.x);
        Assert.AreEqual(46, mul.c1.y);
    }

    [TestMethod]
    public void TestMatrixVectorMultiplication()
    {
        var matrix = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var vector = new int2(5, 6);

        // Matrix-vector multiplication
        var result = math.mul(matrix, vector);

        // Expected: [1 3] * [5] = [1*5 + 3*6] = [23]
        //           [2 4]   [6]   [2*5 + 4*6]   [34]
        Assert.AreEqual(23, result.x);
        Assert.AreEqual(34, result.y);
    }

    [TestMethod]
    public void TestTranspose()
    {
        var matrix = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var transposed = math.transpose(matrix);

        // [1 3] -> [1 2]
        // [2 4]    [3 4]
        Assert.AreEqual(1, transposed.c0.x);
        Assert.AreEqual(3, transposed.c0.y);
        Assert.AreEqual(2, transposed.c1.x);
        Assert.AreEqual(4, transposed.c1.y);
    }

    [TestMethod]
    public void TestDeterminant()
    {
        var matrix = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var det = math.determinant(matrix);

        // det = 1*4 - 2*3 = 4 - 6 = -2
        Assert.AreEqual(-2, det);
    }

    [TestMethod]
    public void TestIdentityMatrix()
    {
        var identity = int2x2.identity;

        var testMatrix = new int2x2(
            new int2(5, 6),
            new int2(7, 8)
        );

        var result = math.mul(identity, testMatrix);

        Assert.AreEqual(testMatrix.c0.x, result.c0.x);
        Assert.AreEqual(testMatrix.c0.y, result.c0.y);
        Assert.AreEqual(testMatrix.c1.x, result.c1.x);
        Assert.AreEqual(testMatrix.c1.y, result.c1.y);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var matrix = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        Assert.AreEqual(1, matrix[0].x);
        Assert.AreEqual(2, matrix[0].y);
        Assert.AreEqual(3, matrix[1].x);
        Assert.AreEqual(4, matrix[1].y);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var b = new int2x2(
            new int2(1, 2),
            new int2(3, 4)
        );

        var c = new int2x2(
            new int2(5, 6),
            new int2(7, 8)
        );

        // Test equality
        var isEqual = math.all(a.c0 == b.c0) && math.all(a.c1 == b.c1);
        Assert.IsTrue(isEqual);

        var isDifferent = math.any(a.c0 != c.c0) || math.any(a.c1 != c.c1);
        Assert.IsTrue(isDifferent);
    }
}
