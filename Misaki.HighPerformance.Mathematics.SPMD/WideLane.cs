using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Misaki.HighPerformance.Mathematics.SPMD;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct WideLane<T> : ISPMD<WideLane<T>, T>
    where T : unmanaged, INumber<T>, IMinMaxValue<T>, IBitwiseOperators<T, T, T>
{
    private static readonly Vector<T> s_indices;

    private static readonly uint* s_shuffleTable512_32bit;
    private static readonly ulong* s_shuffleTable512_64bit;
    private static readonly uint* s_shuffleTable256_32bit;
    private static readonly ulong* s_shuffleTable256_64bit;
    private static readonly uint* s_shuffleTable128_32bit;
    private static readonly ulong* s_shuffleTable128_64bit;

    public readonly Vector<T> value;

    public static int LaneWidth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Vector<T>.Count;
    }

    public static WideLane<T> Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Vector<T>.Zero);
    }

    public static WideLane<T> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Vector<T>.One);
    }

    public static WideLane<T> MinValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Create(T.MinValue);
    }

    public static WideLane<T> MaxValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Create(T.MaxValue);
    }

    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => value[index];
    }

    static WideLane()
    {
        var pValues = stackalloc T[LaneWidth];
        for (var i = 0; i < LaneWidth; i++)
        {
            pValues[i] = T.CreateChecked(i);
        }

        s_indices = Vector.Load(pValues);

        s_shuffleTable512_32bit = ShuffleTableGenerator.ComputeShuffleTable512_32Bit();
        s_shuffleTable512_64bit = ShuffleTableGenerator.ComputeShuffleTable512_64Bit();
        s_shuffleTable256_32bit = ShuffleTableGenerator.ComputeShuffleTable256_32Bit();
        s_shuffleTable256_64bit = ShuffleTableGenerator.ComputeShuffleTable256_64Bit();
        s_shuffleTable128_32bit = ShuffleTableGenerator.ComputeShuffleTable128_32Bit();
        s_shuffleTable128_64bit = ShuffleTableGenerator.ComputeShuffleTable128_64Bit();
    }

    public WideLane(Vector<T> value)
    {
        this.value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<T> VectorFloor(Vector<T> vector)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<Vector<T>, Vector<float>>(ref vector);
            var floored = Vector.Floor(v);
            return Unsafe.As<Vector<float>, Vector<T>>(ref floored);
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<Vector<T>, Vector<double>>(ref vector);
            var floored = Vector.Floor(v);
            return Unsafe.As<Vector<double>, Vector<T>>(ref floored);
        }

        return vector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<T> VectorTruncate(Vector<T> vector)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<Vector<T>, Vector<float>>(ref vector);
            var truncated = Vector.Truncate(v);
            return Unsafe.As<Vector<float>, Vector<T>>(ref truncated);
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<Vector<T>, Vector<double>>(ref vector);
            var truncated = Vector.Truncate(v);
            return Unsafe.As<Vector<double>, Vector<T>>(ref truncated);
        }

        return vector;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Create(T value) => new(Vector.Create(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Create(params ReadOnlySpan<T> values) => new(Vector.Create(values));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Create(Vector<T> value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Sequence(T start, T step) => new(Vector.Create(start) + (Vector.Create(step) * s_indices));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Load(ref T value) => new(Vector.LoadUnsafe(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Load(T* pValue) => new(Vector.Load(pValue));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> CastFrom<U>(WideLane<U> value)
        where U : unmanaged, INumber<U>, IMinMaxValue<U>, IBitwiseOperators<U, U, U>
    {
        return new(Unsafe.As<WideLane<U>, Vector<T>>(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(ref T destination) => value.StoreUnsafe(ref destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(T* pDestination) => value.Store(pDestination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(WideLane<T> mask, ref T destination)
    {
        return CompressStore(mask, (T*)Unsafe.AsPointer(in destination));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(WideLane<T> mask, T* pDestination)
    {
        var size = sizeof(T);

        if (LaneWidth == Vector512<T>.Count && Vector512.IsHardwareAccelerated)
        {
            if (size == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<T>, Vector512<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<T>, Vector512<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 16) because each control vector has 16 elements
                var shuffle = Vector512.Load(s_shuffleTable512_32bit + (moveMask * 16));
                var compressed = Vector512.Shuffle(vec, shuffle);

                compressed.Store((uint*)pDestination);
                return BitOperations.PopCount(moveMask);
            }

            if (size == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<T>, Vector512<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<T>, Vector512<ulong>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 8) because each control vector has 8 elements
                var shuffle = Vector512.Load(s_shuffleTable512_64bit + (moveMask * 8));
                var compressed = Vector512.Shuffle(vec, shuffle);

                compressed.Store((ulong*)pDestination);
                return BitOperations.PopCount(moveMask);
            }
        }
        else if (LaneWidth == Vector256<T>.Count && Vector256.IsHardwareAccelerated)
        {
            if (size == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<T>, Vector256<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<T>, Vector256<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 8) because each control vector has 8 elements
                var shuffle = Vector256.Load(s_shuffleTable256_32bit + (moveMask * 8));
                var compressed = Vector256.Shuffle(vec, shuffle);

                compressed.Store((uint*)pDestination);
                return BitOperations.PopCount(moveMask);
            }

            if (size == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<T>, Vector256<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<T>, Vector256<ulong>>(ref mask);

                // For 64-bit, ExtractMostSignificantBits only populates 4 bits (0-15)
                var moveMask = m.ExtractMostSignificantBits();

                // Offset is (moveMask * 4) because each control vector has 4 elements
                var shuffle = Vector256.Load(s_shuffleTable256_64bit + (moveMask * 4));
                var compressed = Vector256.Shuffle(vec, shuffle);

                compressed.Store((ulong*)pDestination);
                return BitOperations.PopCount(moveMask);
            }
        }
        else if (LaneWidth == Vector128<T>.Count && Vector128.IsHardwareAccelerated)
        {
            if (size == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<T>, Vector128<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<T>, Vector128<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 4) because each control vector has 4 elements
                var shuffle = Vector128.Load(s_shuffleTable128_32bit + (moveMask * 4));
                var compressed = Vector128.Shuffle(vec, shuffle);

                compressed.Store((uint*)pDestination);
                return BitOperations.PopCount(moveMask);
            }

            if (size == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<T>, Vector128<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<T>, Vector128<ulong>>(ref mask);
                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 2) because each control vector has 2 elements
                var shuffle = Vector128.Load(s_shuffleTable128_64bit + (moveMask * 2));
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
            if (mask.value[i] == ~T.Zero)
            {
                pDestination[count++] = value[i];
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector<T> AsVector() => value;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator +(WideLane<T> a, WideLane<T> b) => new(a.value + b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator +(WideLane<T> a, T b) => new(a.value + Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator -(WideLane<T> a, WideLane<T> b) => new(a.value - b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator -(WideLane<T> a, T b) => new(a.value - Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator *(WideLane<T> a, WideLane<T> b) => new(a.value * b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator *(WideLane<T> a, T b) => new(a.value * Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator /(WideLane<T> a, WideLane<T> b) => new(a.value / b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator /(WideLane<T> a, T b) => new(a.value / Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator %(WideLane<T> a, WideLane<T> b) => new(a.value - VectorFloor(a.value / b.value) * b.value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator %(WideLane<T> a, T b)
    {
        var vb = Vector.Create(b);
        return new(a.value - VectorFloor(a.value / vb) * vb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator -(WideLane<T> a) => new(-a.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator &(WideLane<T> a, WideLane<T> b) => new(a.value & b.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator &(WideLane<T> a, T b) => new(a.value & Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator |(WideLane<T> a, WideLane<T> b) => new(a.value | b.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator |(WideLane<T> a, T b) => new(a.value | Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator ^(WideLane<T> a, WideLane<T> b) => new(a.value ^ b.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator ^(WideLane<T> a, T b) => new(a.value ^ Vector.Create(b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> operator ~(WideLane<T> a) => new(~a.value);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Abs(WideLane<T> value) => new(Vector.Abs(value.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Floor(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var floored = Vector.Floor(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref floored));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var floored = Vector.Floor(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref floored));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Frac(WideLane<T> value) => new(value.value - VectorFloor(value.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Sqrt(WideLane<T> value) => new(Vector.SquareRoot(value.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Lerp(WideLane<T> a, WideLane<T> b, WideLane<T> t) => new(a.value + (b.value - a.value) * t.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> MultipleAdd(WideLane<T> a, WideLane<T> b, WideLane<T> c)
    {
        if (typeof(T) == typeof(float))
        {
            ref var va = ref Unsafe.As<WideLane<T>, Vector<float>>(ref a);
            ref var vb = ref Unsafe.As<WideLane<T>, Vector<float>>(ref b);
            ref var vc = ref Unsafe.As<WideLane<T>, Vector<float>>(ref c);
            var result = Vector.FusedMultiplyAdd(va, vb, vc);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var va = ref Unsafe.As<WideLane<T>, Vector<double>>(ref a);
            ref var vb = ref Unsafe.As<WideLane<T>, Vector<double>>(ref b);
            ref var vc = ref Unsafe.As<WideLane<T>, Vector<double>>(ref c);
            var result = Vector.FusedMultiplyAdd(va, vb, vc);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }
        else
        {
            return new((a.value * b.value) + c.value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Min(WideLane<T> a, WideLane<T> b) => new(Vector.Min(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Max(WideLane<T> a, WideLane<T> b) => new(Vector.Max(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Clamp(WideLane<T> value, WideLane<T> min, WideLane<T> max) => new(Vector.Clamp(value.value, min.value, max.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Saturate(WideLane<T> value) => Clamp(value, Create(T.Zero), Create(T.One));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Sin(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Sin(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Sin(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Cos(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Cos(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Cos(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (WideLane<T> sin, WideLane<T> cos) SinCos(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var (sin, cos) = Vector.SinCos(v);
            return (new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref sin)), new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref cos)));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var (sin, cos) = Vector.SinCos(v);
            return (new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref sin)), new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref cos)));
        }

        return (value, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Tan(WideLane<T> value)
    {
        // 1. Range Reduction
        // Transform value into range [-pi/4, pi/4]. 
        // This is complex to do right (Payne-Hanek), but for games
        // a simple approximation: value = value - (PI * Round(value / PI)) is good enough.

        var pi = Create(T.CreateChecked(Math.PI));
        var x = value - pi * Round(value / pi);

        // 2. The Approximation (Remez Polynomial)
        // tan(value) ~= value + c1*value^3 + c2*value^5
        // Factored (Horner's Method) for fewer ops: value * (1 + value^2 * (c1 + c2*value^2))

        var x2 = x * x;
        var vc1 = Create(T.CreateChecked(0.3333314036)); // 1/3
        var vc2 = Create(T.CreateChecked(0.1333923995)); // 2/15

        // x2 * (c1 + c2 * x2)
        var poly = MultipleAdd(x2, vc2, vc1);
        // value * (1 + x2 * poly)
        return MultipleAdd(x, MultipleAdd(x2, poly, One), Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Asin(WideLane<T> value)
    {
        // asin(value) = pi/2 - acos(value)

        var piOver2 = Create(T.CreateChecked(Math.PI / 2));
        return piOver2 - Acos(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Acos(WideLane<T> value)
    {
        // 0 <= value <= 1 : acos(value) = sqrt(1 - value) * (c0 + c1*value + c2*value^2 + c3*value^3)
        // value < 0 : acos(value) = pi - acos(-value)

        var x = Abs(value);

        var c0 = Create(T.CreateChecked(1.5707288f)); // pi/2
        var c1 = Create(T.CreateChecked(-0.2121144f));
        var c2 = Create(T.CreateChecked(0.0742610f));
        var c3 = Create(T.CreateChecked(-0.0187293f));

        var term1 = MultipleAdd(x, c3, c2);
        var term2 = MultipleAdd(x, term1, c1);
        var poly = MultipleAdd(x, term2, c0);

        var sqrtTerm = Sqrt(One - x);
        var result = poly * sqrtTerm;

        var pi = Create(T.CreateChecked(Math.PI));
        var isNegative = LessThan(value, Zero);

        return Select(isNegative, pi - result, result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Atan(WideLane<T> value)
    {
        // atan(value) = value * (c1 + c2*value^2)

        var c1 = Create(T.CreateChecked(0.97239411f));
        var c2 = Create(T.CreateChecked(-0.19194795f));

        var x2 = value * value;
        var poly = MultipleAdd(x2, c2, c1);
        return value * poly;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Atan2(WideLane<T> y, WideLane<T> x)
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
        var c1 = Create(T.CreateChecked(0.97239411f));
        var c2 = Create(T.CreateChecked(-0.19194795f));

        // (c1 + c2 * t2)
        var poly = MultipleAdd(c2, t2, c1);

        // result = t * poly
        var result = t * poly;

        // 3. Reconstruct the angle
        // If we swapped value/y (yGtX), the identity is: atan(value/y) = PI/2 - atan(y/value)
        var halfPi = Create(T.CreateChecked(1.570796327f));
        result = Select(yGtX, halfPi - result, result);

        // 4. Adjust for Quadrants (Signs)
        // If value < 0, we are in quadrants 2 or 3, so we need to add PI
        var pi = Create(T.CreateChecked(3.141592654f));
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
    public static WideLane<T> Pow(WideLane<T> x, WideLane<T> y) => Exp(y * Log(x));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Exp(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Exp(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Exp(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Exp2(WideLane<T> value)
    {
        return Pow(Create(T.CreateChecked(2)), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Log(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Log(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Log(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Log2(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Log2(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Log2(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Ceil(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Ceiling(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Ceiling(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Round(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Round(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Round(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Trunc(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<float>>(ref value);
            var result = Vector.Truncate(v);
            return new WideLane<T>(Unsafe.As<Vector<float>, Vector<T>>(ref result));
        }
        else if (typeof(T) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<T>, Vector<double>>(ref value);
            var result = Vector.Truncate(v);
            return new WideLane<T>(Unsafe.As<Vector<double>, Vector<T>>(ref result));
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Sign(WideLane<T> value) => Select(
            GreaterThan(value, Zero),
            One,
            Select(
                LessThan(value, Zero),
                ~Zero,
                Zero));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> CopySign(WideLane<T> magnitude, WideLane<T> sign) => new(Vector.CopySign(magnitude.value, sign.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Rcp(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            if (Sse.IsSupported && LaneWidth == Vector128<float>.Count)
            {
                var vf = Unsafe.As<WideLane<T>, Vector128<float>>(ref value);
                var result = Sse.Reciprocal(vf);
                return Unsafe.As<Vector128<float>, WideLane<T>>(ref result);
            }
            else if (Avx.IsSupported && LaneWidth == Vector256<float>.Count)
            {
                var vf = Unsafe.As<WideLane<T>, Vector256<float>>(ref value);
                var result = Avx.Reciprocal(vf);
                return Unsafe.As<Vector256<float>, WideLane<T>>(ref result);
            }
        }

        return Create(T.One) / value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Rsqrt(WideLane<T> value)
    {
        if (typeof(T) == typeof(float))
        {
            if (Sse.IsSupported && LaneWidth == Vector128<float>.Count)
            {
                var vf = Unsafe.As<WideLane<T>, Vector128<float>>(ref value);
                var result = Sse.ReciprocalSqrt(vf);
                return Unsafe.As<Vector128<float>, WideLane<T>>(ref result);
            }
            else if (Avx.IsSupported && LaneWidth == Vector256<float>.Count)
            {
                var vf = Unsafe.As<WideLane<T>, Vector256<float>>(ref value);
                var result = Avx.ReciprocalSqrt(vf);
                return Unsafe.As<Vector256<float>, WideLane<T>>(ref result);
            }
        }

        return Create(T.One) / Sqrt(value);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Select(WideLane<T> conditionMask, WideLane<T> ifTrue, WideLane<T> ifFalse)
        => new(Vector.ConditionalSelect(
            conditionMask.value,
            ifTrue.value,
            ifFalse.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> GreaterThan(WideLane<T> a, WideLane<T> b) => new(Vector.GreaterThan(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> GreaterThanOrEqual(WideLane<T> a, WideLane<T> b) => new(Vector.GreaterThanOrEqual(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> LessThan(WideLane<T> a, WideLane<T> b) => new(Vector.LessThan(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> LessThanOrEqual(WideLane<T> a, WideLane<T> b) => new(Vector.LessThanOrEqual(a.value, b.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<T> Equal(WideLane<T> a, WideLane<T> b) => new(Vector.Equals(a.value, b.value));



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(WideLane<T> mask) => !Vector.EqualsAll(mask.value, Vector<T>.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All(WideLane<T> mask) => Vector.EqualsAll(mask.value, Vector<T>.AllBitsSet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool None(WideLane<T> mask) => Vector.EqualsAll(mask.value, Vector<T>.Zero);


    public override string ToString()
    {
        return value.ToString();
    }
}
