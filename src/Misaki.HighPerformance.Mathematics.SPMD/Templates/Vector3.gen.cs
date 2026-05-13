
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public unsafe struct Vector3<TLane, TNumber> : IEquatable<Vector3<TLane, TNumber>>
    where TLane : ISPMDLane<TLane, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    public TLane x;
    public TLane y;
    public TLane z;

    public static Vector3<TLane, TNumber> Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new Vector3<TLane, TNumber>
            {
                x = TLane.Zero,
                y = TLane.Zero,
                z = TLane.Zero,
            };
        }
    }

    public static Vector3<TLane, TNumber> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new Vector3<TLane, TNumber>
            {
                x = TLane.One,
                y = TLane.One,
                z = TLane.One,
            };
        }
    }

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
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
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
        x.Store(pDst + 0 * TLane.LaneWidth);
        y.Store(pDst + 1 * TLane.LaneWidth);
        z.Store(pDst + 2 * TLane.LaneWidth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber dst)
    {
        x.Store(ref Unsafe.Add(ref dst, 0 * TLane.LaneWidth));
        y.Store(ref Unsafe.Add(ref dst, 1 * TLane.LaneWidth));
        z.Store(ref Unsafe.Add(ref dst, 2 * TLane.LaneWidth));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(TNumber* px, TNumber* py, TNumber* pz)
    {
        x.Store(px);
        y.Store(py);
        z.Store(pz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber x, ref TNumber y, ref TNumber z)
    {
        this.x.Store(ref x);
        this.y.Store(ref y);
        this.z.Store(ref z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CompressStore(TNumber* pDst, Vector3<TLane, TNumber> mask)
    {
        x.CompressStore(pDst + 0 * TLane.LaneWidth, mask.x);
        y.CompressStore(pDst + 1 * TLane.LaneWidth, mask.y);
        z.CompressStore(pDst + 2 * TLane.LaneWidth, mask.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CompressStore(ref TNumber dst, Vector3<TLane, TNumber> mask)
    {
        x.CompressStore(ref Unsafe.Add(ref dst, 0 * TLane.LaneWidth), mask.x);
        y.CompressStore(ref Unsafe.Add(ref dst, 1 * TLane.LaneWidth), mask.y);
        z.CompressStore(ref Unsafe.Add(ref dst, 2 * TLane.LaneWidth), mask.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(TNumber* pDst, TLane indices)
    {
        x.Scatter(pDst + 0, indices);
        y.Scatter(pDst + 1, indices);
        z.Scatter(pDst + 2, indices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(TNumber* pDst, int* pIndices)
    {
        x.Scatter(pDst + 0, pIndices);
        y.Scatter(pDst + 1, pIndices);
        z.Scatter(pDst + 2, pIndices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(ref TNumber dst, TLane indices)
    {
        x.Scatter(ref Unsafe.Add(ref dst, 0), indices);
        y.Scatter(ref Unsafe.Add(ref dst, 1), indices);
        z.Scatter(ref Unsafe.Add(ref dst, 2), indices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(ref TNumber dst, int* pIndices)
    {
        x.Scatter(ref Unsafe.Add(ref dst, 0), pIndices);
        y.Scatter(ref Unsafe.Add(ref dst, 1), pIndices);
        z.Scatter(ref Unsafe.Add(ref dst, 2), pIndices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(TNumber* pDst, TLane indices, TLane mask)
    {
        x.MaskScatter(pDst + 0, indices, mask);
        y.MaskScatter(pDst + 1, indices, mask);
        z.MaskScatter(pDst + 2, indices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(TNumber* pDst, int* pIndices, TLane mask)
    {
        x.MaskScatter(pDst + 0, pIndices, mask);
        y.MaskScatter(pDst + 1, pIndices, mask);
        z.MaskScatter(pDst + 2, pIndices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(ref TNumber dst, TLane indices, TLane mask)
    {
        x.MaskScatter(ref Unsafe.Add(ref dst, 0), indices, mask);
        y.MaskScatter(ref Unsafe.Add(ref dst, 1), indices, mask);
        z.MaskScatter(ref Unsafe.Add(ref dst, 2), indices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(ref TNumber dst, int* pIndices, TLane mask)
    {
        x.MaskScatter(ref Unsafe.Add(ref dst, 0), pIndices, mask);
        y.MaskScatter(ref Unsafe.Add(ref dst, 1), pIndices, mask);
        z.MaskScatter(ref Unsafe.Add(ref dst, 2), pIndices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator -(in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = -vector.x,
            y = -vector.y,
            z = -vector.z,
        };
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
    public static Vector3<TLane, TNumber> operator +(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane + vector.x,
            y = lane + vector.y,
            z = lane + vector.z,
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
    public static Vector3<TLane, TNumber> operator -(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane - vector.x,
            y = lane - vector.y,
            z = lane - vector.z,
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
    public static Vector3<TLane, TNumber> operator *(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane * vector.x,
            y = lane * vector.y,
            z = lane * vector.z,
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
    public static Vector3<TLane, TNumber> operator /(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x / lane,
            y = vector.y / lane,
            z = vector.z / lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator /(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane / vector.x,
            y = lane / vector.y,
            z = lane / vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator ==(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x == right.x,
            y = left.y == right.y,
            z = left.z == right.z,
        };
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator ==(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x == lane,
            y = vector.y == lane,
            z = vector.z == lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator ==(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane == vector.x,
            y = lane == vector.y,
            z = lane == vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator !=(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x != right.x,
            y = left.y != right.y,
            z = left.z != right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator !=(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x != lane,
            y = vector.y != lane,
            z = vector.z != lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator !=(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane != vector.x,
            y = lane != vector.y,
            z = lane != vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator >(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x > right.x,
            y = left.y > right.y,
            z = left.z > right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator >(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x > lane,
            y = vector.y > lane,
            z = vector.z > lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator >(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane > vector.x,
            y = lane > vector.y,
            z = lane > vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator >=(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x >= right.x,
            y = left.y >= right.y,
            z = left.z >= right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator >=(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x >= lane,
            y = vector.y >= lane,
            z = vector.z >= lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator >=(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane >= vector.x,
            y = lane >= vector.y,
            z = lane >= vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator <(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x < right.x,
            y = left.y < right.y,
            z = left.z < right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator <(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x < lane,
            y = vector.y < lane,
            z = vector.z < lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator <(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane < vector.x,
            y = lane < vector.y,
            z = lane < vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator <=(in Vector3<TLane, TNumber> left, in Vector3<TLane, TNumber> right)
    {
        return new Vector3<TLane, TNumber>
        {
            x = left.x <= right.x,
            y = left.y <= right.y,
            z = left.z <= right.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator <=(in Vector3<TLane, TNumber> vector, TLane lane)
    {
        return new Vector3<TLane, TNumber>
        {
            x = vector.x <= lane,
            y = vector.y <= lane,
            z = vector.z <= lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TLane, TNumber> operator <=(TLane lane, in Vector3<TLane, TNumber> vector)
    {
        return new Vector3<TLane, TNumber>
        {
            x = lane <= vector.x,
            y = lane <= vector.y,
            z = lane <= vector.z,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3<TLane, TNumber> other)
    {
        return this.x.Equals(other.x) && this.y.Equals(other.y) && this.z.Equals(other.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals(object? obj)
    {
        return obj is Vector3<TLane, TNumber> other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(x);
        hash.Add(y);
        hash.Add(z);
        return hash.ToHashCode();
    }
}
