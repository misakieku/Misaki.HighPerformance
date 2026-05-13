using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public readonly ref struct MemoryDiagnostic : IDisposable
{
#if MHP_ENABLE_SAFETY_CHECKS
    [ThreadStatic]
    private static int s_diagnosticDepth;

    [ThreadStatic]
    private static List<MemoryHandle>? s_localAllocations;

    private readonly int _initialThreadId;
    private readonly int _startIndex;

    public MemoryDiagnostic()
    {
        _initialThreadId = Environment.CurrentManagedThreadId;
        _startIndex = s_localAllocations?.Count ?? 0;

        s_diagnosticDepth++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReserveLocalAllocation()
    {
        if (s_diagnosticDepth > 0)
        {
            s_localAllocations ??= new List<MemoryHandle>(256);

            var idx = s_localAllocations.Count;
            s_localAllocations.Add(MemoryHandle.Invalid);

            return idx;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetLocalAllocation(int idx, MemoryHandle handle)
    {
        if (idx != -1 && s_localAllocations != null)
        {
            s_localAllocations[idx] = handle;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RemoveLocalAllocation(int idx)
    {
        if (s_diagnosticDepth > 0 && idx != -1 && s_localAllocations != null)
        {
            var list = s_localAllocations;
            if (idx < list.Count)
            {
                list[idx] = MemoryHandle.Invalid;

                if (idx == list.Count - 1)
                {
                    var lastActive = idx - 1;
                    while (lastActive >= 0 && list[lastActive].IsInvalid)
                    {
                        lastActive--;
                    }

                    list.RemoveRange(lastActive + 1, list.Count - (lastActive + 1));
                }
            }
        }
    }

    public void Dispose()
    {
        if (Environment.CurrentManagedThreadId != _initialThreadId)
        {
            throw new InvalidOperationException("UnsafeMemoryDiagnostic must be disposed on the same thread it was created on.");
        }

        s_diagnosticDepth--;

        if (s_localAllocations != null)
        {
            var currentCount = s_localAllocations.Count;

            if (currentCount > _startIndex)
            {
                var leakedInfos = new List<AllocationInfo>();

                for (var i = _startIndex; i < currentCount; i++)
                {
                    var handle = s_localAllocations[i];
                    if (handle.IsValid && AllocationManager.TryGetAllocation(handle, out var info))
                    {
                        leakedInfos.Add(info);
                    }
                }

                if (leakedInfos.Count > 0)
                {
                    throw new MemoryLeakException(leakedInfos);
                }
            }
        }
    }
#else
    public MemoryDiagnostic() { }
    public void Dispose() { }
#endif
}

#if false
// This is the Union-Find Jump List optimization. 
// It guarantees O(1) worst-case algorithmic complexity for array trimming,
// but is technically slower in C# benchmarks than the Tombstone while-loop.
// The slowdown is due to increased struct size causing L1 cache misses, 
// and the added branches/pointer chasing out-of-order frees required,
// compared to the linear cache-line iteration of the simple while loop.

// public readonly struct AllocationInfo
// {
//     public int ThreadLocalIndex { get; init; }
//     public int NearestReleased { get; set; } // Adds extra layout padding/cache line breaks
//     // ...
// }

// public static bool RemoveAllocation(MemoryHandle handle)
// {
//     // ... (Inside safety check remove logic) ...
//     
//     int idx = info.ThreadLocalIndex;
//     
//     // Branch predicting nightmares:
//     if (idx - 1 >= 0 && list[idx - 1].IsReleased)
//         info.NearestReleased = list[idx - 1].NearestReleased;
//     else
//         info.NearestReleased = idx;
//
//     if (idx + 1 < list.Count && list[idx + 1].IsReleased)
//         list[idx + 1].NearestReleased = info.NearestReleased;
// 
//     if (idx == list.Count - 1)
//     {
//         list.RemoveRange(info.NearestReleased, list.Count - info.NearestReleased);
//     }
//     // ...
// }
#endif