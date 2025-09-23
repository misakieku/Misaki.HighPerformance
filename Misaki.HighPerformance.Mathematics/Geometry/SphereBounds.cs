using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics.Geometry;

/// <summary>
/// Represents a three-dimensional sphere defined by a center point and a radius, typically used for spatial bounding or
/// intersection tests.
/// </summary>
public struct SphereBounds : IEquatable<SphereBounds>
{
    /// <summary>
    /// Gets or sets the coordinates of the center point in three-dimensional space.
    /// </summary>
    public float3 Center
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the radius of the shape.
    /// </summary>
    public float Radius
    {
        get; set;
    }

    public SphereBounds(float3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>
    /// Determines whether the specified point is contained within the sphere.
    /// </summary>
    /// <param name="point">The point to test for inclusion within the sphere.</param>
    /// <returns>true if the point lies inside or on the boundary of the sphere; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(float3 point)
    {
        var toPoint = point - Center;
        return math.lengthsq(toPoint) <= Radius * Radius;
    }

    /// <summary>
    /// Determines whether this sphere intersects with the specified sphere.
    /// </summary>
    /// <param name="other">The sphere to test for intersection with this sphere.</param>
    /// <returns>true if the spheres intersect or touch; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(SphereBounds other)
    {
        var toOther = other.Center - Center;
        var radiusSum = Radius + other.Radius;
        return math.lengthsq(toOther) <= radiusSum * radiusSum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float3 ClosestPoint(float3 point)
    {
        var toPoint = point - Center;
        var toPointLength = math.length(toPoint);
        if (toPointLength <= Radius || toPointLength == 0.0f)
        {
            return point;
        }

        return Center + toPoint / toPointLength * Radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encapsulate(SphereBounds other)
    {
        var toOther = other.Center - Center;
        var toOtherLength = math.length(toOther);

        if (toOtherLength + other.Radius > Radius)
        {
            var newRadius = (Radius + toOtherLength + other.Radius) * 0.5f;
            var k = (newRadius - Radius) / toOtherLength;
            Center += toOther * k;
            Radius = newRadius;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encapsulate(float3 point)
    {
        var toPoint = point - Center;
        var toPointLength = math.length(toPoint);

        if (toPointLength > Radius)
        {
            var newRadius = (Radius + toPointLength) * 0.5f;
            var k = (newRadius - Radius) / toPointLength;
            Center += toPoint * k;
            Radius = newRadius;
        }
    }

    public readonly override string ToString()
    {
        return $"Center: {Center}, Radius: {Radius}";
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Center, Radius);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is SphereBounds bound && Equals(bound);
    }

    public readonly bool Equals(SphereBounds other)
    {
        return Center.Equals(other.Center) && Radius.Equals(other.Radius);
    }

    public static bool operator ==(SphereBounds left, SphereBounds right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SphereBounds left, SphereBounds right)
    {
        return !(left == right);
    }
}