using System.Numerics;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Misaki.HighPerformance.Mathematics.SPMD;

/// <summary>
/// Common marker interface for SPMD lane types.
/// </summary>
public interface ISPMD
{
    /// <summary>
    /// Gets the number of lanes (vector width) for the SPMD implementation.
    /// </summary>
    static abstract int LaneWidth
    {
        get;
    }
}

/// <summary>
/// Represents a single-lane or multi-lane (vectorized) SPMD value and the operations supported on it.
/// </summary>
/// <typeparam name="TSelf">The concrete SPMD lane type implementing this interface.</typeparam>
/// <typeparam name="TNumber">The underlying numeric element type.</typeparam>
public interface ISPMD<TSelf, TNumber> : ISPMD
    where TSelf : ISPMD<TSelf, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
{
    /// <summary>
    /// Gets a lane value where all lanes are set to numeric zero.
    /// </summary>
    static abstract TSelf Zero
    {
        get;
    }

    /// <summary>
    /// Gets a lane value where all lanes are set to numeric one.
    /// </summary>
    static abstract TSelf One
    {
        get;
    }

    /// <summary>
    /// Gets a lane value where all lanes are set to the minimum representable value of the underlying numeric type.
    /// </summary>
    static abstract TSelf MinValue
    {
        get;
    }

    /// <summary>
    /// Gets a lane value where all lanes are set to the maximum representable value of the underlying numeric type.
    /// </summary>
    static abstract TSelf MaxValue
    {
        get;
    }

    /// <summary>
    /// Gets the element value for the specified lane index.
    /// </summary>
    /// <param name="index">The zero-based lane index.</param>
    TNumber this[int index]
    {
        get;
    }

    /// <summary>
    /// Creates a lane value where all lanes are set to the specified value.
    /// </summary>
    /// <param name="value">The value to set for all lanes.</param>
    /// <returns>The created lane value.</returns>
    static abstract TSelf Create(TNumber value);
    /// <summary>
    /// Creates a new instance of the type from the specified sequence of numeric values.
    /// </summary>
    /// <param name="values">A parameter array of read-only spans containing the numeric values to use for initialization.</param>
    /// <returns>A new instance of the type initialized with the provided numeric values.</returns>
    static abstract TSelf Create(params ReadOnlySpan<TNumber> values);
    /// <summary>
    /// Creates a lane value from the specified vector.
    /// </summary>
    /// <param name="value">The vector to create the lane value from.</param>
    /// <returns>The lane value built from the vector.</returns>
    static abstract TSelf Create(Vector<TNumber> value);
    /// <summary>
    /// Creates a lane value with a sequence starting from the specified value with the given step.
    /// </summary>
    /// <param name="start">The starting value.</param>
    /// <param name="step">The step value for the sequence.</param>
    /// <returns>The lane value containing the arithmetic sequence.</returns>
    /// <remarks>
    /// Implementations may rely on vector creation helpers and assume that the resulting sequence length matches <see cref="LaneWidth"/>.
    /// </remarks>
    static abstract TSelf Sequence(TNumber start, TNumber step);
    /// <summary>
    /// Loads a lane value from the specified reference.
    /// </summary>
    /// <param name="value">The reference to load from.</param>
    /// <returns>The loaded lane value.</returns>
    static abstract TSelf Load(ref TNumber value);
    /// <summary>
    /// Loads a lane value from the specified pointer.
    /// </summary>
    /// <param name="pValue">The pointer to load from.</param>
    /// <returns>The loaded lane value.</returns>
    /// <remarks>
    /// Unsafe pointer overloads are provided for scenarios where sequential lane data is already contiguous in memory.
    /// </remarks>
    static abstract unsafe TSelf Load(TNumber* pValue);

    /// <summary>
    /// Stores the lane value to the specified reference.
    /// </summary>
    /// <param name="destination">The reference to store to.</param>
    void Store(ref TNumber destination);
    /// <summary>
    /// Stores the lane value to the specified pointer.
    /// </summary>
    /// <param name="pDestination">The pointer to store to.</param>
    unsafe void Store(TNumber* pDestination);
    /// <summary>
    /// Compresses the data specified by the given mask and stores the compressed result in the provided destination
    /// variable.
    /// </summary>
    /// <param name="mask">A mask value that determines which elements are included in the compression operation.</param>
    /// <param name="destination">A reference to the variable where the compressed data will be stored.</param>
    /// <returns>The number of elements written to the destination as a result of the compression. Returns 0 if no elements are compressed.</returns>
    /// <remarks>
    /// Implementations may use hardware-specific shuffle tables to reorder the selected lanes before storing, falling back to a scalar loop otherwise.
    /// </remarks>
    int CompressStore(TSelf mask, ref TNumber destination);
    /// <summary>
    /// Compresses the data specified by the given mask and stores the compressed result in the provided destination
    /// variable.
    /// </summary>
    /// <param name="mask">A mask value that determines which elements are included in the compression operation.</param>
    /// <param name="pDestination">A pointer to the variable where the compressed data will be stored.</param>
    /// <returns>The number of elements written to the destination as a result of the compression. Returns 0 if no elements are compressed.</returns>
    /// <remarks>
    /// Implementations may use hardware-specific shuffle tables to reorder the selected lanes before storing, falling back to a scalar loop otherwise.
    /// </remarks>
    unsafe int CompressStore(TSelf mask, TNumber* pDestination);

    /// <summary>
    /// Converts the lane value to a vector.
    /// </summary>
    /// <returns>The backing vector representation.</returns>
    Vector<TNumber> AsVector();

    /// <summary>
    /// Adds two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise sum.</returns>
    static abstract TSelf operator +(TSelf a, TSelf b);
    /// <summary>
    /// Adds a lane value and a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The lane value with the scalar added to each element.</returns>
    static abstract TSelf operator +(TSelf a, TNumber b);
    /// <summary>
    /// Subtracts two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise difference.</returns>
    static abstract TSelf operator -(TSelf a, TSelf b);
    /// <summary>
    /// Subtracts a scalar from a lane value element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The lane value with the scalar subtracted from each element.</returns>
    static abstract TSelf operator -(TSelf a, TNumber b);
    /// <summary>
    /// Multiplies two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise product.</returns>
    static abstract TSelf operator *(TSelf a, TSelf b);
    /// <summary>
    /// Multiplies a lane value by a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The lane value scaled by the scalar.</returns>
    static abstract TSelf operator *(TSelf a, TNumber b);
    /// <summary>
    /// Divides two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise quotient.</returns>
    static abstract TSelf operator /(TSelf a, TSelf b);
    /// <summary>
    /// Divides a lane value by a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The lane value divided by the scalar.</returns>
    static abstract TSelf operator /(TSelf a, TNumber b);
    /// <summary>
    /// Computes the modulus of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise modulus.</returns>
    static abstract TSelf operator %(TSelf a, TSelf b);
    /// <summary>
    /// Computes the modulus of a lane value and a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The lane value modulus scalar.</returns>
    static abstract TSelf operator %(TSelf a, TNumber b);

    /// <summary>
    /// Negates the lane value element-wise.
    /// </summary>
    /// <param name="a">The lane value to negate.</param>
    /// <returns>The negated lane value.</returns>
    static abstract TSelf operator -(TSelf a);

    /// <summary>
    /// Computes the bitwise AND of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The result of the bitwise AND.</returns>
    static abstract TSelf operator &(TSelf a, TSelf b);
    /// <summary>
    /// Computes the bitwise AND of a lane value and a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The result of the bitwise AND.</returns>
    static abstract TSelf operator &(TSelf a, TNumber b);
    /// <summary>
    /// Computes the bitwise OR of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The result of the bitwise OR.</returns>
    static abstract TSelf operator |(TSelf a, TSelf b);
    /// <summary>
    /// Computes the bitwise OR of a lane value and a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The result of the bitwise OR.</returns>
    static abstract TSelf operator |(TSelf a, TNumber b);
    /// <summary>
    /// Computes the bitwise XOR of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The result of the bitwise XOR.</returns>
    static abstract TSelf operator ^(TSelf a, TSelf b);
    /// <summary>
    /// Computes the bitwise XOR of a lane value and a scalar element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>The result of the bitwise XOR.</returns>
    static abstract TSelf operator ^(TSelf a, TNumber b);
    /// <summary>
    /// Computes the bitwise NOT of a lane value element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <returns>The bitwise complement of the lane value.</returns>
    static abstract TSelf operator ~(TSelf a);

    /// <summary>
    /// Computes the absolute value of the lane value element-wise.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The absolute lane value.</returns>
    static abstract TSelf Abs(TSelf value);
    /// <summary>
    /// Computes the floor of the lane value element-wise.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The lane value with each element rounded toward negative infinity.</returns>
    static abstract TSelf Floor(TSelf value);
    /// <summary>
    /// Computes the fractional part of the lane value element-wise.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The fractional lane value.</returns>
    static abstract TSelf Frac(TSelf value);
    /// <summary>
    /// Computes the square root of the lane value element-wise.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The square root lane value.</returns>
    static abstract TSelf Sqrt(TSelf value);
    /// <summary>
    /// Performs linear interpolation between two lane values.
    /// </summary>
    /// <param name="a">The start lane value.</param>
    /// <param name="b">The end lane value.</param>
    /// <param name="t">The interpolation factor.</param>
    /// <returns>The interpolated lane value.</returns>
    static abstract TSelf Lerp(TSelf a, TSelf b, TSelf t);
    /// <summary>
    /// Computes a * b + c element-wise.
    /// </summary>
    /// <param name="a">The first multiplier.</param>
    /// <param name="b">The second multiplier.</param>
    /// <param name="c">The addend.</param>
    /// <returns>The result of the fused multiply-add operation.</returns>
    /// <remarks>
    /// Float and double implementations should use fused multiply-add instructions when available for both accuracy and performance.
    /// </remarks>
    static abstract TSelf MultipleAdd(TSelf a, TSelf b, TSelf c);
    /// <summary>
    /// Returns the minimum of the two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane value containing the minimum of each element.</returns>
    static abstract TSelf Min(TSelf a, TSelf b);
    /// <summary>
    /// Returns the maximum of the two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane value containing the maximum of each element.</returns>
    static abstract TSelf Max(TSelf a, TSelf b);
    /// <summary>
    /// Clamps each element of the lane value between the specified minimum and maximum values.
    /// </summary>
    /// <param name="value">The lane value to clamp.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="max">The inclusive maximum.</param>
    /// <returns>The clamped lane value.</returns>
    static abstract TSelf Clamp(TSelf value, TSelf min, TSelf max);
    /// <summary>
    /// Saturates each element in the lane value to the 0..1 range.
    /// </summary>
    /// <param name="value">The lane value to saturate.</param>
    /// <returns>The saturated lane value.</returns>
    static abstract TSelf Saturate(TSelf value);
    /// <summary>
    /// Computes the sine of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The sine of each lane element.</returns>
    /// <remarks>
    /// Implementations may rely on vectorized math intrinsics for float/double and approximate values for other types.
    /// </remarks>
    static abstract TSelf Sin(TSelf value);
    /// <summary>
    /// Computes the cosine of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The cosine of each lane element.</returns>
    /// <remarks>
    /// Implementations may rely on vectorized math intrinsics for float/double and approximate values for other types.
    /// </remarks>
    static abstract TSelf Cos(TSelf value);
    /// <summary>
    /// Computes both sine and cosine of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>A tuple containing sine and cosine lane values.</returns>
    /// <remarks>
    /// Implementations returning both sin and cos simultaneously can reuse intermediate values for better performance.
    /// </remarks>
    static abstract (TSelf sin, TSelf cos) SinCos(TSelf value);
    /// <summary>
    /// Computes the tangent of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The tangent of each lane element.</returns>
    /// <remarks>
    /// Many implementations use polynomial approximations and assume the input is reduced to [-pi/4, pi/4] for accuracy.
    /// </remarks>
    static abstract TSelf Tan(TSelf value);
    /// <summary>
    /// Computes the arcsine of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The arcsine of each lane element.</returns>
    /// <remarks>
    /// Implementations typically assume input is within [-1, 1] and may use polynomial approximations for performance.
    /// </remarks>
    static abstract TSelf Asin(TSelf value);
    /// <summary>
    /// Computes the arccosine of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The arccosine of each lane element.</returns>
    /// <remarks>
    /// Input is expected to be in [-1, 1]; implementations often rely on approximation polynomials combined with range reduction.
    /// </remarks>
    static abstract TSelf Acos(TSelf value);
    /// <summary>
    /// Computes the arctangent of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The arctangent of each lane element.</returns>
    /// <remarks>
    /// Polynomial approximations with restricted input ranges are commonly used for performance-sensitive implementations.
    /// </remarks>
    static abstract TSelf Atan(TSelf value);
    /// <summary>
    /// Computes the arctangent of y/x for each lane element.
    /// </summary>
    /// <param name="y">The numerator lane value.</param>
    /// <param name="x">The denominator lane value.</param>
    /// <returns>The arctangent of each lane pair.</returns>
    /// <remarks>
    /// Implementations often rely on quadrant-aware polynomial routines and assume inputs are finite to avoid NaNs.
    /// </remarks>
    static abstract TSelf Atan2(TSelf y, TSelf x);
    /// <summary>
    /// Raises each lane element to the specified power.
    /// </summary>
    /// <param name="x">The base lane value.</param>
    /// <param name="y">The exponent lane value. Cannot be negative.</param>
    /// <returns>The power result for each lane.</returns>
    static abstract TSelf Pow(TSelf x, TSelf y);
    /// <summary>
    /// Computes the exponential of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The exponential of each lane element.</returns>
    /// <remarks>
    /// Float and double implementations typically call into vectorized exp intrinsics; other types may fall back to scalar paths.
    /// </remarks>
    static abstract TSelf Exp(TSelf value);
    /// <summary>
    /// Computes 2 raised to each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The base-2 exponential of each lane element.</returns>
    /// <remarks>
    /// This can be implemented via <see cref="Exp(TSelf)"/> when no dedicated base-2 intrinsic exists.
    /// </remarks>
    static abstract TSelf Exp2(TSelf value);
    /// <summary>
    /// Computes the natural logarithm of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The natural logarithm of each lane element.</returns>
    /// <remarks>
    /// Vectorized logarithm instructions may only exist for floating-point types; other types should mimic the scalar behavior.
    /// </remarks>
    static abstract TSelf Log(TSelf value);
    /// <summary>
    /// Computes the base-2 logarithm of each lane element.
    /// </summary>
    /// <param name="value">The source lane value.</param>
    /// <returns>The base-2 logarithm of each lane element.</returns>
    /// <remarks>
    /// If a dedicated base-2 intrinsic is unavailable, the implementation may compute <c>Log(value)/Log(2)</c>.
    /// </remarks>
    static abstract TSelf Log2(TSelf value);
    /// <summary>
    /// Computes the ceiling of each lane element.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The smallest integral value greater than or equal to each element.</returns>
    /// <remarks>
    /// Implementations should use <see cref="Vector"/> helpers for floating-point types when available.
    /// </remarks>
    static abstract TSelf Ceil(TSelf value);
    /// <summary>
    /// Rounds each lane element to the nearest integer value.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The rounded lane value.</returns>
    /// <remarks>
    /// Implementations should prefer vectorized round intrinsics for floating-point implementations.
    /// </remarks>
    static abstract TSelf Round(TSelf value);
    /// <summary>
    /// Truncates each lane element toward zero.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The truncated lane value.</returns>
    /// <remarks>
    /// Floating-point truncation typically maps to <see cref="Vector.Truncate(Vector{TNumber})"/>.
    /// </remarks>
    static abstract TSelf Trunc(TSelf value);
    /// <summary>
    /// Returns the sign of each lane element.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>-1, 0, or 1 per lane.</returns>
    static abstract TSelf Sign(TSelf value);
    /// <summary>
    /// Copies the sign of the second lane value to the magnitude of the first.
    /// </summary>
    /// <param name="magnitude">The magnitude lane value.</param>
    /// <param name="sign">The sign lane value.</param>
    /// <returns>The result of merging magnitude with sign.</returns>
    static abstract TSelf CopySign(TSelf magnitude, TSelf sign);
    /// <summary>
    /// Computes the reciprocal of each lane element.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The reciprocal lane value.</returns>
    /// <remarks>
    /// Fast paths may use <c>Sse.Reciprocal</c> or <c>Avx.Reciprocal</c> when <c>TNumber</c> is <c>float</c>.
    /// </remarks>
    static abstract TSelf Rcp(TSelf value);
    /// <summary>
    /// Computes the reciprocal square root of each lane element.
    /// </summary>
    /// <param name="x">The lane value.</param>
    /// <returns>The reciprocal square root lane value.</returns>
    /// <remarks>
    /// Float implementations may prefer hardware reciprocal-sqrt intrinsics and fallback to <c>Create(TNumber.One)/Sqrt(x)</c> otherwise.
    /// </remarks>
    static abstract TSelf Rsqrt(TSelf value);

    /// <summary>
    /// Selects values from two lane values based on a condition mask.
    /// </summary>
    /// <param name="conditionMask">The condition mask.</param>
    /// <param name="ifTrue">The value to select if true.</param>
    /// <param name="ifFalse">The value to select if false.</param>
    /// <returns>The selected lane value.</returns>
    static abstract TSelf Select(TSelf conditionMask, TSelf ifTrue, TSelf ifFalse);
    /// <summary>
    /// Compares two lane values for greater than element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The mask representing the greater than comparison result.</returns>
    static abstract TSelf GreaterThan(TSelf a, TSelf b);
    /// <summary>
    /// Compares two lane values for greater than or equal element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The mask representing the greater than or equal comparison result.</returns>
    static abstract TSelf GreaterThanOrEqual(TSelf a, TSelf b);
    /// <summary>
    /// Compares two lane values for less than element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The mask representing the less than comparison result.</returns>
    static abstract TSelf LessThan(TSelf a, TSelf b);
    /// <summary>
    /// Compares two lane values for less than or equal element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The mask representing the less than or equal comparison result.</returns>
    static abstract TSelf LessThanOrEqual(TSelf a, TSelf b);
    /// <summary>
    /// Compares two lane values for equality element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The mask representing the equality comparison result.</returns>
    static abstract TSelf Equal(TSelf a, TSelf b);

    /// <summary>
    /// Checks if any lane in the mask is true.
    /// </summary>
    /// <param name="mask">The mask to check.</param>
    /// <returns>True if any lane is true; otherwise, false.</returns>
    static abstract bool Any(TSelf mask);
    /// <summary>
    /// Checks if all lanes in the mask are true.
    /// </summary>
    /// <param name="mask">The mask to check.</param>
    /// <returns>True if all lanes are true; otherwise, false.</returns>
    static abstract bool All(TSelf mask);
    /// <summary>
    /// Checks if no lanes in the mask are true.
    /// </summary>
    /// <param name="mask">The mask to check.</param>
    /// <returns>True if no lanes are true; otherwise, false.</returns>
    static abstract bool None(TSelf mask);
}