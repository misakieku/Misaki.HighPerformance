using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

public readonly struct JobHandle(int jobCount) : IDisposable
{
    private readonly CountdownEvent _jobCompletionEvent = new(jobCount);

    public readonly bool IsCompleted => _jobCompletionEvent.IsSet;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void CompleteOne()
    {
        _jobCompletionEvent.Signal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WaitComplete()
    {
        _jobCompletionEvent.Wait();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _jobCompletionEvent.Dispose();
    }
}