using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct ScalarLane<TNumber> : ISPMDLane<ScalarLane<TNumber>, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    public readonly TNumber value;

    public static int LaneWidth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1;
    }

    public static ScalarLane<TNumber> Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ScalarLane<TNumber>(TNumber.Zero);
    }

    public static ScalarLane<TNumber> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ScalarLane<TNumber>(TNumber.One);
    }

    public static ScalarLane<TNumber> MinValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ScalarLane<TNumber>(TNumber.MinValue);
    }

    public static ScalarLane<TNumber> MaxValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ScalarLane<TNumber>(TNumber.MaxValue);
    }

    public readonly TNumber this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => value;
    }

    public ScalarLane(TNumber value)
    {
        this.value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Create(TNumber value)
    {
        return new ScalarLane<TNumber>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Create(params ReadOnlySpan<TNumber> values)
    {
        return new ScalarLane<TNumber>(values[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Create(Vector<TNumber> value)
    {
        return new ScalarLane<TNumber>(value[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Sequence(TNumber start, TNumber step)
    {
        return new ScalarLane<TNumber>(start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Load(ref TNumber value)
    {
        return new ScalarLane<TNumber>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Load(TNumber* pValue)
    {
        return new ScalarLane<TNumber>(*pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> MaskLoad(ScalarLane<TNumber> mask, ref TNumber value)
    {
        return new ScalarLane<TNumber>(mask.value != TNumber.Zero ? value : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> MaskLoad(ScalarLane<TNumber> mask, TNumber* pValue)
    {
        return new ScalarLane<TNumber>(mask.value != TNumber.Zero ? *pValue : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Gather(TNumber* pData, ScalarLane<TNumber> indices, int scale)
    {
        return new ScalarLane<TNumber>(pData[int.CreateTruncating(indices.value) * scale / sizeof(TNumber)]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Gather(TNumber* pData, int* pIndices, int scale)
    {
        return new ScalarLane<TNumber>(pData[pIndices[0] * scale / sizeof(TNumber)]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Gather(ref TNumber baseAddress, ScalarLane<TNumber> indices, int scale)
    {
        return new ScalarLane<TNumber>(Unsafe.Add(ref baseAddress, int.CreateTruncating(indices.value) * scale / sizeof(TNumber)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Gather(ref TNumber baseAddress, ref int baseIndex, int scale)
    {
        return new ScalarLane<TNumber>(Unsafe.Add(ref baseAddress, int.CreateTruncating(baseIndex) * scale / sizeof(TNumber)));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(ref TNumber destination)
    {
        destination = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(TNumber* pDestination)
    {
        *pDestination = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(ScalarLane<TNumber> mask, ref TNumber destination)
    {
        return CompressStore(mask, (TNumber*)Unsafe.AsPointer(in destination));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(ScalarLane<TNumber> mask, TNumber* pDestination)
    {
        if (mask.value != TNumber.Zero)
        {
            *pDestination = value;
            return 1;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector<TNumber> AsVector()
    {
        return Vector.Create(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly TNumber* GetUnsafePtr()
    {
        return (TNumber*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOther Cast<TOther, TOtherNumber>()
        where TOther : ISPMDLane<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>
    {
        return TOther.Create(TOtherNumber.CreateChecked(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOther BitCast<TOther, TOtherNumber>()
        where TOther : ISPMDLane<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>
    {
        return Unsafe.BitCast<ScalarLane<TNumber>, TOther>(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator +(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value + b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator -(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value - b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator *(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value * b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator /(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value / b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator %(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value % b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator -(ScalarLane<TNumber> a)
    {
        return new ScalarLane<TNumber>(-a.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator &(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value & b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator |(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value | b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator ^(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value ^ b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator ~(ScalarLane<TNumber> a)
    {
        return new ScalarLane<TNumber>(~a.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator ==(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return Equal(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator !=(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return ~Equal(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator >(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return GreaterThan(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator >=(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return GreaterThanOrEqual(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator <(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return LessThan(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> operator <=(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return LessThanOrEqual(a, b);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ScalarLane<TNumber>(TNumber value)
    {
        return new ScalarLane<TNumber>(value);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Abs(ScalarLane<TNumber> value)
    {
        return new ScalarLane<TNumber>(TNumber.Abs(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Floor(ScalarLane<TNumber> value)
    {
        // Note: INumber<TLane> does not provide Floor method, so we need to handle float and double specifically.
        // This is acceptable for performance because JIT generates specialized code for each TLane as long as they are struct.
        // Which mean for ScalarLane<float>, typeof(TLane) == typeof(float) is always true and jit will optimize away the other branches.
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Floor(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Floor(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Frac(ScalarLane<TNumber> value)
    {
        return new ScalarLane<TNumber>(value.value - TNumber.CreateTruncating(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Sqrt(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Sqrt(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Sqrt(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Lerp(ScalarLane<TNumber> a, ScalarLane<TNumber> b, ScalarLane<TNumber> t)
    {
        return new ScalarLane<TNumber>(a.value + (b.value - a.value) * t.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> MultipleAdd(ScalarLane<TNumber> a, ScalarLane<TNumber> b, ScalarLane<TNumber> c)
    {
        return new ScalarLane<TNumber>(TNumber.MultiplyAddEstimate(a.value, b.value, c.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Min(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(TNumber.Min(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Max(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(TNumber.Max(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Clamp(ScalarLane<TNumber> value, ScalarLane<TNumber> min, ScalarLane<TNumber> max)
    {
        return new ScalarLane<TNumber>(TNumber.Clamp(value.value, min.value, max.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Saturate(ScalarLane<TNumber> value)
    {
        return Clamp(value, new ScalarLane<TNumber>(TNumber.Zero), new ScalarLane<TNumber>(TNumber.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Sin(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Sin(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Sin(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Cos(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Cos(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Cos(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (ScalarLane<TNumber> sin, ScalarLane<TNumber> cos) SinCos(ScalarLane<TNumber> value)
    {
        return (Sin(value), Cos(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Tan(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Tan(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Tan(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Asin(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Asin(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Asin(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Acos(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Acos(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Acos(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Atan(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Atan(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Atan(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Atan2(ScalarLane<TNumber> y, ScalarLane<TNumber> x)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var fy = Unsafe.As<ScalarLane<TNumber>, float>(ref y);
            var fx = Unsafe.As<ScalarLane<TNumber>, float>(ref x);
            var result = MathF.Atan2(fy, fx);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var dy = Unsafe.As<ScalarLane<TNumber>, double>(ref y);
            var dx = Unsafe.As<ScalarLane<TNumber>, double>(ref x);
            var result = Math.Atan2(dy, dx);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Pow(ScalarLane<TNumber> x, ScalarLane<TNumber> y)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var fx = Unsafe.As<ScalarLane<TNumber>, float>(ref x);
            var fy = Unsafe.As<ScalarLane<TNumber>, float>(ref y);
            var result = MathF.Pow(fx, fy);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var dx = Unsafe.As<ScalarLane<TNumber>, double>(ref x);
            var dy = Unsafe.As<ScalarLane<TNumber>, double>(ref y);
            var result = Math.Pow(dx, dy);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Exp(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Exp(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Log(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Exp2(ScalarLane<TNumber> value)
    {
        return Pow(new ScalarLane<TNumber>(TNumber.CreateTruncating(2)), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Log(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Log(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Log(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Log2(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Log2(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Log2(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Ceil(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Ceiling(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Ceiling(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(decimal))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, decimal>(ref value);
            var result = Math.Ceiling(d);
            return Unsafe.As<decimal, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Round(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Round(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Round(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(decimal))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, decimal>(ref value);
            var result = Math.Round(d);
            return Unsafe.As<decimal, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Trunc(ScalarLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<TNumber>, float>(ref value);
            var result = MathF.Truncate(f);
            return Unsafe.As<float, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, double>(ref value);
            var result = Math.Truncate(d);
            return Unsafe.As<double, ScalarLane<TNumber>>(ref result);
        }
        else if (typeof(TNumber) == typeof(decimal))
        {
            var d = Unsafe.As<ScalarLane<TNumber>, decimal>(ref value);
            var result = Math.Truncate(d);
            return Unsafe.As<decimal, ScalarLane<TNumber>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Sign(ScalarLane<TNumber> value)
    {
        return new ScalarLane<TNumber>((value.value > TNumber.Zero) ? TNumber.One : (value.value < TNumber.Zero) ? ~TNumber.Zero : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> CopySign(ScalarLane<TNumber> magnitude, ScalarLane<TNumber> sign)
    {
        return new ScalarLane<TNumber>(TNumber.CopySign(magnitude.value, sign.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Rcp(ScalarLane<TNumber> value)
    {
        return new ScalarLane<TNumber>(TNumber.One / value.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Rsqrt(ScalarLane<TNumber> value)
    {
        return Sqrt(Rcp(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Select(ScalarLane<TNumber> conditionMask, ScalarLane<TNumber> ifTrue, ScalarLane<TNumber> ifFalse)
    {
        return new ScalarLane<TNumber>(conditionMask.value != TNumber.Zero ? ifTrue.value : ifFalse.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> GreaterThan(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value > b.value ? ~TNumber.Zero : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> GreaterThanOrEqual(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value >= b.value ? ~TNumber.Zero : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> LessThan(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value < b.value ? ~TNumber.Zero : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> LessThanOrEqual(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value <= b.value ? ~TNumber.Zero : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<TNumber> Equal(ScalarLane<TNumber> a, ScalarLane<TNumber> b)
    {
        return new ScalarLane<TNumber>(a.value == b.value ? ~TNumber.Zero : TNumber.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(ScalarLane<TNumber> mask)
    {
        return mask.value != TNumber.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All(ScalarLane<TNumber> mask)
    {
        return mask.value != TNumber.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool None(ScalarLane<TNumber> mask)
    {
        return mask.value == TNumber.Zero;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ScalarLane<TNumber> other)
    {
        return value.Equals(other.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is ScalarLane<TNumber> other && value.Equals(other.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return value.ToString() ?? string.Empty;
    }
}
