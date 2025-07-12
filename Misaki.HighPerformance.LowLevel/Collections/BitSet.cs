using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Collections;

public sealed class BitSet
{
    private const int _BIT_SIZE = sizeof(uint) * 8 - 1;           // 31
    private const int _INDEX_SIZE = 5;                              // log_2(BitSize + 1)
    private const int _MASK = (1 << 5) - 1;                         // 0x1F, the mask to get the bit index inside a uint
    private static readonly int s_padding = Vector<uint>.Count;      // The padding used for vectorization, the amount of uints required for being vectorized basically

    /// <summary>
    /// Determines the required length of an <see cref="BitSet"/> to hold the passed id or bit.
    /// </summary>
    /// <param name="id">The id or bit.</param>
    /// <returns>A size of required <see cref="uint"/>s for the bitset.</returns>
    public static int RequiredLength(int id)
    {
        return (id >> 5) + int.Sign(id & _BIT_SIZE);
    }

    /// <summary>
    /// Rounds the given length to the next padding size.
    /// </summary>
    /// <param name="length">The length to round.</param>
    /// <returns>The rounded length.</returns>
    public static int RoundToPadding(int length)
    {
        return (length + s_padding - 1) / s_padding * s_padding;
    }

    /// <summary>
    ///     The bits from the bitset.
    /// </summary>
    private uint[] _bits;

    /// <summary>
    ///     The highest bit set.
    /// </summary>
    private int _highestBit;

    /// <summary>
    ///     The maximum <see cref="_bits"/>-index current in use.
    /// </summary>
    private int _max;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public BitSet()
    {
        _bits = new uint[s_padding];
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public BitSet(int minimalLength)
    {
        var uints = (minimalLength >> _INDEX_SIZE) + int.Sign(minimalLength & _BIT_SIZE);
        var length = RoundToPadding(uints);
        _bits = new uint[length];
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public BitSet(params Span<uint> bits)
    {
        _bits = bits.ToArray();
        _highestBit = 0;
        _max = _bits.Length * (_BIT_SIZE + 1) - 1; // Calculate the maximum index in use
        for (var i = 0; i < _bits.Length; i++)
        {
            if (_bits[i] != 0)
            {
                _highestBit = Math.Max(_highestBit, i * (_BIT_SIZE + 1) + BitOperations.Log2(_bits[i]) + 1);
            }
        }
    }

    /// <summary>
    ///     The highest uint index in use inside the <see cref="_bits"/>-array.
    /// </summary>
    public int HighestIndex
    {
        get => _max;
    }

    /// <summary>
    ///     The highest bit set.
    /// </summary>
    public int HighestBit
    {
        get => _highestBit;
    }

    /// <summary>
    ///     Returns the length of the bitset, how many ints it consists of.
    /// </summary>
    public int Length
    {
        get => _bits.Length;
    }

    /// <summary>
    ///     Checks whether a bit is set at the index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>True if it is, otherwise false</returns>
    public bool IsSet(int index)
    {
        var b = index >> _INDEX_SIZE;
        if (b >= _bits.Length)
        {
            return false;
        }

        return (_bits[b] & 1 << (index & _BIT_SIZE)) != 0;
    }

    /// <summary>
    ///     Sets a bit at the given index.
    ///     Resizes its internal array if necessary.
    /// </summary>
    /// <param name="index">The index.</param>
    public void SetBit(int index)
    {
        var b = index >> _INDEX_SIZE;
        if (b >= _bits.Length)
        {
            Array.Resize(ref _bits, RoundToPadding(b));
        }

        // Track highest set bit
        _highestBit = Math.Max(_highestBit, index);
        _max = _highestBit / (_BIT_SIZE + 1) + 1;
        _bits[b] |= 1u << (index & _BIT_SIZE);
    }

    /// <summary>
    ///     Clears the bit at the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    public void ClearBit(int index)
    {
        var b = index >> _INDEX_SIZE;
        if (b >= _bits.Length)
        {
            return;
        }

        _bits[b] &= ~(1u << (index & _BIT_SIZE));
    }

    /// <summary>
    ///     Sets all bits.
    /// </summary>
    public void SetAll()
    {
        var count = _bits.Length;
        for (var i = 0; i < count; i++)
        {
            _bits[i] = 0xffffffff;
        }

        _highestBit = _bits.Length * (_BIT_SIZE + 1) - 1;
        _max = _highestBit / (_BIT_SIZE + 1) + 1;
    }

    /// <summary>
    ///     Clears all set bits.
    /// </summary>
    public void ClearAll()
    {
        Array.Clear(_bits);
        _highestBit = 0;
        _max = 0;
    }

    /// <summary>
    /// Finds the next set bit at or after `startIndex`, or -1 if none.
    /// </summary>
    public int NextSetBit(int startIndex)
    {
        var wordIndex = startIndex >> _BIT_SIZE;
        if (wordIndex >= _bits.Length)
        {
            return -1;
        }

        // Mask off bits below startIndex in the first word:
        var word = _bits[wordIndex] & ~0u << (startIndex & _MASK);
        while (true)
        {
            if (word != 0)
            {
                // get the least-significant set bit
                var bit = BitOperations.TrailingZeroCount(word);
                return (wordIndex << _BIT_SIZE) + bit;
            }

            wordIndex++;
            if (wordIndex >= _bits.Length)
            {
                return -1;
            }

            word = _bits[wordIndex];
        }
    }

    /// <summary>
    ///     Checks if all bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if they match, false if not.</returns>
    [SkipLocalsInit]
    public bool All(BitSet other)
    {
        var min = Math.Min(Math.Min(Length, other.Length), _max);
        if (!Vector.IsHardwareAccelerated || min < s_padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise and
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit & otherBits[i]) != bit)
                {
                    return false;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i++)
            {
                if (bits[i] != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise and
            for (var i = 0; i < min; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.BitwiseAnd(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, vector))
                {
                    return false;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                if (!Vector.EqualsAll(vector, Vector<uint>.Zero)) // Vectors are not zero bits[0] != 0 basically
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if any bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if they match, false if not.</returns>
    public bool Any(BitSet other)
    {
        var min = Math.Min(Math.Min(Length, other.Length), _max);
        if (!Vector.IsHardwareAccelerated || min < s_padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise and, return true since any is met
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit & otherBits[i]) > 0)
                {
                    return true;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i++)
            {
                if (bits[i] > 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise and, return true since any is met
            for (var i = 0; i < min; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.BitwiseAnd(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, Vector<uint>.Zero))
                {
                    return true;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                if (!Vector.EqualsAll(vector, Vector<uint>.Zero)) // Vectors are not zero bits[0] != 0 basically
                {
                    return false;
                }
            }
        }

        return _highestBit <= 0;
    }

    /// <summary>
    ///     Checks if none bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if none match, false if not.</returns>
    public bool None(BitSet other)
    {
        var min = Math.Min(Math.Min(Length, other.Length), _max);
        if (!Vector.IsHardwareAccelerated || min < s_padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise and, return true since any is met
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit & otherBits[i]) != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise and, return true since any is met
            for (var i = 0; i < min; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.BitwiseAnd(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, Vector<uint>.Zero))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if exactly all bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if they match, false if not.</returns>
    public bool Exclusive(BitSet other)
    {
        var min = Math.Min(Math.Min(Length, other.Length), _max);

        if (!Vector.IsHardwareAccelerated || min < s_padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise xor, if both are not totally equal, return false
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit ^ otherBits[i]) != 0)
                {
                    return false;
                }
            }

            // handle extra bits on our side that might just be all zero
            for (var i = min; i < _max; i++)
            {
                if (bits[i] != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise xor, return true since any is met
            for (var i = 0; i < min; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.Xor(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, Vector<uint>.Zero))
                {
                    return false;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i += s_padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                if (!Vector.EqualsAll(vector, Vector<uint>.Zero)) // Vectors are not zero bits[0] != 0 basically
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static BitSet operator &(BitSet left, BitSet right)
    {
        var min = Math.Min(left.Length, right.Length);
        var result = new BitSet(min);
        if (!Vector.IsHardwareAccelerated || min < s_padding)
        {
            for (var i = 0; i < min; i++)
            {
                result._bits[i] = left._bits[i] & right._bits[i];
            }
        }
        else
        {
            for (var i = 0; i < min; i += s_padding)
            {
                var vectorLeft = new Vector<uint>(left._bits.AsSpan()[i..]);
                var vectorRight = new Vector<uint>(right._bits.AsSpan()[i..]);
                var resultVector = Vector.BitwiseAnd(vectorLeft, vectorRight);
                resultVector.CopyTo(result._bits.AsSpan(i, s_padding));
            }
        }
        return result;
    }

    public static BitSet operator |(BitSet left, BitSet right)
    {
        var min = Math.Min(left.Length, right.Length);
        var result = new BitSet(min);
        if (!Vector.IsHardwareAccelerated || min < s_padding)
        {
            for (var i = 0; i < min; i++)
            {
                result._bits[i] = left._bits[i] | right._bits[i];
            }
        }
        else
        {
            for (var i = 0; i < min; i += s_padding)
            {
                var vectorLeft = new Vector<uint>(left._bits.AsSpan()[i..]);
                var vectorRight = new Vector<uint>(right._bits.AsSpan()[i..]);
                var resultVector = Vector.BitwiseOr(vectorLeft, vectorRight);
                resultVector.CopyTo(result._bits.AsSpan(i, s_padding));
            }
        }
        return result;
    }

    public static BitSet operator ~(BitSet bitSet)
    {
        if (!Vector.IsHardwareAccelerated || bitSet.Length < s_padding)
        {
            for (var i = 0; i < bitSet.Length; i++)
            {
                bitSet._bits[i] = ~bitSet._bits[i];
            }
        }
        else
        {
            for (var i = 0; i < bitSet.Length; i += s_padding)
            {
                var vector = new Vector<uint>(bitSet._bits.AsSpan()[i..]);
                var resultVector = ~vector;
                resultVector.CopyTo(bitSet._bits.AsSpan(i, s_padding));
            }
        }

        return bitSet;
    }

    /// <summary>
    ///     Creates a <see cref="Span{T}"/> to access the <see cref="_bits"/>.
    /// </summary>
    /// <returns>The hash.</returns>
    public Span<uint> AsSpan()
    {
        var max = _highestBit / (_BIT_SIZE + 1) + 1;
        return _bits.AsSpan()[..max];
    }

    /// <summary>
    ///     Copies the bits into a <see cref="Span{T}"/> and returns a slice containing the copied <see cref="_bits"/>.
    /// </summary>
    /// <param name="span">The <see cref="Span{T}"/> to copy into.</param>
    /// <param name="zero">If true, it will zero the unused space from the <see cref="span"/>.</param>
    /// <returns>The <see cref="Span{T}"/>.</returns>
    public Span<uint> AsSpan(Span<uint> span, bool zero = true)
    {
        // Copy everything thats possible from one to another
        var length = Math.Min(Length, span.Length);
        for (var index = 0; index < length; index++)
        {
            span[index] = _bits[index];
        }

        // Zero the rest space which was not overriden due to the copy.
        for (var index = length; zero && index < span.Length; index++)
        {
            span[index] = 0;
        }

        return span[..Length];
    }

    /// <summary>
    ///     Prints the content of this instance.
    /// </summary>
    /// <returns>The string.</returns>
    public override string ToString()
    {
        // Convert uint to binary form for pretty printing
        var binaryBuilder = new StringBuilder();
        foreach (var bit in _bits)
        {
            binaryBuilder.Append(Convert.ToString(bit, 2).PadLeft(32, '0')).Append(',');
        }
        binaryBuilder.Length--;

        return $"{nameof(_bits)}: {binaryBuilder}, {nameof(Length)}: {Length}";
    }
}

/// <summary>
///     The <see cref="SpanBitSet"/> struct
///     represents a non resizable collection of bits.
///     Used to set, check and clear bits on a allocated <see cref="BitSet"/> or on the stack.
/// </summary>
public readonly ref struct SpanBitSet
{
    private const int _BIT_SIZE = sizeof(uint) * 8 - 1; // 31
    // NOTE: Is a byte not 8 bits?
    private const int _BYTE_SIZE = 5; // log_2(BitSize + 1)

    /// <summary>
    ///     The bits from the bitset.
    /// </summary>
    private readonly Span<uint> _bits;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public SpanBitSet(Span<uint> bits)
    {
        _bits = bits;
    }

    /// <summary>
    ///     Checks whether a bit is set at the index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>True if it is, otherwise false</returns>

    public bool IsSet(int index)
    {
        var b = index >> _BYTE_SIZE;
        if (b >= _bits.Length)
        {
            return false;
        }

        return (_bits[b] & 1 << (index & _BIT_SIZE)) != 0;
    }

    /// <summary>
    ///     Sets a bit at the given index.
    ///     Resizes its internal array if necessary.
    /// </summary>
    /// <param name="index">The index.</param>

    public void SetBit(int index)
    {
        var b = index >> _BYTE_SIZE;
        if (b >= _bits.Length)
        {
            return;
        }

        _bits[b] |= 1u << (index & _BIT_SIZE);
    }

    /// <summary>
    ///     Clears the bit at the given index.
    /// </summary>
    /// <param name="index">The index.</param>

    public void ClearBit(int index)
    {
        var b = index >> _BYTE_SIZE;
        if (b >= _bits.Length)
        {
            return;
        }

        _bits[b] &= ~(1u << (index & _BIT_SIZE));
    }

    /// <summary>
    ///
    /// </summary>

    public void SetAll()
    {
        var count = _bits.Length;
        for (var i = 0; i < count; i++)
        {
            _bits[i] = 0xffffffff;
        }
    }

    /// <summary>
    ///     Clears all set bits.
    /// </summary>

    public void ClearAll()
    {
        _bits.Clear();
    }

    /// <summary>
    ///     Creates a <see cref="Span{T}"/> to access the <see cref="_bits"/>.
    /// </summary>
    /// <returns>The hash.</returns>

    public Span<uint> AsSpan()
    {
        return _bits;
    }

    /// <summary>
    ///     Copies the bits into a <see cref="Span{T}"/> and returns a slice containing the copied <see cref="_bits"/>.
    /// </summary>
    /// <param name=""></param>
    /// <returns>The hash.</returns>

    public Span<uint> AsSpan(Span<uint> span, bool zero = true)
    {
        // Prevent exception because target array is to small for copy operation
        var length = Math.Min(_bits.Length, span.Length);
        for (var index = 0; index < length; index++)
        {
            span[index] = _bits[index];
        }

        // Zero the rest space which was not overriden due to the copy.
        for (var index = length; zero && index < span.Length; index++)
        {
            span[index] = 0;
        }

        return span[.._bits.Length];
    }

    /// <summary>
    ///     Prints the content of this instance.
    /// </summary>
    /// <returns>The string.</returns>

    public override string ToString()
    {
        // Convert uint to binary form for pretty printing
        var binaryBuilder = new StringBuilder();
        foreach (var bit in _bits)
        {
            binaryBuilder.Append(Convert.ToString(bit, 2).PadLeft(32, '0')).Append(',');
        }
        binaryBuilder.Length--;

        return $"{nameof(_bits)}: {string.Join(",", binaryBuilder)}";
    }
}