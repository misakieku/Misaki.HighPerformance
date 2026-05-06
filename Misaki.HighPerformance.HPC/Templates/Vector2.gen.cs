
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.HPC;

public unsafe struct Vector2<TLane, TNumber> : IEquatable<Vector2<TLane, TNumber>>
    where TLane : ISPMDLane<TLane, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    public TLane x;
    public TLane y;

    public static Vector2<TLane, TNumber> Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new Vector2<TLane, TNumber>
            {
                x = TLane.Zero,
                y = TLane.Zero,
            };
        }
    }

    public static Vector2<TLane, TNumber> One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new Vector2<TLane, TNumber>
            {
                x = TLane.One,
                y = TLane.One,
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
        if (index < 0 || index >= 2)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range for Vector2.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(TNumber* pDst)
    {
        x.Store(pDst + 0 * TLane.LaneWidth);
        y.Store(pDst + 1 * TLane.LaneWidth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber dst)
    {
        x.Store(ref Unsafe.Add(ref dst, 0 * TLane.LaneWidth));
        y.Store(ref Unsafe.Add(ref dst, 1 * TLane.LaneWidth));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(TNumber* px, TNumber* py)
    {
        x.Store(px);
        y.Store(py);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber x, ref TNumber y)
    {
        this.x.Store(ref x);
        this.y.Store(ref y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CompressStore(TNumber* pDst, Vector2<TLane, TNumber> mask)
    {
        x.CompressStore(pDst + 0 * TLane.LaneWidth, mask.x);
        y.CompressStore(pDst + 1 * TLane.LaneWidth, mask.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CompressStore(ref TNumber dst, Vector2<TLane, TNumber> mask)
    {
        x.CompressStore(ref Unsafe.Add(ref dst, 0 * TLane.LaneWidth), mask.x);
        y.CompressStore(ref Unsafe.Add(ref dst, 1 * TLane.LaneWidth), mask.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(TNumber* pDst, TLane indices)
    {
        x.Scatter(pDst + 0, indices);
        y.Scatter(pDst + 1, indices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(TNumber* pDst, int* pIndices)
    {
        x.Scatter(pDst + 0, pIndices);
        y.Scatter(pDst + 1, pIndices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(ref TNumber dst, TLane indices)
    {
        x.Scatter(ref Unsafe.Add(ref dst, 0), indices);
        y.Scatter(ref Unsafe.Add(ref dst, 1), indices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scatter(ref TNumber dst, int* pIndices)
    {
        x.Scatter(ref Unsafe.Add(ref dst, 0), pIndices);
        y.Scatter(ref Unsafe.Add(ref dst, 1), pIndices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(TNumber* pDst, TLane indices, TLane mask)
    {
        x.MaskScatter(pDst + 0, indices, mask);
        y.MaskScatter(pDst + 1, indices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(TNumber* pDst, int* pIndices, TLane mask)
    {
        x.MaskScatter(pDst + 0, pIndices, mask);
        y.MaskScatter(pDst + 1, pIndices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(ref TNumber dst, TLane indices, TLane mask)
    {
        x.MaskScatter(ref Unsafe.Add(ref dst, 0), indices, mask);
        y.MaskScatter(ref Unsafe.Add(ref dst, 1), indices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MaskScatter(ref TNumber dst, int* pIndices, TLane mask)
    {
        x.MaskScatter(ref Unsafe.Add(ref dst, 0), pIndices, mask);
        y.MaskScatter(ref Unsafe.Add(ref dst, 1), pIndices, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator -(in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = -vector.x,
            y = -vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator +(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x + right.x,
            y = left.y + right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator +(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x + lane,
            y = vector.y + lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator +(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane + vector.x,
            y = lane + vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator -(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x - right.x,
            y = left.y - right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator -(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x - lane,
            y = vector.y - lane,
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator -(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane - vector.x,
            y = lane - vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator *(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x * right.x,
            y = left.y * right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator *(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x * lane,
            y = vector.y * lane,
        };
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator *(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane * vector.x,
            y = lane * vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator /(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x / right.x,
            y = left.y / right.y,
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator /(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x / lane,
            y = vector.y / lane,
        };
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator /(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane / vector.x,
            y = lane / vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator ==(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x == right.x,
            y = left.y == right.y,
        };
    }

        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator ==(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x == lane,
            y = vector.y == lane,
        };
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator ==(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane == vector.x,
            y = lane == vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator !=(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x != right.x,
            y = left.y != right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator !=(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x != lane,
            y = vector.y != lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator !=(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane != vector.x,
            y = lane != vector.y,
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator >(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x > right.x,
            y = left.y > right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator >(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x > lane,
            y = vector.y > lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator >(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane > vector.x,
            y = lane > vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator >=(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x >= right.x,
            y = left.y >= right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator >=(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x >= lane,
            y = vector.y >= lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator >=(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane >= vector.x,
            y = lane >= vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator <(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x < right.x,
            y = left.y < right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator <(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x < lane,
            y = vector.y < lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator <(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane < vector.x,
            y = lane < vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator <=(in Vector2<TLane, TNumber> left, in Vector2<TLane, TNumber> right)
    {
        return new Vector2<TLane, TNumber>
        {
            x = left.x <= right.x,
            y = left.y <= right.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator <=(in Vector2<TLane, TNumber> vector, TLane lane)
    {
        return new Vector2<TLane, TNumber>
        {
            x = vector.x <= lane,
            y = vector.y <= lane,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TLane, TNumber> operator <=(TLane lane, in Vector2<TLane, TNumber> vector)
    {
        return new Vector2<TLane, TNumber>
        {
            x = lane <= vector.x,
            y = lane <= vector.y,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2<TLane, TNumber> other)
    {
        return this.x.Equals(other.x) && this.y.Equals(other.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals(object? obj)
    {
        return obj is Vector2<TLane, TNumber> other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(x);
        hash.Add(y);
        return hash.ToHashCode();
    }
}
