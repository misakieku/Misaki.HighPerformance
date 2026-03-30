using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Utilities;

/// <summary>
/// Provides extension methods for copying elements between unsafe collections and spans, converting collections to
/// arrays or lists, and searching for values.
/// </summary>
public static unsafe class UnsafeCollectionExtensions
{
    /// <summary>
    /// Converts a managed array to an UnsafeArray by copying its elements to unmanaged memory.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array, which must be unmanaged.</typeparam>
    /// <param name="source">The managed array to convert.</param>
    /// <param name="allocator">The allocator to use for memory allocation of the UnsafeArray.</param>
    /// <returns>A new UnsafeArray containing a copy of the source array elements.</returns>
    public static UnsafeArray<T> ToUnsafeArray<T>(this T[] source, Allocator allocator)
        where T : unmanaged
    {
        var array = new UnsafeArray<T>(source.Length, allocator);
        fixed (T* pSrc = source)
        {
            MemCpy(array.GetUnsafePtr(), pSrc, (uint)(source.Length * sizeof(T)));
        }

        return array;
    }

    /// <summary>
    /// Converts a managed List to an UnsafeList by copying its elements to unmanaged memory.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list, which must be unmanaged.</typeparam>
    /// <param name="source">The managed List to convert.</param>
    /// <param name="allocator">The allocator to use for memory allocation of the UnsafeList.</param>
    /// <returns>A new UnsafeList containing a copy of the source list elements.</returns>
    public static UnsafeList<T> ToUnsafeList<T>(this List<T> source, Allocator allocator)
        where T : unmanaged
    {
        var list = new UnsafeList<T>(source.Count, allocator);
        fixed (T* pSrc = CollectionsMarshal.AsSpan(source))
        {
            MemCpy(list.GetUnsafePtr(), pSrc, (uint)(source.Count * sizeof(T)));
        }

        return list;
    }
}
