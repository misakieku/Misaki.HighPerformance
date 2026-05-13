# Best Practices and API Selection

## Which job type to use

| If you need | Use |
|---|---|
| Run one piece of work once | `IJob` |
| Run the same operation across many independent elements | `IJobParallelFor` |
| Run a parallel operation with per-batch setup overhead | `IJobParallel` |
| Full control over execution and cleanup, or dynamic dispatch | `ICustomJob<TSelf>` |
| Debug or test a job without threading overhead | `Run` / `RunRef` |

## IJob

Use `IJob` for any unit of work that can't be broken into smaller parallel pieces. Examples:

- Apply velocity to a single entity
- Compute a sum, product, or aggregate over data that's already been processed
- Trigger an action after dependencies complete

`IJob` runs once on one worker thread. If you find yourself scheduling many `IJob` instances that do the same operation, consider batching them into an `IJobParallelFor`.

## IJobParallelFor

Use `IJobParallelFor` when you need to apply the same transformation to every element of an array or buffer. The system distributes indices across worker threads in batches.

**Choose the right batch size:**

- Small batches (1-16): Best load balancing, more stealing overhead. Use when work per element varies.
- Medium batches (32-128): Good balance. A reasonable default for most workloads.
- Large batches (256+): Less overhead, but can cause uneven distribution. Use when work per element is uniform.

A good starting point is `batchSize = 64`. Profile and adjust from there.

**Avoid writing to overlapping indices.** Each index should be independent. If two indices write to the same location, you have a race condition.

## IJobParallel

Use `IJobParallel` when each batch of work has setup cost that you want to amortize. For example:

- Processing chunks of data where each chunk requires preparing local state
- Operations where computing the output for a range is cheaper per-element than per-index

The API is the same as `IJobParallelFor`, but `Execute` receives `(startIndex, endIndex)` instead of a single `index`. This lets you write loops with local accumulators or per-batch initialization.

## ICustomJob

Use `ICustomJob<TSelf>` when you need:

- A job type that isn't known at compile time (dynamic dispatch via function pointers)
- Custom cleanup logic that runs after the job completes
- To control `JobRanges` directly for non-standard iteration patterns

The overhead is slightly higher than the standard interfaces due to the function pointer indirection. Only use it when the standard interfaces don't fit.

## Scheduler configuration

**ThreadCount:** Set to `Environment.ProcessorCount` for general use. The scheduler caps at the number of logical processors. For workloads that share cores with rendering or other systems, consider leaving one or two cores free.

**DependencyChainCapacity:** This is the total number of dependency edges the scheduler can track at once. Set it to cover your peak concurrent dependencies. If you run out, jobs will still work but dependency enforcement may be incomplete. When in doubt, set it higher — unused capacity costs nothing.

**ThreadPriority:** Use `Normal` for most cases. Use `AboveNormal` if the job system is the primary consumer of CPU time and you want to prioritize it over other system threads.

## Memory and allocation

- **Pre-allocate everything.** The scheduler allocates all internal structures (queues, edge pool, slot maps) at creation. No per-job GC allocations occur during scheduling or execution.
- **Job data is copied.** When you schedule a struct job, the data is copied into an internal pool. Pointers and references remain valid for the job's lifetime.
- **Managed payloads work.** Unlike many job systems, this library supports class-based jobs and jobs holding managed types (`List`, `string`, arrays). The same zero-allocation guarantees apply.
- **Free custom resources in `ICustomJob.Free`.** If your custom job allocates unmanaged memory, the `Free` callback is the right place to release it.

## Schedule and complete timing

It's best practice to call `Schedule` on a job as soon as you have the data it needs, and don't call `Complete` on it until you need the results.

You can schedule less important jobs in a part of the frame where they aren't competing with more important jobs.

For example, if there is a period between the end of one frame and the beginning of the next frame where no jobs are running, and a one frame latency is acceptable, you can schedule the job towards the end of a frame and use its results in the following frame. Alternatively, if your application saturates that changeover period with other jobs, and there's an under-utilized period somewhere else in the frame, it's more efficient to schedule your job there instead.

## Dependencies

- **Prefer multiple dependencies over deep chains.** A job that waits on 10 handles directly is better than a chain of 10 jobs each waiting on one. This gives the scheduler more freedom to parallelize independent work.
- **Use `CombineDependencies` for large dependency sets.** If a job depends on more than a handful of other jobs, combine them to reduce scheduling overhead.

## Avoid long running jobs

Unlike threads, jobs don't yield execution. Once a job starts, that job worker thread commits to completing the job before running any other job. As such, it's best practice to break up long running jobs into smaller jobs that depend on one another, instead of submitting jobs that take a long time to complete relative to other jobs in the system.

The job system usually runs multiple chains of job dependencies, so if you break up long running tasks into multiple pieces there is a chance for multiple job chains to progress. If instead the job system is filled with long running jobs, they might completely consume all worker threads and block independent jobs from executing. This might push out the completion time of important jobs that the main thread explicitly waits for, resulting in stalls on the main thread that otherwise wouldn't exist.

In particular, long running `IJobParallelFor` jobs impact negatively on the job system because these job types intentionally try to run on as many worker threads as possible for the job batch size. If you can't break up long parallel jobs, consider increasing the batch size of your job when scheduling it to limit how many workers pick up the long running job.

## Priorities

- **Reserve High for critical-path work.** Jobs on the critical path (the chain that the main thread is waiting on) benefit most from High priority.
- **Use Low for background tasks.** Deferred work like cleanup, analytics, or pre-computation that isn't needed this frame should use Low priority.
- **Most jobs should be Normal.** Overusing High priority dilutes its effectiveness.

## Inline execution

By default, `Wait` helps execute the job inline while waiting. This reduces latency because the calling thread contributes CPU time to the work it needs. Leave this enabled unless:

- The calling thread has other work to do while waiting (use async variants instead)
- You're relying on thread-local storage and can't have an external thread execute jobs

## Thread safety

- **No two threads should write to the same memory.** Use dependencies to serialize writes.
- **Multiple readers are safe.** `IJobParallelFor` indices are independent by design — each index writes to its own location.
- **Don't access mutable static data from jobs.** The job system can't protect against race conditions on static fields.

## Additional resources

- [Creating and Scheduling Jobs](creating-jobs.md)
- [Job Dependencies and Coordination](job-dependencies.md)
- [Threading Fundamentals](threading-fundamentals.md)
