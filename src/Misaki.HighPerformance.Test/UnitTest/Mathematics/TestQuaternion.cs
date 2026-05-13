using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestQuaternion
{
    [TestMethod]
    public void TestConstructors()
    {
        // Default constructor (should be identity)
        var q1 = new quaternion();

        // Component constructor
        var q2 = new quaternion(0.5f, 0.5f, 0.5f, 0.5f);
        Assert.AreEqual(0.5f, q2.value.x, 1e-6f);
        Assert.AreEqual(0.5f, q2.value.y, 1e-6f);
        Assert.AreEqual(0.5f, q2.value.z, 1e-6f);
        Assert.AreEqual(0.5f, q2.value.w, 1e-6f);

        // float4 constructor
        var q3 = new quaternion(new float4(0.1f, 0.2f, 0.3f, 0.4f));
        Assert.AreEqual(0.1f, q3.value.x, 1e-6f);
        Assert.AreEqual(0.2f, q3.value.y, 1e-6f);
        Assert.AreEqual(0.3f, q3.value.z, 1e-6f);
        Assert.AreEqual(0.4f, q3.value.w, 1e-6f);

        // Identity quaternion
        var identity = quaternion.identity;
        Assert.AreEqual(0f, identity.value.x, 1e-6f);
        Assert.AreEqual(0f, identity.value.y, 1e-6f);
        Assert.AreEqual(0f, identity.value.z, 1e-6f);
        Assert.AreEqual(1f, identity.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestEulerRotations()
    {
        // Test Euler angle constructors
        var euler = new float3(math.PI / 4f, math.PI / 3f, math.PI / 6f);

        // Test different rotation orders
        var qXYZ = quaternion.EulerXYZ(euler);
        var qXZY = quaternion.EulerXZY(euler);
        var qYXZ = quaternion.EulerYXZ(euler);
        var qYZX = quaternion.EulerYZX(euler);
        var qZXY = quaternion.EulerZXY(euler);
        var qZYX = quaternion.EulerZYX(euler);

        // All should be unit quaternions
        Assert.AreEqual(1f, math.length(qXYZ.value), 1e-6f);
        Assert.AreEqual(1f, math.length(qXZY.value), 1e-6f);
        Assert.AreEqual(1f, math.length(qYXZ.value), 1e-6f);
        Assert.AreEqual(1f, math.length(qYZX.value), 1e-6f);
        Assert.AreEqual(1f, math.length(qZXY.value), 1e-6f);
        Assert.AreEqual(1f, math.length(qZYX.value), 1e-6f);

        // Test generic Euler function with default order (ZXY)
        var qDefault = quaternion.Euler(euler);
        var qDefaultExplicit = quaternion.Euler(euler, math.RotationOrder.ZXY);

        // Should be equal
        Assert.AreEqual(qZXY.value.x, qDefault.value.x, 1e-6f);
        Assert.AreEqual(qZXY.value.y, qDefault.value.y, 1e-6f);
        Assert.AreEqual(qZXY.value.z, qDefault.value.z, 1e-6f);
        Assert.AreEqual(qZXY.value.w, qDefault.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestAxisAngleRotation()
    {
        // Test rotation around Y-axis by 90 degrees
        var axis = new float3(0f, 1f, 0f);
        var angle = math.PI / 2f; // 90 degrees

        var q = quaternion.AxisAngle(axis, angle);

        // Should be unit quaternion
        Assert.AreEqual(1f, math.length(q.value), 1e-6f);

        // For 90-degree rotation around Y-axis, should be (0, sin(45°), 0, cos(45°))
        var expectedSin = math.sin(angle / 2f);
        var expectedCos = math.cos(angle / 2f);

        Assert.AreEqual(0f, q.value.x, 1e-6f);
        Assert.AreEqual(expectedSin, q.value.y, 1e-6f);
        Assert.AreEqual(0f, q.value.z, 1e-6f);
        Assert.AreEqual(expectedCos, q.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestQuaternionMultiplication()
    {
        var q1 = quaternion.identity;
        var q2 = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.PI / 4f);

        // Identity * quaternion = quaternion
        var result1 = math.mul(q1, q2);
        Assert.AreEqual(q2.value.x, result1.value.x, 1e-6f);
        Assert.AreEqual(q2.value.y, result1.value.y, 1e-6f);
        Assert.AreEqual(q2.value.z, result1.value.z, 1e-6f);
        Assert.AreEqual(q2.value.w, result1.value.w, 1e-6f);

        // Quaternion * identity = quaternion
        var result2 = math.mul(q2, q1);
        Assert.AreEqual(q2.value.x, result2.value.x, 1e-6f);
        Assert.AreEqual(q2.value.y, result2.value.y, 1e-6f);
        Assert.AreEqual(q2.value.z, result2.value.z, 1e-6f);
        Assert.AreEqual(q2.value.w, result2.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestQuaternionVectorRotation()
    {
        // Test rotating a vector with quaternion
        var vector = new float3(1f, 0f, 0f);
        var q = quaternion.AxisAngle(new float3(0f, 0f, 1f), math.PI / 2f); // 90° around Z-axis

        var rotated = math.mul(q, vector);

        // Should rotate (1,0,0) to approximately (0,1,0)
        Assert.AreEqual(0f, rotated.x, 1e-6f);
        Assert.AreEqual(1f, rotated.y, 1e-6f);
        Assert.AreEqual(0f, rotated.z, 1e-6f);
    }

    [TestMethod]
    public void TestQuaternionInverse()
    {
        var q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.PI / 3f);
        var qInverse = math.inverse(q);

        // q * inverse(q) should equal identity
        var result = math.mul(q, qInverse);

        Assert.AreEqual(quaternion.identity.value.x, result.value.x, 1e-6f);
        Assert.AreEqual(quaternion.identity.value.y, result.value.y, 1e-6f);
        Assert.AreEqual(quaternion.identity.value.z, result.value.z, 1e-6f);
        Assert.AreEqual(quaternion.identity.value.w, result.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestQuaternionNormalize()
    {
        // Create non-unit quaternion
        var q = new quaternion(1f, 2f, 3f, 4f);

        var normalized = math.normalize(q);

        // Should have length 1
        Assert.AreEqual(1f, math.length(normalized.value), 1e-6f);
    }

    [TestMethod]
    public void TestQuaternionNormalizeSafe()
    {
        // Create non-unit quaternion
        var q = new quaternion(1f, 2f, 3f, 4f);

        var normalized = math.normalizesafe(q);

        // Should have length 1
        Assert.AreEqual(1f, math.length(normalized.value), 1e-6f);
    }

    [TestMethod]
    public void TestMatrixFromQuaternion()
    {
        // Test conversion from quaternion to rotation matrix
        var q = quaternion.identity;
        var matrix = new float3x3(q);

        // Identity quaternion should produce identity matrix
        Assert.AreEqual(1f, matrix.c0.x, 1e-6f);
        Assert.AreEqual(0f, matrix.c0.y, 1e-6f);
        Assert.AreEqual(0f, matrix.c0.z, 1e-6f);

        Assert.AreEqual(0f, matrix.c1.x, 1e-6f);
        Assert.AreEqual(1f, matrix.c1.y, 1e-6f);
        Assert.AreEqual(0f, matrix.c1.z, 1e-6f);

        Assert.AreEqual(0f, matrix.c2.x, 1e-6f);
        Assert.AreEqual(0f, matrix.c2.y, 1e-6f);
        Assert.AreEqual(1f, matrix.c2.z, 1e-6f);
    }

    [TestMethod]
    public void TestQuaternionFromMatrix()
    {
        // Test conversion from matrix to quaternion
        var identity3x3 = new float3x3(
            new float3(1f, 0f, 0f),
            new float3(0f, 1f, 0f),
            new float3(0f, 0f, 1f)
        );

        var q = new quaternion(identity3x3);

        // Should produce identity quaternion
        Assert.AreEqual(quaternion.identity.value.x, q.value.x, 1e-6f);
        Assert.AreEqual(quaternion.identity.value.y, q.value.y, 1e-6f);
        Assert.AreEqual(quaternion.identity.value.z, q.value.z, 1e-6f);
        Assert.AreEqual(quaternion.identity.value.w, q.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestSlerp()
    {
        var q1 = quaternion.identity;
        var q2 = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.PI / 2f);

        // Test interpolation
        var halfway = math.slerp(q1, q2, 0.5f);

        // Should be unit quaternion
        Assert.AreEqual(1f, math.length(halfway.value), 1e-6f);

        // At t=0, should equal q1
        var start = math.slerp(q1, q2, 0f);
        Assert.AreEqual(q1.value.x, start.value.x, 1e-6f);
        Assert.AreEqual(q1.value.y, start.value.y, 1e-6f);
        Assert.AreEqual(q1.value.z, start.value.z, 1e-6f);
        Assert.AreEqual(q1.value.w, start.value.w, 1e-6f);

        // At t=1, should equal q2
        var end = math.slerp(q1, q2, 1f);
        Assert.AreEqual(q2.value.x, end.value.x, 1e-6f);
        Assert.AreEqual(q2.value.y, end.value.y, 1e-6f);
        Assert.AreEqual(q2.value.z, end.value.z, 1e-6f);
        Assert.AreEqual(q2.value.w, end.value.w, 1e-6f);
    }

    [TestMethod]
    public void TestLookRotation()
    {
        var forward = new float3(0f, 0f, -1f);
        var up = new float3(0f, 1f, 0f);
        var q = quaternion.LookRotation(forward, up);

        // Should be unit quaternion
        Assert.AreEqual(1f, math.length(q.value), 1e-6f);

        // Should rotate (0,0,1) to approximately (0,0,1)
        var rotatedForward = math.mul(q, forward);
        Assert.AreEqual(0f, rotatedForward.x, 1e-6f);
        Assert.AreEqual(0f, rotatedForward.y, 1e-6f);
        Assert.AreEqual(1f, rotatedForward.z, 1e-6f);

        // Should rotate (0,1,0) to approximately (0,1,0)
        var rotatedUp = math.mul(q, up);
        Assert.AreEqual(0f, rotatedUp.x, 1e-6f);
        Assert.AreEqual(1f, rotatedUp.y, 1e-6f);
        Assert.AreEqual(0f, rotatedUp.z, 1e-6f);
    }

    [TestMethod]
    public void TestLookRotationSafe()
    {
        var forward = new float3(0f, 0f, -1f);
        var up = new float3(0f, 1f, 0f);
        var q = quaternion.LookRotationSafe(forward, up);

        // Should be unit quaternion
        Assert.AreEqual(1f, math.length(q.value), 1e-6f);

        // Should rotate (0,0,1) to approximately (0,0,1)
        var rotatedForward = math.mul(q, forward);
        Assert.AreEqual(0f, rotatedForward.x, 1e-6f);
        Assert.AreEqual(0f, rotatedForward.y, 1e-6f);
        Assert.AreEqual(1f, rotatedForward.z, 1e-6f);

        // Should rotate (0,1,0) to approximately (0,1,0)
        var rotatedUp = math.mul(q, up);
        Assert.AreEqual(0f, rotatedUp.x, 1e-6f);
        Assert.AreEqual(1f, rotatedUp.y, 1e-6f);
        Assert.AreEqual(0f, rotatedUp.z, 1e-6f);
    }
}
