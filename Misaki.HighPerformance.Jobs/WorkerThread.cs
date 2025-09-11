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

    private JobHandle FindJob()
    {
        var handle = JobHandle.Invalid;
        if (_localQueue.TryDequeue(out handle)
            || _scheduler.TryStealJob(-1, out handle))
        {
            return handle;
        }

        while (true)
        {
            var randomIndex = _random.Next(0, _scheduler.WorkerCount);
            if (_scheduler.TryStealJob(randomIndex, out handle))
            {
                return handle;
            }
        }
    }

    private unsafe void WorkLoop()
    {
        while (!_scheduler.IsCancellationRequested)
        {
            var spinner = new SpinWait();
            for (var i = 0; i < 25; i++)
            {
                spinner.SpinOnce(-1);

                if (_scheduler.HasWork())
                {
                    // Instead of goto, we still need to go through the WaitForWork to claim a release.
                    // This causes lock and lots of branches inside the SemaphoreSlim, which lost 0.03ms.
                    // goto DoWork;
                    break;
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

            //DoWork:
            var handle = FindJob();
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