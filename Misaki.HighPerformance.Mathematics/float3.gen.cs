
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics;

public struct float3
{
    public float x;
    public float y;
    public float z;

    public float3(float value)
    {
        this.x = value;
        this.y = value;
        this.z = value;
    }

    public float3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float3(float2 value)
    {
        this.x = value.x;
        this.y = value.y;
        this.z = 0.0f;
    }

    public float3(float4 value)
    {
        this.x = value.x;
        this.y = value.y;
        this.z = value.z;
    }

    public static float3 operator +(float3 lhs, float3 rhs)
    {
        return (lhs.AsVector128() + rhs.AsVector128()).AsFloat3();
    }

    public static float3 operator +(float3 lhs, float rhs)
    {
        return lhs + new float3(rhs);
    }

    public static float3 operator -(float3 lhs, float3 rhs)
    {
        return (lhs.AsVector128() - rhs.AsVector128()).AsFloat3();
    }

    public static float3 operator -(float3 lhs, float rhs)
    {
        return lhs - new float3(rhs);
    }

    public static float3 operator *(float3 lhs, float3 rhs)
    {
        return (lhs.AsVector128() * rhs.AsVector128()).AsFloat3();
    }

    public static float3 operator *(float3 lhs, float rhs)
    {
        return (lhs.AsVector128() * rhs).AsFloat3();
    }

    public static float3 operator /(float3 lhs, float3 rhs)
    {
        return (lhs.AsVector128() / rhs.AsVector128()).AsFloat3();
    }

    public static float3 operator /(float3 lhs, float rhs)
    {
        return (lhs.AsVector128() / rhs).AsFloat3();
    }

    public static float3 operator -(float3 value)
    {
        return (-value.AsVector128()).AsFloat3();
    }

    public float2 xx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.x, this.x);
    }

    public float2 xy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.x, this.y);
    }

    public float2 xz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.x, this.z);
    }

    public float2 yx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.y, this.x);
    }

    public float2 yy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.y, this.y);
    }

    public float2 yz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.y, this.z);
    }

    public float2 zx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.z, this.x);
    }

    public float2 zy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.z, this.y);
    }

    public float2 zz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.z, this.z);
    }

    public float3 xxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.x, this.x);
    }

    public float3 xxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.x, this.y);
    }

    public float3 xxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.x, this.z);
    }

    public float3 xyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.y, this.x);
    }

    public float3 xyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.y, this.y);
    }

    public float3 xyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.y, this.z);
    }

    public float3 xzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.z, this.x);
    }

    public float3 xzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.z, this.y);
    }

    public float3 xzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.z, this.z);
    }

    public float3 yxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.x, this.x);
    }

    public float3 yxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.x, this.y);
    }

    public float3 yxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.x, this.z);
    }

    public float3 yyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.y, this.x);
    }

    public float3 yyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.y, this.y);
    }

    public float3 yyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.y, this.z);
    }

    public float3 yzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.z, this.x);
    }

    public float3 yzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.z, this.y);
    }

    public float3 yzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.z, this.z);
    }

    public float3 zxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.x, this.x);
    }

    public float3 zxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.x, this.y);
    }

    public float3 zxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.x, this.z);
    }

    public float3 zyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.y, this.x);
    }

    public float3 zyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.y, this.y);
    }

    public float3 zyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.y, this.z);
    }

    public float3 zzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.z, this.x);
    }

    public float3 zzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.z, this.y);
    }

    public float3 zzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.z, this.z);
    }

    public float4 xxxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.x, this.x);
    }

    public float4 xxxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.x, this.y);
    }

    public float4 xxxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.x, this.z);
    }

    public float4 xxyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.y, this.x);
    }

    public float4 xxyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.y, this.y);
    }

    public float4 xxyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.y, this.z);
    }

    public float4 xxzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.z, this.x);
    }

    public float4 xxzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.z, this.y);
    }

    public float4 xxzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.z, this.z);
    }

    public float4 xyxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.x, this.x);
    }

    public float4 xyxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.x, this.y);
    }

    public float4 xyxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.x, this.z);
    }

    public float4 xyyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.y, this.x);
    }

    public float4 xyyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.y, this.y);
    }

    public float4 xyyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.y, this.z);
    }

    public float4 xyzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.z, this.x);
    }

    public float4 xyzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.z, this.y);
    }

    public float4 xyzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.z, this.z);
    }

    public float4 xzxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.x, this.x);
    }

    public float4 xzxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.x, this.y);
    }

    public float4 xzxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.x, this.z);
    }

    public float4 xzyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.y, this.x);
    }

    public float4 xzyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.y, this.y);
    }

    public float4 xzyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.y, this.z);
    }

    public float4 xzzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.z, this.x);
    }

    public float4 xzzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.z, this.y);
    }

    public float4 xzzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.z, this.z);
    }

    public float4 yxxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.x, this.x);
    }

    public float4 yxxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.x, this.y);
    }

    public float4 yxxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.x, this.z);
    }

    public float4 yxyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.y, this.x);
    }

    public float4 yxyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.y, this.y);
    }

    public float4 yxyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.y, this.z);
    }

    public float4 yxzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.z, this.x);
    }

    public float4 yxzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.z, this.y);
    }

    public float4 yxzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.z, this.z);
    }

    public float4 yyxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.x, this.x);
    }

    public float4 yyxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.x, this.y);
    }

    public float4 yyxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.x, this.z);
    }

    public float4 yyyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.y, this.x);
    }

    public float4 yyyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.y, this.y);
    }

    public float4 yyyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.y, this.z);
    }

    public float4 yyzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.z, this.x);
    }

    public float4 yyzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.z, this.y);
    }

    public float4 yyzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.z, this.z);
    }

    public float4 yzxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.x, this.x);
    }

    public float4 yzxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.x, this.y);
    }

    public float4 yzxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.x, this.z);
    }

    public float4 yzyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.y, this.x);
    }

    public float4 yzyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.y, this.y);
    }

    public float4 yzyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.y, this.z);
    }

    public float4 yzzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.z, this.x);
    }

    public float4 yzzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.z, this.y);
    }

    public float4 yzzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.z, this.z);
    }

    public float4 zxxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.x, this.x);
    }

    public float4 zxxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.x, this.y);
    }

    public float4 zxxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.x, this.z);
    }

    public float4 zxyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.y, this.x);
    }

    public float4 zxyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.y, this.y);
    }

    public float4 zxyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.y, this.z);
    }

    public float4 zxzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.z, this.x);
    }

    public float4 zxzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.z, this.y);
    }

    public float4 zxzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.z, this.z);
    }

    public float4 zyxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.x, this.x);
    }

    public float4 zyxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.x, this.y);
    }

    public float4 zyxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.x, this.z);
    }

    public float4 zyyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.y, this.x);
    }

    public float4 zyyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.y, this.y);
    }

    public float4 zyyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.y, this.z);
    }

    public float4 zyzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.z, this.x);
    }

    public float4 zyzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.z, this.y);
    }

    public float4 zyzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.z, this.z);
    }

    public float4 zzxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.x, this.x);
    }

    public float4 zzxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.x, this.y);
    }

    public float4 zzxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.x, this.z);
    }

    public float4 zzyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.y, this.x);
    }

    public float4 zzyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.y, this.y);
    }

    public float4 zzyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.y, this.z);
    }

    public float4 zzzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.z, this.x);
    }

    public float4 zzzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.z, this.y);
    }

    public float4 zzzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.z, this.z);
    }

    public override readonly string ToString()
    {
        return $"(x: {this.x}, y: {this.y}, z: {this.z})";
    }
}