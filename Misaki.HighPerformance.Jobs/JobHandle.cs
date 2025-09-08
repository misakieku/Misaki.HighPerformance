namespace Misaki.HighPerformance.Jobs;

public readonly struct JobHandle : IEquatable<JobHandle>
{
    internal readonly int _id;
    internal readonly int _generation;

    public static JobHandle Invalid => new(-1, -1);

    public bool IsValid => this != Invalid;

    internal JobHandle(int id, int generation)
    {
        _id = id;
        _generation = generation;
    }

    public bool Equals(JobHandle other)
    {
        return _id == other._id && _generation == other._generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is JobHandle handle && Equals(handle);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_id, _generation);
    }

    public override string ToString()
    {
        return IsValid ? $"JobHandle({_id}, {_generation})" : "JobHandle(Invalid)";
    }

    public static bool operator ==(JobHandle left, JobHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JobHandle left, JobHandle right)
    {
        return !(left == right);
    }
}