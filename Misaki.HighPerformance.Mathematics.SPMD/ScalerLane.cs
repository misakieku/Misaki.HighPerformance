using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct ScalarLane<T> : ISPMD<ScalarLane<T>, T>
    where T : unmanaged, INumber<T>, IMinMaxValue<T>, IBitwiseOperators<T, T, T>
{
    public readonly T value;

    public static int LaneWidth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1;
    }

    public static ScalarLane<T> Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(T.Zero);
    }

    public static ScalarLane<T> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(T.One);
    }

    public static ScalarLane<T> MinValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(T.MinValue);
    }

    public static ScalarLane<T> MaxValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(T.MaxValue);
    }

    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => value;
    }

    public ScalarLane(T value)
    {
        this.value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Create(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Create(params ReadOnlySpan<T> values) => new(values[0]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Create(Vector<T> value) => new(value[0]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Sequence(T start, T step) => new(start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Load(ref T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Load(T* pValue) => new(*pValue);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(ref T destination) => destination = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(T* pDestination) => *pDestination = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(ScalarLane<T> mask, ref T destination)
    {
        return CompressStore(mask, (T*)Unsafe.AsPointer(in destination));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(ScalarLane<T> mask, T* pDestination)
    {
        if (mask.value != T.Zero)
        {
            *pDestination = value;
            return 1;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector<T> AsVector() => Vector.Create(value);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator +(ScalarLane<T> a, ScalarLane<T> b) => new(a.value + b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator +(ScalarLane<T> a, T b) => new(a.value + b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator -(ScalarLane<T> a, ScalarLane<T> b) => new(a.value - b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator -(ScalarLane<T> a, T b) => new(a.value - b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator *(ScalarLane<T> a, ScalarLane<T> b) => new(a.value * b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator *(ScalarLane<T> a, T b) => new(a.value * b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator /(ScalarLane<T> a, ScalarLane<T> b) => new(a.value / b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator /(ScalarLane<T> a, T b) => new(a.value / b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator %(ScalarLane<T> a, ScalarLane<T> b) => new(a.value % b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator %(ScalarLane<T> a, T b) => new(a.value % b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator -(ScalarLane<T> a) => new(-a.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator &(ScalarLane<T> a, ScalarLane<T> b) => new(a.value & b.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator &(ScalarLane<T> a, T b) => new(a.value & b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator |(ScalarLane<T> a, ScalarLane<T> b) => new(a.value | b.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator |(ScalarLane<T> a, T b) => new(a.value | b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator ^(ScalarLane<T> a, ScalarLane<T> b) => new(a.value ^ b.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator ^(ScalarLane<T> a, T b) => new(a.value ^ b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> operator ~(ScalarLane<T> a) => new(~a.value);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Abs(ScalarLane<T> value) => new(T.Abs(value.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Floor(ScalarLane<T> value)
    {
        // Note: INumber<T> does not provide Floor method, so we need to handle float and double specifically.
        // This is acceptable for performance because JIT generates specialized code for each T as long as they are struct.
        // Which mean for ScalarLane<float>, typeof(T) == typeof(float) is always true and jit will optimize away the other branches.
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Floor(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Floor(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Frac(ScalarLane<T> value) => new(value.value - T.CreateTruncating(value.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Sqrt(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Sqrt(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Sqrt(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Lerp(ScalarLane<T> a, ScalarLane<T> b, ScalarLane<T> t) => new(a.value + (b.value - a.value) * t.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> MultipleAdd(ScalarLane<T> a, ScalarLane<T> b, ScalarLane<T> c) => new(T.MultiplyAddEstimate(a.value, b.value, c.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Min(ScalarLane<T> a, ScalarLane<T> b) => new(T.Min(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Max(ScalarLane<T> a, ScalarLane<T> b) => new(T.Max(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Clamp(ScalarLane<T> value, ScalarLane<T> min, ScalarLane<T> max) => new(T.Clamp(value.value, min.value, max.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Saturate(ScalarLane<T> value) => Clamp(value, new(T.Zero), new(T.One));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Sin(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Sin(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Sin(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Cos(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Cos(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Cos(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (ScalarLane<T> sin, ScalarLane<T> cos) SinCos(ScalarLane<T> value) => (Sin(value), Cos(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Tan(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Tan(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Tan(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Asin(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Asin(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Asin(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Acos(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Acos(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Acos(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Atan(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Atan(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Atan(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Atan2(ScalarLane<T> y, ScalarLane<T> x)
    {
        if (typeof(T) == typeof(float))
        {
            var fy = Unsafe.As<ScalarLane<T>, float>(ref y);
            var fx = Unsafe.As<ScalarLane<T>, float>(ref x);
            var result = MathF.Atan2(fy, fx);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var dy = Unsafe.As<ScalarLane<T>, double>(ref y);
            var dx = Unsafe.As<ScalarLane<T>, double>(ref x);
            var result = Math.Atan2(dy, dx);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Pow(ScalarLane<T> x, ScalarLane<T> y)
    {
        if (typeof(T) == typeof(float))
        {
            var fx = Unsafe.As<ScalarLane<T>, float>(ref x);
            var fy = Unsafe.As<ScalarLane<T>, float>(ref y);
            var result = MathF.Pow(fx, fy);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var dx = Unsafe.As<ScalarLane<T>, double>(ref x);
            var dy = Unsafe.As<ScalarLane<T>, double>(ref y);
            var result = Math.Pow(dx, dy);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Exp(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Exp(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Log(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Exp2(ScalarLane<T> value)
    {
        return Pow(new ScalarLane<T>(T.CreateChecked(2)), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Log(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Log(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Log(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Log2(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Log2(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Log2(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Ceil(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Ceiling(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Ceiling(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(decimal))
        {
            var d = Unsafe.As<ScalarLane<T>, decimal>(ref value);
            var result = Math.Ceiling(d);
            return Unsafe.As<decimal, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Round(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Round(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Round(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(decimal))
        {
            var d = Unsafe.As<ScalarLane<T>, decimal>(ref value);
            var result = Math.Round(d);
            return Unsafe.As<decimal, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Trunc(ScalarLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            var f = Unsafe.As<ScalarLane<T>, float>(ref value);
            var result = MathF.Truncate(f);
            return Unsafe.As<float, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            var d = Unsafe.As<ScalarLane<T>, double>(ref value);
            var result = Math.Truncate(d);
            return Unsafe.As<double, ScalarLane<T>>(ref result);
        }
        else if (typeof(T) == typeof(decimal))
        {
            var d = Unsafe.As<ScalarLane<T>, decimal>(ref value);
            var result = Math.Truncate(d);
            return Unsafe.As<decimal, ScalarLane<T>>(ref result);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Sign(ScalarLane<T> value) => new((value.value > T.Zero) ? T.One : (value.value < T.Zero) ? ~T.Zero : T.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> CopySign(ScalarLane<T> magnitude, ScalarLane<T> sign) => new(T.CopySign(magnitude.value, sign.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Rcp(ScalarLane<T> value) => new(T.One / value.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Rsqrt(ScalarLane<T> value) => Sqrt(Rcp(value));



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Select(ScalarLane<T> conditionMask, ScalarLane<T> ifTrue, ScalarLane<T> ifFalse) => new(conditionMask.value != T.Zero ? ifTrue.value : ifFalse.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> GreaterThan(ScalarLane<T> a, ScalarLane<T> b) => new(a.value > b.value ? ~T.Zero : T.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> GreaterThanOrEqual(ScalarLane<T> a, ScalarLane<T> b) => new(a.value >= b.value ? ~T.Zero : T.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> LessThan(ScalarLane<T> a, ScalarLane<T> b) => new(a.value < b.value ? ~T.Zero : T.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> LessThanOrEqual(ScalarLane<T> a, ScalarLane<T> b) => new(a.value <= b.value ? ~T.Zero : T.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScalarLane<T> Equal(ScalarLane<T> a, ScalarLane<T> b) => new(a.value == b.value ? ~T.Zero : T.Zero);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(ScalarLane<T> mask) => mask.value != T.Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All(ScalarLane<T> mask) => mask.value != T.Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool None(ScalarLane<T> mask) => mask.value == T.Zero;

    public override string ToString()
    {
        return value.ToString() ?? string.Empty;
    }
}
