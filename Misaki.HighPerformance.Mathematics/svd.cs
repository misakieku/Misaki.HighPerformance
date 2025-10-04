using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics;

// SVD algorithm as described in:
// Computing the singular value decomposition of 3x3 matrices with minimal branching and elementary floating point operations,
// A.McAdams, A.Selle, R.Tamstorf, J.Teran and E.Sifakis, University of Wisconsin - Madison technical report TR1690, May 2011
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
public static class svd
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
{
    public const float EPSILON_DETERMINANT = 1e-6f;
    public const float EPSILON_RCP = 1e-9f;
    public const float EPSILON_NORMAL_SQRT = 1e-15f;
    public const float EPSILON_NORMAL = 1e-30f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void condSwap(bool c, ref float x, ref float y)
    {
        var tmp = x;
        x = math.select(x, y, c);
        y = math.select(y, tmp, c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void condNegSwap(bool c, ref float3 x, ref float3 y)
    {
        var tmp = -x;
        x = math.select(x, y, c);
        y = math.select(y, tmp, c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static quaternion condNegSwapQuat(bool c, quaternion q, float4 mask)
    {
        const float halfSqrt2 = 0.707106781186548f;
        return math.mul(q, math.select(quaternion.identity.value, mask * halfSqrt2, c));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void sortSingularValues(ref float3x3 b, ref quaternion v)
    {
        var l0 = math.lengthsq(b.c0);
        var l1 = math.lengthsq(b.c1);
        var l2 = math.lengthsq(b.c2);

        var c = l0 < l1;
        condNegSwap(c, ref b.c0, ref b.c1);
        v = condNegSwapQuat(c, v, new float4(0f, 0f, 1f, 1f));
        condSwap(c, ref l0, ref l1);

        c = l0 < l2;
        condNegSwap(c, ref b.c0, ref b.c2);
        v = condNegSwapQuat(c, v, new float4(0f, -1f, 0f, 1f));
        condSwap(c, ref l0, ref l2);

        c = l1 < l2;
        condNegSwap(c, ref b.c1, ref b.c2);
        v = condNegSwapQuat(c, v, new float4(1f, 0f, 0f, 1f));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static quaternion approxGivensQuat(float3 pq, float4 mask)
    {
        const float c8 = 0.923879532511287f; // cos(pi/8)
        const float s8 = 0.38268343236509f; // sin(pi/8)
        const float g = 5.82842712474619f; // 3 + 2 * sqrt(2)

        var ch = 2f * (pq.x - pq.y); // approx cos(a/2)
        var sh = pq.z; // approx sin(a/2)
        var r = math.select(new float4(s8, s8, s8, c8), new float4(sh, sh, sh, ch), g * sh * sh < ch * ch) * mask;
        return math.normalize(r);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static quaternion qrGivensQuat(float2 pq, float4 mask)
    {
        var l = math.sqrt(pq.x * pq.x + pq.y * pq.y);
        var sh = math.select(0f, pq.y, l > EPSILON_NORMAL_SQRT);
        var ch = math.abs(pq.x) + math.max(l, EPSILON_NORMAL_SQRT);
        condSwap(pq.x < 0f, ref sh, ref ch);

        return math.normalize(new float4(sh, sh, sh, ch) * mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static quaternion givensQRFactorization(float3x3 b, out float3x3 r)
    {
        var u = qrGivensQuat(new float2(b.c0.x, b.c0.y), new float4(0f, 0f, 1f, 1f));
        var qmt = new float3x3(math.conjugate(u));
        r = math.mul(qmt, b);

        var q = qrGivensQuat(new float2(r.c0.x, r.c0.z), new float4(0f, -1f, 0f, 1f));
        u = math.mul(u, q);
        qmt = new float3x3(math.conjugate(q));
        r = math.mul(qmt, r);

        q = qrGivensQuat(new float2(r.c1.y, r.c1.z), new float4(1f, 0f, 0f, 1f));
        u = math.mul(u, q);
        qmt = new float3x3(math.conjugate(q));
        r = math.mul(qmt, r);

        return u;
    }

    private static quaternion jacobiIteration(ref float3x3 s, int iterations = 5)
    {
        float3x3 qm;
        quaternion q;
        var v = quaternion.identity;

        for (var i = 0; i < iterations; ++i)
        {
            q = approxGivensQuat(new float3(s.c0.x, s.c1.y, s.c0.y), new float4(0f, 0f, 1f, 1f));
            v = math.mul(v, q);
            qm = new float3x3(q);
            s = math.mul(math.mul(math.transpose(qm), s), qm);

            q = approxGivensQuat(new float3(s.c1.y, s.c2.z, s.c1.z), new float4(1f, 0f, 0f, 1f));
            v = math.mul(v, q);
            qm = new float3x3(q);
            s = math.mul(math.mul(math.transpose(qm), s), qm);

            q = approxGivensQuat(new float3(s.c2.z, s.c0.x, s.c2.x), new float4(0f, 1f, 0f, 1f));
            v = math.mul(v, q);
            qm = new float3x3(q);
            s = math.mul(math.mul(math.transpose(qm), s), qm);
        }

        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 singularValuesDecomposition(float3x3 a, out quaternion u, out quaternion v)
    {
        u = quaternion.identity;
        v = quaternion.identity;

        var s = math.mul(math.transpose(a), a);
        v = jacobiIteration(ref s);
        var b = new float3x3(v);
        b = math.mul(a, b);
        sortSingularValues(ref b, ref v);
        u = givensQRFactorization(b, out var e);

        return new float3(e.c0.x, e.c1.y, e.c2.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 rcpsafe(float3 x, float epsilon = EPSILON_RCP) =>
        math.select(math.rcp(x), float3.zero, math.abs(x) < epsilon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3x3 svdInverse(float3x3 a)
    {
        var e = singularValuesDecomposition(a, out var u, out var v);
        var um = new float3x3(u);
        var vm = new float3x3(v);

        return math.mul(vm, math.scaleMul(rcpsafe(e, EPSILON_DETERMINANT), math.transpose(um)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion svdRotation(float3x3 a)
    {
        singularValuesDecomposition(a, out var u, out var v);
        return math.mul(u, math.conjugate(v));
    }
}
