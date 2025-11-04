namespace Misaki.HighPerformance.Mathematics.Geometry;

public struct OBB : IEquatable<OBB>
{
    public quaternion Rotation
    {
        get; set;
    }

    public float3 Center
    {
        get; set;
    }

    public float3 Extents
    {
        get; set;
    }

    public OBB(quaternion rotation, float3 extents)
    {
        Rotation = rotation;
        Extents = extents;
    }

    public readonly bool Contains(float3 point)
    {
        var localPoint = math.mul(math.conjugate(Rotation), point - Center);
        return math.all(math.abs(localPoint) <= Extents);
    }

    private readonly void ProjectOntoAxis(float3 axis, out float min, out float max)
    {
        var localAxis = math.mul(math.conjugate(Rotation), axis);
        var r = math.abs(localAxis.x) * Extents.x + math.abs(localAxis.y) * Extents.y + math.abs(localAxis.z) * Extents.z;
        var centerProjection = math.dot(Center, axis);
        min = centerProjection - r;
        max = centerProjection + r;
    }

    public unsafe readonly bool Overlaps(OBB other)
    {
        // Using the Separating Axis Theorem (SAT) for OBB-OBB intersection test
        var axes = stackalloc float3[15];
        var thisRotation = new float3x3(Rotation);
        var otherRotation = new float3x3(other.Rotation);

        // 3 axes from this OBB
        axes[0] = thisRotation.c0;
        axes[1] = thisRotation.c1;
        axes[2] = thisRotation.c2;

        // 3 axes from other OBB
        axes[3] = otherRotation.c0;
        axes[4] = otherRotation.c1;
        axes[5] = otherRotation.c2;

        // 9 cross products of edges
        var index = 6;
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                axes[index++] = math.cross(thisRotation[i], otherRotation[j]);
            }
        }

        var toOther = other.Center - Center;
        for (var i = 0; i < 15; i++)
        {
            var axis = axes[i];
            if (math.lengthsq(axis) < 1e-6f)
            {
                continue;  // Skip near-zero axes
            }

            axis = math.normalize(axis);

            // Project both OBBs onto the axis
            ProjectOntoAxis(axis, out var thisMin, out var thisMax);
            other.ProjectOntoAxis(axis, out var otherMin, out var otherMax);

            // Project the vector between centers onto the axis
            var distance = math.dot(toOther, axis);

            // Check for overlap
            if (thisMax + distance < otherMin || otherMax + distance < thisMin)
            {
                return false; // Found a separating axis
            }
        }
        return true; // No separating axis found, OBBs overlap
    }

    public readonly AABB ToAABB()
    {
        var absRotation = new float3x3
        {
            c0 = math.abs(Rotation.value.x),
            c1 = math.abs(Rotation.value.y),
            c2 = math.abs(Rotation.value.z)
        };
        var worldExtents = absRotation.c0 * Extents.x + absRotation.c1 * Extents.y + absRotation.c2 * Extents.z;
        return new AABB(Center - worldExtents, Center + worldExtents);
    }

    public readonly bool Equals(OBB other)
    {
        return Rotation.Equals(other.Rotation) && Center.Equals(other.Center) && Extents.Equals(other.Extents);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is OBB obb && Equals(obb);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Rotation, Center, Extents);
    }

    public static bool operator ==(OBB left, OBB right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OBB left, OBB right)
    {
        return !(left == right);
    }
}