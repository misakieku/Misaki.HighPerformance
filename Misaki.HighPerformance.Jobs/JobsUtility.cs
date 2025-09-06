using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// Utilities for job execution, similar to Unity's JobsUtility.
/// Provides low-level job management functions.
/// </summary>
internal static unsafe class JobsUtility
{
    private static ulong s_nextJobId = 1;

    /// <summary>
    /// Gets the next unique job ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetNextJobId()
    {
        return (ulong)Interlocked.Increment(ref Unsafe.As<ulong, long>(ref s_nextJobId));
    }

    /// <summary>
    /// Implements work stealing for parallel jobs.
    /// Returns false when no more work is available.
    /// </summary>
    /// <param name="ranges">The job ranges containing work distribution information.</param>
    /// <param name="jobIndex">The index of the current worker thread.</param>
    /// <param name="beginIndex">Output: The starting index for this work batch.</param>
    /// <param name="endIndex">Output: The ending index for this work batch.</param>
    /// <returns>True if work was acquired, false if no more work is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetWorkStealingRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex)
    {
        var currentIndex = Interlocked.Add(ref *ranges.CurrentIndex, ranges.BatchSize);

        beginIndex = currentIndex - ranges.BatchSize;
        endIndex = Math.Min(currentIndex, ranges.TotalLength);

        return beginIndex < ranges.TotalLength;
    }
}
