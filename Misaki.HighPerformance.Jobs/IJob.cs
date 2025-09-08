namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Represents a job that performs a single unit of work.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Executes the job logic.
    /// </summary>
    /// <param name="threadIndex">The index of the thread executing the job, useful for thread-specific operations.</param>
    void Execute(int threadIndex);
}

/// <summary>
/// Represents a job that performs the same operation for a set of items, executed in parallel.
/// </summary>
public interface IJobParallelFor
{
    /// <summary>
    /// Executes the job for a single item at the given index.
    /// </summary>
    /// <param name="loopIndex">The index of the item to process.</param>
    /// <param name="threadIndex">The index of the thread executing the job, useful for thread-specific operations.</param>
    void Execute(int loopIndex, int threadIndex);
}