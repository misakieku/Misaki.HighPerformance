namespace Misaki.HighPerformance.Jobs;

public readonly struct JobHandle : IEquatable<JobHandle>
{
    private readonly int _id;
    private readonly int _generation;

    public int ID => _id - 1;
    public int Generation => _generation - 1;

    public static JobHandle Invalid => default;

    public bool IsValid => this != Invalid;

    internal JobHandle(int id, int generation)
    {
        _id = id + 1;
        _generation = generation + 1;
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