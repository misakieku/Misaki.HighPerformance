using System.Diagnostics;

namespace Misaki.HighPerformance.Jobs;

internal class WorkerThread : IDisposable
{
    // Sequence table for job priority selection
    // 0(00), 1(01), 2(10) -> reverse order for bitwise operations
    // Tick 0~3: 2,1,0 -> 10_01_00 (0x24)
    // Tick 4~6: 0,2,1 -> 00_10_01 (0x09)
    // Tick 7:   1,0,2 -> 01_00_10 (0x12)
    private const ulong SEQUENCE_TABLE =
        (0x12UL << 42) |
        (0x09UL << 36) | (0x09UL << 30) | (0x09UL << 24) |
        (0x24UL << 18) | (0x24UL << 12) | (0x24UL << 6) | 0x24UL;

    [ThreadStatic]
    private static int t_threadIndex;
    [ThreadStatic]
    private static bool t_isWorkerThread;

    private readonly SPMCQueue<JobHandle>[] _localQueue;
    private readonly Thread _thread;
    private readonly int _threadIndex;

    private readonly JobScheduler _scheduler;

    private uint _priorityTick;

    public static int ThreadIndex => t_threadIndex;
    public static bool IsWorkerThread => t_isWorkerThread;

    public ReadOnlySpan<SPMCQueue<JobHandle>> LocalQueues => _localQueue;

    public WorkerThread(int index, JobScheduler scheduler, ThreadPriority priority)
    {
        _scheduler = scheduler;

        _localQueue = new SPMCQueue<JobHandle>[3];
        for (var i = 0; i < 3; i++)
        {
            _localQueue[i] = new SPMCQueue<JobHandle>(1024);
        }

        _threadIndex = index;
        _thread = new Thread(WorkLoop)
        {
            IsBackground = true,
            Name = $"WorkerThread-{index}",
            Priority = priority,
        };

        _priorityTick = (uint)Random.Shared.Next(0, 8);
    }

    public void Start()
    {
        _thread.Start(_threadIndex);
    }

    private unsafe bool TryFindJob(out JobHandle handle)
    {
        Debug.Assert(_localQueue != null);

        _priorityTick++;

        var tick = (int)(_priorityTick & 7);
        var seq = (int)((SEQUENCE_TABLE >> (tick * 6)) & 0x3F);
        var helperThreadCount = _scheduler.ExternalHelperThreadCount;

        for (var offset = 0; offset < 3; offset++)
        {
            var p = (seq >> (offset * 2)) & 0x3;

            if (_localQueue[p].TryPop(out handle))
            {
                return true;
            }

            if (_scheduler.TryStealFromMain(p, out handle))
            {
                return true;
            }
        }

        for (var offset = 0; offset < helperThreadCount; offset++)
        {
            var p = (seq >> (offset * 2)) & 0x3;

            for (var i = 1; i < _scheduler.WorkerCount; i++)
            {
                // Calculate the target deterministically using modulo arithmetic 
                var targetIndex = ((t_threadIndex - helperThreadCount + i) % _scheduler.WorkerCount) + helperThreadCount;

                if (_scheduler.TryStealFromWorker(targetIndex, p, out handle))
                {
                    return true;
                }
            }
        }

        handle = JobHandle.Invalid;
        return false;
    }

    private void WorkLoop(object? index)
    {
        Debug.Assert(index != null);

        t_threadIndex = (int)index;
        t_isWorkerThread = true;

        _scheduler.BroadcastStateChange(t_threadIndex, WorkerThreadState.Spinning);

        while (!_scheduler.IsCancellationRequested)
        {
            try
            {
                var handle = JobHandle.Invalid;
                var spin = new SpinWait();
                var found = false;

                while (!spin.NextSpinWillYield)
                {
                    if (TryFindJob(out handle))
                    {
                        _scheduler.WaitForWork(0); // Consume the signal if we found work immediately

                        found = true;
                        break;
                    }

                    spin.SpinOnce(-1);
                }

                // If we didn't find a job after spinning, wait for a signal
                if (!found)
                {
                    _scheduler.BroadcastStateChange(t_threadIndex, WorkerThreadState.Idle);

                    try
                    {
                        _scheduler.WaitForWork(Timeout.Infinite);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    _scheduler.BroadcastStateChange(t_threadIndex, WorkerThreadState.Spinning);

                    if (!TryFindJob(out handle))
                    {
                        continue;
                    }
                }

                JobUtility.TryHelpExecuteJob(_scheduler, handle, t_threadIndex);
            }
            catch (Exception ex)
            {
                Debug.Fail($"Worker thread {t_threadIndex} encountered an exception: {ex}");
            }
        }
    }

    public void Dispose()
    {
        _thread.Join();
    }
}
