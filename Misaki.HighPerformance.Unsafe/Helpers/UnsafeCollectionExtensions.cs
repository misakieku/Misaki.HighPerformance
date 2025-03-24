using Misaki.HighPerformance.Unsafe.Collections.Contracts;

namespace Misaki.HighPerformance.Unsafe.Helpers;

public unsafe static class UnsafeCollectionExtensions
{
    /// <summary>
    /// Copies elements from a source UnsafeCollection to a destination Span, ensuring both have the same size.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="source">Represents the source collection from which elements are copied.</param>
    /// <param name="destination">Represents the target span where elements are copied to.</param>
    /// <exception cref="ArgumentException">Thrown when the sizes of the source collection and destination span do not match.</exception>
    public static void CopyTo<T>(this IUnsafeCollection<T> source, Span<T> destination) where T : unmanaged
    {
        if (source.Size > destination.Length)
        {
            throw new ArgumentException("Source collection is larger than the destination span.");
        }

        fixed (T* ptr = destination)
        {
            SystemUnsfae.CopyBlock(ptr, source.Buffer, (uint)(source.Size * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies elements from a source span to a destination unsafe collection, ensuring both have the same size.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="destination">Represents the unsafe collection that will receive the copied elements.</param>
    /// <param name="source">Represents the span containing the elements to be copied to the unsafe collection.</param>
    /// <exception cref="ArgumentException">Thrown when the source span and destination collection have different sizes.</exception>
    public static void CopyFrom<T>(this IUnsafeCollection<T> destination, Span<T> source) where T : unmanaged
    {
        if (destination.Size > source.Length)
        {
            throw new ArgumentException("Destination collection is larger than the source span.");
        }

        fixed (T* ptr = source)
        {
            SystemUnsfae.CopyBlock(destination.Buffer, ptr, (uint)(source.Length * sizeof(T)));
        }
    }

    /// <summary>
    /// Converts an UnsafeCollection of unmanaged types into a standard collection.
    /// </summary>
    /// <typeparam name="T">Represents a type that is unmanaged, allowing for direct memory manipulation.</typeparam>
    /// <param name="source">The UnsafeCollection instance that contains the data to be converted.</param>
    /// <returns>A new collection containing the elements from the UnsafeCollection.</returns>
    public static T[] ToArray<T>(this IUnsafeCollection<T> source) where T : unmanaged
    {
        var array = new T[source.Size];
        fixed (T* ptr = array)
        {
            SystemUnsfae.CopyBlock(ptr, source.Buffer, (uint)(source.Size * sizeof(T)));
        }

        return array;
    }

    /// <summary>
    /// Converts an unmanaged collection into a list by copying its elements into a new list.
    /// </summary>
    /// <typeparam name="T">Represents a type that is unmanaged, allowing for direct memory manipulation.</typeparam>
    /// <param name="source">The collection from which elements are copied to create the new list.</param>
    /// <returns>A list containing the elements from the specified unmanaged collection.</returns>
    public static List<T> ToList<T>(this IUnsafeCollection<T> source) where T : unmanaged
    {
        var list = new List<T>(source.Size);
        fixed (T* ptr = list.ToArray())
        {
            SystemUnsfae.CopyBlock(ptr, source.Buffer, (uint)(source.Size * sizeof(T)));
        }
        return list;
    }

    /// <summary>
    /// Converts an UnsafeCollection into a Span for efficient memory access.
    /// </summary>
    /// <typeparam name="T">Represents a type that can be stored in unmanaged memory.</typeparam>
    /// <param name="source">The UnsafeCollection instance to be converted into a Span.</param>
    /// <returns>A Span that provides a view over the elements of the UnsafeCollection.</returns>
    public static Span<T> AsSpan<T>(this IUnsafeCollection<T> source) where T : unmanaged
    {
        return new(source.Buffer, source.Size);
    }
}