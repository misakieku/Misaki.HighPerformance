using Misaki.HighPerformance.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

internal static class JobDataPool<T>
{
    private static readonly ConcurrentSlotMap<T> s_slots = new ConcurrentSlotMap<T>(8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Allocate(ref readonly T data, out int generation)
    {
        return s_slots.Add(data, out generation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference(int id, int generation, out bool exists)
    {
        return ref s_slots.GetElementReferenceAt(id, generation, out exists);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(ref readonly JobInfo jobInfo)
    {
        s_slots.Remove(jobInfo.dataID, jobInfo.dataGeneration);
    }
}