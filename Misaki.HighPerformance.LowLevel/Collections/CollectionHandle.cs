namespace Misaki.HighPerformance.LowLevel.Collections;

public readonly struct CollectionHandle
{
    public readonly int id;
    public readonly int generation;

    public static CollectionHandle Invalid => new(-1, -1);

    public bool IsValid => this != Invalid;

    internal CollectionHandle(int id, int generation)
    {
        this.id = id;
        this.generation = generation;
    }

    public bool Equals(CollectionHandle other)
    {
        return id == other.id && generation == other.generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is CollectionHandle handle && Equals(handle);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(id, generation);
    }

    public override string ToString()
    {
        return IsValid ? $"CollectionHandle({id}, {generation})" : "CollectionHandle(Invalid)";
    }

    public static bool operator ==(CollectionHandle left, CollectionHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CollectionHandle left, CollectionHandle right)
    {
        return !(left == right);
    }
}