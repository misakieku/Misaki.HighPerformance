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

/// <summary>
/// Represents a job that performs the same operation for a set of items, executed in parallel.
/// </summary>
public interface IJobParallel
{
    /// <summary>
    /// Executes an operation over a specified range, optionally associating the execution with a particular thread index.
    /// </summary>
    /// <param name="startIndex">The zero-based index at which to begin the operation.</param>
    /// <param name="endIndex">The zero-based index at which to end the operation.</param>
    /// <param name="threadIndex">The index of the thread executing the job, useful for thread-specific operations.</param>
    void Execute(int startIndex, int endIndex, int threadIndex);
}

public static class IJobExtensions
{
    public static void Run<T>(this ref T job, int threadIndex)
        where T : struct, IJob
    {
        job.Execute(threadIndex);
    }
}

public static class IJobParallelForExtensions
{
    public static void Run<T>(this ref T job, int totalIterations, int threadIndex)
    where T : struct, IJobParallelFor
    {
        for (int i = 0; i < totalIterations; i++)
        {
            job.Execute(i, threadIndex);
        }
    }
}

public static class IJobParallelExtensions
{
    public static void Run<T>(this ref T job, int totalIterations, int threadIndex)
    where T : struct, IJobParallel
    {
        job.Execute(0, totalIterations, threadIndex);
    }
}