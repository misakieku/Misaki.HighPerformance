using Misaki.HighPerformance.Mathematics.Attributes;

namespace Misaki.HighPerformance.Mathematics;

[NumericType(typeof(double), sizeof(double), 2, 1, "global::Misaki.HighPerformance.Mathematics.double")]
[NumericConvertable("(double){v}.{c}", typeof(int2), typeof(uint2), typeof(float2))]
[NumericConvertable("{v}.{c} ? 1.0 : 0.0", typeof(bool2))]
public partial struct double2
{
}

[NumericType(typeof(double2), sizeof(double), 2, 2, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double2x2
{
}

[NumericType(typeof(double2), sizeof(double), 2, 3, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double2x3
{
}

[NumericType(typeof(double2), sizeof(double), 2, 4, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double2x4
{
}

[NumericType(typeof(double), sizeof(double), 3, 1, "global::Misaki.HighPerformance.Mathematics.double")]
[NumericConvertable("(double){v}.{c}", typeof(int3), typeof(uint3), typeof(float3))]
[NumericConvertable("{v}.{c} ? 1.0 : 0.0", typeof(bool3))]
public partial struct double3
{
}

[NumericType(typeof(double3), sizeof(double), 3, 2, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double3x2
{
}

[NumericType(typeof(double3), sizeof(double), 3, 3, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double3x3
{
}

[NumericType(typeof(double3), sizeof(double), 3, 4, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double3x4
{
}

[NumericType(typeof(double), sizeof(double), 4, 1, "global::Misaki.HighPerformance.Mathematics.double")]
[NumericConvertable("(double){v}.{c}", typeof(int4), typeof(uint4), typeof(float4))]
[NumericConvertable("{v}.{c} ? 1.0 : 0.0", typeof(bool4))]
public partial struct double4
{
}

[NumericType(typeof(double4), sizeof(double), 4, 2, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double4x2
{
}

[NumericType(typeof(double4), sizeof(double), 4, 3, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double4x3
{
}

[NumericType(typeof(double4), sizeof(double), 4, 4, "global::Misaki.HighPerformance.Mathematics.double", elementType: typeof(double))]
public partial struct double4x4
{
}