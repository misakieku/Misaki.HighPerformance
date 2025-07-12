
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics;

public struct float2
{
    public float x;
    public float y;

    public float2(float value)
    {
        this.x = value;
        this.y = value;
    }

    public float2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public float2(float3 value)
    {
        this.x = value.x;
        this.y = value.y;
    }
    public float2(float4 value)
    {
        this.x = value.x;
        this.y = value.y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator +(float2 lhs, float2 rhs)
    {
        return (lhs.AsVector128() + rhs.AsVector128()).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator +(float2 lhs, float rhs)
    {
        return lhs + new float2(rhs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator -(float2 lhs, float2 rhs)
    {
        return (lhs.AsVector128() - rhs.AsVector128()).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator -(float2 lhs, float rhs)
    {
        return lhs - new float2(rhs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator *(float2 lhs, float2 rhs)
    {
        return (lhs.AsVector128() * rhs.AsVector128()).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator *(float2 lhs, float rhs)
    {
        return (lhs.AsVector128() * rhs).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator /(float2 lhs, float2 rhs)
    {
        return (lhs.AsVector128() / rhs.AsVector128()).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator /(float2 lhs, float rhs)
    {
        return (lhs.AsVector128() / rhs).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 operator -(float2 value)
    {
        return (-value.AsVector128()).AsFloat2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(float2 lhs, float2 rhs)
    {
        return lhs.AsVector128() == rhs.AsVector128();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(float2 lhs, float2 rhs)
    {
        return !(lhs == rhs);
    }

    public readonly float2 xx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.x, this.x);
    }

    public readonly float2 xy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.x, this.y);
    }

    public readonly float2 yx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.y, this.x);
    }

    public readonly float2 yy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(this.y, this.y);
    }

    public readonly float3 xxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.x, this.x);
    }

    public readonly float3 xxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.x, this.y);
    }

    public readonly float3 xyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.y, this.x);
    }

    public readonly float3 xyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.x, this.y, this.y);
    }

    public readonly float3 yxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.x, this.x);
    }

    public readonly float3 yxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.x, this.y);
    }

    public readonly float3 yyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.y, this.x);
    }

    public readonly float3 yyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float3(this.y, this.y, this.y);
    }

    public readonly float4 xxxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.x, this.x);
    }

    public readonly float4 xxxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.x, this.y);
    }

    public readonly float4 xxyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.y, this.x);
    }

    public readonly float4 xxyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.x, this.y, this.y);
    }

    public readonly float4 xyxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.x, this.x);
    }

    public readonly float4 xyxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.x, this.y);
    }

    public readonly float4 xyyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.y, this.x);
    }

    public readonly float4 xyyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.x, this.y, this.y, this.y);
    }

    public readonly float4 yxxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.x, this.x);
    }

    public readonly float4 yxxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.x, this.y);
    }

    public readonly float4 yxyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.y, this.x);
    }

    public readonly float4 yxyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.x, this.y, this.y);
    }

    public readonly float4 yyxx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.x, this.x);
    }

    public readonly float4 yyxy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.x, this.y);
    }

    public readonly float4 yyyx 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.y, this.x);
    }

    public readonly float4 yyyy 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float4(this.y, this.y, this.y, this.y);
    }

    public override readonly string ToString()
    {
        return $"(x: {this.x}, y: {this.y})";
    }
}