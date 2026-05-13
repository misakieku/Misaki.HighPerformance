# Job Dependencies and Coordination

Often, one job depends on the results of another job. For example, job A might write velocity data that job B reads to update positions. You must tell the scheduler about such a dependency when you schedule the dependent job. The scheduler won't run the dependent job until all jobs it depends on have finished.

A job can depend on any number of other jobs. You can also create chains of jobs where each job depends on the previous one. However, dependencies delay job execution, so you should design your dependency graph to allow independent chains to run in parallel.

## Dependencies on completed jobs

If the job you're depending on has already completed by the time you schedule the dependent job, the scheduler detects this and skips the wait. The dependent job becomes eligible to run immediately. This means there's no penalty for passing handles that are already complete — safe to use in patterns where the completion timing isn't guaranteed.

## Single dependency

Pass a `JobHandle` from one job's schedule call as a dependency to the next.

```csharp
using Misaki.HighPerformance.Jobs;

public unsafe struct AddJob : IJob
{
    public float* result;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *result += 1;
    }
}

float result = 0;

AddJob jobA = new AddJob { result = &result };
JobHandle handleA = scheduler.Schedule(ref jobA);

AddJob jobB = new AddJob { result = &result };
JobHandle handleB = scheduler.Schedule(ref jobB, handleA);

scheduler.Wait(handleB);
// result == 2
```

Job B won't start until job A completes. Because both jobs write to the same data, the dependency ensures there is no race condition.

## Multiple dependencies

A job can wait on several jobs at once. Pass multiple handles to `Schedule`.

```csharp
JobHandle handle1 = scheduler.Schedule(ref job1);
JobHandle handle2 = scheduler.Schedule(ref job2);

// Job 3 waits for both job1 and job2 to finish
JobHandle handle3 = scheduler.Schedule(ref job3, handle1, handle2);
scheduler.Wait(handle3);
```

## Combined dependencies

For a large number of dependencies, use `CombineDependencies` to create a single handle that represents all of them. This avoids deep dependency chains and reduces scheduling overhead.

```csharp
// Collect handles from many scheduled jobs
JobHandle handle1 = scheduler.Schedule(ref job1);
JobHandle handle2 = scheduler.Schedule(ref job2);
JobHandle handle3 = scheduler.Schedule(ref job3);

// Combine into one handle, then pass as a single dependency
JobHandle combined = scheduler.CombineDependencies(handle1, handle2, handle3);
JobHandle finalHandle = scheduler.Schedule(ref finalJob, combined);
scheduler.Wait(finalHandle);
```

## Full example

The following example chains three jobs together: add a value to each element of an array, multiply each element, then compute the sum.

```csharp
using Misaki.HighPerformance.Jobs;

public unsafe struct ParallelAddJob : IJobParallel
{
    public float value;
    public float* inout;

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (int i = startIndex; i < endIndex; i++)
            inout[i] += value;
    }
}

public unsafe struct ParallelMultiplyJob : IJobParallel
{
    public float multiplier;
    public float* inout;

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        for (int i = startIndex; i < endIndex; i++)
            inout[i] *= multiplier;
    }
}

public unsafe struct SumJob : IJob
{
    public float* input;
    public int length;
    public float* output;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        float sum = 0;
        for (int i = 0; i < length; i++)
            sum += input[i];
        *output = sum;
    }
}

const int arraySize = 10000;
float* data = stackalloc float[arraySize];
float result = 0;

// Chain: add -> multiply -> sum
JobHandle handle1 = scheduler.ScheduleParallel(ref new ParallelAddJob { value = 10f, inout = data }, arraySize, 64);
JobHandle handle2 = scheduler.ScheduleParallel(ref new ParallelMultiplyJob { multiplier = 2f, inout = data }, arraySize, 64, handle1);
JobHandle handle3 = scheduler.Schedule(ref new SumJob { input = data, length = arraySize, output = &result }, handle2);

scheduler.Wait(handle3);
```

## Async wait

The scheduler provides async variants that offload the wait to the thread pool. This lets the calling thread continue other work while waiting.

```csharp
// Wait asynchronously for a single job
await scheduler.WaitAsync(handle);

// Wait asynchronously for all jobs
await scheduler.WaitAllAsync(new Memory<JobHandle>(new[] { handle1, handle2 }));

// Wait asynchronously for any job to complete
JobHandle completed = await scheduler.WaitAnyAsync(new ReadOnlyMemory<JobHandle>(new[] { handle1, handle2 }));
```

Unlike synchronous `Wait`, the async variants do **not** execute the job inline on the calling thread. The wait is fully offloaded to the thread pool, so the calling thread can continue other work without contributing CPU time to the job's completion.

Each async method accepts an optional `CancellationToken` to cancel the wait.

```csharp
var cts = new CancellationTokenSource();
await scheduler.WaitAsync(handle, cts.Token);
```

## WaitAll and WaitAny

The synchronous variants reorder the collection in-place, moving completed handles to the front. This allows you to efficiently check which handles are still pending.

```csharp
// After WaitAll, completed handles are at the front of the span
scheduler.WaitAll(handles);

// WaitAny returns the first handle that completed
JobHandle firstCompleted = scheduler.WaitAny(handle1, handle2);
```

## Get job status

You can check a job's current state without waiting.

```csharp
JobState state = scheduler.GetJobStatus(handle);
// Returns Created, Scheduled, Running, Completed, or Invalid
```

## Additional resources

- [Creating and Scheduling Jobs](creating-jobs.md)
- [Threading Fundamentals](threading-fundamentals.md)
- [Best Practices and API Selection](best-practices.md)
