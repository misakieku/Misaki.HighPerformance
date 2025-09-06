using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// A handle that represents a scheduled job and can be used to manage dependencies and wait for completion.
/// JobHandle is designed to be a lightweight value type to avoid allocations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct JobHandle : IEquatable<JobHandle>
{
    internal readonly ulong _id;
    internal readonly int _version;

    internal JobHandle(ulong id, int version)
    {
        _id = id;
        _version = version;
    }

    /// <summary>
    /// A completed job handle that can be used as a dependency that is already satisfied.
    /// </summary>
    public static JobHandle Completed => new(0, 0);

    /// <summary>
    /// Gets whether this job handle represents a completed job.
    /// </summary>
    public bool IsCompleted => _id == 0 || JobScheduler.IsCompleted(this);

    /// <summary>
    /// Blocks the calling thread until the job completes.
    /// </summary>
    public void Complete()
    {
        if (_id != 0)
        {
            JobScheduler.Complete(this);
        }
    }

    /// <summary>
    /// Combines multiple job handles into a single dependency.
    /// The resulting handle will be complete when all input handles are complete.
    /// </summary>
    /// <param name="dependencies">The job handles to combine.</param>
    /// <returns>A new job handle that depends on all input handles.</returns>
    public static JobHandle CombineDependencies(params ReadOnlySpan<JobHandle> dependencies)
    {
        if (dependencies.Length == 0)
        {
            return Completed;
        }

        if (dependencies.Length == 1)
        {
            return dependencies[0];
        }

        return JobScheduler.CombineDependencies(dependencies);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(JobHandle other)
    {
        return _id == other._id && _version == other._version;
    }

    public override bool Equals(object? obj)
    {
        return obj is JobHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_id, _version);
    }

    public static bool operator ==(JobHandle left, JobHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JobHandle left, JobHandle right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return _id == 0 ? "JobHandle(Completed)" : $"JobHandle(ID:{_id}, Version:{_version})";
    }
}
