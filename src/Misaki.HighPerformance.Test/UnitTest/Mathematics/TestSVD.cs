using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestSVD
{
    private const float TOLERANCE = 1e-5f;

    private static void AssertFloat3x3Equal(float3x3 expected, float3x3 actual, float tolerance = TOLERANCE)
    {
        Assert.AreEqual(expected.c0.x, actual.c0.x, tolerance, "c0.x mismatch");
        Assert.AreEqual(expected.c0.y, actual.c0.y, tolerance, "c0.y mismatch");
        Assert.AreEqual(expected.c0.z, actual.c0.z, tolerance, "c0.z mismatch");
        Assert.AreEqual(expected.c1.x, actual.c1.x, tolerance, "c1.x mismatch");
        Assert.AreEqual(expected.c1.y, actual.c1.y, tolerance, "c1.y mismatch");
        Assert.AreEqual(expected.c1.z, actual.c1.z, tolerance, "c1.z mismatch");
        Assert.AreEqual(expected.c2.x, actual.c2.x, tolerance, "c2.x mismatch");
        Assert.AreEqual(expected.c2.y, actual.c2.y, tolerance, "c2.y mismatch");
        Assert.AreEqual(expected.c2.z, actual.c2.z, tolerance, "c2.z mismatch");
    }

    private static float3x3 CreateDiagonal(float3 s)
    {
        return new float3x3(new float3(s.x, 0, 0), new float3(0, s.y, 0), new float3(0, 0, s.z));
    }

    [TestMethod]
    public void TestSVDInverse_Identity()
    {
        var identity = float3x3.identity;
        var inverse = svd.svdInverse(identity);
        AssertFloat3x3Equal(identity, inverse);
    }

    [TestMethod]
    public void TestSVDInverse_Diagonal()
    {
        var scale = new float3(2f, 0.5f, 4f);
        var a = CreateDiagonal(scale);
        var expected = CreateDiagonal(1f / scale);
        var actual = svd.svdInverse(a);
        AssertFloat3x3Equal(expected, actual);
    }

    [TestMethod]
    public void TestSVDRotation_PureRotation()
    {
        var q = quaternion.AxisAngle(math.normalize(new float3(1f, 2f, 3f)), 1.2f);
        var a = new float3x3(q);
        var result = svd.svdRotation(a);

        // SVD rotation should extract the rotation part. 
        // For a pure rotation matrix, result should be the same as input q (or -q)
        var dot = math.dot(q.value, result.value);
        Assert.IsTrue(math.abs(dot) > 0.999f, $"Rotation mismatch: dot product {dot}");
    }

    [TestMethod]
    public void TestSVDInverse_RotationAndScale()
    {
        var q = quaternion.AxisAngle(math.normalize(new float3(0.5f, -0.2f, 0.8f)), 0.5f);
        var r = new float3x3(q);
        var scale = new float3(1.5f, 0.8f, 2.0f);

        // A = R * S
        var a = math.mulScale(r, scale);
        var inverse = svd.svdInverse(a);

        // Check inverse * A = Identity
        var identityResult = math.mul(inverse, a);
        AssertFloat3x3Equal(float3x3.identity, identityResult, 1e-4f);
    }
}
