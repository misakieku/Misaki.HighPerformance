using Misaki.HighPerformance.Mathematics.Attributes;

namespace Misaki.HighPerformance.Mathematics;

[NumericType(typeof(bool), sizeof(bool), 2, 1, "global::Misaki.HighPerformance.Mathematics.bool", false, vectorType: typeof(byte))]
[NumericConvertable("{v}.{c} != 0", typeof(int2), typeof(uint2), typeof(float2), typeof(double2))]
public partial struct bool2
{
}

[NumericType(typeof(bool2), sizeof(bool), 2, 2, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool2x2
{
}

[NumericType(typeof(bool2), sizeof(bool), 2, 3, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool2x3
{
}

[NumericType(typeof(bool2), sizeof(bool), 2, 4, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool2x4
{
}

[NumericType(typeof(bool), sizeof(bool), 3, 1, "global::Misaki.HighPerformance.Mathematics.bool", false, vectorType: typeof(byte))]
public partial struct bool3
{
}

[NumericType(typeof(bool3), sizeof(bool), 3, 2, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool3x2
{
}

[NumericType(typeof(bool3), sizeof(bool), 3, 3, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool3x3
{
}

[NumericType(typeof(bool3), sizeof(bool), 3, 4, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool3x4
{
}

[NumericType(typeof(bool), sizeof(bool), 4, 1, "global::Misaki.HighPerformance.Mathematics.bool", false, vectorType: typeof(byte))]
public partial struct bool4
{
}

[NumericType(typeof(bool4), sizeof(bool), 4, 2, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool4x2
{
}

[NumericType(typeof(bool4), sizeof(bool), 4, 3, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool4x3
{
}

[NumericType(typeof(bool4), sizeof(bool), 4, 4, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(bool))]
public partial struct bool4x4
{
}