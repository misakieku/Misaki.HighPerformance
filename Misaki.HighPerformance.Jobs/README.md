# Misaki.HighPerformance.Jobs

A zero-allocation-oriented job system for C#.

This package provides job contracts, scheduling, worker threads, dependency handling, and temporary allocation support for high-throughput work execution.

## What it includes

- single-job execution
- parallel-for job execution
- parallel range jobs
- job handles and dependency tracking
- worker thread management
- temporary job allocation support

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

var job = new AddJob
{
    pA = &a,
    pB = &b,
    pResult = &result
};

JobHandle handle = jobScheduler.Schedule(job);
jobScheduler.Wait(handle);

```

## Package reference

```bash
dotnet add package Misaki.HighPerformance.Jobs
```

## Notes

This project targets `net10.0`, enables unsafe code, and is packaged as content files for downstream consumption.
