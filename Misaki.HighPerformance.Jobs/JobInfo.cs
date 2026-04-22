using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// The state of a job in its lifecycle.
/// </summary>
public enum JobState
{
    /// <summary>
    /// The job is in an invalid state, indicating an error or uninitialized state.
    /// </summary>
    Invalid = -1,
    /// <summary>
    /// The job has been created but not yet scheduled for execution.
    /// </summary>
    Created = 0,
    /// <summary>
    /// The job is scheduled and waiting to be executed.
    /// </summary>
    Scheduled = 1,
    /// <summary>
    /// The job is currently being executed.
    /// </summary>
    Running = 2,
    /// <summary>
    /// The job has completed execution.
    /// </summary>
    Completed = 3
}

public enum JobPriority
{
    High = 0,
    Normal = 1,
    Low = 2
}

public struct JobRanges
{
    public int batchSize;
    public int totalIteration;
    public int currentIndex;

    public static JobRanges Single => new JobRanges()
    {
        batchSize = 1,
        totalIteration = 1,
        currentIndex = 0,
    };

    public readonly int TotalBatches => (totalIteration + batchSize - 1) / batchSize;
}

public unsafe ref struct CustomJobDesc<T>
{
    public required ref readonly T data;
    public required delegate*<int, int, ref JobRanges, ref readonly JobExecutionContext, void> pExecutionFunc;
    public required delegate*<int, int, void> pFreeFunc;
    public JobRanges jobRanges;
    public JobPriority priority;
}

internal unsafe struct JobInfo
{
    public ref struct DependentIterator
    {
        private readonly ReadOnlySpan<JobEdge> _edgePool;
        private int _currentEdgeIndex;
        private int _nextEdgeIndex;

        public DependentIterator(int firstDependentEdgeIndex, ReadOnlySpan<JobEdge> edgePool)
        {
            _edgePool = edgePool;
            _nextEdgeIndex = firstDependentEdgeIndex;
            _currentEdgeIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_nextEdgeIndex == -1)
            {
                return false;
            }

            _currentEdgeIndex = _nextEdgeIndex;
            _nextEdgeIndex = _edgePool[_currentEdgeIndex].nextEdgeIndex;
            return true;
        }

        public readonly JobHandle Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _edgePool[_currentEdgeIndex].dependentJob;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ref JobInfo jobInfo)
        {
            _nextEdgeIndex = jobInfo.firstDependentEdgeIndex;
            _currentEdgeIndex = -1;
        }
    }

    public delegate*<int, int, ref JobRanges, ref readonly JobExecutionContext, void> pExecutionFunc;
    public delegate*<int, int, void> pFreeFunc;

    public int dataID;
    public int dataGeneration;

    public JobRanges jobRanges;
    public JobPriority priority;

    public int firstDependentEdgeIndex; // Index of the first dependent edge in the global edge list, -1 if no dependents
    public int state;

    public int dependencyCount; // Numbers of jobs that this job depends on, when it reaches 0, the job can be executed

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DependentIterator GetDependentIterator(ReadOnlySpan<JobEdge> edgePool)
    {
        return new DependentIterator(firstDependentEdgeIndex, edgePool);
    }
}

internal struct JobEdge
{
    public JobHandle dependentJob;
    public int nextEdgeIndex;
}

internal static class JobUtility
{
    // Lock-Free constants: State mask (low 16 bits) and RC unit (1 << 16)
    public const int STATE_MASK = 0xFFFF;
    public const int RC_ONE = 0x10000;

    public const int JOBSTATE_INVALID = (int)JobState.Invalid & STATE_MASK;
    public const int JOBSTATE_CREATED = (int)JobState.Created & STATE_MASK;
    public const int JOBSTATE_SCHEDULED = (int)JobState.Scheduled & STATE_MASK;
    public const int JOBSTATE_RUNNING = (int)JobState.Running & STATE_MASK;
    public const int JOBSTATE_COMPLETED = (int)JobState.Completed & STATE_MASK;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JobState ReadState(ref JobInfo jobInfo)
    {
        var stateVal = Volatile.Read(ref jobInfo.state);
        return (JobState)(stateVal & STATE_MASK);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadStateValue(ref JobInfo jobInfo)
    {
        var stateVal = Volatile.Read(ref jobInfo.state);
        return stateVal & STATE_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStateValue(JobState state)
    {
        return (int)state & STATE_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JobState GetState(int value)
    {
        return (JobState)(value & STATE_MASK);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadRefCount(ref JobInfo jobInfo)
    {
        var stateVal = Volatile.Read(ref jobInfo.state);
        return stateVal >> 16; // RC is stored in the high 16 bits
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRefCount(int stateValue)
    {
        return stateValue >> 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReleaseRC(ref int jobState)
    {
        return GetRefCount(Interlocked.Add(ref jobState, -RC_ONE));
    }
}