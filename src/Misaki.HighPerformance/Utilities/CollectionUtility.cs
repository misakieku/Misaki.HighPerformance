using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Utilities;

public static class CollectionUtility
{
    /// <summary>
    /// Creates a span over the elements of the specified list.
    /// </summary>
    /// <remarks>
    /// The span will become invalid if the list is modified (e.g., elements are added or removed).
    /// </remarks>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list whose elements the span will cover. Can be null.</param>
    /// <returns>A span over the elements of the list, or an empty span if the list is null or empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T>? list)
    {
        return CollectionsMarshal.AsSpan(list);
    }

    /// <summary>
    /// Removes the element at the specified index from the list by replacing it with the last element, then removing
    ///     the last element. This operation does not preserve the order of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list from which to remove the element. Cannot be null.</param>
    /// <param name="index">The zero-based index of the element to remove. Must be within the bounds of the list.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index is less than 0 or greater than or equal to the number of elements in the list.</exception>
    /// <returns>True if the element was successfully removed; otherwise, false.</returns>
    public static bool RemoveAndSwapBack<T>(this List<T> list, int index)
    {
        var lastIndex = list.Count - 1;
        if (index < 0 || index > lastIndex)
        {
            return false;
        }

        if (index != lastIndex)
        {
            list[index] = list[lastIndex];
        }

        list.RemoveAt(lastIndex);
        return true;
    }

    /// <summary>
    /// Returns a reference to the element at the specified index within the given span without performing bounds checking.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the span.</typeparam>
    /// <param name="span">The span from which to retrieve the element.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>A reference to the element at the specified index in the span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetElementUnsafe<T>(this Span<T> span, int index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
    }

    /// <summary>
    /// Returns a read-only reference to the element at the specified index within the given span without performing bounds checking.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the span.</typeparam>
    /// <param name="span">The read-only span from which to retrieve the element.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>A read-only reference to the element at the specified index in the span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetElementUnsafe<T>(this ReadOnlySpan<T> span, int index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
    }
}