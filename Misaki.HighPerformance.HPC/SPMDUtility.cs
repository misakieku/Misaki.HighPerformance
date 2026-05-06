using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Misaki.HighPerformance.HPC;

internal static unsafe class SPMDUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> GetIndicesAs128Int32<TNumber>(Vector<TNumber> Indices)
        where TNumber : unmanaged
    {
        var vGeneric = Indices.AsVector128();

        if (typeof(TNumber) == typeof(float))
        {
            var vFloat = Unsafe.BitCast<Vector128<TNumber>, Vector128<float>>(vGeneric);
            return Vector128.ConvertToInt32(vFloat);
        }
        else if (typeof(TNumber) == typeof(int) || typeof(TNumber) == typeof(uint))
        {
            return Unsafe.BitCast<Vector128<TNumber>, Vector128<int>>(vGeneric);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> GetIndicesAs256Int32<TNumber>(Vector<TNumber> indices)
        where TNumber : unmanaged
    {
        var vGeneric = indices.AsVector256();

        if (typeof(TNumber) == typeof(float))
        {
            var vFloat = Unsafe.BitCast<Vector256<TNumber>, Vector256<float>>(vGeneric);
            return Vector256.ConvertToInt32(vFloat);
        }
        else if (typeof(TNumber) == typeof(int) || typeof(TNumber) == typeof(uint))
        {
            return Unsafe.BitCast<Vector256<TNumber>, Vector256<int>>(vGeneric);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> GetIndicesAs128Int64<TNumber>(Vector<TNumber> indices)
        where TNumber : unmanaged
    {
        var vGeneric = indices.AsVector128();

        if (typeof(TNumber) == typeof(double))
        {
            var vDouble = Unsafe.BitCast<Vector128<TNumber>, Vector128<double>>(vGeneric);
            return Vector128.ConvertToInt64(vDouble);
        }
        else if (typeof(TNumber) == typeof(long) || typeof(TNumber) == typeof(ulong))
        {
            return Unsafe.BitCast<Vector128<TNumber>, Vector128<long>>(vGeneric);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<long> GetIndicesAs256Int64<TNumber>(Vector<TNumber> indices)
        where TNumber : unmanaged
    {
        var vGeneric = indices.AsVector256();

        if (typeof(TNumber) == typeof(double))
        {
            var vDouble = Unsafe.BitCast<Vector256<TNumber>, Vector256<double>>(vGeneric);
            return Vector256.ConvertToInt64(vDouble);
        }
        else if (typeof(TNumber) == typeof(long) || typeof(TNumber) == typeof(ulong))
        {
            return Unsafe.BitCast<Vector256<TNumber>, Vector256<long>>(vGeneric);
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
