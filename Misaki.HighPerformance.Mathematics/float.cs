using Misaki.HighPerformance.Mathematics.Attributes;

namespace Misaki.HighPerformance.Mathematics;

[NumericType(typeof(float), sizeof(float), 2, 1, "global::Misaki.HighPerformance.Mathematics.float")]
public partial struct float2
{
}

[NumericType(typeof(float2), sizeof(float), 2, 2, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float2x2
{
}

[NumericType(typeof(float2), sizeof(float), 2, 3, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float2x3
{
}

[NumericType(typeof(float2), sizeof(float), 2, 4, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float2x4
{
}

[NumericType(typeof(float), sizeof(float), 3, 1, "global::Misaki.HighPerformance.Mathematics.float")]
public partial struct float3
{
}

[NumericType(typeof(float3), sizeof(float), 3, 2, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float3x2
{
}

[NumericType(typeof(float3), sizeof(float), 3, 3, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float3x3
{
    public float3x3(quaternion q)
    {
        var v = q.value;
        var v2 = v + v;

        var npn = new uint3(0x80000000, 0x00000000, 0x80000000);
        var nnp = new uint3(0x80000000, 0x80000000, 0x00000000);
        var pnn = new uint3(0x00000000, 0x80000000, 0x80000000);
        c0 = v2.y * math.asfloat(math.asuint(v.yxw) ^ npn) - v2.z * math.asfloat(math.asuint(v.zwx) ^ pnn) + new float3(1, 0, 0);
        c1 = v2.z * math.asfloat(math.asuint(v.wzy) ^ nnp) - v2.x * math.asfloat(math.asuint(v.yxw) ^ npn) + new float3(0, 1, 0);
        c2 = v2.x * math.asfloat(math.asuint(v.zwx) ^ pnn) - v2.y * math.asfloat(math.asuint(v.wzy) ^ nnp) + new float3(0, 0, 1);
    }
}

[NumericType(typeof(float3), sizeof(float), 3, 4, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float3x4
{
}

[NumericType(typeof(float), sizeof(float), 4, 1, "global::Misaki.HighPerformance.Mathematics.float")]
public partial struct float4
{
}

[NumericType(typeof(float4), sizeof(float), 4, 2, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float4x2
{
}

[NumericType(typeof(float4), sizeof(float), 4, 3, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float4x3
{
}

[NumericType(typeof(float4), sizeof(float), 4, 4, "global::Misaki.HighPerformance.Mathematics.float", elementType: typeof(float))]
public partial struct float4x4
{
}