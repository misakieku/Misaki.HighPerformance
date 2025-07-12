
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics;

public struct float4
{
    public float x;
    public float y;
    public float z;
    public float w;

    public float4(float value)
    {
        this.x = value;
        this.y = value;
        this.z = value;
        this.w = value;
    }

    public float4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public float4(float2 value)
    {
        this.x = value.x;
        this.y = value.y;
        this.z = 0.0f;
        this.w = 0.0f;
    }

    public float4(float3 value)
    {
        this.x = value.x;
        this.y = value.y;
        this.z = value.z;
        this.w = 0.0f;
    }


    public static float4 operator +(float4 lhs, float4 rhs)
    {
        return (lhs.AsVector128() + rhs.AsVector128()).AsFloat4();
    }

    public static float4 operator +(float4 lhs, float rhs)
    {
        return lhs + new float4(rhs);
    }

    public static float4 operator -(float4 lhs, float4 rhs)
    {
        return (lhs.AsVector128() - rhs.AsVector128()).AsFloat4();
    }

    public static float4 operator -(float4 lhs, float rhs)
    {
        return lhs - new float4(rhs);
    }

    public static float4 operator *(float4 lhs, float4 rhs)
    {
        return (lhs.AsVector128() * rhs.AsVector128()).AsFloat4();
    }

    public static float4 operator *(float4 lhs, float rhs)
    {
        return (lhs.AsVector128() * rhs).AsFloat4();
    }

    public static float4 operator /(float4 lhs, float4 rhs)
    {
        return (lhs.AsVector128() / rhs.AsVector128()).AsFloat4();
    }

    public static float4 operator /(float4 lhs, float rhs)
    {
        return (lhs.AsVector128() / rhs).AsFloat4();
    }

    public static float4 operator -(float4 value)
    {
        return (-value.AsVector128()).AsFloat4();
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

    public float2 xw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.x, this.w);
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

    public float2 yw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.y, this.w);
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

    public float2 zw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.z, this.w);
    }

    public float2 wx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.w, this.x);
    }

    public float2 wy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.w, this.y);
    }

    public float2 wz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.w, this.z);
    }

    public float2 ww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.w, this.w);
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

    public float3 xxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.x, this.w);
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

    public float3 xyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.y, this.w);
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

    public float3 xzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.z, this.w);
    }

    public float3 xwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.w, this.x);
    }

    public float3 xwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.w, this.y);
    }

    public float3 xwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.w, this.z);
    }

    public float3 xww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.w, this.w);
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

    public float3 yxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.x, this.w);
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

    public float3 yyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.y, this.w);
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

    public float3 yzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.z, this.w);
    }

    public float3 ywx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.w, this.x);
    }

    public float3 ywy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.w, this.y);
    }

    public float3 ywz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.w, this.z);
    }

    public float3 yww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.w, this.w);
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

    public float3 zxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.x, this.w);
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

    public float3 zyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.y, this.w);
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

    public float3 zzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.z, this.w);
    }

    public float3 zwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.w, this.x);
    }

    public float3 zwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.w, this.y);
    }

    public float3 zwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.w, this.z);
    }

    public float3 zww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.z, this.w, this.w);
    }

    public float3 wxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.x, this.x);
    }

    public float3 wxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.x, this.y);
    }

    public float3 wxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.x, this.z);
    }

    public float3 wxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.x, this.w);
    }

    public float3 wyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.y, this.x);
    }

    public float3 wyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.y, this.y);
    }

    public float3 wyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.y, this.z);
    }

    public float3 wyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.y, this.w);
    }

    public float3 wzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.z, this.x);
    }

    public float3 wzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.z, this.y);
    }

    public float3 wzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.z, this.z);
    }

    public float3 wzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.z, this.w);
    }

    public float3 wwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.w, this.x);
    }

    public float3 wwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.w, this.y);
    }

    public float3 wwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.w, this.z);
    }

    public float3 www 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.w, this.w, this.w);
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

    public float4 xxxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.x, this.w);
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

    public float4 xxyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.y, this.w);
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

    public float4 xxzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.z, this.w);
    }

    public float4 xxwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.w, this.x);
    }

    public float4 xxwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.w, this.y);
    }

    public float4 xxwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.w, this.z);
    }

    public float4 xxww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.w, this.w);
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

    public float4 xyxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.x, this.w);
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

    public float4 xyyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.y, this.w);
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

    public float4 xyzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.z, this.w);
    }

    public float4 xywx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.w, this.x);
    }

    public float4 xywy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.w, this.y);
    }

    public float4 xywz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.w, this.z);
    }

    public float4 xyww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.w, this.w);
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

    public float4 xzxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.x, this.w);
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

    public float4 xzyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.y, this.w);
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

    public float4 xzzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.z, this.w);
    }

    public float4 xzwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.w, this.x);
    }

    public float4 xzwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.w, this.y);
    }

    public float4 xzwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.w, this.z);
    }

    public float4 xzww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.z, this.w, this.w);
    }

    public float4 xwxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.x, this.x);
    }

    public float4 xwxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.x, this.y);
    }

    public float4 xwxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.x, this.z);
    }

    public float4 xwxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.x, this.w);
    }

    public float4 xwyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.y, this.x);
    }

    public float4 xwyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.y, this.y);
    }

    public float4 xwyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.y, this.z);
    }

    public float4 xwyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.y, this.w);
    }

    public float4 xwzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.z, this.x);
    }

    public float4 xwzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.z, this.y);
    }

    public float4 xwzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.z, this.z);
    }

    public float4 xwzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.z, this.w);
    }

    public float4 xwwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.w, this.x);
    }

    public float4 xwwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.w, this.y);
    }

    public float4 xwwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.w, this.z);
    }

    public float4 xwww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.w, this.w, this.w);
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

    public float4 yxxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.x, this.w);
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

    public float4 yxyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.y, this.w);
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

    public float4 yxzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.z, this.w);
    }

    public float4 yxwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.w, this.x);
    }

    public float4 yxwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.w, this.y);
    }

    public float4 yxwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.w, this.z);
    }

    public float4 yxww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.w, this.w);
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

    public float4 yyxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.x, this.w);
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

    public float4 yyyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.y, this.w);
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

    public float4 yyzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.z, this.w);
    }

    public float4 yywx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.w, this.x);
    }

    public float4 yywy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.w, this.y);
    }

    public float4 yywz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.w, this.z);
    }

    public float4 yyww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.w, this.w);
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

    public float4 yzxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.x, this.w);
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

    public float4 yzyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.y, this.w);
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

    public float4 yzzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.z, this.w);
    }

    public float4 yzwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.w, this.x);
    }

    public float4 yzwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.w, this.y);
    }

    public float4 yzwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.w, this.z);
    }

    public float4 yzww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.z, this.w, this.w);
    }

    public float4 ywxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.x, this.x);
    }

    public float4 ywxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.x, this.y);
    }

    public float4 ywxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.x, this.z);
    }

    public float4 ywxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.x, this.w);
    }

    public float4 ywyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.y, this.x);
    }

    public float4 ywyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.y, this.y);
    }

    public float4 ywyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.y, this.z);
    }

    public float4 ywyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.y, this.w);
    }

    public float4 ywzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.z, this.x);
    }

    public float4 ywzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.z, this.y);
    }

    public float4 ywzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.z, this.z);
    }

    public float4 ywzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.z, this.w);
    }

    public float4 ywwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.w, this.x);
    }

    public float4 ywwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.w, this.y);
    }

    public float4 ywwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.w, this.z);
    }

    public float4 ywww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.w, this.w, this.w);
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

    public float4 zxxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.x, this.w);
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

    public float4 zxyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.y, this.w);
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

    public float4 zxzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.z, this.w);
    }

    public float4 zxwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.w, this.x);
    }

    public float4 zxwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.w, this.y);
    }

    public float4 zxwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.w, this.z);
    }

    public float4 zxww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.x, this.w, this.w);
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

    public float4 zyxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.x, this.w);
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

    public float4 zyyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.y, this.w);
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

    public float4 zyzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.z, this.w);
    }

    public float4 zywx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.w, this.x);
    }

    public float4 zywy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.w, this.y);
    }

    public float4 zywz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.w, this.z);
    }

    public float4 zyww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.y, this.w, this.w);
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

    public float4 zzxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.x, this.w);
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

    public float4 zzyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.y, this.w);
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

    public float4 zzzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.z, this.w);
    }

    public float4 zzwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.w, this.x);
    }

    public float4 zzwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.w, this.y);
    }

    public float4 zzwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.w, this.z);
    }

    public float4 zzww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.z, this.w, this.w);
    }

    public float4 zwxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.x, this.x);
    }

    public float4 zwxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.x, this.y);
    }

    public float4 zwxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.x, this.z);
    }

    public float4 zwxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.x, this.w);
    }

    public float4 zwyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.y, this.x);
    }

    public float4 zwyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.y, this.y);
    }

    public float4 zwyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.y, this.z);
    }

    public float4 zwyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.y, this.w);
    }

    public float4 zwzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.z, this.x);
    }

    public float4 zwzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.z, this.y);
    }

    public float4 zwzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.z, this.z);
    }

    public float4 zwzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.z, this.w);
    }

    public float4 zwwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.w, this.x);
    }

    public float4 zwwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.w, this.y);
    }

    public float4 zwwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.w, this.z);
    }

    public float4 zwww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.z, this.w, this.w, this.w);
    }

    public float4 wxxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.x, this.x);
    }

    public float4 wxxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.x, this.y);
    }

    public float4 wxxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.x, this.z);
    }

    public float4 wxxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.x, this.w);
    }

    public float4 wxyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.y, this.x);
    }

    public float4 wxyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.y, this.y);
    }

    public float4 wxyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.y, this.z);
    }

    public float4 wxyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.y, this.w);
    }

    public float4 wxzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.z, this.x);
    }

    public float4 wxzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.z, this.y);
    }

    public float4 wxzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.z, this.z);
    }

    public float4 wxzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.z, this.w);
    }

    public float4 wxwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.w, this.x);
    }

    public float4 wxwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.w, this.y);
    }

    public float4 wxwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.w, this.z);
    }

    public float4 wxww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.x, this.w, this.w);
    }

    public float4 wyxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.x, this.x);
    }

    public float4 wyxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.x, this.y);
    }

    public float4 wyxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.x, this.z);
    }

    public float4 wyxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.x, this.w);
    }

    public float4 wyyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.y, this.x);
    }

    public float4 wyyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.y, this.y);
    }

    public float4 wyyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.y, this.z);
    }

    public float4 wyyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.y, this.w);
    }

    public float4 wyzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.z, this.x);
    }

    public float4 wyzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.z, this.y);
    }

    public float4 wyzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.z, this.z);
    }

    public float4 wyzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.z, this.w);
    }

    public float4 wywx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.w, this.x);
    }

    public float4 wywy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.w, this.y);
    }

    public float4 wywz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.w, this.z);
    }

    public float4 wyww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.y, this.w, this.w);
    }

    public float4 wzxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.x, this.x);
    }

    public float4 wzxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.x, this.y);
    }

    public float4 wzxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.x, this.z);
    }

    public float4 wzxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.x, this.w);
    }

    public float4 wzyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.y, this.x);
    }

    public float4 wzyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.y, this.y);
    }

    public float4 wzyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.y, this.z);
    }

    public float4 wzyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.y, this.w);
    }

    public float4 wzzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.z, this.x);
    }

    public float4 wzzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.z, this.y);
    }

    public float4 wzzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.z, this.z);
    }

    public float4 wzzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.z, this.w);
    }

    public float4 wzwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.w, this.x);
    }

    public float4 wzwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.w, this.y);
    }

    public float4 wzwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.w, this.z);
    }

    public float4 wzww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.z, this.w, this.w);
    }

    public float4 wwxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.x, this.x);
    }

    public float4 wwxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.x, this.y);
    }

    public float4 wwxz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.x, this.z);
    }

    public float4 wwxw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.x, this.w);
    }

    public float4 wwyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.y, this.x);
    }

    public float4 wwyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.y, this.y);
    }

    public float4 wwyz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.y, this.z);
    }

    public float4 wwyw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.y, this.w);
    }

    public float4 wwzx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.z, this.x);
    }

    public float4 wwzy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.z, this.y);
    }

    public float4 wwzz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.z, this.z);
    }

    public float4 wwzw 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.z, this.w);
    }

    public float4 wwwx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.w, this.x);
    }

    public float4 wwwy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.w, this.y);
    }

    public float4 wwwz 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.w, this.z);
    }

    public float4 wwww 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.w, this.w, this.w, this.w);
    }

    public override readonly string ToString()
    {
        return $"(x: {this.x}, y: {this.y}, z: {this.z}, w: {this.w})";
    }
}