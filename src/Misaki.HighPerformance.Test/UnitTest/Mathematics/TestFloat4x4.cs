using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestFloat4x4
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor
        var m1 = new float4x4();

        var identity = float4x4.identity;
        Assert.AreEqual(1f, identity.c0.x, 1e-6f);
        Assert.AreEqual(0f, identity.c0.y, 1e-6f);
        Assert.AreEqual(0f, identity.c0.z, 1e-6f);
        Assert.AreEqual(0f, identity.c0.w, 1e-6f);

        Assert.AreEqual(0f, identity.c1.x, 1e-6f);
        Assert.AreEqual(1f, identity.c1.y, 1e-6f);
        Assert.AreEqual(0f, identity.c1.z, 1e-6f);
        Assert.AreEqual(0f, identity.c1.w, 1e-6f);

        // Column constructor
        var c0 = new float4(1f, 0f, 0f, 0f);
        var c1 = new float4(0f, 1f, 0f, 0f);
        var c2 = new float4(0f, 0f, 1f, 0f);
        var c3 = new float4(0f, 0f, 0f, 1f);

        var m2 = new float4x4(c0, c1, c2, c3);
        Assert.AreEqual(1f, m2.c0.x, 1e-6f);
        Assert.AreEqual(1f, m2.c1.y, 1e-6f);
        Assert.AreEqual(1f, m2.c2.z, 1e-6f);
        Assert.AreEqual(1f, m2.c3.w, 1e-6f);
    }

    [TestMethod]
    public void TestArithmeticOperators()
    {
        var a = new float4x4(
            new float4(1f, 2f, 3f, 4f),
            new float4(5f, 6f, 7f, 8f),
            new float4(9f, 10f, 11f, 12f),
            new float4(13f, 14f, 15f, 16f)
        );

        var b = new float4x4(
            new float4(1f, 1f, 1f, 1f),
            new float4(1f, 1f, 1f, 1f),
            new float4(1f, 1f, 1f, 1f),
            new float4(1f, 1f, 1f, 1f)
        );

        // Addition
        var add = a + b;
        Assert.AreEqual(2f, add.c0.x, 1e-6f);
        Assert.AreEqual(3f, add.c0.y, 1e-6f);
        Assert.AreEqual(6f, add.c1.x, 1e-6f);
        Assert.AreEqual(7f, add.c1.y, 1e-6f);

        // Subtraction
        var sub = a - b;
        Assert.AreEqual(0f, sub.c0.x, 1e-6f);
        Assert.AreEqual(1f, sub.c0.y, 1e-6f);
        Assert.AreEqual(4f, sub.c1.x, 1e-6f);
        Assert.AreEqual(5f, sub.c1.y, 1e-6f);

        // Scalar multiplication
        var scalarMul = a * 2f;
        Assert.AreEqual(2f, scalarMul.c0.x, 1e-6f);
        Assert.AreEqual(4f, scalarMul.c0.y, 1e-6f);
        Assert.AreEqual(10f, scalarMul.c1.x, 1e-6f);
        Assert.AreEqual(12f, scalarMul.c1.y, 1e-6f);
    }

    [TestMethod]
    public void TestMatrixMultiplication()
    {
        // Identity matrix multiplication
        var identity = new float4x4(
            new float4(1f, 0f, 0f, 0f),
            new float4(0f, 1f, 0f, 0f),
            new float4(0f, 0f, 1f, 0f),
            new float4(0f, 0f, 0f, 1f)
        );

        var testMatrix = new float4x4(
            new float4(2f, 3f, 4f, 5f),
            new float4(6f, 7f, 8f, 9f),
            new float4(10f, 11f, 12f, 13f),
            new float4(14f, 15f, 16f, 17f)
        );

        // Matrix multiplication with identity should return original matrix
        var result = math.mul(identity, testMatrix);

        Assert.AreEqual(testMatrix.c0.x, result.c0.x, 1e-6f);
        Assert.AreEqual(testMatrix.c0.y, result.c0.y, 1e-6f);
        Assert.AreEqual(testMatrix.c1.x, result.c1.x, 1e-6f);
        Assert.AreEqual(testMatrix.c1.y, result.c1.y, 1e-6f);
    }

    [TestMethod]
    public void TestMatrixVectorMultiplication()
    {
        var matrix = new float4x4(
            new float4(1f, 0f, 0f, 0f),
            new float4(0f, 1f, 0f, 0f),
            new float4(0f, 0f, 1f, 0f),
            new float4(0f, 0f, 0f, 1f)
        );

        var vector = new float4(1f, 2f, 3f, 1f);

        // Identity matrix multiplication should return original vector
        var result = math.mul(matrix, vector);

        Assert.AreEqual(1f, result.x, 1e-6f);
        Assert.AreEqual(2f, result.y, 1e-6f);
        Assert.AreEqual(3f, result.z, 1e-6f);
        Assert.AreEqual(1f, result.w, 1e-6f);
    }

    [TestMethod]
    public void TestTranspose()
    {
        var matrix = new float4x4(
            new float4(1f, 2f, 3f, 4f),
            new float4(5f, 6f, 7f, 8f),
            new float4(9f, 10f, 11f, 12f),
            new float4(13f, 14f, 15f, 16f)
        );

        var transposed = math.transpose(matrix);

        // Check that rows and columns are swapped
        Assert.AreEqual(matrix.c0.x, transposed.c0.x, 1e-6f); // (0,0) stays same
        Assert.AreEqual(matrix.c1.x, transposed.c0.y, 1e-6f); // (1,0) becomes (0,1)
        Assert.AreEqual(matrix.c2.x, transposed.c0.z, 1e-6f); // (2,0) becomes (0,2)
        Assert.AreEqual(matrix.c3.x, transposed.c0.w, 1e-6f); // (3,0) becomes (0,3)

        Assert.AreEqual(matrix.c0.y, transposed.c1.x, 1e-6f); // (0,1) becomes (1,0)
        Assert.AreEqual(matrix.c1.y, transposed.c1.y, 1e-6f); // (1,1) stays same
    }

    [TestMethod]
    public void TestDeterminant()
    {
        // Test determinant of identity matrix (should be 1)
        var identity = new float4x4(
            new float4(1f, 0f, 0f, 0f),
            new float4(0f, 1f, 0f, 0f),
            new float4(0f, 0f, 1f, 0f),
            new float4(0f, 0f, 0f, 1f)
        );

        var det = math.determinant(identity);
        Assert.AreEqual(1f, det, 1e-6f);
    }

    [TestMethod]
    public void TestInverse()
    {
        // Test inverse of identity matrix (should be identity)
        var identity = new float4x4(
            new float4(1f, 0f, 0f, 0f),
            new float4(0f, 1f, 0f, 0f),
            new float4(0f, 0f, 1f, 0f),
            new float4(0f, 0f, 0f, 1f)
        );

        var inverse = math.inverse(identity);

        Assert.AreEqual(1f, inverse.c0.x, 1e-6f);
        Assert.AreEqual(0f, inverse.c0.y, 1e-6f);
        Assert.AreEqual(0f, inverse.c1.x, 1e-6f);
        Assert.AreEqual(1f, inverse.c1.y, 1e-6f);
        Assert.AreEqual(1f, inverse.c2.z, 1e-6f);
        Assert.AreEqual(1f, inverse.c3.w, 1e-6f);
    }

    [TestMethod]
    public void TestComparisonOperators()
    {
        var a = new float4x4(
            new float4(1f, 2f, 3f, 4f),
            new float4(5f, 6f, 7f, 8f),
            new float4(9f, 10f, 11f, 12f),
            new float4(13f, 14f, 15f, 16f)
        );

        var b = new float4x4(
            new float4(1f, 2f, 3f, 4f),
            new float4(5f, 6f, 7f, 8f),
            new float4(9f, 10f, 11f, 12f),
            new float4(13f, 14f, 15f, 16f)
        );

        // Test equality
        var isEqual = math.all(a.c0 == b.c0) &&
                     math.all(a.c1 == b.c1) &&
                     math.all(a.c2 == b.c2) &&
                     math.all(a.c3 == b.c3);
        Assert.IsTrue(isEqual);
    }

    [TestMethod]
    public void TestIndexer()
    {
        var matrix = new float4x4(
            new float4(1f, 2f, 3f, 4f),
            new float4(5f, 6f, 7f, 8f),
            new float4(9f, 10f, 11f, 12f),
            new float4(13f, 14f, 15f, 16f)
        );

        // Test column access
        Assert.AreEqual(1f, matrix[0].x, 1e-6f);
        Assert.AreEqual(2f, matrix[0].y, 1e-6f);
        Assert.AreEqual(5f, matrix[1].x, 1e-6f);
        Assert.AreEqual(6f, matrix[1].y, 1e-6f);
    }
}
