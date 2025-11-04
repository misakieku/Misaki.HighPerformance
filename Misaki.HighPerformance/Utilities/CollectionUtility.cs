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
    /// <returns>The modified list after the element has been removed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index is less than 0 or greater than or equal to the number of elements in the list.</exception>
    public static List<T> RemoveAndSwapBack<T>(this List<T> list, int index)
    {
        var lastIndex = list.Count - 1;
        if (index < 0 || index > lastIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        if (index != lastIndex)
        {
            list[index] = list[lastIndex];
        }
        
        list.RemoveAt(lastIndex);
        return list;
    }
}