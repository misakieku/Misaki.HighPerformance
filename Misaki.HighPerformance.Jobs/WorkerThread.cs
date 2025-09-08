using System.Collections.Concurrent;

namespace Misaki.HighPerformance.Jobs;

internal class WorkerThread : IDisposable
{
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

    private unsafe void WorkLoop()
    {
        while (!_scheduler.IsCancellationRequested)
        {
            var handle = JobHandle.Invalid;

            // Always try the local thread and main thread queue first.
            if (!_localQueue.TryDequeue(out handle)
                && !_scheduler.TryStealJob(-1, out handle))
            {
                var randomIndex = _random.Next(0, _scheduler.WorkerCount);
                if (_scheduler.TryStealJob(randomIndex, out var tempHandle))
                {
                    handle = tempHandle;
                }
            }

            ref var jobInfo = ref _scheduler.GetJobInfoReference(handle, out var exist);

            if (exist)
            {
                Interlocked.CompareExchange(ref jobInfo.status, JobStatus.Running, JobStatus.Scheduled);
                var executeDelegate = jobInfo.executeDelegate;

                if (executeDelegate == null
                    || executeDelegate(jobInfo.pJobData, ref jobInfo.jobRanges, ref jobInfo.remainingBatches, _index))
                {
                    _scheduler.MarkJobComplete(handle);
                }
            }
            else
            {
                var spinner = new SpinWait();
                for (var i = 0; i < 25; i++)
                {
                    spinner.SpinOnce(-1);

                    if (_scheduler.HasWork())
                    {
                        goto FoundWork;
                    }
                }

                try
                {
                    _scheduler.WaitForWork();
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }

        FoundWork:
            ;
        }
    }

    public void Dispose()
    {
        _thread.Join();
        _localQueue.Clear();
    }
}