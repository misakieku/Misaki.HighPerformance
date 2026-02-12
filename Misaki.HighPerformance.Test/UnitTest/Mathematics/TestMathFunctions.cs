using Misaki.HighPerformance.Mathematics;

namespace Misaki.HighPerformance.Test.UnitTest.Mathematics;

[TestClass]
public class TestMathFunctions
{
    [TestMethod]
    public void TestTrigonometricFunctions()
    {
        // Test sin, cos, tan
        var angle = math.PI / 4f; // 45 degrees

        var sinValue = math.sin(angle);
        var cosValue = math.cos(angle);
        var tanValue = math.tan(angle);

        Assert.AreEqual(math.SQRT2 / 2f, sinValue, 1e-6f);
        Assert.AreEqual(math.SQRT2 / 2f, cosValue, 1e-6f);
        Assert.AreEqual(1f, tanValue, 1e-6f);

        // Test vector versions
        var angles = new float2(0f, math.PI / 2f);
        var sinValues = math.sin(angles);
        var cosValues = math.cos(angles);

        Assert.AreEqual(0f, sinValues.x, 1e-6f);
        Assert.AreEqual(1f, sinValues.y, 1e-6f);
        Assert.AreEqual(1f, cosValues.x, 1e-6f);
        Assert.AreEqual(0f, cosValues.y, 1e-6f);
    }

    [TestMethod]
    public void TestHyperbolicFunctions()
    {
        var value = 1f;

        var sinhValue = math.sinh(value);
        var coshValue = math.cosh(value);
        var tanhValue = math.tanh(value);

        // sinh(1) ≈ 1.175, cosh(1) ≈ 1.543, tanh(1) ≈ 0.762
        Assert.AreEqual(1.175201f, sinhValue, 1e-3f);
        Assert.AreEqual(1.543081f, coshValue, 1e-3f);
        Assert.AreEqual(0.761594f, tanhValue, 1e-3f);
    }

    [TestMethod]
    public void TestExponentialAndLogarithmic()
    {
        // Test exp, log
        var value = 2f;

        var expValue = math.exp(value);
        var logValue = math.log(expValue);

        Assert.AreEqual(value, logValue, 1e-6f);

        // Test exp2, log2
        var exp2Value = math.exp2(value);
        var log2Value = math.log2(exp2Value);

        Assert.AreEqual(value, log2Value, 1e-6f);

        // Test exp10, log10
        var exp10Value = math.exp10(value);
        var log10Value = math.log10(exp10Value);

        Assert.AreEqual(value, log10Value, 1e-6f);
    }

    [TestMethod]
    public void TestPowerFunctions()
    {
        // Test pow
        var baseValue = 2f;
        var exponent = 3f;
        var powValue = math.pow(baseValue, exponent);

        Assert.AreEqual(8f, powValue, 1e-6f);

        // Test sqrt
        var sqrtValue = math.sqrt(16f);
        Assert.AreEqual(4f, sqrtValue, 1e-6f);

        // Test rsqrt (reciprocal square root)
        var rsqrtValue = math.rsqrt(16f);
        Assert.AreEqual(0.25f, rsqrtValue, 1e-6f);
    }

    [TestMethod]
    public void TestMinMaxClamp()
    {
        // Test min, max
        var a = 3f;
        var b = 7f;

        Assert.AreEqual(3f, math.min(a, b));
        Assert.AreEqual(7f, math.max(a, b));

        // Test clamp
        var value = 5f;
        var clamped = math.clamp(value, 2f, 4f);
        Assert.AreEqual(4f, clamped, 1e-6f);

        var clampedLow = math.clamp(1f, 2f, 4f);
        Assert.AreEqual(2f, clampedLow, 1e-6f);

        var clampedNormal = math.clamp(3f, 2f, 4f);
        Assert.AreEqual(3f, clampedNormal, 1e-6f);

        // Test vector versions
        var va = new float3(1f, 5f, 9f);
        var vb = new float3(3f, 3f, 3f);
        var minResult = math.min(va, vb);
        var maxResult = math.max(va, vb);

        Assert.AreEqual(1f, minResult.x, 1e-6f);
        Assert.AreEqual(3f, minResult.y, 1e-6f);
        Assert.AreEqual(3f, minResult.z, 1e-6f);

        Assert.AreEqual(3f, maxResult.x, 1e-6f);
        Assert.AreEqual(5f, maxResult.y, 1e-6f);
        Assert.AreEqual(9f, maxResult.z, 1e-6f);
    }

    [TestMethod]
    public void TestRoundingFunctions()
    {
        var value = 3.7f;

        // Test floor, ceil, round, trunc
        Assert.AreEqual(3f, math.floor(value), 1e-6f);
        Assert.AreEqual(4f, math.ceil(value), 1e-6f);
        Assert.AreEqual(4f, math.round(value), 1e-6f);
        Assert.AreEqual(3f, math.trunc(value), 1e-6f);

        var negativeValue = -3.7f;
        Assert.AreEqual(-4f, math.floor(negativeValue), 1e-6f);
        Assert.AreEqual(-3f, math.ceil(negativeValue), 1e-6f);
        Assert.AreEqual(-4f, math.round(negativeValue), 1e-6f);
        Assert.AreEqual(-3f, math.trunc(negativeValue), 1e-6f);
    }

    [TestMethod]
    public void TestAbsAndSign()
    {
        // Test abs
        Assert.AreEqual(5f, math.abs(-5f), 1e-6f);
        Assert.AreEqual(5f, math.abs(5f), 1e-6f);
        Assert.AreEqual(0f, math.abs(0f), 1e-6f);

        // Test sign
        Assert.AreEqual(-1f, math.sign(-5f), 1e-6f);
        Assert.AreEqual(1f, math.sign(5f), 1e-6f);
        Assert.AreEqual(0f, math.sign(0f), 1e-6f);

        // Test vector versions
        var v = new float3(-2f, 0f, 3f);
        var absResult = math.abs(v);
        var signResult = math.sign(v);

        Assert.AreEqual(2f, absResult.x, 1e-6f);
        Assert.AreEqual(0f, absResult.y, 1e-6f);
        Assert.AreEqual(3f, absResult.z, 1e-6f);

        Assert.AreEqual(-1f, signResult.x, 1e-6f);
        Assert.AreEqual(0f, signResult.y, 1e-6f);
        Assert.AreEqual(1f, signResult.z, 1e-6f);
    }

    [TestMethod]
    public void TestInterpolation()
    {
        // Test lerp (linear interpolation)
        var a = 0f;
        var b = 10f;
        var t = 0.3f;

        var lerped = math.lerp(a, b, t);
        Assert.AreEqual(3f, lerped, 1e-6f);

        // Test vector lerp
        var va = new float3(0f, 0f, 0f);
        var vb = new float3(10f, 20f, 30f);
        var vLerped = math.lerp(va, vb, t);

        Assert.AreEqual(3f, vLerped.x, 1e-6f);
        Assert.AreEqual(6f, vLerped.y, 1e-6f);
        Assert.AreEqual(9f, vLerped.z, 1e-6f);

        // Test unlerp (inverse lerp)
        var unlerpResult = math.unlerp(a, b, 3f);
        Assert.AreEqual(0.3f, unlerpResult, 1e-6f);
    }

    [TestMethod]
    public void TestStep()
    {
        // Test step function
        var edge = 5f;

        Assert.AreEqual(0f, math.step(edge, 3f), 1e-6f);
        Assert.AreEqual(1f, math.step(edge, 5f), 1e-6f);
        Assert.AreEqual(1f, math.step(edge, 7f), 1e-6f);

        // Test smoothstep
        var smoothResult1 = math.smoothstep(0f, 10f, 5f);
        Assert.AreEqual(0.5f, smoothResult1, 1e-6f);

        var smoothResult2 = math.smoothstep(0f, 10f, 0f);
        Assert.AreEqual(0f, smoothResult2, 1e-6f);

        var smoothResult3 = math.smoothstep(0f, 10f, 10f);
        Assert.AreEqual(1f, smoothResult3, 1e-6f);
    }

    [TestMethod]
    public void TestModAndRemainder()
    {
        // Test fmod
        var fmodResult = math.fmod(7.5f, 3f);
        Assert.AreEqual(1.5f, fmodResult, 1e-6f);

        // Test remainder functions if available
        var a = 7f;
        var b = 3f;
        var remainder = a % b;
        Assert.AreEqual(1f, remainder, 1e-6f);
    }

    [TestMethod]
    public void TestAngleConversions()
    {
        var degrees = 180f;
        var radians = math.radians(degrees);
        Assert.AreEqual(math.PI, radians, 1e-6f);

        var backToDegrees = math.degrees(radians);
        Assert.AreEqual(degrees, backToDegrees, 1e-6f);
    }

    [TestMethod]
    public void TestFloatingPointChecks()
    {
        // Test isnan
        Assert.IsTrue(math.isnan(float.NaN));
        Assert.IsFalse(math.isnan(1f));

        // Test isinf
        Assert.IsTrue(math.isinf(float.PositiveInfinity));
        Assert.IsTrue(math.isinf(float.NegativeInfinity));
        Assert.IsFalse(math.isinf(1f));

        // Test isfinite
        Assert.IsFalse(math.isfinite(float.PositiveInfinity));
        Assert.IsFalse(math.isfinite(float.NaN));
        Assert.IsTrue(math.isfinite(1f));
    }

    [TestMethod]
    public void TestBitOperations()
    {
        // Test asfloat, asint, asuint
        var floatValue = 1.5f;
        var intBits = math.asint(floatValue);
        var floatBack = math.asfloat(intBits);

        Assert.AreEqual(floatValue, floatBack, 1e-6f);

        var uintBits = math.asuint(floatValue);
        var floatFromUint = math.asfloat(uintBits);

        Assert.AreEqual(floatValue, floatFromUint, 1e-6f);
    }

    [TestMethod]
    public void TestSelectFunction()
    {
        // Test select function
        var condition = new bool3(true, false, true);
        var a = new float3(1f, 2f, 3f);
        var b = new float3(4f, 5f, 6f);

        var result = math.select(a, b, condition);

        Assert.AreEqual(1f, result.x, 1e-6f); // condition is true, so select a.x
        Assert.AreEqual(5f, result.y, 1e-6f); // condition is false, so select b.y
        Assert.AreEqual(3f, result.z, 1e-6f); // condition is true, so select a.z
    }
}
