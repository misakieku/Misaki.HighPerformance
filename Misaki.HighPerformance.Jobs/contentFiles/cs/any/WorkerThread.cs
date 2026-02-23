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

    public WorkerThread(int index, JobScheduler scheduler)
    {
        _index = index;
        _localQueue = new();
        _scheduler = scheduler;
        _random = new Random(index * 9973 + Environment.TickCount);

        _thread = new Thread(WorkLoop)
        {
            IsBackground = true,
            Name = $"WorkerThread-{index}"
        };
    }

    public void Start() => _thread.Start();

    private bool TryFindJob(out JobHandle handle)
    {
        if (Interlocked.CompareExchange(ref _scheduler._totalJobCount, 0, 0) == 0)
        {
            handle = JobHandle.Invalid;
            return false;
        }

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
            if (exist && Interlocked.CompareExchange(ref jobInfo.state, JobState.Running, JobState.Scheduled) == JobState.Scheduled)
            {
                if (jobInfo.pExecutionFunc == null
                    || jobInfo.pExecutionFunc(jobInfo.pJobData, ref jobInfo.jobRanges, ref jobInfo.remainingBatches, _index))
                {
                    _scheduler.MarkJobComplete(handle);
                }
            }
        }
    }

    public void Dispose()
    {
        _thread.Join();
        _localQueue.Clear();
    }
}