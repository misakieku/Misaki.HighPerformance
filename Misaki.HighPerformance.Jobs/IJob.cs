namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Represents a job that performs a single unit of work.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Executes the job logic.
    /// </summary>
    /// <param name="ctx">The context of the job execution, providing access to thread-specific information and job scheduling capabilities.</param>
    void Execute(ref readonly JobExecutionContext ctx);
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
    /// <param name="ctx">The context of the job execution, providing access to thread-specific information and job scheduling capabilities.</param>
    void Execute(int loopIndex, ref readonly JobExecutionContext ctx);
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
    /// <param name="ctx">The context of the job execution, providing access to thread-specific information and job scheduling capabilities.</param>
    void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx);
}

public static class IJobExtensions
{
    public static void Run<T>(this T job, ref readonly JobExecutionContext ctx)
        where T : IJob
    {
        job.Execute(in ctx);
    }

    public static void RunRef<T>(this ref T job, ref readonly JobExecutionContext ctx)
        where T : struct, IJob
    {
        job.Execute(in ctx);
    }
}

public static class IJobParallelForExtensions
{
    public static void Run<T>(this T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : IJobParallelFor
    {
        for (var i = 0; i < totalIterations; i++)
        {
            job.Execute(i, in ctx);
        }
    }

    public static void RunRef<T>(this ref T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : struct, IJobParallelFor
    {
        for (var i = 0; i < totalIterations; i++)
        {
            job.Execute(i, in ctx);
        }
    }
}

public static class IJobParallelExtensions
{
    public static void Run<T>(this T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : IJobParallel
    {
        job.Execute(0, totalIterations, in ctx);
    }

    public static void RunRef<T>(this ref T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : struct, IJobParallel
    {
        job.Execute(0, totalIterations, in ctx);
    }
}