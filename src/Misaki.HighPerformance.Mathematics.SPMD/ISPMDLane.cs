using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Misaki.HighPerformance.Mathematics.SPMD;

/// <summary>
/// Common marker interface for SPMD lane types.
/// </summary>
public interface ISPMDLane
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
public unsafe interface ISPMDLane<TSelf, TNumber> : ISPMDLane, IEquatable<TSelf>
    where TSelf : ISPMDLane<TSelf, TNumber>
    where TNumber : unmanaged, INumber<TNumber>, IBinaryNumber<TNumber>, IMinMaxValue<TNumber>, IBitwiseOperators<TNumber, TNumber, TNumber>
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
    /// Gets a lane value where all bits are set to 1 for each lane.
    /// </summary>
    static abstract TSelf AllBitsSet
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
    /// Implementations may rely on vector creation helpers and assume that the resulting sequence length matches <see cref="ISPMDLane.LaneWidth"/>.
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
    static abstract TSelf Load(TNumber* pValue);

    /// <summary>
    /// Uses the specified mask to conditionally load lane values from the given reference, returning a lane value where masked lanes are loaded and unmasked lanes are set to zero.
    /// </summary>
    /// <param name="value">The reference to load from.</param>
    /// <param name="mask">The mask to use for conditional loading.</param>
    /// <returns>The loaded lane value.</returns>
    static abstract TSelf MaskLoad(ref TNumber value, TSelf mask);
    /// <summary>
    /// Uses the specified mask to conditionally load lane values from the given pointer, returning a lane value where masked lanes are loaded and unmasked lanes are set to zero.
    /// </summary>
    /// <param name="pValue">The pointer to load from.</param>
    /// <param name="mask">The mask to use for conditional loading.</param>
    /// <returns>The loaded lane value.</returns>
    static abstract TSelf MaskLoad(TNumber* pValue, TSelf mask);

    /// <summary>
    /// Gathers lane values from the specified base address and indices, returning a lane value where each lane is loaded from the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="pData">The base address from which to gather values.</param>
    /// <param name="indices">The indices of the values to gather.</param>
    /// <param name="scale">The scale factor for the indices.</param>
    /// <returns>The gathered lane value.</returns>
    static abstract TSelf Gather(TNumber* pData, TSelf indices, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale);
    /// <summary>
    /// Gathers lane values from the specified base address and indices, returning a lane value where each lane is loaded from the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="pData">The base address from which to gather values.</param>
    /// <param name="pIndices">The pointer to the indices of the values to gather.</param>
    /// <param name="scale">The scale factor for the indices.</param>
    /// <returns>The gathered lane value.</returns>
    static abstract TSelf Gather(TNumber* pData, int* pIndices, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale);
    /// <summary>
    /// Gathers lane values from the specified base address and indices, returning a lane value where each lane is loaded from the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="baseAddress">The base address from which to gather values.</param>
    /// <param name="indices">The indices of the values to gather.</param>
    /// <param name="scale">The scale factor for the indices.</param>
    /// <returns>The gathered lane value.</returns>
    static abstract TSelf Gather(ref TNumber baseAddress, TSelf indices, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale);
    /// <summary>
    /// Gathers lane values from the specified base address and indices, returning a lane value where each lane is loaded from the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="baseAddress">The base address from which to gather values.</param>
    /// <param name="baseIndex">The reference to the base index.</param>
    /// <param name="scale">The scale factor for the indices.</param>
    /// <returns>The gathered lane value.</returns>
    static abstract TSelf Gather(ref TNumber baseAddress, ref int baseIndex, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale);
    /// <summary>
    /// Gathers lane values from the specified base address and indices, returning a lane value where each lane is loaded from the address computed by adding the corresponding index (multiplied by the scale) to the base address, but only for lanes where the corresponding mask bit is set; other lanes are set to zero.
    /// </summary>
    /// <param name="pData">The base address from which to gather values.</param>
    /// <param name="indices">The indices of the values to gather.</param>
    /// <param name="mask">The mask value that determines which elements are included in the gathering operation.</param>
    /// <param name="scale">The scale factor for the indices.</param>
    /// <returns>The gathered lane value.</returns>
    static abstract TSelf MaskGather(TNumber* pData, TSelf indices, TSelf mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale);
    /// <summary>
    /// Gathers lane values from the specified base address and indices, returning a lane value where each lane is loaded from the address computed by adding the corresponding index (multiplied by the scale) to the base address, but only for lanes where the corresponding mask bit is set; other lanes are set to zero.
    /// </summary>
    /// <param name="pData">The base address from which to gather values.</param>
    /// <param name="pIndices">The pointer to the indices of the values to gather.</param>
    /// <param name="mask">The mask value that determines which elements are included in the gathering operation.</param>
    /// <param name="scale">The scale factor for the indices.</param>
    /// <returns>The gathered lane value.</returns>
    static abstract TSelf MaskGather(TNumber* pData, int* pIndices, TSelf mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale);

    /// <summary>
    /// Stores the lane value to the specified reference.
    /// </summary>
    /// <param name="destination">The reference to store to.</param>
    void Store(ref TNumber destination);
    /// <summary>
    /// Stores the lane value to the specified pointer.
    /// </summary>
    /// <param name="pDestination">The pointer to store to.</param>
    void Store(TNumber* pDestination);
    /// <summary>
    /// Compresses the data specified by the given mask and stores the compressed result in the provided destination
    /// variable.
    /// </summary>
    /// <param name="destination">A reference to the variable where the compressed data will be stored.</param>
    /// <param name="mask">A mask value that determines which elements are included in the compression operation.</param>
    /// <returns>The number of elements written to the destination as a result of the compression. Returns 0 if no elements are compressed.</returns>
    /// <remarks>
    /// Implementations may use hardware-specific shuffle tables to reorder the selected lanes before storing, falling back to a scalar loop otherwise.
    /// </remarks>
    int CompressStore(ref TNumber destination, TSelf mask);
    /// <summary>
    /// Compresses the data specified by the given mask and stores the compressed result in the provided destination
    /// variable.
    /// </summary>
    /// <param name="pDestination">A pointer to the variable where the compressed data will be stored.</param>
    /// <param name="mask">A mask value that determines which elements are included in the compression operation.</param>
    /// <returns>The number of elements written to the destination as a result of the compression. Returns 0 if no elements are compressed.</returns>
    /// <remarks>
    /// Implementations may use hardware-specific shuffle tables to reorder the selected lanes before storing, falling back to a scalar loop otherwise.
    /// </remarks>
    int CompressStore(TNumber* pDestination, TSelf mask);
    /// <summary>
    /// Masks the lane value with the specified mask and stores the result to the given reference, where masked lanes are stored and unmasked lanes are left unchanged in the destination.
    /// </summary>
    /// <param name="pDestination">A pointer to the variable where the masked data will be stored.</param>
    /// <param name="mask">A mask value that determines which elements are included in the masking operation.</param>
    void MaskStore(TNumber* pDestination, TSelf mask);
    /// <summary>
    /// Masks the lane value with the specified mask and stores the result to the given reference, where masked lanes are stored and unmasked lanes are left unchanged in the destination.
    /// </summary>
    /// <param name="destination">A reference to the variable where the masked data will be stored.</param>
    /// <param name="mask">A mask value that determines which elements are included in the masking operation.</param>
    void MaskStore(ref TNumber destination, TSelf mask);

    /// <summary>
    /// Scatters the lane value to the specified base address and indices, where each lane is stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="pDst">A pointer to the base address where the data will be scattered.</param>
    /// <param name="indices">A vector of indices that determine the destinations of each lane.</param>
    void Scatter(TNumber* pDst, TSelf indices);
    /// <summary>
    /// Scatters the lane value to the specified base address and indices, where each lane is stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="destination">A reference to the variable where the scattered data will be stored.</param>
    /// <param name="indices">A vector of indices that determine the destinations of each lane.</param>
    void Scatter(ref TNumber destination, TSelf indices);
    /// <summary>
    /// Scatters the lane value to the specified base address and indices, where each lane is stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="pDst">A pointer to the base address where the data will be scattered.</param>
    /// <param name="pIndices">A pointer to the array of indices that determine the destinations of each lane.</param>
    void Scatter(TNumber* pDst, int* pIndices);
    /// <summary>
    /// Scatters the lane value to the specified base address and indices, where each lane is stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address.
    /// </summary>
    /// <param name="destination">A reference to the variable where the scattered data will be stored.</param>
    /// <param name="pIndices">A pointer to the array of indices that determine the destinations of each lane.</param>
    void Scatter(ref TNumber destination, int* pIndices);
    /// <summary>
    /// Masks the lane value with the specified mask and scatters the result to the given base address and indices, where masked lanes are stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address, and unmasked lanes are left unchanged in the destination.
    /// </summary>
    /// <param name="pDst">A pointer to the base address where the data will be scattered.</param>
    /// <param name="indices">A vector of indices that determine the destinations of each lane.</param>
    /// <param name="mask">A vector of boolean values that determine which lanes to scatter.</param>
    void MaskScatter(TNumber* pDst, TSelf indices, TSelf mask);
    /// <summary>
    /// Masks the lane value with the specified mask and scatters the result to the given base address and indices, where masked lanes are stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address, and unmasked lanes are left unchanged in the destination.
    /// </summary>
    /// <param name="destination">A reference to the variable where the scattered data will be stored.</param>
    /// <param name="indices">A vector of indices that determine the destinations of each lane.</param>
    /// <param name="mask">A vector of boolean values that determine which lanes to scatter.</param>
    void MaskScatter(ref TNumber destination, TSelf indices, TSelf mask);
    /// <summary>
    /// Masks the lane value with the specified mask and scatters the result to the given base address and indices, where masked lanes are stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address, and unmasked lanes are left unchanged in the destination.
    /// </summary>
    /// <param name="pDst">A pointer to the base address where the data will be scattered.</param>
    /// <param name="pIndices">A pointer to the array of indices that determine the destinations of each lane.</param>
    /// <param name="mask">A vector of boolean values that determine which lanes to scatter.</param>
    void MaskScatter(TNumber* pDst, int* pIndices, TSelf mask);
    /// <summary>
    /// Masks the lane value with the specified mask and scatters the result to the given base address and indices, where masked lanes are stored to the address computed by adding the corresponding index (multiplied by the scale) to the base address, and unmasked lanes are left unchanged in the destination.
    /// </summary>
    /// <param name="destination">A reference to the variable where the scattered data will be stored.</param>
    /// <param name="pIndices">A pointer to the array of indices that determine the destinations of each lane.</param>
    /// <param name="mask">A vector of boolean values that determine which lanes to scatter.</param>
    void MaskScatter(ref TNumber destination, int* pIndices, TSelf mask);

    /// <summary>
    /// Converts the lane value to a vector.
    /// </summary>
    /// <returns>The backing vector representation.</returns>
    Vector<TNumber> AsVector();

    /// <summary>
    /// Gets an pointer to the lane's underlying data.
    /// </summary>
    /// <returns>An pointer to the lane's underlying data.</returns>
    TNumber* GetUnsafePtr();

    /// <summary>
    /// Casts the lane value to another SPMD lane type with a different underlying numeric type.
    /// </summary>
    /// <typeparam name="TOther">The type of the other SPMD lane.</typeparam>
    /// <typeparam name="TOtherNumber">The underlying numeric type of the other SPMD lane.</typeparam>
    /// <returns>The casted lane value.</returns>
    TOther Cast<TOther, TOtherNumber>()
        where TOther : ISPMDLane<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>;

    /// <summary>
    /// Bitwise reinterprets the lane value as another SPMD lane type with a different underlying numeric type.
    /// </summary>
    /// <typeparam name="TOther">The type of the other SPMD lane.</typeparam>
    /// <typeparam name="TOtherNumber">The underlying numeric type of the other SPMD lane.</typeparam>
    /// <returns>The bit-cast lane value.</returns>
    TOther BitCast<TOther, TOtherNumber>()
        where TOther : ISPMDLane<TOther, TOtherNumber>
        where TOtherNumber : unmanaged, INumber<TOtherNumber>, IBinaryNumber<TOtherNumber>, IMinMaxValue<TOtherNumber>, IBitwiseOperators<TOtherNumber, TOtherNumber, TOtherNumber>;

    /// <summary>
    /// Adds two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise sum.</returns>
    static abstract TSelf operator +(TSelf a, TSelf b);
    /// <summary>
    /// Subtracts two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise difference.</returns>
    static abstract TSelf operator -(TSelf a, TSelf b);
    /// <summary>
    /// Multiplies two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise product.</returns>
    static abstract TSelf operator *(TSelf a, TSelf b);
    /// <summary>
    /// Divides two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise quotient.</returns>
    static abstract TSelf operator /(TSelf a, TSelf b);
    /// <summary>
    /// Computes the modulus of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The lane-wise modulus.</returns>
    static abstract TSelf operator %(TSelf a, TSelf b);

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
    /// Computes the bitwise OR of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The result of the bitwise OR.</returns>
    static abstract TSelf operator |(TSelf a, TSelf b);
    /// <summary>
    /// Computes the bitwise XOR of two lane values element-wise.
    /// </summary>
    /// <param name="a">The first lane value.</param>
    /// <param name="b">The second lane value.</param>
    /// <returns>The result of the bitwise XOR.</returns>
    static abstract TSelf operator ^(TSelf a, TSelf b);
    /// <summary>
    /// Computes the bitwise NOT of a lane value element-wise.
    /// </summary>
    /// <param name="a">The lane value.</param>
    /// <returns>The bitwise complement of the lane value.</returns>
    static abstract TSelf operator ~(TSelf a);
    /// <summary>
    /// Determines whether two instances of the type are equal component-wise.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>All bits set where the elements are equal; otherwise, all bits cleared.</returns>
    static abstract TSelf operator ==(TSelf a, TSelf b);
    /// <summary>
    /// Determines whether two instances of the type are not equal component-wise.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>All bits set where the elements are not equal; otherwise, all bits cleared.</returns>
    static abstract TSelf operator !=(TSelf a, TSelf b);
    /// <summary>
    /// Determines whether one instance of the type is greater than another instance component-wise.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>A value indicating whether the first parameter is greater than the second parameter.</returns>
    static abstract TSelf operator >(TSelf a, TSelf b);
    /// <summary>
    /// Determines whether the first operand is greater than or equal to the second operand component-wise.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>All bits set where the first parameter is greater than or equal to the second parameter; otherwise, all bits cleared.</returns>
    static abstract TSelf operator >=(TSelf a, TSelf b);
    /// <summary>
    /// Determines whether one instance of the type is less than another instance component-wise.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>All bits set where the first parameter is less than the second parameter; otherwise, all bits cleared.</returns>
    static abstract TSelf operator <(TSelf a, TSelf b);
    /// <summary>
    /// Determines whether the first operand is less than or equal to the second operand component-wise.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>All bits set where the first parameter is less than or equal to the second parameter; otherwise, all bits cleared.</returns>
    static abstract TSelf operator <=(TSelf a, TSelf b);

    /// <summary>
    /// Implicitly converts a scalar numeric value to a lane value where all lanes are set to that value.
    /// </summary>
    /// <param name="value">The scalar numeric value to convert.</param>
    static abstract implicit operator TSelf(TNumber value);

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
    static abstract TSelf MultiplyAdd(TSelf a, TSelf b, TSelf c);
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
    static abstract void SinCos(TSelf value, out TSelf sin, out TSelf cos);
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
    /// Reduces the lane value to a single scalar by adding all lanes together.
    /// </summary>
    /// <param name="value">The lane value to reduce.</param>
    /// <returns>The reduced scalar value.</returns>
    static abstract TNumber ReduceAdd(TSelf value);
    /// <summary>
    /// Reduces the lane value to a single scalar by finding the maximum element.
    /// </summary>
    /// <param name="value">The lane value to reduce.</param>
    /// <returns>The reduced scalar value.</returns>
    static abstract TNumber ReduceMax(TSelf value);
    /// <summary>
    /// Reduces the lane value to a single scalar by finding the minimum element.
    /// </summary>
    /// <param name="value">The lane value to reduce.</param>
    /// <returns>The reduced scalar value.</returns>
    static abstract TNumber ReduceMin(TSelf value);

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