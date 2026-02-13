using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public unsafe struct Vector3<TLane, TNumber> : IEquatable<Vector3<TLane, TNumber>>
    where TLane : ISPMD<TLane, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    public TLane x;
    public TLane y;
    public TLane z;

    public TLane this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            RangeCheck(index);
            return Unsafe.Add(ref x, index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("ENABLE_COLLECTION_CHECKS")]
    private static void RangeCheck(int index)
    {
        if (index < 0 || index >= 3)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range for Vector3.");
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(TNumber* pDst)
    {
        var width = TLane.LaneWidth;

        var x = stackalloc TNumber[width];
        var y = stackalloc TNumber[width];
        var z = stackalloc TNumber[width];

        this.x.Store(x);
        this.y.Store(y);
        this.z.Store(z);

        for (var i = 0; i < width; i++)
        {
            pDst[i * 3 + 0] = x[i];
            pDst[i * 3 + 1] = y[i];
            pDst[i * 3 + 2] = z[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber dst)
    {
        Store((TNumber*)Unsafe.AsPointer(ref dst));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(TNumber* pX, TNumber* pY, TNumber* pZ)
    {
        x.Store(pX);
        y.Store(pY);
        z.Store(pZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber x, ref TNumber y, ref TNumber z)
    {
        this.x.Store(ref x);
        this.y.Store(ref y);
        this.z.Store(ref z);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator +(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x + right.x,
            y = left.y + right.y,
            z = left.z + right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator +(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x + lane,
            y = vector.y + lane,
            z = vector.z + lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator -(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x - right.x,
            y = left.y - right.y,
            z = left.z - right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator -(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x - lane,
            y = vector.y - lane,
            z = vector.z - lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator *(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x * right.x,
            y = left.y * right.y,
            z = left.z * right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator *(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x * lane,
            y = vector.y * lane,
            z = vector.z * lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator /(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x / right.x,
            y = left.y / right.y,
            z = left.z / right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator == (in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x == right.x,
            y = left.y == right.y,
            z = left.z == right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator != (in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x != right.x,
            y = left.y != right.y,
            z = left.z != right.z,
        };
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3<TLane, TNumber> other)
    {
        return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals(object? obj)
    {
        return obj is Vector3<TLane, TNumber> other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}