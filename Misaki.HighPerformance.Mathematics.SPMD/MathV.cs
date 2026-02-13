using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public static unsafe partial class MathV
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> LoadVector3<TLane, TNumber>(TNumber* pSrc)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        var width = TLane.LaneWidth;

        var x = stackalloc TNumber[width];
        var y = stackalloc TNumber[width];
        var z = stackalloc TNumber[width];

        for (var i = 0; i < width; i++)
        {
            x[i] = pSrc[i * 3 + 0];
            y[i] = pSrc[i * 3 + 1];
            z[i] = pSrc[i * 3 + 2];
        }

        return new Vector3<TLane, TNumber>
        {
            x = TLane.Load(x),
            y = TLane.Load(y),
            z = TLane.Load(z)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Load<TLane, TNumber>(ref TNumber src)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return LoadVector3<TLane, TNumber>((TNumber*)Unsafe.AsPointer(ref src));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Load<TLane, TNumber>(TNumber* pX, TNumber* pY, TNumber* pZ)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return new Vector3<TLane, TNumber>
        {
            x = TLane.Load(pX),
            y = TLane.Load(pY),
            z = TLane.Load(pZ)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Load<TLane, TNumber>(ref TNumber x, ref TNumber y, ref TNumber z)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return new Vector3<TLane, TNumber>
        {
            x = TLane.Load(ref x),
            y = TLane.Load(ref y),
            z = TLane.Load(ref z)
        };
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Abs<TLane, TNumber>(in Vector3<TLane, TNumber> vector)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return new Vector3<TLane, TNumber>
        {
            x = TLane.Abs(vector.x),
            y = TLane.Abs(vector.y),
            z = TLane.Abs(vector.z),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TLane Dot<TLane, TNumber>(in Vector3<TLane, TNumber> a, in Vector3<TLane, TNumber> b)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TLane LengthSquared<TLane, TNumber>(in Vector3<TLane, TNumber> vector)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return Dot(vector, vector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Sqrt<TLane, TNumber>(in Vector3<TLane, TNumber> vector)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return new Vector3<TLane, TNumber>
        {
            x = TLane.Sqrt(vector.x),
            y = TLane.Sqrt(vector.y),
            z = TLane.Sqrt(vector.z),
        };
    }

    public static Vector3<TLane, TNumber> Rsqrt<TLane, TNumber>(in Vector3<TLane, TNumber> vector)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return new Vector3<TLane, TNumber>
        {
            x = TLane.Rsqrt(vector.x),
            y = TLane.Rsqrt(vector.y),
            z = TLane.Rsqrt(vector.z),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Normalize<TLane, TNumber>(in Vector3<TLane, TNumber> vector)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return vector * TLane.Rsqrt(Dot(vector, vector));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Cross<TLane, TNumber>(in Vector3<TLane, TNumber> a, in Vector3<TLane, TNumber> b)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        return new Vector3<TLane, TNumber>
        {
            x = a.y * b.z - a.z * b.y,
            y = a.z * b.x - a.x * b.z,
            z = a.x * b.y - a.y * b.x,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> Reflect<TLane, TNumber>(in Vector3<TLane, TNumber> incident, in Vector3<TLane, TNumber> normal)
        where TLane : ISPMD<TLane, TNumber>
        where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
    {
        var dot = Dot(incident, normal);
        return incident - normal * (dot + dot);
    }
}
