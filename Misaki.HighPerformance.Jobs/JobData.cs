using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Internal data structure representing job ranges for parallel execution.
/// This matches Unity's JobRanges structure for work stealing.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JobRanges
{
    public int JobIndex;
    public int BeginIndex;
    public int EndIndex;
    public int TotalLength;
    public int BatchSize;

    /// <summary>
    /// Pointer to atomic counter for work stealing.
    /// </summary>
    public int* CurrentIndex;
}

/// <summary>
/// Internal job data structure that holds job execution information.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JobData
{
    /// <summary>
    /// Unique identifier for this job.
    /// </summary>
    public ulong Id;

    /// <summary>
    /// Version counter to detect reused job slots.
    /// </summary>
    public int Version;

    /// <summary>
    /// Job state using atomic operations.
    /// 0 = Scheduled, 1 = Running, 2 = Completed
    /// </summary>
    public int State;

    /// <summary>
    /// Number of dependencies this job has.
    /// </summary>
    public int DependencyCount;

    /// <summary>
    /// Number of completed dependencies.
    /// </summary>
    public int CompletedDependencies;

    /// <summary>
    /// Type of job (0 = IJob, 1 = IJobParallelFor).
    /// </summary>
    public JobType JobType;

    /// <summary>
    /// Function pointer to the job execution method.
    /// </summary>
    public ExecuteJobDelegate? ExecuteJobFunction;

    /// <summary>
    /// Function pointer to the parallel job execution method.
    /// </summary>
    public ExecuteParallelJobDelegate? ExecuteParallelJobFunction;

    /// <summary>
    /// Reference to the job data object.
    /// </summary>
    public object? JobDataObject;    /// <summary>
                                     /// For parallel jobs, the total number of iterations.
                                     /// </summary>
    public int TotalIterations;

    /// <summary>
    /// For parallel jobs, the batch size per worker.
    /// </summary>
    public int BatchSize;

    /// <summary>
    /// Array of dependency job IDs (inline for small counts).
    /// </summary>
    public fixed ulong Dependencies[8]; // Inline dependencies for performance

    /// <summary>
    /// Pointer to additional dependencies if more than 8.
    /// </summary>
    public ulong* AdditionalDependencies;

    /// <summary>
    /// Size of additional dependencies array.
    /// </summary>
    public int AdditionalDependencyCount;

    public readonly bool IsCompleted => Volatile.Read(ref Unsafe.AsRef<int>(in State)) == 2;

    public readonly bool CanExecute =>
        Volatile.Read(ref Unsafe.AsRef<int>(in State)) == 0 &&
        Volatile.Read(ref Unsafe.AsRef<int>(in CompletedDependencies)) >= DependencyCount;
}

/// <summary>
/// Type of job being executed.
/// </summary>
internal enum JobType : byte
{
    Job = 0,
    ParallelFor = 1
}

/// <summary>
/// Function pointer delegate for IJob execution.
/// </summary>
internal delegate void ExecuteJobDelegate(object jobData);

/// <summary>
/// Function pointer delegate for IJobParallelFor execution.
/// </summary>
internal unsafe delegate void ExecuteParallelJobDelegate(object jobData, ref JobRanges ranges, int jobIndex);
