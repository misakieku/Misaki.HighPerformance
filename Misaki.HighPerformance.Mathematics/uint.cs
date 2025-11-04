namespace Misaki.HighPerformance.Mathematics;

[NumericType(typeof(uint), sizeof(uint), 2, 1, "global::Misaki.HighPerformance.Mathematics.uint")]
[NumericConvertable("(uint){v}.{c}", typeof(int2), typeof(float2), typeof(double2))]
[NumericConvertable("{v}.{c} ? 1u : 0u", typeof(bool2))]
public partial struct uint2
{
}

[NumericType(typeof(uint2), sizeof(uint), 2, 2, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint2x2
{
}

[NumericType(typeof(uint2), sizeof(uint), 2, 3, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint2x3
{
}

[NumericType(typeof(uint2), sizeof(uint), 2, 4, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint2x4
{
}

[NumericType(typeof(uint), sizeof(uint), 3, 1, "global::Misaki.HighPerformance.Mathematics.uint")]
[NumericConvertable("(uint){v}.{c}", typeof(int3), typeof(float3), typeof(double3))]
[NumericConvertable("{v}.{c} ? 1u : 0u", typeof(bool3))]
public partial struct uint3
{
}

[NumericType(typeof(uint3), sizeof(uint), 3, 2, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint3x2
{
}

[NumericType(typeof(uint3), sizeof(uint), 3, 3, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint3x3
{
}

[NumericType(typeof(uint3), sizeof(uint), 3, 4, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint3x4
{
}

[NumericType(typeof(uint), sizeof(uint), 4, 1, "global::Misaki.HighPerformance.Mathematics.uint")]
[NumericConvertable("(uint){v}.{c}", typeof(int4), typeof(float4), typeof(double4))]
[NumericConvertable("{v}.{c} ? 1u : 0u", typeof(bool4))]
public partial struct uint4
{
}

[NumericType(typeof(uint4), sizeof(uint), 4, 2, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint4x2
{
}

[NumericType(typeof(uint4), sizeof(uint), 4, 3, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint4x3
{
}

[NumericType(typeof(uint4), sizeof(uint), 4, 4, "global::Misaki.HighPerformance.Mathematics.uint", canInverse: false, elementType: typeof(uint))]
public partial struct uint4x4
{
}