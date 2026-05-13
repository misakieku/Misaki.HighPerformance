# Introduction

The job system lets you write safe, multithreaded code so your application can use all available CPU cores efficiently. It provides a zero-allocation, lock-free scheduling layer designed for game engines, simulations, and any high-throughput runtime.

## Why a dedicated job system?

Standard .NET primitives weren't designed for fine-grained game workloads:

- `Task` produces GC allocations per invocation, lacks native dependency chains, and doesn't support work stealing.
- `Parallel.For` allocates per call and offers no dependency or priority control.
- `ThreadPool` isn't built for low-latency job dispatch or batch-aware scheduling.

This library solves these problems with pre-allocated memory pools, lock-free scheduling, full DAG-based dependency tracking, and automatic work stealing across all CPU cores.

## Feature highlights

| Feature | Description |
|---|---|
| Zero allocation | All memory for scheduling and execution is pre-allocated at scheduler creation |
| Lock-free scheduling | No `lock` statements or `Monitor` enters on the hot path |
| Job dependencies | Full directed-acyclic-graph chain per job, bounded only by the global capacity set at scheduler creation |
| Work stealing | Idle workers pull work from busy workers, naturally balancing load across P-cores and E-cores |
| Priority scheduling | High (50%), Normal (37.5%), and Low (12.5%) dispatch ratios |
| Managed + unmanaged | Supports both struct jobs and class-based jobs |
| Three job contracts | `IJob`, `IJobParallelFor`, `IJobParallel`, plus `ICustomJob<TSelf>` for custom execution logic |
| Async wait | `WaitAsync`, `WaitAllAsync`, `WaitAnyAsync` for non-blocking coordination |
| Inline execution | Calling threads can help execute the job they're waiting on, reducing latency |

## Basic usage

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

JobSchedulerDesc desc = new JobSchedulerDesc
{
    ThreadCount = Environment.ProcessorCount,
    ThreadPriority = ThreadPriority.Normal,
    DependencyChainCapacity = 64,
};

JobScheduler jobScheduler = new JobScheduler(in desc);

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

Console.WriteLine($"Result: {result}"); // Output: Result: 15
```

## Who this is for

- Custom game engine developers who need a scheduling backbone without GC pauses
- Simulation and batch-processing authors who need predictable parallelism
- .NET developers who have hit the limits of `Task`-based approaches in tight loops

## Requirements

- .NET 10.0 or later
- `unsafe` code enabled

## Install

```bash
dotnet add package Misaki.HighPerformance.Jobs
```

## Additional resources

- [Threading Fundamentals](threading-fundamentals.md)
- [Creating and Scheduling Jobs](creating-jobs.md)
- [Job Dependencies and Coordination](job-dependencies.md)
- [Best Practices and API Selection](best-practices.md)
