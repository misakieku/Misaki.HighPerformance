using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// High-performance job scheduler that manages job execution and dependencies.
/// Designed to minimize allocations and provide efficient work distribution.
/// </summary>
public static unsafe class JobScheduler
{
    private const int _Init_INLINE_JOBS = 64;
    private const int _MAX_WORKER_THREADS = 64;

    private static readonly Lock _lock = new();
    private static SlotMap<JobData>? _jobPool;
    private static int _jobVersion;
    private static volatile bool _isInitialized;
    private static volatile bool _isShuttingDown;

    // Worker thread management
    private static Thread[]? _workerThreads;
    private static int _workerThreadCount;
    private static readonly ManualResetEventSlim _workAvailableEvent = new ManualResetEventSlim(false);
    private static readonly ConcurrentQueue<int> _readyJobs = new ConcurrentQueue<int>();

    // Fast lookup for active jobs
    private static readonly ConcurrentDictionary<ulong, int> _activeJobs = new ConcurrentDictionary<ulong, int>();

    static JobScheduler()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes the job scheduler with default settings.
    /// </summary>
    public static void Initialize(int InitialJobsSize = _Init_INLINE_JOBS, int workerThreadCount = -1)
    {
        if (_isInitialized)
            return;

        lock (_lock)
        {
            if (_isInitialized)
                return;

            _jobPool = new SlotMap<JobData>(InitialJobsSize);

            // Initialize worker threads
            if (workerThreadCount <= 0)
                workerThreadCount = Math.Min(Environment.ProcessorCount, _MAX_WORKER_THREADS);

            _workerThreadCount = workerThreadCount;
            _workerThreads = new Thread[workerThreadCount];

            for (var i = 0; i < workerThreadCount; i++)
            {
                var thread = new Thread(WorkerThreadLoop)
                {
                    Name = $"JobWorker-{i}",
                    IsBackground = true
                };
                _workerThreads[i] = thread;
                thread.Start(i);
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Shuts down the job scheduler and cleans up resources.
    /// </summary>
    public static void Shutdown()
    {
        if (!_isInitialized || _isShuttingDown)
            return;

        lock (_lock)
        {
            if (_isShuttingDown)
                return;
            _isShuttingDown = true;

            // Signal all worker threads to exit
            _workAvailableEvent.Set();

            // Wait for all worker threads to complete
            if (_workerThreads != null)
            {
                for (var i = 0; i < _workerThreadCount; i++)
                {
                    _workerThreads[i]?.Join(5000); // 5 second timeout
                }
            }

            _jobPool?.Clear();
            _jobPool = null;

            _isInitialized = false;
            _isShuttingDown = false;
        }
    }

    /// <summary>
    /// Schedules a job for execution.
    /// </summary>
    internal static JobHandle ScheduleJob(object jobData, ExecuteJobDelegate executeFunction, JobType jobType,
        int totalIterations, int batchSize, JobHandle dependsOn)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("JobScheduler is not initialized");

        var jobSlot = AllocateJobSlot();
        var jobId = JobsUtility.GetNextJobId();
        var version = Interlocked.Increment(ref _jobVersion);

        ref var job = ref _jobPool![jobSlot];
        job.Id = jobId;
        job.Version = version;
        job.State = 0; // Scheduled
        job.JobType = jobType;
        job.ExecuteJobFunction = executeFunction;
        job.ExecuteParallelJobFunction = null;
        job.JobDataObject = jobData;
        job.TotalIterations = totalIterations;
        job.BatchSize = batchSize;
        job.DependencyCount = dependsOn._id == 0 ? 0 : 1;
        job.CompletedDependencies = 0;
        job.AdditionalDependencies = null;
        job.AdditionalDependencyCount = 0;

        // Set up dependencies
        if (dependsOn._id != 0)
        {
            job.Dependencies[0] = dependsOn._id;
        }

        _activeJobs.TryAdd(jobId, jobSlot);

        // Check if job can be executed immediately
        if (job.CanExecute)
        {
            _readyJobs.Enqueue(jobSlot);
            _workAvailableEvent.Set();
        }

        return new JobHandle(jobId, version);
    }

    /// <summary>
    /// Schedules a parallel job for execution.
    /// </summary>
    internal static JobHandle ScheduleParallelJob(object jobData, ExecuteParallelJobDelegate executeFunction,
        int totalIterations, int batchSize, JobHandle dependsOn)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("JobScheduler is not initialized");

        var jobSlot = AllocateJobSlot();
        var jobId = JobsUtility.GetNextJobId();
        var version = Interlocked.Increment(ref _jobVersion);

        ref var job = ref _jobPool![jobSlot];
        job.Id = jobId;
        job.Version = version;
        job.State = 0; // Scheduled
        job.JobType = JobType.ParallelFor;
        job.ExecuteJobFunction = null;
        job.ExecuteParallelJobFunction = executeFunction;
        job.JobDataObject = jobData;
        job.TotalIterations = totalIterations;
        job.BatchSize = batchSize;
        job.DependencyCount = dependsOn._id == 0 ? 0 : 1;
        job.CompletedDependencies = 0;
        job.AdditionalDependencies = null;
        job.AdditionalDependencyCount = 0;

        // Set up dependencies
        if (dependsOn._id != 0)
        {
            job.Dependencies[0] = dependsOn._id;
        }

        _activeJobs.TryAdd(jobId, jobSlot);

        // Check if job can be executed immediately
        if (job.CanExecute)
        {
            _readyJobs.Enqueue(jobSlot);
            _workAvailableEvent.Set();
        }

        return new JobHandle(jobId, version);
    }

    /// <summary>
    /// Combines multiple job dependencies into a single handle.
    /// </summary>
    internal static JobHandle CombineDependencies(ReadOnlySpan<JobHandle> dependencies)
    {
        if (dependencies.Length == 0)
        {
            return JobHandle.Completed;
        }

        if (dependencies.Length == 1)
        {
            return dependencies[0];
        }

        // Filter out completed dependencies
        var activeDeps = stackalloc JobHandle[dependencies.Length];
        var activeCount = 0;

        for (var i = 0; i < dependencies.Length; i++)
        {
            if (dependencies[i]._id != 0 && !IsCompleted(dependencies[i]))
            {
                activeDeps[activeCount++] = dependencies[i];
            }
        }

        if (activeCount == 0)
            return JobHandle.Completed;

        if (activeCount == 1)
            return activeDeps[0];

        // Create a combined dependency job
        var jobSlot = AllocateJobSlot();
        var jobId = JobsUtility.GetNextJobId();
        var version = Interlocked.Increment(ref _jobVersion);

        ref var job = ref _jobPool![jobSlot];
        job.Id = jobId;
        job.Version = version;
        job.State = 0; // Scheduled
        job.JobType = JobType.Job; // Dependency-only job
        job.ExecuteJobFunction = null; // No execution needed
        job.ExecuteParallelJobFunction = null;
        job.JobDataObject = null;
        job.TotalIterations = 0;
        job.BatchSize = 0;
        job.DependencyCount = activeCount;
        job.CompletedDependencies = 0;

        // Set up dependencies
        for (var i = 0; i < Math.Min(activeCount, 8); i++)
        {
            job.Dependencies[i] = activeDeps[i]._id;
        }

        // Handle additional dependencies if more than 8
        if (activeCount > 8)
        {
            var additionalSize = activeCount - 8;
            var handle = AllocationManager.GetAllocationHandle(Allocator.Temp);
            job.AdditionalDependencies = (ulong*)handle.Alloc(handle.Allocator, (nuint)(sizeof(ulong) * additionalSize), sizeof(ulong), AllocationOption.None);
            job.AdditionalDependencyCount = additionalSize;

            for (var i = 0; i < additionalSize; i++)
            {
                job.AdditionalDependencies[i] = activeDeps[i + 8]._id;
            }
        }

        _activeJobs.TryAdd(jobId, jobSlot);

        return new JobHandle(jobId, version);
    }

    /// <summary>
    /// Checks if a job is completed.
    /// </summary>
    internal static bool IsCompleted(JobHandle handle)
    {
        if (handle._id == 0)
            return true;

        if (_activeJobs.TryGetValue(handle._id, out var jobSlot))
        {
            return _jobPool![jobSlot].IsCompleted;
        }

        return true; // Job not found, assume completed
    }

    /// <summary>
    /// Blocks until the specified job completes.
    /// </summary>
    internal static void Complete(JobHandle handle)
    {
        if (handle._id == 0)
            return;

        while (!IsCompleted(handle))
        {
            // Try to help with work while waiting
            if (_readyJobs.TryDequeue(out var jobSlot))
            {
                ExecuteJob(jobSlot);
            }
            else
            {
                Thread.Yield();
            }
        }
    }

    private static int AllocateJobSlot()
    {
        // Create a new JobData and add it to the SlotMap
        var jobData = new JobData();
        return _jobPool!.Add(jobData);
    }

    private static void WorkerThreadLoop(object? threadIndexObj)
    {
        var threadIndex = (int)threadIndexObj!;

        while (!_isShuttingDown)
        {
            if (_readyJobs.TryDequeue(out var jobSlot))
            {
                ExecuteJob(jobSlot);
            }
            else
            {
                _workAvailableEvent.Wait(100); // Wait with timeout
                _workAvailableEvent.Reset();
            }
        }
    }

    private static void ExecuteJob(int jobSlot)
    {
        ref var job = ref _jobPool![jobSlot];

        // Mark as running
        if (Interlocked.CompareExchange(ref job.State, 1, 0) != 0)
            return; // Job already taken by another thread

        try
        {
            if (job.ExecuteJobFunction != null)
            {
                if (job.JobType == JobType.Job)
                {
                    // Execute IJob
                    job.ExecuteJobFunction(job.JobDataObject!);
                }
            }
            else if (job.ExecuteParallelJobFunction != null)
            {
                if (job.JobType == JobType.ParallelFor)
                {
                    // Execute IJobParallelFor
                    ExecuteParallelJob(ref job);
                }
            }
        }
        finally
        {
            // Mark as completed
            Volatile.Write(ref job.State, 2);

            // Clean up additional dependencies
            if (job.AdditionalDependencies != null)
            {
                var handle = AllocationManager.GetAllocationHandle(Allocator.Temp);
                handle.Free(handle.Allocator, job.AdditionalDependencies);
                job.AdditionalDependencies = null;
            }

            // Remove from active jobs and notify dependent jobs
            _activeJobs.TryRemove(job.Id, out _);
            NotifyDependentJobs(job.Id);
        }
    }

    private static void ExecuteParallelJob(ref JobData job)
    {
        var batchSize = job.BatchSize > 0 ? job.BatchSize : Math.Max(1, job.TotalIterations / (_workerThreadCount * 4));
        var currentIndex = 0;

        var ranges = new JobRanges
        {
            JobIndex = 0,
            BeginIndex = 0,
            EndIndex = job.TotalIterations,
            TotalLength = job.TotalIterations,
            BatchSize = batchSize,
            CurrentIndex = &currentIndex
        };

        var executeDelegate = job.ExecuteParallelJobFunction!;
        var jobDataObject = job.JobDataObject!;

        // Execute in parallel using available threads
        Parallel.For(0, _workerThreadCount, threadIndex =>
        {
            executeDelegate(jobDataObject, ref ranges, threadIndex);
        });
    }

    // TODO: Optimize by maintaining a reverse dependency graph
    private static void NotifyDependentJobs(ulong completedJobId)
    {
        // Scan for jobs that depend on this completed job
        for (var i = 0; i < _jobPool!.Count; i++)
        {
            ref var job = ref _jobPool[i];
            if (job.State == 0 && job.DependencyCount > 0) // Scheduled and has dependencies
            {
                var isDependent = false;

                // Check inline dependencies
                for (var j = 0; j < Math.Min(job.DependencyCount, 8); j++)
                {
                    if (job.Dependencies[j] == completedJobId)
                    {
                        isDependent = true;
                        break;
                    }
                }

                // Check additional dependencies
                if (!isDependent && job.AdditionalDependencies != null)
                {
                    for (var j = 0; j < job.AdditionalDependencyCount; j++)
                    {
                        if (job.AdditionalDependencies[j] == completedJobId)
                        {
                            isDependent = true;
                            break;
                        }
                    }
                }

                if (isDependent)
                {
                    var completedCount = Interlocked.Increment(ref job.CompletedDependencies);
                    if (completedCount >= job.DependencyCount)
                    {
                        _readyJobs.Enqueue(i);
                        _workAvailableEvent.Set();
                    }
                }
            }
        }
    }
}
