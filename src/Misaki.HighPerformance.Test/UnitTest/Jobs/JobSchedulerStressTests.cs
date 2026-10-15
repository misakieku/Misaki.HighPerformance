using Misaki.HighPerformance.Jobs;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Test.UnitTest.Jobs;

[TestClass]
public unsafe class JobSchedulerStressTests
{
    private JobScheduler _scheduler = null!;

    [TestInitialize]
    public void Setup()
    {
        var desc = new JobSchedulerDesc
        {
            ThreadCount = Environment.ProcessorCount,
            DependencyChainCapacity = 16384,
            ThreadPriority = ThreadPriority.Normal,
            HelperThreadCount = 1
        };

        _scheduler = new JobScheduler(in desc);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scheduler?.Dispose();
    }

    // =========================================================================
    // Shared Test Workloads
    // =========================================================================

    private struct WorkloadHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SimulateWork(int complexity)
        {
            if (complexity <= 0)
                return;

            // Heterogeneous work: burn CPU cycles with math
            var x = complexity;
            for (var i = 0; i < complexity * 50; i++)
            {
                x = (x * 1664525 + 1013904223);
            }

            if (x == 42)
                Trace.WriteLine("rare");
        }
    }

    private struct GrandchildJob : IJob
    {
        public int* counter;
        public int complexity;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            WorkloadHelper.SimulateWork(complexity);
            Interlocked.Increment(ref *counter);
        }
    }

    private struct ChildJob : IJob
    {
        public int* counter;
        public int complexity;
        public bool spawnGrandchild;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            WorkloadHelper.SimulateWork(complexity);
            Interlocked.Increment(ref *counter);

            if (spawnGrandchild)
            {
                var gc = new GrandchildJob
                {
                    counter = counter,
                    complexity = complexity / 2
                };

                // Schedule grandchild attached to this child
                ctx.JobScheduler.Schedule(ref gc, preferLocal: true, JobPriority.Low, ctx.SelfHandle);
            }
        }
    }

    private struct ParentSystemJob : IJob
    {
        public int* counter;
        public int childCount;
        public int complexity;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            WorkloadHelper.SimulateWork(complexity);
            Interlocked.Increment(ref *counter);

            // Schedule multiple children on worker thread with mixed priorities
            for (var i = 0; i < childCount; i++)
            {
                var child = new ChildJob
                {
                    counter = counter,
                    complexity = (i % 3 == 0) ? complexity * 2 : 0, // Mix micro and heavy jobs
                    spawnGrandchild = (i == 0)
                };

                var prio = (JobPriority)(i % 3);
                ctx.JobScheduler.Schedule(ref child, preferLocal: true, prio, ctx.SelfHandle);
            }
        }
    }

    private struct LeafCollectorJob : IJob
    {
        public int* counter;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            Interlocked.Increment(ref *counter);
        }
    }

    // =========================================================================
    // Test 1: Full Chaos DAG (Dependencies + Child Jobs + Grandchild Jobs)
    // =========================================================================

    [TestMethod]
    [Timeout(15000)] // Fail fast after 15s if deadlock occurs
    public void StressTest_ChaosDAG_WithChildJobs_MultipleIterations()
    {
        const int ITERATIONS = 200;
        const int BRANCHES = 8;
        const int CHILDREN_PER_BRANCH = 4;

        // Per branch: 1 parent + 4 children + 1 grandchild = 6 jobs
        // 8 branches = 48 jobs + 1 leaf = 49 jobs per iteration
        const int EXPECTED_PER_ITERATION = (BRANCHES * (1 + CHILDREN_PER_BRANCH + 1)) + 1;

        var counter = 0;
        var pCounter = &counter;

        var branchHandles = stackalloc JobHandle[BRANCHES];

        for (var iter = 0; iter < ITERATIONS; iter++)
        {
            counter = 0;

            // 1. Fan-out: Spawn multiple independent parent systems
            for (var b = 0; b < BRANCHES; b++)
            {
                var parent = new ParentSystemJob
                {
                    counter = pCounter,
                    childCount = CHILDREN_PER_BRANCH,
                    complexity = (b % 2 == 0) ? 0 : 200 // Alternate between micro and medium
                };

                var prio = (JobPriority)(b % 3);
                branchHandles[b] = _scheduler.Schedule(ref parent, preferLocal: false, prio);
            }

            // 2. Fan-in: Leaf depends on ALL branches
            var leafJob = new LeafCollectorJob { counter = pCounter };
            var leafHandle = _scheduler.Schedule(
                ref leafJob,
                preferLocal: false,
                JobPriority.High,
                default,
                new ReadOnlySpan<JobHandle>(branchHandles, BRANCHES));

            // Alternate between inlineExecution = true and inlineExecution = false
            var inlineExec = (iter % 2 == 0);
            _scheduler.Wait(leafHandle, inlineExecution: inlineExec);

            Assert.AreEqual(EXPECTED_PER_ITERATION, counter,
                $"Iteration {iter} failed! Expected {EXPECTED_PER_ITERATION} completed jobs, but got {counter}.");
        }
    }

    // =========================================================================
    // Test 2: Stealing Starvation (Specifically targets WorkerThread Line 86)
    // =========================================================================

    [TestMethod]
    [Timeout(10000)]
    public void StressTest_WorkerLocalStealing_AllPriorities()
    {
        const int ITERATIONS = 300;
        var counter = 0;
        var pCounter = &counter;

        for (var iter = 0; iter < ITERATIONS; iter++)
        {
            counter = 0;

            // Schedule a parent on a worker that pushes ONLY Normal & Low priority jobs to its local queue
            var parent = new ParentSystemJob
            {
                counter = pCounter,
                childCount = 6,
                complexity = 50
            };

            // Schedule parent at Normal priority
            var handle = _scheduler.Schedule(ref parent, preferLocal: true, JobPriority.Normal);

            // Wait with inlineExecution disabled to force idle workers to steal Normal & Low jobs
            _scheduler.Wait(handle, inlineExecution: false);

            Assert.AreEqual(1 + 6 + 1, counter, $"Starvation detected on iteration {iter}!");
        }
    }

    // =========================================================================
    // Test 3: Edge Pool Rapid Churn (Catches ABA and Freelist Corruption)
    // =========================================================================

    [TestMethod]
    [Timeout(10000)]
    public void StressTest_DeepDependencyDiamond_EdgeRecycling()
    {
        const int ITERATIONS = 500;
        var counter = 0;
        var pCounter = &counter;

        var deps = stackalloc JobHandle[4];

        for (var iter = 0; iter < ITERATIONS; iter++)
        {
            counter = 0;

            // Diamond: Root -> A, B, C, D -> Leaf
            var root = new GrandchildJob { counter = pCounter, complexity = 0 };
            var rootHandle = _scheduler.Schedule(ref root);

            for (var i = 0; i < 4; i++)
            {
                var mid = new GrandchildJob { counter = pCounter, complexity = (i % 2 == 0) ? 0 : 50 };
                deps[i] = _scheduler.Schedule(ref mid, JobPriority.Normal, rootHandle);
            }

            var leaf = new GrandchildJob { counter = pCounter, complexity = 0 };
            var leafHandle = _scheduler.Schedule(ref leaf, JobPriority.Normal, new ReadOnlySpan<JobHandle>(deps, 4));

            _scheduler.Wait(leafHandle, inlineExecution: (iter % 3 != 0));

            Assert.AreEqual(6, counter, $"Diamond failed at iteration {iter}");
        }
    }
}