using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public static unsafe class WideLane
{
    internal static readonly uint* s_shuffleTable512_32bit;
    internal static readonly ulong* s_shuffleTable512_64bit;
    internal static readonly uint* s_shuffleTable256_32bit;
    internal static readonly ulong* s_shuffleTable256_64bit;
    internal static readonly uint* s_shuffleTable128_32bit;
    internal static readonly ulong* s_shuffleTable128_64bit;

    /// <summary>
    /// Gets whether WideLane is supported on the current hardware.
    /// </summary>
    public static bool IsSupported => Vector.IsHardwareAccelerated;

    static WideLane()
    {
        s_shuffleTable512_32bit = ShuffleTableGenerator.ComputeShuffleTable512_32Bit();
        s_shuffleTable512_64bit = ShuffleTableGenerator.ComputeShuffleTable512_64Bit();
        s_shuffleTable256_32bit = ShuffleTableGenerator.ComputeShuffleTable256_32Bit();
        s_shuffleTable256_64bit = ShuffleTableGenerator.ComputeShuffleTable256_64Bit();
        s_shuffleTable128_32bit = ShuffleTableGenerator.ComputeShuffleTable128_32Bit();
        s_shuffleTable128_64bit = ShuffleTableGenerator.ComputeShuffleTable128_64Bit();
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe partial struct WideLane<TNumber> : ISPMD<WideLane<TNumber>, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    private static readonly Vector<TNumber> s_indices;

    public readonly Vector<TNumber> value;

    public static int LaneWidth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Vector<TNumber>.Count;
    }

    public static WideLane<TNumber> Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Vector<TNumber>.Zero);
    }

    public static WideLane<TNumber> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Vector<TNumber>.One);
    }

    public static WideLane<TNumber> MinValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Create(TNumber.MinValue);
    }

    public static WideLane<TNumber> MaxValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Create(TNumber.MaxValue);
    }

    public readonly TNumber this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => value[index];
    }

    static WideLane()
    {
        var pValues = stackalloc TNumber[LaneWidth];
        for (var i = 0; i < LaneWidth; i++)
        {
            pValues[i] = TNumber.CreateTruncating(i);
        }

        s_indices = Vector.Load(pValues);
    }

    public WideLane(Vector<TNumber> value)
    {
        this.value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<TNumber> VectorFloor(Vector<TNumber> vector)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<Vector<TNumber>, Vector<float>>(ref vector);
            var floored = Vector.Floor(v);
            return Unsafe.As<Vector<float>, Vector<TNumber>>(ref floored);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<Vector<TNumber>, Vector<double>>(ref vector);
            var floored = Vector.Floor(v);
            return Unsafe.As<Vector<double>, Vector<TNumber>>(ref floored);
        }

        return vector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<TNumber> VectorTruncate(Vector<TNumber> vector)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<Vector<TNumber>, Vector<float>>(ref vector);
            var truncated = Vector.Truncate(v);
            return Unsafe.As<Vector<float>, Vector<TNumber>>(ref truncated);
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<Vector<TNumber>, Vector<double>>(ref vector);
            var truncated = Vector.Truncate(v);
            return Unsafe.As<Vector<double>, Vector<TNumber>>(ref truncated);
        }

        return vector;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Create(TNumber value)
    {
        return new(Vector.Create(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Create(params ReadOnlySpan<TNumber> values)
    {
        return new(Vector.Create(values));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Create(Vector<TNumber> value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sequence(TNumber start, TNumber step)
    {
        return new(Vector.Create(start) + (Vector.Create(step) * s_indices));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Load(ref TNumber value)
    {
        return new(Vector.LoadUnsafe(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Load(TNumber* pValue)
    {
        return new(Vector.Load(pValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(ref TNumber destination)
    {
        value.StoreUnsafe(ref destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(TNumber* pDestination)
    {
        value.Store(pDestination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(WideLane<TNumber> mask, ref TNumber destination)
    {
        return CompressStore(mask, (TNumber*)Unsafe.AsPointer(in destination));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(WideLane<TNumber> mask, TNumber* pDestination)
    {
        var size = sizeof(TNumber);

        if (LaneWidth == Vector512<TNumber>.Count && Vector512.IsHardwareAccelerated)
        {
            if (size == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector512<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector512<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 16) because each control vector has 16 elements
                var shuffle = Vector512.Load(WideLane.s_shuffleTable512_32bit + (moveMask * 16));
                var compressed = Vector512.Shuffle(vec, shuffle);

                compressed.Store((uint*)pDestination);
                return BitOperations.PopCount(moveMask);
            }

            if (size == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector512<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector512<ulong>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 8) because each control vector has 8 elements
                var shuffle = Vector512.Load(WideLane.s_shuffleTable512_64bit + (moveMask * 8));
                var compressed = Vector512.Shuffle(vec, shuffle);

                compressed.Store((ulong*)pDestination);
                return BitOperations.PopCount(moveMask);
            }
        }
        else if (LaneWidth == Vector256<TNumber>.Count && Vector256.IsHardwareAccelerated)
        {
            if (size == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector256<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector256<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 8) because each control vector has 8 elements
                var shuffle = Vector256.Load(WideLane.s_shuffleTable256_32bit + (moveMask * 8));
                var compressed = Vector256.Shuffle(vec, shuffle);

                compressed.Store((uint*)pDestination);
                return BitOperations.PopCount(moveMask);
            }

            if (size == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector256<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector256<ulong>>(ref mask);

                // For 64-bit, ExtractMostSignificantBits only populates 4 bits (0-15)
                var moveMask = m.ExtractMostSignificantBits();

                // Offset is (moveMask * 4) because each control vector has 4 elements
                var shuffle = Vector256.Load(WideLane.s_shuffleTable256_64bit + (moveMask * 4));
                var compressed = Vector256.Shuffle(vec, shuffle);

                compressed.Store((ulong*)pDestination);
                return BitOperations.PopCount(moveMask);
            }
        }
        else if (LaneWidth == Vector128<TNumber>.Count && Vector128.IsHardwareAccelerated)
        {
            if (size == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector128<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector128<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 4) because each control vector has 4 elements
                var shuffle = Vector128.Load(WideLane.s_shuffleTable128_32bit + (moveMask * 4));
                var compressed = Vector128.Shuffle(vec, shuffle);

                compressed.Store((uint*)pDestination);
                return BitOperations.PopCount(moveMask);
            }

            if (size == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector128<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector128<ulong>>(ref mask);
                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 2) because each control vector has 2 elements
                var shuffle = Vector128.Load(WideLane.s_shuffleTable128_64bit + (moveMask * 2));
                var compressed = Vector128.Shuffle(vec, shuffle);
                compressed.Store((ulong*)pDestination);
                return BitOperations.PopCount(moveMask);
            }
        }

        // This is slow, but correct on ANY hardware.
        // Check sign bit of the mask lane
        var count = 0;
        for (var i = 0; i < LaneWidth; i++)
        {
            if (mask.value[i] == ~TNumber.Zero)
            {
                pDestination[count++] = value[i];
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector<TNumber> AsVector()
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOther BitCast<TOther, TOtherNumber>()
        where TOther : ISPMD<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>
    {
        return Unsafe.BitCast<WideLane<TNumber>, TOther>(this);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator +(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value + b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator -(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value - b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator *(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value * b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator /(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value / b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator %(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value - VectorFloor(a.value / b.value) * b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator -(WideLane<TNumber> a)
    {
        return new(-a.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator &(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value & b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator |(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value | b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator ^(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(a.value ^ b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator ~(WideLane<TNumber> a)
    {
        return new(~a.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator ==(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return Equal(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator !=(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return ~Equal(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator >(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return GreaterThan(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator >=(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return GreaterThanOrEqual(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator <(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return LessThan(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator <=(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return LessThanOrEqual(a, b);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator WideLane<TNumber>(TNumber value)
    {
        return Create(value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Abs(WideLane<TNumber> value)
    {
        return new(Vector.Abs(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Floor(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var floored = Vector.Floor(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref floored));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var floored = Vector.Floor(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref floored));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Frac(WideLane<TNumber> value)
    {
        return new(value.value - VectorFloor(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sqrt(WideLane<TNumber> value)
    {
        return new(Vector.SquareRoot(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Lerp(WideLane<TNumber> a, WideLane<TNumber> b, WideLane<TNumber> t)
    {
        return new(a.value + (b.value - a.value) * t.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> MultipleAdd(WideLane<TNumber> a, WideLane<TNumber> b, WideLane<TNumber> c)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var va = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref a);
            ref var vb = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref b);
            ref var vc = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref c);
            var result = Vector.FusedMultiplyAdd(va, vb, vc);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var va = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref a);
            ref var vb = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref b);
            ref var vc = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref c);
            var result = Vector.FusedMultiplyAdd(va, vb, vc);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }
        else
        {
            return new((a.value * b.value) + c.value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Min(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.Min(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Max(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.Max(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Clamp(WideLane<TNumber> value, WideLane<TNumber> min, WideLane<TNumber> max)
    {
        return new(Vector.Clamp(value.value, min.value, max.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Saturate(WideLane<TNumber> value)
    {
        return Clamp(value, Create(TNumber.Zero), Create(TNumber.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sin(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Sin(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Sin(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Cos(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Cos(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Cos(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (WideLane<TNumber> sin, WideLane<TNumber> cos) SinCos(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var (sin, cos) = Vector.SinCos(v);
            return (new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref sin)), new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref cos)));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var (sin, cos) = Vector.SinCos(v);
            return (new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref sin)), new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref cos)));
        }

        return (value, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Tan(WideLane<TNumber> value)
    {
        // 1. Range Reduction
        // Transform value into range [-pi/4, pi/4]. 
        // This is complex to do right (Payne-Hanek), but for games
        // a simple approximation: value = value - (PI * Round(value / PI)) is good enough.

        var pi = Create(TNumber.CreateTruncating(Math.PI));
        var x = value - pi * Round(value / pi);

        // 2. The Approximation (Remez Polynomial)
        // tan(value) ~= value + c1*value^3 + c2*value^5
        // Factored (Horner's Method) for fewer ops: value * (1 + value^2 * (c1 + c2*value^2))

        var x2 = x * x;
        var vc1 = Create(TNumber.CreateTruncating(0.3333314036)); // 1/3
        var vc2 = Create(TNumber.CreateTruncating(0.1333923995)); // 2/15

        // x2 * (c1 + c2 * x2)
        var poly = MultipleAdd(x2, vc2, vc1);
        // value * (1 + x2 * poly)
        return MultipleAdd(x, MultipleAdd(x2, poly, One), Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Asin(WideLane<TNumber> value)
    {
        // asin(value) = pi/2 - acos(value)

        var piOver2 = Create(TNumber.CreateTruncating(Math.PI / 2));
        return piOver2 - Acos(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Acos(WideLane<TNumber> value)
    {
        // 0 <= value <= 1 : acos(value) = sqrt(1 - value) * (c0 + c1*value + c2*value^2 + c3*value^3)
        // value < 0 : acos(value) = pi - acos(-value)

        var x = Abs(value);

        var c0 = Create(TNumber.CreateTruncating(1.5707288f)); // pi/2
        var c1 = Create(TNumber.CreateTruncating(-0.2121144f));
        var c2 = Create(TNumber.CreateTruncating(0.0742610f));
        var c3 = Create(TNumber.CreateTruncating(-0.0187293f));

        var term1 = MultipleAdd(x, c3, c2);
        var term2 = MultipleAdd(x, term1, c1);
        var poly = MultipleAdd(x, term2, c0);

        var sqrtTerm = Sqrt(One - x);
        var result = poly * sqrtTerm;

        var pi = Create(TNumber.CreateTruncating(Math.PI));
        var isNegative = LessThan(value, Zero);

        return Select(isNegative, pi - result, result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Atan(WideLane<TNumber> value)
    {
        // atan(value) = value * (c1 + c2*value^2)

        var c1 = Create(TNumber.CreateTruncating(0.97239411f));
        var c2 = Create(TNumber.CreateTruncating(-0.19194795f));

        var x2 = value * value;
        var poly = MultipleAdd(x2, c2, c1);
        return value * poly;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Atan2(WideLane<TNumber> y, WideLane<TNumber> x)
    {
        var absX = Abs(x);
        var absY = Abs(y);

        // 1. Determine the ratio (input to Atan)
        // If |value| > |y|, we are in the "shallow" region, ratio = y/value
        // If |y| > |value|, we are in the "steep" region, ratio = value/y (and we transform result)
        var yGtX = GreaterThan(absY, absX);

        // Select numerator and denominator to ensure ratio is always in [-1, 1]
        var num = Select(yGtX, absX, absY);
        var den = Select(yGtX, absY, absX);

        var t = num / den; // t is now in [0, 1]
        var t2 = t * t;

        // 2. Polynomial Approximation (Odd function: value * (c1 + c2*value^2))
        var c1 = Create(TNumber.CreateTruncating(0.97239411f));
        var c2 = Create(TNumber.CreateTruncating(-0.19194795f));

        // (c1 + c2 * t2)
        var poly = MultipleAdd(c2, t2, c1);

        // result = t * poly
        var result = t * poly;

        // 3. Reconstruct the angle
        // If we swapped value/y (yGtX), the identity is: atan(value/y) = PI/2 - atan(y/value)
        var halfPi = Create(TNumber.CreateTruncating(1.570796327f));
        result = Select(yGtX, halfPi - result, result);

        // 4. Adjust for Quadrants (Signs)
        // If value < 0, we are in quadrants 2 or 3, so we need to add PI
        var pi = Create(TNumber.CreateTruncating(3.141592654f));
        var xLtZero = LessThan(x, Zero);
        result = Select(xLtZero, pi - result, result);

        // If y < 0, the result should be negative (standard atan2 convention)
        // NOTE: This sign flip strategy depends on exact polynomial range mapping, 
        // but typically just copy the sign of Y to the result.
        var yLtZero = LessThan(y, Zero);
        // If original Y was negative, negate the result
        // (This works because our ratio logic effectively computed atan(|y|/|value|) above)
        var negativeResult = -result;
        return Select(yLtZero, negativeResult, result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Pow(WideLane<TNumber> x, WideLane<TNumber> y)
    {
        return Exp(y * Log(x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Exp(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Exp(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Exp(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Exp2(WideLane<TNumber> value)
    {
        return Pow(Create(TNumber.CreateTruncating(2)), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Log(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Log(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Log(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Log2(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Log2(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Log2(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Ceil(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Ceiling(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Ceiling(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Round(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Round(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Round(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Trunc(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var result = Vector.Truncate(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref result));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var result = Vector.Truncate(v);
            return new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sign(WideLane<TNumber> value)
    {
        return Select(
            GreaterThan(value, Zero),
            One,
            Select(
                LessThan(value, Zero),
                ~Zero,
                Zero));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> CopySign(WideLane<TNumber> magnitude, WideLane<TNumber> sign)
    {
        return new(Vector.CopySign(magnitude.value, sign.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Rcp(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            if (Sse.IsSupported && LaneWidth == Vector128<float>.Count)
            {
                var vf = Unsafe.As<WideLane<TNumber>, Vector128<float>>(ref value);
                var result = Sse.Reciprocal(vf);
                return Unsafe.As<Vector128<float>, WideLane<TNumber>>(ref result);
            }
            else if (Avx.IsSupported && LaneWidth == Vector256<float>.Count)
            {
                var vf = Unsafe.As<WideLane<TNumber>, Vector256<float>>(ref value);
                var result = Avx.Reciprocal(vf);
                return Unsafe.As<Vector256<float>, WideLane<TNumber>>(ref result);
            }
        }

        return Create(TNumber.One) / value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Rsqrt(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            if (Sse.IsSupported && LaneWidth == Vector128<float>.Count)
            {
                var vf = Unsafe.As<WideLane<TNumber>, Vector128<float>>(ref value);
                var result = Sse.ReciprocalSqrt(vf);
                return Unsafe.As<Vector128<float>, WideLane<TNumber>>(ref result);
            }
            else if (Avx.IsSupported && LaneWidth == Vector256<float>.Count)
            {
                var vf = Unsafe.As<WideLane<TNumber>, Vector256<float>>(ref value);
                var result = Avx.ReciprocalSqrt(vf);
                return Unsafe.As<Vector256<float>, WideLane<TNumber>>(ref result);
            }
        }

        return Create(TNumber.One) / Sqrt(value);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Select(WideLane<TNumber> conditionMask, WideLane<TNumber> ifTrue, WideLane<TNumber> ifFalse)
    {
        return new(Vector.ConditionalSelect(
                conditionMask.value,
                ifTrue.value,
                ifFalse.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> GreaterThan(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.GreaterThan(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> GreaterThanOrEqual(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.GreaterThanOrEqual(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> LessThan(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.LessThan(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> LessThanOrEqual(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.LessThanOrEqual(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Equal(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new(Vector.Equals(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(WideLane<TNumber> mask)
    {
        return !Vector.EqualsAll(mask.value, Vector<TNumber>.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All(WideLane<TNumber> mask)
    {
        return Vector.EqualsAll(mask.value, Vector<TNumber>.AllBitsSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool None(WideLane<TNumber> mask)
    {
        return Vector.EqualsAll(mask.value, Vector<TNumber>.Zero);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(WideLane<TNumber> other)
    {
        return value.Equals(other.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is WideLane<TNumber> other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return value.ToString();
    }
}
