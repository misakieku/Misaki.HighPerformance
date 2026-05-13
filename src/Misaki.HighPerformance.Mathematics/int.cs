namespace Misaki.HighPerformance.Mathematics;

[NumericType(typeof(int), sizeof(int), 2, 1, "global::Misaki.HighPerformance.Mathematics.int")]
[NumericConvertable("(int){v}.{c}", typeof(uint2), typeof(float2), typeof(double2), typeof(bool2))]
[NumericConvertable("(int){v}", typeof(uint), typeof(float), typeof(double))]
public partial struct int2
{
}

[NumericType(typeof(int2), sizeof(int), 2, 2, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int2x2
{
}

[NumericType(typeof(int2), sizeof(int), 2, 3, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int2x3
{
}

[NumericType(typeof(int2), sizeof(int), 2, 4, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int2x4
{
}

[NumericType(typeof(int), sizeof(int), 3, 1, "global::Misaki.HighPerformance.Mathematics.int")]
[NumericConvertable("(int){v}.{c}", typeof(uint3), typeof(float3), typeof(double3), typeof(bool3))]
[NumericConvertable("(int){v}", typeof(uint), typeof(float), typeof(double))]
public partial struct int3
{
}

[NumericType(typeof(int3), sizeof(int), 3, 2, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int3x2
{
}

[NumericType(typeof(int3), sizeof(int), 3, 3, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int3x3
{
}

[NumericType(typeof(int3), sizeof(int), 3, 4, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int3x4
{
}

[NumericType(typeof(int), sizeof(int), 4, 1, "global::Misaki.HighPerformance.Mathematics.int")]
[NumericConvertable("(int){v}.{c}", typeof(uint4), typeof(float4), typeof(double4), typeof(bool4))]
[NumericConvertable("(int){v}", typeof(uint), typeof(float), typeof(double))]
public partial struct int4
{
}

[NumericType(typeof(int4), sizeof(int), 4, 2, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int4x2
{
}

[NumericType(typeof(int4), sizeof(int), 4, 3, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int4x3
{
}

[NumericType(typeof(int4), sizeof(int), 4, 4, "global::Misaki.HighPerformance.Mathematics.int", canInverse: false, elementType: typeof(int))]
public partial struct int4x4
{
}