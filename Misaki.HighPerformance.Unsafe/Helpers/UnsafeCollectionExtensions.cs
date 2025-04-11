using Misaki.HighPerformance.Unsafe.Collections.Contracts;

namespace Misaki.HighPerformance.Unsafe.Helpers;

/// <summary>
/// Provides extension methods for copying elements between unsafe collections and spans, converting collections to
/// arrays or lists, and searching for values.
/// </summary>
public unsafe static class UnsafeCollectionExtensions
{
    /// <summary>
    /// Copies elements from a source UnsafeCollection to a destination Span, ensuring both have the same size.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="source">Represents the source collection from which elements are copied.</param>
    /// <param name="destination">Represents the target span where elements are copied to.</param>
    /// <exception cref="ArgumentException">Thrown when the sizes of the source collection and destination span do not match.</exception>
    public static void CopyTo<T>(this IUnsafeCollection<T> source, Span<T> destination)
        where T : unmanaged
    {
        if (source.Count > destination.Length)
        {
            throw new ArgumentException("Source collection is larger than the destination span.");
        }

        fixed (T* ptr = destination)
        {
            SystemUnsfae.CopyBlock(ptr, source.GetUnsafePtr(), (uint)(source.Count * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies a range of elements from a source collection to a destination span, ensuring both are adequately sized.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be a value type.</typeparam>
    /// <param name="source">The collection from which elements are copied.</param>
    /// <param name="destination">The span where the elements will be copied to.</param>
    /// <param name="sourceIndex">The starting index in the source collection for the copy operation.</param>
    /// <param name="destinationIndex">The starting index in the destination span where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source to the destination.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source collection or destination span.</exception>
    public static void CopyTo<T>(this IUnsafeCollection<T> source, Span<T> destination, int sourceIndex, int destinationIndex, int length)
        where T : unmanaged
    {
        if (sourceIndex + length > source.Count || destinationIndex + length > destination.Length)
        {
            throw new ArgumentException("Source collection or destination span is too small for the specified range.");
        }

        fixed (T* ptr = destination)
        {
            SystemUnsfae.CopyBlock(ptr + destinationIndex, (byte*)source.GetUnsafePtr() + (sourceIndex * sizeof(T)), (uint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies elements from a source span to a destination unsafe collection, ensuring both have the same size.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="destination">Represents the unsafe collection that will receive the copied elements.</param>
    /// <param name="source">Represents the span containing the elements to be copied to the unsafe collection.</param>
    /// <exception cref="ArgumentException">Thrown when the source span and destination collection have different sizes.</exception>
    public static void CopyFrom<T>(this IUnsafeCollection<T> destination, Span<T> source)
        where T : unmanaged
    {
        if (destination.Count > source.Length)
        {
            throw new ArgumentException("Destination collection is larger than the source span.");
        }

        fixed (T* ptr = source)
        {
            SystemUnsfae.CopyBlock(destination.GetUnsafePtr(), ptr, (uint)(source.Length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies a specified range of elements from a source span to a destination collection.
    /// </summary>
    /// <typeparam name="T">Represents the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="destination">The collection where elements will be copied to.</param>
    /// <param name="source">The span containing the elements to be copied.</param>
    /// <param name="sourceIndex">The starting index in the source span from which to begin copying.</param>
    /// <param name="destinationIndex">The starting index in the destination collection where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source span to the destination collection.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source span or destination collection.</exception>
    public static void CopyFrom<T>(this IUnsafeCollection<T> destination, Span<T> source, int sourceIndex, int destinationIndex, int length)
        where T : unmanaged
    {
        if (sourceIndex + length > source.Length || destinationIndex + length > destination.Count)
        {
            throw new ArgumentException("Source span or destination collection is too small for the specified range.");
        }

        fixed (T* ptr = source)
        {
            SystemUnsfae.CopyBlock((byte*)destination.GetUnsafePtr() + (destinationIndex * sizeof(T)), ptr + sourceIndex, (uint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Converts an UnsafeCollection of unmanaged types into a standard collection.
    /// </summary>
    /// <typeparam name="T">Represents a type that is unmanaged, allowing for direct memory manipulation.</typeparam>
    /// <param name="source">The UnsafeCollection instance that contains the data to be converted.</param>
    /// <returns>A new collection containing the elements from the UnsafeCollection.</returns>
    public static T[] ToArray<T>(this IUnsafeCollection<T> source)
        where T : unmanaged
    {
        var array = new T[source.Count];
        fixed (T* ptr = array)
        {
            SystemUnsfae.CopyBlock(ptr, source.GetUnsafePtr(), (uint)(source.Count * sizeof(T)));
        }

        return array;
    }

    /// <summary>
    /// Converts an unmanaged collection into a list by copying its elements into a new list.
    /// </summary>
    /// <typeparam name="T">Represents a type that is unmanaged, allowing for direct memory manipulation.</typeparam>
    /// <param name="source">The collection from which elements are copied to create the new list.</param>
    /// <returns>A list containing the elements from the specified unmanaged collection.</returns>
    public static List<T> ToList<T>(this IUnsafeCollection<T> source)
        where T : unmanaged
    {
        var list = new List<T>(source.Count);
        fixed (T* ptr = list.ToArray())
        {
            SystemUnsfae.CopyBlock(ptr, source.GetUnsafePtr(), (uint)(source.Count * sizeof(T)));
        }
        return list;
    }

    /// <summary>
    /// Converts an UnsafeCollection into a Span for efficient memory access.
    /// </summary>
    /// <typeparam name="T">Represents a type that can be stored in unmanaged memory.</typeparam>
    /// <param name="source">The UnsafeCollection instance to be converted into a Span.</param>
    /// <returns>A Span that provides a view over the elements of the UnsafeCollection.</returns>
    public static Span<T> AsSpan<T>(this IUnsafeCollection<T> source)
        where T : unmanaged
    {
        return new(source.GetUnsafePtr(), source.Count);
    }

    /// <summary>
    /// Finds the index of a specified value in a collection. Returns -1 if the value is not found.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection, which must support equality comparison.</typeparam>
    /// <param name="source">The collection to search for the specified value.</param>
    /// <param name="value">The value to locate within the collection.</param>
    /// <param name="index">Outputs the index of the found value or -1 if not found.</param>
    public static void IndexOf<T>(this IUnsafeCollection<T> source, T value, out int index)
        where T : unmanaged, IEquatable<T>
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (UnsafeUtilities.ReadArrayElement<T>(source.GetUnsafePtr(), i).Equals(value))
            {
                index = i;
                return;
            }
        }
        index = -1;
    }

    /// <summary>
    /// Checks if a specified value exists within an unsafe collection of unmanaged types.
    /// </summary>
    /// <typeparam name="T">Represents a type that is unmanaged and supports equality comparison.</typeparam>
    /// <param name="source">The collection being searched for the specified value.</param>
    /// <param name="value">The value being searched for within the collection.</param>
    /// <returns>Returns true if the value is found; otherwise, returns false.</returns>
    public static bool Conations<T>(this IUnsafeCollection<T> source, T value)
        where T : unmanaged, IEquatable<T>
    {
        source.IndexOf(value, out var index);
        return index != -1;
    }
}