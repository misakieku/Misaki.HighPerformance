# Creating and Scheduling Jobs

To create and run a job, you must:

1. Define a struct or class that implements one of the job interfaces.
2. Schedule the job on a `JobScheduler` instance.
3. Wait for the job to complete before accessing its results.

## Job types

| Type | Description |
|---|---|
| `IJob` | A single job that runs once on one worker thread |
| `IJobParallelFor` | A parallel job that runs `Execute` once per index |
| `IJobParallel` | A parallel job that runs `Execute` once per range segment |
| `ICustomJob<TSelf>` | A job with user-defined execution and cleanup function pointers |

## Managed and unmanaged jobs

Jobs can hold both **unmanaged** data (pointers, primitive types, blittable structs) and **managed** data (arrays, `List<T>`, strings, class references). The job scheduler copies the job data into an internal pool at schedule time and frees it after completion.

```csharp
// Managed job: holds an array reference
public struct ArraySumJob : IJob
{
    public int[] data;     // managed array
    public int* result;    // pointer to output

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        *result = sum;
    }
}
```

This differs from many job systems that restrict payloads to blittable types only. However, using managed references inside jobs means standard threading rules still apply — multiple jobs must not write to the same managed object simultaneously without proper synchronization.

## Create a scheduler

Before you can schedule jobs, create a `JobScheduler` with a description that defines the thread count and dependency capacity.

```csharp
using Misaki.HighPerformance.Jobs;

JobSchedulerDesc desc = new JobSchedulerDesc
{
    ThreadCount = Environment.ProcessorCount,
    ThreadPriority = ThreadPriority.Normal,
    DependencyChainCapacity = 64,
};

JobScheduler scheduler = new JobScheduler(in desc);
```

`ThreadCount` controls how many worker threads the scheduler spawns. `DependencyChainCapacity` is the maximum number of dependency edges the scheduler can track simultaneously. Set this to a value that covers the peak number of outstanding dependencies your workload needs.

The scheduler also reserves one helper thread slot for external threads. Use `scheduler.ThreadLocalCount` when allocating thread-local storage to ensure every possible executor has a valid slot.

## IJob

`IJob` runs a single unit of work once on one worker thread. Implement the `Execute` method with the work you want to perform.

```csharp
public struct ApplyVelocityJob : IJob
{
    public Vector3* position;
    public Vector3 velocity;
    public float deltaTime;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *position += velocity * deltaTime;
    }
}
```

To schedule the job, call `Schedule` on the scheduler. This returns a `JobHandle` that you can use to track completion.

```csharp
Vector3 pos = new Vector3(0, 0, 0);
Vector3 vel = new Vector3(10, 0, 0);

ApplyVelocityJob job = new ApplyVelocityJob
{
    position = &pos,
    velocity = vel,
    deltaTime = 0.016f,
};

JobHandle handle = scheduler.Schedule(ref job);
scheduler.Wait(handle);

// Result: pos == (0.16, 0, 0)
```

## IJobParallelFor

`IJobParallelFor` runs the same operation across a range of indices in parallel. Each worker thread picks up batches of indices, processes them, then steals remaining batches from other workers.

This is useful for updating arrays of entities, processing particle data, or any operation where each element is independent.

```csharp
public struct UpdatePositionJob : IJobParallelFor
{
    public Vector3* positions;
    public Vector3 velocity;
    public float deltaTime;

    public void Execute(int index, ref readonly JobExecutionContext ctx)
    {
        positions[index] += velocity * deltaTime;
    }
}
```

Schedule a parallel-for job with the total iteration count and the batch size. The batch size controls how many indices each worker claims at once. Smaller batches give better load balancing. Larger batches reduce stealing overhead.

```csharp
const int entityCount = 10000;

UpdatePositionJob job = new UpdatePositionJob
{
    positions = positionsPtr,
    velocity = new Vector3(10, 0, 0),
    deltaTime = 0.016f,
};

JobHandle handle = scheduler.ScheduleParallelFor(ref job, entityCount, 64);
scheduler.Wait(handle);
```

## IJobParallel

`IJobParallel` is similar to `IJobParallelFor`, but receives a start and end index instead of a single index. This is useful when the work per batch has setup overhead that you want to amortize across multiple elements.

```csharp
public struct ProcessChunkJob : IJobParallel
{
    public float* data;
    public int* output;

    public void Execute(int startIndex, int endIndex, ref readonly JobExecutionContext ctx)
    {
        float sum = 0;
        for (int i = startIndex; i < endIndex; i++)
        {
            sum += data[i];
        }
        // Store per-chunk result
        output[startIndex] = (int)sum;
    }
}
```

Schedule it the same way as a parallel-for job:

```csharp
JobHandle handle = scheduler.ScheduleParallel(ref job, totalLength, batchSize);
scheduler.Wait(handle);
```

## ICustomJob

`ICustomJob<TSelf>` gives you full control over execution and cleanup by letting you provide function pointers. This is useful when you need custom resource management or when the job's execution logic isn't known until runtime.

```csharp
public unsafe struct MyCustomJob : ICustomJob<MyCustomJob>
{
    public int* value;

    public static void Execute(ref MyCustomJob job, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
    {
        *job.value += 1;
    }

    public static void Free(ref MyCustomJob job)
    {
        // Clean up any unmanaged resources here
    }
}
```

Schedule it using `ScheduleCustom` with a `CustomJobDesc`:

```csharp
int value = 0;

MyCustomJob customJob = new MyCustomJob { value = &value };

CustomJobDesc<MyCustomJob> desc = new CustomJobDesc<MyCustomJob>
{
    data = ref customJob,
    pExecutionFunc = &MyCustomJob.Execute,
    pFreeFunc = &MyCustomJob.Free,
    jobRanges = JobRanges.Single,
    priority = JobPriority.Normal,
};

JobHandle handle = scheduler.ScheduleCustom(ref desc);
scheduler.Wait(handle);
```

## Run inline

You can also run a job immediately on the calling thread. This is useful for debugging or when the work is too small to justify threading overhead.

```csharp
// IJob
job.Run(default);

// IJobParallelFor
job.Run(totalIterations, default);

// IJobParallel
job.Run(totalIterations, default);
```

For struct jobs, use `RunRef` to avoid a copy:

```csharp
ref MyJob jobRef = ref someJob;
jobRef.RunRef(default);
```

## Priority

You can assign a priority when scheduling a job.

```csharp
JobHandle handle = scheduler.Schedule(ref job, JobPriority.High);
```

For more information on how priorities affect scheduling, see [Threading Fundamentals](threading-fundamentals.md).

## preferLocal

When you schedule with `preferLocal: true`, the scheduler pushes the job onto the calling thread's local queue first. This keeps the job's data hot in the CPU cache for that thread.

```csharp
JobHandle handle = scheduler.Schedule(ref job, preferLocal: true);
```

Use this when the calling thread is likely to be the one that executes the job, such as when scheduling from a dedicated system thread.

## Wait for completion

After scheduling, call one of the wait methods to block until the job finishes:

```csharp
// Block until a single job completes
scheduler.Wait(handle);

// Block until all specified jobs complete
scheduler.WaitAll(handle1, handle2);

// Block until any of the specified jobs completes
JobHandle completed = scheduler.WaitAny(handle1, handle2);
```

By default, `Wait` helps execute the job inline while waiting. Pass `inlineExecution: false` to disable this.

## Dispose

When you no longer need the scheduler, call `Dispose` to stop all worker threads and release resources:

```csharp
scheduler.Dispose();
```

## Additional resources

- [Threading Fundamentals](threading-fundamentals.md)
- [Job Dependencies and Coordination](job-dependencies.md)
- [Best Practices and API Selection](best-practices.md)
