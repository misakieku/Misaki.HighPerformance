using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.Mathematics;

internal static class Vectorize
{
    public static Vector128<float> AsVector128(this float2 value)
    {
        Unsafe.SkipInit(out Vector128<float> result);
        Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
        return result;
    }

    public static Vector128<float> AsVector128(this float3 value)
    {
        Unsafe.SkipInit(out Vector128<float> result);
        Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
        return result;
    }

    public static Vector128<float> AsVector128(this float4 value)
    {
        Unsafe.SkipInit(out Vector128<float> result);
        Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 AsFloat2(this Vector128<float> value)
    {
        ref var address = ref Unsafe.As<Vector128<float>, byte>(ref value);
        return Unsafe.ReadUnaligned<float2>(ref address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 AsFloat3(this Vector128<float> value)
    {
        ref var address = ref Unsafe.As<Vector128<float>, byte>(ref value);
        return Unsafe.ReadUnaligned<float3>(ref address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 AsFloat4(this Vector128<float> value)
    {
        ref var address = ref Unsafe.As<Vector128<float>, byte>(ref value);
        return Unsafe.ReadUnaligned<float4>(ref address);
    }
}