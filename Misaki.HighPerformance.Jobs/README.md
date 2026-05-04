# Misaki.HighPerformance.Jobs

A zero-allocation-oriented job system for C#.

This package provides job contracts, scheduling, worker threads, and dependency handling for high-throughput work execution.

## What it includes

- single-job execution
- parallel-for job execution
- parallel range jobs
- job handles and dependency tracking
- worker thread management

## Highlights

- designed to minimize allocations during scheduling and execution
- supports dependency composition and wait operations
- suitable for frame-based engines, simulations, batch processing, and custom runtimes
- integrates with the low-level allocation layer

## Main types

- `IJob`
- `IJobParallelFor`
- `IJobParallel`
- `JobScheduler`
- `JobHandle`
- `JobExecutionContext`
- `JobState`
- `WorkerThread`
- `TempJobAllocator`

## Example

### IJob example

```csharp
using Misaki.HighPerformance.Jobs;

public struct AddJob : IJob
{
    public int* pA;
    public int* pB;
    public int* pResult;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        *pResult = *pA + *pB;
    }
}

int a = 5;
int b = 10;
int result = 0;

AddJob job = new AddJob
{
    pA = &a,
    pB = &b,
    pResult = &result
};

JobHandle handle = jobScheduler.Schedule(job);
jobScheduler.Wait(handle);
```

### IJobParallelFor example

```csharp
using Misaki.HighPerformance.Jobs;

public struct MultiplyJob : IJobParallelFor
{
    public int[] a;
    public int[] b;
    public int[] result;

    public void Execute(int index, ref readonly JobExecutionContext ctx)
    {
        result[index] = a[index] * b[index];
    }
}

int[] a = { 1, 2, 3, 4 };
int[] b = { 5, 6, 7, 8 };
int[] result = new int[4];

MultiplyJob job = new MultiplyJob
{
    a = a,
    b = b,
    result = result
};

JobHandle handle = jobScheduler.ScheduleParallelFor(job, a.Length, 4);
jobScheduler.Wait(handle);
```

### Custom job

```csharp

public unsafe struct CustomJob : ICustomJob<CustomJob>
{
    public int* value;

    public static void Execute(ref CustomJob job, ref JobRanges jobRanges, ref readonly JobExecutionContext ctx)
    {
        *job.value += 1;
    }

    public static void Free(ref CustomJob job)
    {
        // No resources to free in this example.
    }
}

int value = 0;

CustomJob customJob = new CustomJob
{
    value = &value
};

CustomJobDesc<CustomJob> customJobDesc = new CustomJobDesc<CustomJob>
{
    data = ref customJob,
    pExecutionFunc = &CustomJob.Execute,
    pFreeFunc = &CustomJob.Free,
    jobRanges = JobRanges.Single,
    priority = JobPriority.Normal,
};

JobHandle customJobHandle = jobScheduler.ScheduleCustom(ref customJobDesc);
jobScheduler.Wait(customJobHandle);
```

## Package reference

```bash
dotnet add package Misaki.HighPerformance.Jobs
```

## Notes

This project targets `net10.0`, enables unsafe code, and is packaged as content files for downstream consumption.
