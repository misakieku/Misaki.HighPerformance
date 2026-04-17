using System.Collections.Concurrent;

namespace Misaki.HighPerformance.Jobs;

internal class WorkerThread : IDisposable
{
    private const int _MAX_STEAL_ATTEMPTS = 8;

    private readonly int _index;
    private readonly Thread _thread;
    private readonly ConcurrentQueue<JobHandle> _localQueue;

    private readonly JobScheduler _scheduler;
    private readonly Random _random;

    internal ConcurrentQueue<JobHandle> LocalQueue => _localQueue;

    public WorkerThread(int index, JobScheduler scheduler, ThreadPriority priority)
    {
        _index = index;
        _localQueue = new();
        _scheduler = scheduler;
        _random = new Random(index * 9973 + Environment.TickCount);

        _thread = new Thread(WorkLoop)
        {
            IsBackground = true,
            Name = $"WorkerThread-{index}",
            Priority = priority
        };
    }

    public void Start() => _thread.Start();

    private bool TryFindJob(out JobHandle handle)
    {
        if (_localQueue.TryDequeue(out handle))
        {
            return true;
        }

        if (_scheduler.TryStealFromMain(-1, out handle))
        {
            return true;
        }

        for (var i = 0; i < _MAX_STEAL_ATTEMPTS; i++)
        {
            var randomIndex = _random.Next(0, _scheduler.WorkerCount);
            if (randomIndex != _index && _scheduler.TryStealFromWorker(randomIndex, out handle))
            {
                return true;
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

                _scheduler.MarkJobComplete(handle);
            }
        }
    }

    public void Dispose()
    {
        _thread.Join();
    }
}