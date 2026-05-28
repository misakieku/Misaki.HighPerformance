using System.Diagnostics;

namespace Misaki.HighPerformance.Jobs;

internal class WorkerThread : IDisposable
{
    [ThreadStatic]
    private static int t_threadIndex;
    [ThreadStatic]
    private static bool t_isWorkerThread;

    private readonly SPMCQueue<JobHandle>[] _localQueue;
    private readonly Thread _thread;
    private readonly int _threadIndex;

    private readonly JobScheduler _scheduler;
    private readonly int _maxStealAttems;

    private uint _priorityTick;

    public static int ThreadIndex => t_threadIndex;
    public static bool IsWorkerThread => t_isWorkerThread;

    public ReadOnlySpan<SPMCQueue<JobHandle>> LocalQueues => _localQueue;

    public WorkerThread(int index, JobScheduler scheduler, ThreadPriority priority)
    {
        _scheduler = scheduler;
        _maxStealAttems = Math.Max((int)(_scheduler.WorkerCount * 0.5f), 3);

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
        // Ratio: 4 High (50%), 3 Normal (37.5%), 1 Low (12.5%)
        var cascade = stackalloc int[24] {
            0, 1, 2, // Tick 0 (High)
            0, 1, 2, // Tick 1 (High)
            0, 1, 2, // Tick 2 (High)
            0, 1, 2, // Tick 3 (High)
            1, 2, 0, // Tick 4 (Normal)
            1, 2, 0, // Tick 5 (Normal)
            1, 2, 0, // Tick 6 (Normal)
            2, 0, 1  // Tick 7 (Low)
        };

        var helperThreadCount = _scheduler.ExternalHelperThreadCount;
        var index = tick * 3;
        for (var offset = 0; offset < 3; offset++)
        {
            var p = cascade[index + offset];

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
            var p = cascade[index + offset];

            for (var i = 1; i < _scheduler.WorkerCount; i++)
            {
                // Calculate the target deterministically using modulo arithmetic 
                var targetIndex = ((t_threadIndex + i) % _scheduler.WorkerCount) + helperThreadCount;

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

        while (!_scheduler.IsCancellationRequested)
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
                try
                {
                    _scheduler.WaitForWork(Timeout.Infinite);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (!TryFindJob(out handle))
                {
                    continue;
                }
            }

            JobUtility.TryHelpExecuteJob(_scheduler, handle, t_threadIndex);
        }
    }

    public void Dispose()
    {
        _thread.Join();
    }
}