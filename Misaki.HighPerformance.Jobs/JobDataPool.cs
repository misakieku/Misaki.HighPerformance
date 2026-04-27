using Misaki.HighPerformance.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Jobs;

/// <summary>
/// This class manages pools of job data for different types. It allows allocating, retrieving, and freeing job data instances using unique IDs and generations to ensure safe access and reuse of resources.
/// </summary>
public static class JobDataPool<T>
{
    private static readonly ConcurrentSlotMap<T> s_slots = new ConcurrentSlotMap<T>(8);

    /// <summary>
    /// Allocates a new instance of type T in the pool and returns its ID and generation.
    /// </summary>
    /// <typeparam name="T">The type of the data to allocate.</typeparam>
    /// <param name="data">The data to allocate.</param>
    /// <param name="generation">The generation of the allocated data.</param>
    /// <returns>The ID of the allocated data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Allocate(ref readonly T data, out int generation)
    {
        return s_slots.Add(data, out generation);
    }

    /// <summary>
    /// Gets a reference to the data of type T associated with the given ID and generation. The 'exists' output parameter indicates whether the data exists in the pool.
    /// </summary>
    /// <typeparam name="T">The type of the data to retrieve.</typeparam>
    /// <param name="id">The ID of the data to retrieve.</param>
    /// <param name="generation">The generation of the data to retrieve.</param>
    /// <param name="exists">A value indicating whether the data exists in the pool.</param>
    /// <returns>A reference to the requested data. Undefined if 'exists' is false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference(int id, int generation, out bool exists)
    {
        return ref s_slots.GetElementReferenceAt(id, generation, out exists);
    }

    /// <summary>
    /// Frees the data of type T associated with the given ID and generation, making it available for future allocations. After calling this method, the ID and generation can be reused for new data.
    /// </summary>
    /// <typeparam name="T">The type of the data to free.</typeparam>
    /// <param name="id">The ID of the data to free.</param>
    /// <param name="generation">The generation of the data to free.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(int id, int generation)
    {
        s_slots.Remove(id, generation);
    }
}