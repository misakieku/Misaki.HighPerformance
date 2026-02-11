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
        // 1. Check own local queue first
        if (_localQueue.TryDequeue(out handle))
        {
            return true;
        }

        // 2. Check global queue
        if (_scheduler.TryStealJob(-1, out handle))
        {
            return true;
        }

        // 3. Bounded random work stealing from other workers
        for (var i = 0; i < _MAX_STEAL_ATTEMPTS; i++)
        {
            var randomIndex = _random.Next(0, _scheduler.WorkerCount);
            if (randomIndex != _index && _scheduler.TryStealJob(randomIndex, out handle))
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
            // Wait for work signal directly — the semaphore already acts as
            // both a notification and a count of available work items.
            try
            {
                _scheduler.WaitForWork();
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // After being signaled, try to find and execute a job.
            if (!TryFindJob(out var handle))
            {
                continue;
            }

            ref var jobInfo = ref _scheduler.GetJobInfoReference(handle, out var exist);

            if (exist)
            {
                Interlocked.CompareExchange(ref jobInfo.state, JobState.Running, JobState.Scheduled);
                var executeDelegate = jobInfo.pExecutionFunc;

                if (executeDelegate == null
                    || executeDelegate(jobInfo.pJobData, ref jobInfo.jobRanges, ref jobInfo.remainingBatches, _index))
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