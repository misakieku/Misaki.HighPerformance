using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

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
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<float>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToInt32(vFrom);
            return Unsafe.As<Vector<int>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(float) && typeof(TOtherNumber) == typeof(uint))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<float>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToUInt32(vFrom);
            return Unsafe.As<Vector<uint>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(double) && typeof(TOtherNumber) == typeof(long))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<double>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToInt64(vFrom);
            return Unsafe.As<Vector<long>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(double) && typeof(TOtherNumber) == typeof(ulong))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<double>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToUInt64(vFrom);
            return Unsafe.As<Vector<ulong>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(int) && typeof(TOtherNumber) == typeof(float))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<int>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToSingle(vFrom);
            return Unsafe.As<Vector<float>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(uint) && typeof(TOtherNumber) == typeof(float))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<uint>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToSingle(vFrom);
            return Unsafe.As<Vector<float>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(long) && typeof(TOtherNumber) == typeof(double))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<long>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToDouble(vFrom);
            return Unsafe.As<Vector<double>, TOther>(ref vTo);
        }

        if (typeof(TNumber) == typeof(ulong) && typeof(TOtherNumber) == typeof(double))
        {
            ref var vFrom = ref Unsafe.As<Vector<TNumber>, Vector<ulong>>(ref Unsafe.AsRef(in value));
            var vTo = Vector.ConvertToDouble(vFrom);
            return Unsafe.As<Vector<double>, TOther>(ref vTo);
        }

        var casted = stackalloc TOtherNumber[LaneWidth];
        for (var i = 0; (i < LaneWidth) && (i < TOther.LaneWidth); i++)
        {
            casted[i] = TOtherNumber.CreateTruncating(value[i]);
        }

        return TOther.Load(casted);
    }
}

