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

/// <summary>
/// The priority level of a job.
/// </summary>
public enum JobPriority
{
    /// <summary>
    /// Normal priority. Which will have 37.5% chance to be picked when there are multiple jobs ready to run.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// High priority. Which will have 50.0% chance to be picked when there are multiple jobs ready to run. This is useful for jobs that are on the critical path of the execution and we want to prioritize their completion.
    /// </summary>
    High = 1,
    /// <summary>
    /// Low priority. Which will have 12.5% chance to be picked when there are multiple jobs ready to run.
    /// </summary>
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
    public required ref T data;
    public required delegate*<ref T, ref JobRanges, ref readonly JobExecutionContext, void> pExecutionFunc;
    public required delegate*<ref T, void> pFreeFunc;
    public JobRanges jobRanges;
    public JobPriority priority;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
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
    public delegate*<ref readonly JobInfo, void> pFreeFunc;

    public void* pCustomExecutionFunc;
    public void* pCustomFreeFunc;

    public int dataID;
    public int dataGeneration;

    public JobRanges jobRanges;
    public JobPriority priority;

    public int firstDependentEdgeIndex; // Index of the first dependent edge in the global edge list, -1 if no dependents
    public int state;

    public int dependencyCount; // Numbers of jobs that this job depends on, when it reaches 0, the job can be executed

#if MHP_ENABLE_PROFILING
    public string? jobTypeName;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DependentIterator GetDependentIterator(ReadOnlySpan<JobEdge> edgePool)
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
    public const int RC_SHIFT = 16;

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
        return stateVal >> RC_SHIFT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRefCount(int stateValue)
    {
        return stateValue >> RC_SHIFT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReleaseRC(ref int jobState)
    {
        return (Interlocked.Add(ref jobState, -RC_ONE) & ~STATE_MASK) >> RC_SHIFT;
    }

    public static unsafe bool TryHelpExecuteJob(JobScheduler jobScheduler, JobHandle handle, int callerThreadIndex)
    {
        ref var jobInfo = ref jobScheduler.GetJobInfoReference(handle, out var exist);
        if (!exist)
        {
            return false;
        }

        var rcSpin = new SpinWait();
        var rcAcquired = false;
        int rc;

        while (true)
        {
            jobScheduler.GetJobInfoReference(handle, out var currentExist);
            if (!currentExist)
            {
                return false;
            }

            var stateVal = Volatile.Read(ref jobInfo.state);
            var state = GetState(stateVal);

            // We can't execute it if it's not ready or already done
            if (state == JobState.Created || state == JobState.Completed || state == JobState.Invalid)
            {
                return false;
            }

            // If it's single job and already running, we can't help it unless we restructure it.
            // But if it's a Parallel job, multiple threads CAN safely join the `Running` state.
            if (state == JobState.Running && jobInfo.jobRanges.batchSize == jobInfo.jobRanges.totalIteration)
            {
                // Single execution job is already running on another thread. We just return false.
                return false;
            }

            var newState = stateVal + RC_ONE;
            if (state == JobState.Scheduled)
            {
                newState = (newState & ~STATE_MASK) | JOBSTATE_RUNNING;
            }

            if (Interlocked.CompareExchange(ref jobInfo.state, newState, stateVal) == stateVal)
            {
                jobScheduler.GetJobInfoReference(handle, out currentExist);
                if (!currentExist)
                {
                    rc = ReleaseRC(ref jobInfo.state);
                    if (rc == 0)
                    {
                        jobScheduler.MarkJobComplete(handle);
                    }

                    return false;
                }

                rcAcquired = true;
                break;
            }

            rcSpin.SpinOnce(-1);
        }

        if (!rcAcquired)
        {
            return false;
        }

        // Execute the work inline
        if (jobInfo.pExecutionFunc != null)
        {
#if MHP_ENABLE_PROFILING
            jobScheduler.BroadcastStateChange(callerThreadIndex, WorkerThreadState.Executing, jobInfo.jobTypeName);
#endif

            var ctx = new JobExecutionContext
            {
                ThreadIndex = callerThreadIndex,
                JobScheduler = jobScheduler,
                State = jobScheduler.State,
                SelfHandle = handle,
            };

            jobInfo.pExecutionFunc(jobInfo.dataID, jobInfo.dataGeneration, ref jobInfo.jobRanges, in ctx);

#if MHP_ENABLE_PROFILING
            jobScheduler.BroadcastStateChange(callerThreadIndex, WorkerThread.IsWorkerThread ? WorkerThreadState.Spinning : WorkerThreadState.Idle);
#endif
        }

        rc = ReleaseRC(ref jobInfo.state);
        if (rc == 0)
        {
            jobScheduler.MarkJobComplete(handle);
        }

        return true;
    }
}