using System.Collections.Concurrent;

namespace Misaki.HighPerformance.Jobs;

internal class WorkerThread : IDisposable
{
    private readonly int _index;
    private readonly Thread _thread;
    private readonly ConcurrentQueue<JobHandle>[] _localQueues;

    private readonly JobScheduler _scheduler;
    private readonly int _maxStealAttems;

    private uint _priorityTick;

    internal ReadOnlySpan<ConcurrentQueue<JobHandle>> LocalQueues => _localQueues;

    public WorkerThread(int index, JobScheduler scheduler, ThreadPriority priority)
    {
        _index = index;
        _localQueues = new ConcurrentQueue<JobHandle>[3];

        for (var i = 0; i < _localQueues.Length; i++)
        {
            _localQueues[i] = new ConcurrentQueue<JobHandle>();
        }

        _scheduler = scheduler;
        _maxStealAttems = Math.Max((int)(_scheduler.WorkerCount * 0.5f), 3);

        _thread = new Thread(WorkLoop)
        {
            IsBackground = true,
            Name = $"WorkerThread-{index}",
            Priority = priority
        };
    }

    public void Start()
    {
        _thread.Start();
    }

    private unsafe bool TryFindJob(out JobHandle handle)
    {
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

        var index = tick * 3;
        for (var offset = 0; offset < 3; offset++)
        {
            var p = cascade[index + offset];

            if (_localQueues[p].TryDequeue(out handle))
            {
                return true;
            }

            if (_scheduler.TryStealFromMain(p, out handle))
            {
                return true;
            }

            for (var i = 1; i < _scheduler.WorkerCount; i++)
            {
                // Calculate the target deterministically using modulo arithmetic 
                var targetIndex = (_index + i) % _scheduler.WorkerCount;

                if (_scheduler.TryStealFromWorker(targetIndex, p, out handle))
                {
                    return true;
                }
            }
        }

        handle = JobHandle.Invalid;
        return false;
    }

    private unsafe void WorkLoop()
    {
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

            ref var jobInfo = ref _scheduler.GetJobInfoReference(handle, out var exist);
            if (exist)
            {
                var priorState = Interlocked.CompareExchange(ref jobInfo.state, JobUtility.JOBSTATE_RUNNING, JobUtility.JOBSTATE_SCHEDULED);
                if (priorState != JobUtility.JOBSTATE_SCHEDULED && priorState != JobUtility.JOBSTATE_RUNNING)
                {
                    continue;
                }

                if (jobInfo.pExecutionFunc != null)
                {
                    var ctx = new JobExecutionContext
                    {
                        ThreadIndex = _index,
                        JobScheduler = _scheduler,
                        State = _scheduler.State,
                        SelfHandle = handle,
                    };

                    if (!jobInfo.pExecutionFunc(jobInfo.pJobData, ref jobInfo.jobRanges, ref jobInfo.remainingBatches, in ctx))
                    {
                        // If the job returns false, it means it we are not the last worker to process this job, so we should not mark it as complete yet.
                        continue;
                    }
                }

                _scheduler.MarkJobComplete(handle, _index);
            }
        }
    }

    public void Dispose()
    {
        _thread.Join();
    }
}