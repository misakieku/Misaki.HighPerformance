namespace Misaki.HighPerformance.Mathematics;

[NumericType(typeof(uint), sizeof(uint), 2, 1, "global::Misaki.HighPerformance.Mathematics.bool", false, vectorType: typeof(uint))]
[NumericConvertable("{v}.{c} != 0 ? 0u : ~0u", typeof(int2), typeof(uint2), typeof(float2), typeof(double2))]
public partial struct bool2
{
    public bool2(bool value)
    {
        this.x = math.BoolToMask(value);
        this.y = math.BoolToMask(value);
    }

    public bool2(bool x, bool y)
    {
        this.x = math.BoolToMask(x);
        this.y = math.BoolToMask(y);
    }
}

[NumericType(typeof(bool2), sizeof(uint), 2, 2, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool2x2
{
    public bool2x2(bool value)
    {
        c0 = new bool2(value);
        c1 = new bool2(value);
    }

    public bool2x2(bool m00, bool m01, bool m10, bool m11)
    {
        c0 = new bool2(m00, m10);
        c1 = new bool2(m01, m11);
    }
}

[NumericType(typeof(bool2), sizeof(uint), 2, 3, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool2x3
{
    public bool2x3(bool value)
    {
        c0 = new bool2(value);
        c1 = new bool2(value);
        c2 = new bool2(value);
    }

    public bool2x3(bool m00, bool m01, bool m02, bool m10, bool m11, bool m12)
    {
        c0 = new bool2(m00, m10);
        c1 = new bool2(m01, m11);
        c2 = new bool2(m02, m12);
    }
}

[NumericType(typeof(bool2), sizeof(uint), 2, 4, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool2x4
{
    public bool2x4(bool value)
    {
        c0 = new bool2(value);
        c1 = new bool2(value);
        c2 = new bool2(value);
        c3 = new bool2(value);
    }

    public bool2x4(bool m00, bool m01, bool m02, bool m03, bool m10, bool m11, bool m12, bool m13)
    {
        c0 = new bool2(m00, m10);
        c1 = new bool2(m01, m11);
        c2 = new bool2(m02, m12);
        c3 = new bool2(m03, m13);
    }
}

[NumericType(typeof(uint), sizeof(uint), 3, 1, "global::Misaki.HighPerformance.Mathematics.bool", false, vectorType: typeof(uint))]
[NumericConvertable("{v}.{c} != 0 ? 0u : ~0u", typeof(int3), typeof(uint3), typeof(float3), typeof(double3))]
public partial struct bool3
{
    public bool3(bool value)
    {
        this.x = math.BoolToMask(value);
        this.y = math.BoolToMask(value);
        this.z = math.BoolToMask(value);
    }

    public bool3(bool x, bool y, bool z)
    {
        this.x = math.BoolToMask(x);
        this.y = math.BoolToMask(y);
        this.z = math.BoolToMask(z);
    }
}

[NumericType(typeof(bool3), sizeof(uint), 3, 2, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool3x2
{
    public bool3x2(bool value)
    {
        c0 = new bool3(value);
        c1 = new bool3(value);
    }

    public bool3x2(bool m00, bool m01, bool m10, bool m11, bool m20, bool m21)
    {
        c0 = new bool3(m00, m10, m20);
        c1 = new bool3(m01, m11, m21);
    }
}

[NumericType(typeof(bool3), sizeof(uint), 3, 3, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool3x3
{
    public bool3x3(bool value)
    {
        c0 = new bool3(value);
        c1 = new bool3(value);
        c2 = new bool3(value);
    }

    public bool3x3(bool m00, bool m01, bool m02, bool m10, bool m11, bool m12, bool m20, bool m21, bool m22)
    {
        c0 = new bool3(m00, m10, m20);
        c1 = new bool3(m01, m11, m21);
        c2 = new bool3(m02, m12, m22);
    }
}

[NumericType(typeof(bool3), sizeof(uint), 3, 4, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool3x4
{
    public bool3x4(bool value)
    {
        c0 = new bool3(value);
        c1 = new bool3(value);
        c2 = new bool3(value);
        c3 = new bool3(value);
    }

    public bool3x4(bool m00, bool m01, bool m02, bool m03, bool m10, bool m11, bool m12, bool m13, bool m20, bool m21, bool m22, bool m23)
    {
        c0 = new bool3(m00, m10, m20);
        c1 = new bool3(m01, m11, m21);
        c2 = new bool3(m02, m12, m22);
        c3 = new bool3(m03, m13, m23);
    }
}

[NumericType(typeof(uint), sizeof(uint), 4, 1, "global::Misaki.HighPerformance.Mathematics.bool", false, vectorType: typeof(uint))]
[NumericConvertable("{v}.{c} != 0 ? 0u : ~0u", typeof(int4), typeof(uint4), typeof(float4), typeof(double4))]
public partial struct bool4
{
    public bool4(bool value)
    {
        this.x = math.BoolToMask(value);
        this.y = math.BoolToMask(value);
        this.z = math.BoolToMask(value);
        this.w = math.BoolToMask(value);
    }

    public bool4(bool x, bool y, bool z, bool w)
    {
        this.x = math.BoolToMask(x);
        this.y = math.BoolToMask(y);
        this.z = math.BoolToMask(z);
        this.w = math.BoolToMask(w);
    }
}

[NumericType(typeof(bool4), sizeof(uint), 4, 2, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool4x2
{
    public bool4x2(bool value)
    {
        c0 = new bool4(value);
        c1 = new bool4(value);
    }

    public bool4x2(bool m00, bool m01, bool m10, bool m11, bool m20, bool m21, bool m30, bool m31)
    {
        c0 = new bool4(m00, m10, m20, m30);
        c1 = new bool4(m01, m11, m21, m31);
    }
}

[NumericType(typeof(bool4), sizeof(uint), 4, 3, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool4x3
{
    public bool4x3(bool value)
    {
        c0 = new bool4(value);
        c1 = new bool4(value);
        c2 = new bool4(value);
    }

    public bool4x3(bool m00, bool m01, bool m02, bool m10, bool m11, bool m12, bool m20, bool m21, bool m22, bool m30, bool m31, bool m32)
    {
        c0 = new bool4(m00, m10, m20, m30);
        c1 = new bool4(m01, m11, m21, m31);
        c2 = new bool4(m02, m12, m22, m32);
    }
}

[NumericType(typeof(bool4), sizeof(uint), 4, 4, "global::Misaki.HighPerformance.Mathematics.bool", false, elementType: typeof(uint))]
public partial struct bool4x4
{
    public bool4x4(bool value)
    {
        c0 = new bool4(value);
        c1 = new bool4(value);
        c2 = new bool4(value);
        c3 = new bool4(value);
    }

    public bool4x4(bool m00, bool m01, bool m02, bool m03, bool m10, bool m11, bool m12, bool m13, bool m20, bool m21, bool m22, bool m23, bool m30, bool m31, bool m32, bool m33)
    {
        c0 = new bool4(m00, m10, m20, m30);
        c1 = new bool4(m01, m11, m21, m31);
        c2 = new bool4(m02, m12, m22, m32);
        c3 = new bool4(m03, m13, m23, m33);
    }
}