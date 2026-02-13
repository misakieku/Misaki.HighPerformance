
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics.SPMD;

public unsafe struct Vector2<TLane, TNumber> : IEquatable<Vector2<TLane, TNumber>>
    where TLane : ISPMD<TLane, TNumber>
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
    [Conditional("ENABLE_COLLECTION_CHECKS")]
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
        var width = TLane.LaneWidth;

        var x = stackalloc TNumber[width];
        var y = stackalloc TNumber[width];

        this.x.Store(x);
        this.y.Store(y);

        for (var i = 0; i < width; i++)
        {
            pDst[i * 2 + 0] = x[i];
            pDst[i * 2 + 1] = y[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber dst)
    {
        Store((TNumber*)Unsafe.AsPointer(ref dst));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(TNumber* px,  TNumber* py)
    {
        x.Store(px);
        y.Store(py);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref TNumber x,  ref TNumber y)
    {
        this.x.Store(ref x);
        this.y.Store(ref y);
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
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
