namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Represents a job that performs a single unit of work.
/// Jobs are structs to avoid allocations and enable high-performance execution.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Executes the job logic.
    /// </summary>
    void Execute();
}

/// <summary>
/// Represents a job that performs the same operation for a set of items, executed in parallel.
/// Each job instance processes a range of indices, enabling data parallelism.
/// </summary>
public interface IJobParallelFor
{
    /// <summary>
    /// Executes the job for a single item at the given index.
    /// </summary>
    /// <param name="index">The index of the item to process.</param>
    void Execute(int index);
}