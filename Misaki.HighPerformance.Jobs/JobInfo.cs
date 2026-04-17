using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

internal enum HeapType
{
    Native,
    Managed,
}

internal unsafe struct JobInfo
{
    public ref struct DependentIterator
    {
        private readonly ref JobInfo _jobInfo;
        private int _index;

        public DependentIterator(ref JobInfo jobInfo)
        {
            _jobInfo = ref jobInfo;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _jobInfo.dependentCount;
        }
        
        public JobHandle Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index < MAX_DEPENDENTS)
                {
                    return new JobHandle(_jobInfo.dependentsID[_index], _jobInfo.dependentsGeneration[_index]);
                }
                else
                {
                    return _jobInfo.additionalDependents[_index - MAX_DEPENDENTS];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _index = -1;
        }
    }

    public const int MAX_DEPENDENTS = 8;

    public void* pJobData;
    public JobExecutionFunc pExecutionFunc;

    // The list of jobs that are waiting for THIS job to complete.
    public fixed int dependentsID[MAX_DEPENDENTS]; // The actual list of IDs
    public fixed int dependentsGeneration[MAX_DEPENDENTS]; // The actual list of generations

    public UnsafeList<JobHandle> additionalDependents;
    public int dependentCount;

    public JobRanges jobRanges;

    public int state;
    public int remainingBatches;

    public int threadIndex; // The preferred thread index to run this job on, -1 means any thread
    public int dependencyCount; // Numbers of jobs that this job depends on, when it reaches 0, the job can be executed

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public DependentIterator GetDependentIterator()
    {
        return new DependentIterator(ref this);
    }
}

internal struct JobRanges
{
    public int batchSize;
    public int totalIteration;
    public int currentIndex;

    public static JobRanges Single => new()
    {
        batchSize = 1,
        totalIteration = 1,
        currentIndex = 0,
    };
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
    public static void ReleaseRC(ref int jobState)
    {
        Interlocked.Add(ref jobState, -RC_ONE);
    }
}