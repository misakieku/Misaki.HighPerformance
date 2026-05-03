using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public static unsafe class WideLane
{
    internal static readonly uint* s_pShuffleTable512_32bit;
    internal static readonly ulong* s_pShuffleTable512_64bit;
    internal static readonly uint* s_pShuffleTable256_32bit;
    internal static readonly ulong* s_pShuffleTable256_64bit;
    internal static readonly uint* s_pShuffleTable128_32bit;
    internal static readonly ulong* s_pShuffleTable128_64bit;

    /// <summary>
    /// Gets whether WideLane is supported on the current hardware.
    /// </summary>
    public static bool IsSupported => Vector.IsHardwareAccelerated;

    static WideLane()
    {
        s_pShuffleTable512_32bit = ShuffleTableGenerator.ComputeShuffleTable512_32Bit();
        s_pShuffleTable512_64bit = ShuffleTableGenerator.ComputeShuffleTable512_64Bit();
        s_pShuffleTable256_32bit = ShuffleTableGenerator.ComputeShuffleTable256_32Bit();
        s_pShuffleTable256_64bit = ShuffleTableGenerator.ComputeShuffleTable256_64Bit();
        s_pShuffleTable128_32bit = ShuffleTableGenerator.ComputeShuffleTable128_32Bit();
        s_pShuffleTable128_64bit = ShuffleTableGenerator.ComputeShuffleTable128_64Bit();
    }
}

// TODO: We can use source generator to generate the optimized code for different hardware (e.g.,SSE, AVX, AVX2, etc.) and select the best version at runtime.
// Right now, we rely on Vector API to auto vectorize the code.
// This works fine in jit, but require user to build multiple binaries with different target architectures to get the best performance in NativeAOT via IlcInstructionSet.

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe partial struct WideLane<TNumber> : ISPMDLane<WideLane<TNumber>, TNumber>
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
        get => new WideLane<TNumber>(Vector<TNumber>.Zero);
    }

    public static WideLane<TNumber> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new WideLane<TNumber>(Vector<TNumber>.One);
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

    public static WideLane<TNumber> AllBitsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Create(TNumber.AllBitsSet);
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
        return new WideLane<TNumber>(Vector.Create(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Create(params ReadOnlySpan<TNumber> values)
    {
        return new WideLane<TNumber>(Vector.Create(values));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Create(Vector<TNumber> value)
    {
        return new WideLane<TNumber>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sequence(TNumber start, TNumber step)
    {
        if (LaneWidth == Vector512<TNumber>.Count)
        {
            var v = Vector512.CreateSequence(start, step);
            return Unsafe.As<Vector512<TNumber>, WideLane<TNumber>>(ref v);
        }
        else if (LaneWidth == Vector256<TNumber>.Count)
        {
            var v = Vector256.CreateSequence(start, step);
            return Unsafe.As<Vector256<TNumber>, WideLane<TNumber>>(ref v);
        }
        else if (LaneWidth == Vector128<TNumber>.Count)
        {
            var v = Vector128.CreateSequence(start, step);
            return Unsafe.As<Vector128<TNumber>, WideLane<TNumber>>(ref v);
        }
        else if (LaneWidth == Vector64<TNumber>.Count)
        {
            var v = Vector64.CreateSequence(start, step);
            return Unsafe.As<Vector64<TNumber>, WideLane<TNumber>>(ref v);
        }
        else
        {
            return new WideLane<TNumber>(Vector.Create(start) + (Vector.Create(step) * s_indices));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Load(ref TNumber value)
    {
        return new WideLane<TNumber>(Vector.LoadUnsafe(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Load(TNumber* pValue)
    {
        return new WideLane<TNumber>(Vector.Load(pValue));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> MaskLoad(WideLane<TNumber> mask, ref TNumber value)
    {
        var vector = Vector.LoadUnsafe(ref value);
        return new WideLane<TNumber>(Vector.ConditionalSelect(mask.value, vector, Vector<TNumber>.Zero));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> MaskLoad(WideLane<TNumber> mask, TNumber* pValue)
    {
        return MaskLoad(mask, ref Unsafe.AsRef<TNumber>(pValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Gather(TNumber* pData, WideLane<TNumber> indices, int scale)
    {
        return Gather(ref Unsafe.AsRef<TNumber>(pData), indices, scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Gather(TNumber* pData, int* pIndices, int scale)
    {
        return Gather(ref Unsafe.AsRef<TNumber>(pData), ref Unsafe.AsRef<int>(pIndices), scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Gather(ref TNumber baseAddress, WideLane<TNumber> indices, int scale)
    {
        Unsafe.SkipInit(out Vector<TNumber> result);

        var pResult = (TNumber*)&result;
        var pIndices = (TNumber*)&indices;

        var count = Vector<TNumber>.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = int.CreateTruncating(pIndices[i]);
            pResult[i] = Unsafe.Add(ref baseAddress, idx * scale / sizeof(TNumber));
        }

        return new WideLane<TNumber>(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Gather(ref TNumber baseAddress, ref int baseIndex, int scale)
    {
        Unsafe.SkipInit(out Vector<TNumber> result);

        var pResult = (TNumber*)&result;

        var count = Vector<TNumber>.Count;
        for (var i = 0; i < count; i++)
        {
            pResult[i] = Unsafe.Add(ref baseAddress, Unsafe.Add(ref baseIndex, i) * scale / sizeof(TNumber));
        }

        return new WideLane<TNumber>(result);
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
        if (LaneWidth == Vector512<TNumber>.Count && Vector512.IsHardwareAccelerated)
        {
            if (sizeof(TNumber) == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector512<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector512<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 16) because each control vector has 16 elements
                var shuffle = Vector512.Load(WideLane.s_pShuffleTable512_32bit + (moveMask * 16));
                var compressed = Vector512.Shuffle(vec, shuffle);

                compressed.StoreUnsafe(ref Unsafe.As<TNumber, uint>(ref destination));
                return BitOperations.PopCount(moveMask);
            }

            if (sizeof(TNumber) == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector512<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector512<ulong>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 8) because each control vector has 8 elements
                var shuffle = Vector512.Load(WideLane.s_pShuffleTable512_64bit + (moveMask * 8));
                var compressed = Vector512.Shuffle(vec, shuffle);

                compressed.StoreUnsafe(ref Unsafe.As<TNumber, ulong>(ref destination));
                return BitOperations.PopCount(moveMask);
            }
        }
        else if (LaneWidth == Vector256<TNumber>.Count && Vector256.IsHardwareAccelerated)
        {
            if (sizeof(TNumber) == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector256<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector256<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 8) because each control vector has 8 elements
                var shuffle = Vector256.Load(WideLane.s_pShuffleTable256_32bit + (moveMask * 8));
                var compressed = Vector256.Shuffle(vec, shuffle);

                compressed.StoreUnsafe(ref Unsafe.As<TNumber, uint>(ref destination));
                return BitOperations.PopCount(moveMask);
            }

            if (sizeof(TNumber) == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector256<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector256<ulong>>(ref mask);

                // For 64-bit, ExtractMostSignificantBits only populates 4 bits (0-15)
                var moveMask = m.ExtractMostSignificantBits();

                // Offset is (moveMask * 4) because each control vector has 4 elements
                var shuffle = Vector256.Load(WideLane.s_pShuffleTable256_64bit + (moveMask * 4));
                var compressed = Vector256.Shuffle(vec, shuffle);

                compressed.StoreUnsafe(ref Unsafe.As<TNumber, ulong>(ref destination));
                return BitOperations.PopCount(moveMask);
            }
        }
        else if (LaneWidth == Vector128<TNumber>.Count && Vector128.IsHardwareAccelerated)
        {
            if (sizeof(TNumber) == 4)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector128<uint>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector128<uint>>(ref mask);

                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 4) because each control vector has 4 elements
                var shuffle = Vector128.Load(WideLane.s_pShuffleTable128_32bit + (moveMask * 4));
                var compressed = Vector128.Shuffle(vec, shuffle);

                compressed.StoreUnsafe(ref Unsafe.As<TNumber, uint>(ref destination));
                return BitOperations.PopCount(moveMask);
            }

            if (sizeof(TNumber) == 8)
            {
                ref var vec = ref Unsafe.As<WideLane<TNumber>, Vector128<ulong>>(ref Unsafe.AsRef(in this));
                var m = Unsafe.As<WideLane<TNumber>, Vector128<ulong>>(ref mask);
                var moveMask = m.ExtractMostSignificantBits();
                // Offset is (moveMask * 2) because each control vector has 2 elements
                var shuffle = Vector128.Load(WideLane.s_pShuffleTable128_64bit + (moveMask * 2));
                var compressed = Vector128.Shuffle(vec, shuffle);
                compressed.StoreUnsafe(ref Unsafe.As<TNumber, ulong>(ref destination));
                return BitOperations.PopCount(moveMask);
            }
        }

        // This is slow, but correct on ANY hardware.
        // Check sign bit of the mask lane
        var count = 0;
        for (var i = 0; i < LaneWidth; i++)
        {
            if (mask.value[i] == TNumber.AllBitsSet)
            {
                Unsafe.Add(ref destination, count++) = value[i];
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompressStore(WideLane<TNumber> mask, TNumber* pDestination)
    {
        return CompressStore(mask, ref Unsafe.AsRef<TNumber>(pDestination));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector<TNumber> AsVector()
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly TNumber* GetUnsafePtr()
    {
        return (TNumber*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOther BitCast<TOther, TOtherNumber>()
        where TOther : ISPMDLane<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>
    {
        return Unsafe.BitCast<WideLane<TNumber>, TOther>(this);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator +(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value + b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator -(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value - b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator *(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value * b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator /(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value / b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator %(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value - VectorFloor(a.value / b.value) * b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator -(WideLane<TNumber> a)
    {
        return new WideLane<TNumber>(-a.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator &(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value & b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator |(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value | b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator ^(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(a.value ^ b.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> operator ~(WideLane<TNumber> a)
    {
        return new WideLane<TNumber>(~a.value);
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
        return new WideLane<TNumber>(Vector.Abs(value.value));
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
        return new WideLane<TNumber>(value.value - VectorFloor(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sqrt(WideLane<TNumber> value)
    {
        return new WideLane<TNumber>(Vector.SquareRoot(value.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Lerp(WideLane<TNumber> a, WideLane<TNumber> b, WideLane<TNumber> t)
    {
        return new WideLane<TNumber>(a.value + (b.value - a.value) * t.value);
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

        return new WideLane<TNumber>((a.value * b.value) + c.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Min(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.Min(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Max(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.Max(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Clamp(WideLane<TNumber> value, WideLane<TNumber> min, WideLane<TNumber> max)
    {
        return new WideLane<TNumber>(Vector.Clamp(value.value, min.value, max.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Saturate(WideLane<TNumber> value)
    {
        return Clamp(value, Create(TNumber.Zero), Create(TNumber.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Sin(WideLane<TNumber> value)
    {
#if MHP_FASTMATH
        var invPi = Create(TNumber.CreateTruncating(0.318309886f)); // 1 / PI

        var x_sin = value;
        var y_sin = x_sin * invPi;
        var k_sin = Round(y_sin);
        var z_sin = y_sin - k_sin;

        var half = Create(TNumber.CreateTruncating(0.5f));
        var two = Create(TNumber.CreateTruncating(2.0f));

        var k_even_sin = Round(k_sin * half) * two;
        var sign_sin = One - two * Abs(k_sin - k_even_sin);

        var c1 = Create(TNumber.CreateTruncating(3.14159265f));   // PI
        var c3 = Create(TNumber.CreateTruncating(-5.16771278f));  // -PI^3 / 6
        var c5 = Create(TNumber.CreateTruncating(2.55016404f));   // PI^5 / 120
        var c7 = Create(TNumber.CreateTruncating(-0.59926453f));  // -PI^7 / 5040
        var c9 = Create(TNumber.CreateTruncating(0.08214589f));   // PI^9 / 362880

        var z2_sin = z_sin * z_sin;
        var poly_sin = MultipleAdd(z2_sin, c9, c7);       // c7 + c9*z^2
        poly_sin = MultipleAdd(z2_sin, poly_sin, c5);                   // c5 + z^2*(...)
        poly_sin = MultipleAdd(z2_sin, poly_sin, c3);                   // c3 + z^2*(...)
        poly_sin = MultipleAdd(z2_sin, poly_sin, c1);                   // c1 + z^2*(...)
        poly_sin = z_sin * poly_sin;                                            // z * (...)

        return poly_sin * sign_sin;
#else
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
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Cos(WideLane<TNumber> value)
    {
#if MHP_FASTMATH
        var halfPi = Create(TNumber.CreateTruncating(1.570796327f));
        var invPi = Create(TNumber.CreateTruncating(0.318309886f)); // 1 / PI

        var x_cos = value + halfPi;
        var y_cos = x_cos * invPi;
        var k_cos = Round(y_cos);
        var z_cos = y_cos - k_cos;

        var half = Create(TNumber.CreateTruncating(0.5f));
        var two = Create(TNumber.CreateTruncating(2.0f));

        var k_even_cos = Round(k_cos * half) * two;
        var sign_cos = One - two * Abs(k_cos - k_even_cos);

        var c1 = Create(TNumber.CreateTruncating(3.14159265f));   // PI
        var c3 = Create(TNumber.CreateTruncating(-5.16771278f));  // -PI^3 / 6
        var c5 = Create(TNumber.CreateTruncating(2.55016404f));   // PI^5 / 120
        var c7 = Create(TNumber.CreateTruncating(-0.59926453f));  // -PI^7 / 5040
        var c9 = Create(TNumber.CreateTruncating(0.08214589f));   // PI^9 / 362880

        var z2_cos = z_cos * z_cos;
        var poly_cos = MultipleAdd(z2_cos, c9, c7);
        poly_cos = MultipleAdd(z2_cos, poly_cos, c5);
        poly_cos = MultipleAdd(z2_cos, poly_cos, c3);
        poly_cos = MultipleAdd(z2_cos, poly_cos, c1);
        poly_cos = z_cos * poly_cos;

        return poly_cos * sign_cos;
#else
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
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SinCos(WideLane<TNumber> value, out WideLane<TNumber> sin, out WideLane<TNumber> cos)
    {
#if MHP_FASTMATH
        // We use Taylor/Remez polynomial approximation for Sin(PI * z) and Cos(PI * z) on the reduced range of z in [-0.5, 0.5].

        var halfPi = Create(TNumber.CreateTruncating(1.570796327f));
        var invPi = Create(TNumber.CreateTruncating(0.318309886f)); // 1 / PI

        var x_sin = value;
        var x_cos = value + halfPi;

        // Range Reduction
        // We map any angle to the interval [-0.5, 0.5] (corresponding to the actual angle range [-PI/2, PI/2])
        // y = x * (1 / PI)
        var y_sin = x_sin * invPi;
        var y_cos = x_cos * invPi;

        // k = Round(y)
        var k_sin = Round(y_sin);
        var k_cos = Round(y_cos);

        // z = y - k (Now, the range of z is perfectly reduced to [-0.5, 0.5])
        var z_sin = y_sin - k_sin;
        var z_cos = y_cos - k_cos;

        // 2. Branchless Sign Flip
        // Mathematical principle: Sin(x + k*PI) = Sin(x) * (-1)^k
        // We need to compute (-1)^k. To avoid inefficient bit operations or branches, we compute it with floating-point math:
        // sign = 1.0 - 2.0 * Abs(k - 2.0 * Round(k * 0.5))
        var half = Create(TNumber.CreateTruncating(0.5f));
        var two = Create(TNumber.CreateTruncating(2.0f));
        var one = One;

        var k_even_sin = Round(k_sin * half) * two;
        var sign_sin = one - two * Abs(k_sin - k_even_sin);

        var k_even_cos = Round(k_cos * half) * two;
        var sign_cos = one - two * Abs(k_cos - k_even_cos);

        // 3. Taylor/Remez Polynomial for Sin(PI * z)
        // For z in [-0.5, 0.5]，Calculate sin(PI * z)
        // z * (C1 + z^2 * (C3 + z^2 * (C5 + z^2 * (C7 + z^2 * C9))))
        var c1 = Create(TNumber.CreateTruncating(3.14159265f));   // PI
        var c3 = Create(TNumber.CreateTruncating(-5.16771278f));  // -PI^3 / 6
        var c5 = Create(TNumber.CreateTruncating(2.55016404f));   // PI^5 / 120
        var c7 = Create(TNumber.CreateTruncating(-0.59926453f));  // -PI^7 / 5040
        var c9 = Create(TNumber.CreateTruncating(0.08214589f));   // PI^9 / 362880

        var z2_sin = z_sin * z_sin;
        var poly_sin = MultipleAdd(z2_sin, c9, c7);       // c7 + c9*z^2
        poly_sin = MultipleAdd(z2_sin, poly_sin, c5);                   // c5 + z^2*(...)
        poly_sin = MultipleAdd(z2_sin, poly_sin, c3);                   // c3 + z^2*(...)
        poly_sin = MultipleAdd(z2_sin, poly_sin, c1);                   // c1 + z^2*(...)
        poly_sin = z_sin * poly_sin;                                            // z * (...)

        var z2_cos = z_cos * z_cos;
        var poly_cos = MultipleAdd(z2_cos, c9, c7);
        poly_cos = MultipleAdd(z2_cos, poly_cos, c5);
        poly_cos = MultipleAdd(z2_cos, poly_cos, c3);
        poly_cos = MultipleAdd(z2_cos, poly_cos, c1);
        poly_cos = z_cos * poly_cos;

        sin = poly_sin * sign_sin;
        cos = poly_cos * sign_cos;
#else
        if (typeof(TNumber) == typeof(float))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<float>>(ref value);
            var (sinResult, cosResult) = Vector.SinCos(v);
            sin = new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref sinResult));
            cos = new WideLane<TNumber>(Unsafe.As<Vector<float>, Vector<TNumber>>(ref cosResult));
        }
        else if (typeof(TNumber) == typeof(double))
        {
            ref var v = ref Unsafe.As<WideLane<TNumber>, Vector<double>>(ref value);
            var (sinResult, cosResult) = Vector.SinCos(v);
            sin = new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref sinResult));
            cos = new WideLane<TNumber>(Unsafe.As<Vector<double>, Vector<TNumber>>(ref cosResult));
        }
        else
        {
            sin = value;
            cos = value;
        }
#endif
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
                AllBitsSet,
                Zero));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> CopySign(WideLane<TNumber> magnitude, WideLane<TNumber> sign)
    {
        return new WideLane<TNumber>(Vector.CopySign(magnitude.value, sign.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Rcp(WideLane<TNumber> value)
    {
        if (typeof(TNumber) == typeof(float))
        {
            if (Sse.IsSupported && LaneWidth == Vector128<float>.Count)
            {
                ref var vf = ref Unsafe.As<WideLane<TNumber>, Vector128<float>>(ref value);
                var x0 = Sse.Reciprocal(vf);
#if MHP_FASTMATH
                return Unsafe.As<Vector128<float>, WideLane<TNumber>>(ref x0);
#else
                // SSE and AVX provide fast approximate reciprocal instructions but the precision is very low (11 bits).
                // In non-MHP_FASTMATH path, we can do one step of Newton-Raphson iteration to improve the precision to about 22 bits, which is good enough for most game use cases.
                var x1 = x0 * (Vector128.Create(2.0f) - x0 * vf);
                return Unsafe.As<Vector128<float>, WideLane<TNumber>>(ref x1);
#endif
            }
            else if (Avx.IsSupported && LaneWidth == Vector256<float>.Count)
            {
                ref var vf = ref Unsafe.As<WideLane<TNumber>, Vector256<float>>(ref value);
                var x0 = Avx.Reciprocal(vf);
#if MHP_FASTMATH
                return Unsafe.As<Vector256<float>, WideLane<TNumber>>(ref x0);
#else
                // SSE and AVX provide fast approximate reciprocal instructions but the precision is very low (11 bits).
                // In non-MHP_FASTMATH path, we can do one step of Newton-Raphson iteration to improve the precision to about 22 bits, which is good enough for most game use cases.
                var x1 = x0 * (Vector256.Create(2.0f) - x0 * vf);
                return Unsafe.As<Vector256<float>, WideLane<TNumber>>(ref x1);
#endif
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
                ref var vf = ref Unsafe.As<WideLane<TNumber>, Vector128<float>>(ref value);
                var x0 = Sse.ReciprocalSqrt(vf);
#if MHP_FASTMATH
                return Unsafe.As<Vector128<float>, WideLane<TNumber>>(ref x0);
#else
                // SSE and AVX provide fast approximate reciprocal sqrt instructions but the precision is very low (11 bits).
                // In non-MHP_FASTMATH path, we can do one step of Newton-Raphson iteration to improve the precision to about 22 bits, which is good enough for most game use cases.
                var x1 = x0 * Vector128.Create(0.5f) * (Vector128.Create(3.0f) - (vf * x0 * x0));
                return Unsafe.As<Vector128<float>, WideLane<TNumber>>(ref x1);
#endif
            }
            else if (Avx.IsSupported && LaneWidth == Vector256<float>.Count)
            {
                ref var vf = ref Unsafe.As<WideLane<TNumber>, Vector256<float>>(ref value);
                var x0 = Avx.ReciprocalSqrt(vf);
#if MHP_FASTMATH
                return Unsafe.As<Vector256<float>, WideLane<TNumber>>(ref x0);
#else
                // SSE and AVX provide fast approximate reciprocal sqrt instructions but the precision is very low (11 bits).
                // In non-MHP_FASTMATH path, we can do one step of Newton-Raphson iteration to improve the precision to about 22 bits, which is good enough for most game use cases.
                var x1 = x0 * Vector256.Create(0.5f) * (Vector256.Create(3.0f) - (vf * x0 * x0));
                return Unsafe.As<Vector256<float>, WideLane<TNumber>>(ref x1);
#endif
            }
        }

        return Create(TNumber.One) / Sqrt(value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber ReduceAdd(WideLane<TNumber> value)
    {
        return Vector.Sum(value.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber ReduceMax(WideLane<TNumber> value)
    {
        // TODO: Use shuffle and max.

        var max = TNumber.Zero;
        for (var i = 0; i < LaneWidth; i++)
        {
            if (value[i] > max)
            {
                max = value[i];
            }
        }

        return max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber ReduceMin(WideLane<TNumber> value)
    {
        // TODO: Use shuffle and min.

        var min = TNumber.Zero;
        for (var i = 0; i < LaneWidth; i++)
        {
            if (value[i] < min)
            {
                min = value[i];
            }
        }

        return min;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Select(WideLane<TNumber> conditionMask, WideLane<TNumber> ifTrue, WideLane<TNumber> ifFalse)
    {
        return new WideLane<TNumber>(Vector.ConditionalSelect(
                conditionMask.value,
                ifTrue.value,
                ifFalse.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> GreaterThan(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.GreaterThan(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> GreaterThanOrEqual(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.GreaterThanOrEqual(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> LessThan(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.LessThan(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> LessThanOrEqual(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.LessThanOrEqual(a.value, b.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WideLane<TNumber> Equal(WideLane<TNumber> a, WideLane<TNumber> b)
    {
        return new WideLane<TNumber>(Vector.Equals(a.value, b.value));
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
