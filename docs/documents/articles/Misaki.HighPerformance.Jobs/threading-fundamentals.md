# Threading Fundamentals

The job system uses multiple worker threads to execute your code across all available CPU cores. Each worker thread picks up jobs, executes them, and coordinates with other workers through a lock-free scheduling layer.

## Multithreading

When you use the job system, your code executes over worker threads running in parallel across multiple CPU cores. Instead of tasks running one after another on the main thread, they run simultaneously on separate cores. The worker threads run in parallel to one another, and synchronize their results with the calling thread once completed.

The job system ensures there are only enough threads to match the capacity of the CPU cores. This means you can schedule as many jobs as you need without specifically needing to know how many CPU cores are available.

## Worker threads

When you create a `JobScheduler`, it spawns a configurable number of worker threads. These threads form the backbone of the system. Each worker thread runs a continuous loop:

1. Attempt to find a job to execute.
2. If no job is immediately available, spin-wait briefly.
3. If still no work, wait for a signal that a new job has been scheduled.
4. Execute the found job, then repeat.

The scheduler also reserves one **helper thread** slot for external threads that call `Wait()` with inline execution enabled. The `WorkerCount` property reports the number of managed worker threads, while `ThreadLocalCount` returns the total (workers + helper). Use `ThreadLocalCount` when allocating thread-local storage to ensure every possible executor has a valid slot.

## Thread-local queues

Each worker thread has its own set of thread-local queues. When a job is scheduled with `preferLocal: true`, the scheduler pushes the job onto the calling thread's local queue first. Workers pop from their local queue in last-in-first-out (LIFO) order, which keeps the most recently scheduled job hot in the CPU cache.

If a worker's local queues are empty, it looks to the global queues or steals work from other workers.

## Work stealing

The job system uses work stealing as part of its scheduling strategy to even out the amount of tasks shared across worker threads. Worker threads might process tasks faster than others, so once a worker thread has finished processing all of its tasks, it looks at the other worker threads' queues and then processes tasks assigned to another worker thread.

On CPUs with a mix of performance cores and efficiency cores, faster cores naturally end up stealing more work, which means the overall workload stays balanced without manual partitioning.

## Priority scheduling

Jobs can be assigned one of three priority levels. The scheduler divides dispatch slots to give each priority an appropriate share of execution time:

| Priority | Share | Use case |
|---|---|---|
| High | 50% | Critical-path work that must complete quickly |
| Normal | 37.5% | Default priority for most jobs |
| Low | 12.5% | Background tasks with no immediate deadline |

The scheduler probes queues in a cascade pattern that respects these ratios. Within each priority tier, the worker checks its local queue first, then the global queue, then attempts to steal from other workers before moving to the next tier.

## Lock-free scheduling

All scheduling operations — state transitions, dependency registration, job dispatch — use lock-free techniques such as compare-and-swap (CAS) and interlocked operations. There are no `lock` statements or `Monitor` enters on the hot path.

This design keeps overhead minimal. Thousands of jobs can be scheduled, executed, and completed per frame without kernel transitions, heap allocations, or garbage collection pauses.
