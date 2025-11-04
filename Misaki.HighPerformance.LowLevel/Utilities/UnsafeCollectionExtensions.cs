using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Utilities;

/// <summary>
/// Provides extension methods for copying elements between unsafe collections and spans, converting collections to
/// arrays or lists, and searching for values.
/// </summary>
public static unsafe class UnsafeCollectionExtensions
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

        fixed (T* pDest = destination)
        {
            MemCpy(source.GetUnsafePtr(), pDest, (uint)(source.Count * sizeof(T)));
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
    public static void CopyTo<T>(this IUnsafeCollection<T> source, Span<T> destination, uint sourceIndex, uint destinationIndex, uint length)
        where T : unmanaged
    {
        if (sourceIndex + length > source.Count || destinationIndex + length > destination.Length)
        {
            throw new ArgumentException("Source collection or destination span is too small for the specified range.");
        }

        fixed (T* pDest = destination)
        {
            MemCpy((byte*)source.GetUnsafePtr() + sourceIndex * sizeof(T), pDest + destinationIndex, (uint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies elements from an untyped source collection to a destination span of a specific type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the destination span, which must be unmanaged.</typeparam>
    /// <param name="source">The untyped collection from which data is copied.</param>
    /// <param name="destination">The typed span where the data will be copied to.</param>
    /// <exception cref="ArgumentException">Thrown when the source collection size exceeds the destination span capacity.</exception>
    public static void CopyTo<T>(this IUnTypedCollection source, Span<T> destination)
        where T : unmanaged
    {
        var destSize = (uint)destination.Length * (uint)sizeof(T);
        if (source.Size > destSize)
        {
            throw new ArgumentException("Source collection is larger than the destination span.");
        }

        fixed (T* pDest = destination)
        {
            MemCpy(source.GetUnsafePtr(), pDest, source.Size);
        }
    }

    /// <summary>
    /// Copies a range of bytes from an untyped source collection to a destination span, interpreting the bytes as elements of type T.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the destination span, which must be unmanaged.</typeparam>
    /// <param name="source">The untyped collection from which bytes are copied.</param>
    /// <param name="destination">The typed span where the elements will be placed.</param>
    /// <param name="sourceOffset">The byte offset in the source collection from which to start copying.</param>
    /// <param name="destinationIndex">The element index in the destination span where copying will begin.</param>
    /// <param name="length">The number of elements of type T to copy.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source collection or destination span.</exception>
    public static void CopyTo<T>(this IUnTypedCollection source, Span<T> destination, uint sourceOffset, uint destinationIndex, uint length)
        where T : unmanaged
    {
        var sizeOfElement = (uint)sizeof(T);
        if (sourceOffset + (length * sizeOfElement) > source.Size || destinationIndex + length > destination.Length)
        {
            throw new ArgumentException("Source collection or destination span is too small for the specified range.");
        }

        fixed (T* pDest = destination)
        {
            MemCpy((byte*)source.GetUnsafePtr() + sourceOffset, pDest + destinationIndex, length * sizeOfElement);
        }
    }

    /// <summary>
    /// Copies elements from a source span to a destination unsafe collection, ensuring both have the same size.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="destination">Represents the unsafe collection that will receive the copied elements.</param>
    /// <param name="source">Represents the span containing the elements to be copied to the unsafe collection.</param>
    /// <exception cref="ArgumentException">Thrown when the source span and destination collection have different sizes.</exception>
    public static void CopyFrom<T>(this IUnsafeCollection<T> destination, ReadOnlySpan<T> source)
        where T : unmanaged
    {
        if (destination.Count < source.Length)
        {
            throw new ArgumentException("Destination collection is smaller than the source span.");
        }

        fixed (T* pSrc = source)
        {
            MemCpy(pSrc, destination.GetUnsafePtr(), (uint)(source.Length * sizeof(T)));
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
    public static void CopyFrom<T>(this IUnsafeCollection<T> destination, ReadOnlySpan<T> source, uint sourceIndex, uint destinationIndex, uint length)
        where T : unmanaged
    {
        if (sourceIndex + length > source.Length || destinationIndex + length > destination.Count)
        {
            throw new ArgumentException("Source span or destination collection is too small for the specified range.");
        }

        fixed (T* pSrc = source)
        {
            MemCpy(pSrc + sourceIndex, (byte*)destination.GetUnsafePtr() + destinationIndex * sizeof(T), (uint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies elements from a typed source span to an untyped destination collection.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the source span, which must be unmanaged.</typeparam>
    /// <param name="destination">The untyped collection that will receive the copied data.</param>
    /// <param name="source">The typed span containing the elements to be copied.</param>
    /// <exception cref="ArgumentException">Thrown when the destination collection is smaller than the source span data size.</exception>
    public static void CopyFrom<T>(this IUnTypedCollection destination, ReadOnlySpan<T> source)
        where T : unmanaged
    {
        var sourceSize = (uint)(source.Length * sizeof(T));

        if (destination.Size < sourceSize)
        {
            throw new ArgumentException("Destination collection is smaller than the source span.");
        }

        fixed (T* pSrc = source)
        {
            MemCpy(pSrc, destination.GetUnsafePtr(), sourceSize);
        }
    }

    /// <summary>
    /// Copies a range of elements from a typed source span to an untyped destination collection at a specified byte offset.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the source span, which must be unmanaged.</typeparam>
    /// <param name="destination">The untyped collection where the data will be copied to.</param>
    /// <param name="source">The typed span containing the elements to be copied.</param>
    /// <param name="sourceIndex">The starting element index in the source span from which to begin copying.</param>
    /// <param name="destinationOffset">The byte offset in the destination collection where the data will be placed.</param>
    /// <param name="length">The number of elements to copy from the source span.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source span or destination collection.</exception>
    public static void CopyFrom<T>(this IUnTypedCollection destination, ReadOnlySpan<T> source, uint sourceIndex, uint destinationOffset, uint length)
        where T : unmanaged
    {
        var sizeOfElement = (uint)sizeof(T);
        if (sourceIndex + length > source.Length || destinationOffset + (length * sizeOfElement) > destination.Size)
        {
            throw new ArgumentException("Source span or destination collection is too small for the specified range.");
        }

        fixed (T* pSrc = source)
        {
            MemCpy(pSrc + sourceIndex, (byte*)destination.GetUnsafePtr() + destinationOffset, length * sizeOfElement);
        }
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
    /// Creates a span over a contiguous region of elements in the specified unsafe collection, starting at the given
    /// index and covering the specified number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection. Must be an unmanaged type.</typeparam>
    /// <param name="source">The unsafe collection from which to create the span. Must not be null.</param>
    /// <param name="start">The zero-based index of the first element in the collection to include in the span. Must be greater than or equal to zero and less than the number of elements in the collection.</param>
    /// <param name="length">The number of elements to include in the span. Must be greater than or equal to zero and the range defined by
    /// <paramref name="start"/> and <paramref name="length"/> must not exceed the bounds of the collection.</param>
    /// <returns>A <see cref="Span{T}"/> representing the specified region of the collection.</returns>
    public static Span<T> AsSpan<T>(this IUnsafeCollection<T> source, int start, int length)
    where T : unmanaged
    {
        return new((T*)source.GetUnsafePtr() + start, length);
    }

    /// <summary>
    /// Converts an UnTypedCollection into a Span for efficient memory access.
    /// </summary>
    /// <param name="source">The UnTypedCollection instance to be converted into a Span.</param>
    /// <returns>A Span that provides a view over the elements of the UnsafeCollection.</returns>
    public static Span<byte> AsSpan(this IUnTypedCollection source)
    {
        return new(source.GetUnsafePtr(), (int)source.Size);
    }

    /// <summary>
    /// Creates a span over a contiguous region of elements in the specified unsafe collection, starting at the given
    /// index and covering the specified number of elements.
    /// </summary>
    /// <param name="source">The unsafe collection from which to create the span. Must not be null.</param>
    /// <param name="start">The zero-based index of the first element in the collection to include in the span. Must be greater than or equal to zero and less than the number of elements in the collection.</param>
    /// <param name="length">The number of elements to include in the span. Must be greater than or equal to zero and the range defined by
    /// <paramref name="start"/> and <paramref name="length"/> must not exceed the bounds of the collection.</param>
    /// <returns>A <see cref="Span{byte}"/> representing the specified region of the collection.</returns>
    public static Span<byte> AsSpan(this IUnTypedCollection source, int start, int length)
    {
        return new((byte*)source.GetUnsafePtr() + start, length);
    }

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
            MemCpy(pSrc, array.GetUnsafePtr(), (uint)(source.Length * sizeof(T)));
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
            MemCpy(pSrc, list.GetUnsafePtr(), (uint)(source.Count * sizeof(T)));
        }

        return list;
    }
}