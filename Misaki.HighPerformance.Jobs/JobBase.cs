namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Base class for all jobs. Jobs are now classes to avoid heap allocation complexities.
/// </summary>
public abstract class JobBase
{
    /// <summary>
    /// Called when the job should be executed.
    /// </summary>
    public abstract void Execute();
}

/// <summary>
/// Base class for parallel jobs.
/// </summary>
public abstract class ParallelJobBase
{
    /// <summary>
    /// Called for each item in the parallel job.
    /// </summary>
    /// <param name="index">The index of the current item.</param>
    public abstract void Execute(int index);
}
