using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.HPC;

public readonly unsafe partial struct WideLane<TNumber> : ISPMDLane<WideLane<TNumber>, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOther Cast<TOther, TOtherNumber>()
        where TOther : ISPMDLane<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>
    {
        if (typeof(TNumber) == typeof(float) && typeof(TOtherNumber) == typeof(int))
        {
            return Unsafe.BitCast<Vector<int>, TOther>(Vector.ConvertToInt32(Unsafe.BitCast<Vector<TNumber>, Vector<float>>(value)));
        }

        if (typeof(TNumber) == typeof(float) && typeof(TOtherNumber) == typeof(uint))
        {
            return Unsafe.BitCast<Vector<uint>, TOther>(Vector.ConvertToUInt32(Unsafe.BitCast<Vector<TNumber>, Vector<float>>(value)));
        }

        if (typeof(TNumber) == typeof(double) && typeof(TOtherNumber) == typeof(long))
        {
            return Unsafe.BitCast<Vector<long>, TOther>(Vector.ConvertToInt64(Unsafe.BitCast<Vector<TNumber>, Vector<double>>(value)));
        }

        if (typeof(TNumber) == typeof(double) && typeof(TOtherNumber) == typeof(ulong))
        {
            return Unsafe.BitCast<Vector<ulong>, TOther>(Vector.ConvertToUInt64(Unsafe.BitCast<Vector<TNumber>, Vector<double>>(value)));
        }

        if (typeof(TNumber) == typeof(int) && typeof(TOtherNumber) == typeof(float))
        {
            return Unsafe.BitCast<Vector<float>, TOther>(Vector.ConvertToSingle(Unsafe.BitCast<Vector<TNumber>, Vector<int>>(value)));
        }

        if (typeof(TNumber) == typeof(uint) && typeof(TOtherNumber) == typeof(float))
        {
            return Unsafe.BitCast<Vector<float>, TOther>(Vector.ConvertToSingle(Unsafe.BitCast<Vector<TNumber>, Vector<uint>>(value)));
        }

        if (typeof(TNumber) == typeof(long) && typeof(TOtherNumber) == typeof(double))
        {
            return Unsafe.BitCast<Vector<double>, TOther>(Vector.ConvertToDouble(Unsafe.BitCast<Vector<TNumber>, Vector<long>>(value)));
        }

        if (typeof(TNumber) == typeof(ulong) && typeof(TOtherNumber) == typeof(double))
        {
            return Unsafe.BitCast<Vector<double>, TOther>(Vector.ConvertToDouble(Unsafe.BitCast<Vector<TNumber>, Vector<ulong>>(value)));
        }

        var casted = stackalloc TOtherNumber[LaneWidth];
        for (var i = 0; (i < LaneWidth) && (i < TOther.LaneWidth); i++)
        {
            casted[i] = TOtherNumber.CreateTruncating(value[i]);
        }

        return TOther.Load(casted);
    }
}

