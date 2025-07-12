namespace Misaki.HighPerformance.Jobs;

internal static class WorkerThreadPool
{
    private static readonly int _workerThreadCount = Environment.ProcessorCount;
}