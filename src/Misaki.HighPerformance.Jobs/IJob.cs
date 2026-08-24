using System.Runtime.InteropServices;

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

/// <summary>
/// Represents a custom job with user-defined execution and cleanup logic, allowing for more flexible job definitions beyond the standard interfaces.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public interface ICustomJob<TSelf>
{
    /// <summary>
    /// Executes the job logic, providing access to the job's own state, the job ranges for parallel execution, and the job execution context for thread-specific information and scheduling capabilities.
    /// </summary>
    /// <param name="job">The job instance to execute.</param>
    /// <param name="jobRanges">The ranges of items to process.</param>
    /// <param name="ctx">The context of the job execution.</param>
    static abstract void Execute(ref TSelf job, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx);
    /// <summary>
    /// Frees any resources associated with the job after execution, allowing for cleanup of unmanaged resources or other necessary finalization steps.
    /// </summary>
    /// <param name="job">The job instance to clean up.</param>
    static abstract void Free(ref TSelf job);
}

internal unsafe struct CombinedDependenciesJob : IJob, ICustomJob<CombinedDependenciesJob>
{
    public JobHandle* dependencies;
    public int dependencyCount;

    // NOTE: The native buffer is owned exclusively by Free. Exactly one of
    // {normal completion -> pFreeFunc, Dispose drain -> pFreeFunc} runs it,
    // guaranteed by pool membership: MarkJobComplete removes the entry after
    // pFreeFunc, and the drain only sees entries whose Execute never ran.
    public readonly void Execute(ref readonly JobExecutionContext ctx)
    {
        var span = new Span<JobHandle>(dependencies, dependencyCount);
        ctx.JobScheduler.WaitAll(span);
    }

    public static void Execute(ref CombinedDependenciesJob job, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
        => job.Execute(in ctx);

    public static void Free(ref CombinedDependenciesJob job)
        => NativeMemory.Free(job.dependencies);
}

public static class IJobExtensions
{
    /// <summary>
    /// Runs the job in the calling thread, blocking until the job is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="ctx">The job execution context.</param>
    public static void Run<T>(this T job, ref readonly JobExecutionContext ctx)
        where T : IJob
    {
        job.Execute(in ctx);
    }

    /// <summary>
    /// Runs the job by reference in the calling thread, blocking until the job is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="ctx">The job execution context.</param>
    public static void RunRef<T>(this ref T job, ref readonly JobExecutionContext ctx)
        where T : struct, IJob
    {
        job.Execute(in ctx);
    }
}

public static class IJobParallelForExtensions
{
    /// <summary>
    /// Runs the job in the calling thread, blocking until the job is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="totalIterations">The total number of iterations.</param>
    /// <param name="ctx">The job execution context.</param>
    public static void Run<T>(this T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : IJobParallelFor
    {
        for (var i = 0; i < totalIterations; i++)
        {
            job.Execute(i, in ctx);
        }
    }

    /// <summary>
    /// Runs the job by reference in the calling thread, blocking until the job is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="totalIterations">The total number of iterations.</param>
    /// <param name="ctx">The job execution context.</param>
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
    /// <summary>
    /// Runs the job in the calling thread, blocking until the job is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="totalIterations">The total number of iterations.</param>
    /// <param name="ctx">The job execution context.</param>
    public static void Run<T>(this T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : IJobParallel
    {
        job.Execute(0, totalIterations, in ctx);
    }

    /// <summary>
    /// Runs the job by reference in the calling thread, blocking until the job is complete.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="totalIterations">The total number of iterations.</param>
    /// <param name="ctx">The job execution context.</param>
    public static void RunRef<T>(this ref T job, int totalIterations, ref readonly JobExecutionContext ctx)
        where T : struct, IJobParallel
    {
        job.Execute(0, totalIterations, in ctx);
    }
}