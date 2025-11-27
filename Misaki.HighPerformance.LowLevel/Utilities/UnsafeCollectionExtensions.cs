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
    /// <typeparam name="C">The type of the destination collection, which must implement <see cref="IUnsafeCollection{T}"/>.</typeparam>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="source">Represents the source collection from which elements are copied.</param>
    /// <param name="destination">Represents the target span where elements are copied to.</param>
    /// <exception cref="ArgumentException">Thrown when the sizes of the source collection and destination span do not match.</exception>
    public static void CopyTo<C, T>(this C source, Span<T> destination)
        where C: IUnsafeCollection<T> where T : unmanaged
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
    /// <typeparam name="C">The type of the destination collection, which must implement <see cref="IUnsafeCollection{T}"/>.</typeparam>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be a value type.</typeparam>
    /// <param name="source">The collection from which elements are copied.</param>
    /// <param name="destination">The span where the elements will be copied to.</param>
    /// <param name="sourceIndex">The starting index in the source collection for the copy operation.</param>
    /// <param name="destinationIndex">The starting index in the destination span where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source to the destination.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source collection or destination span.</exception>
    public static void CopyTo<C, T>(this C source, Span<T> destination, uint sourceIndex, uint destinationIndex, uint length)
        where C : IUnsafeCollection<T> where T : unmanaged
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
    /// Copies elements from a source span to a destination unsafe collection, ensuring both have the same size.
    /// </summary>
    /// <typeparam name="C">The type of the destination collection, which must implement <see cref="IUnsafeCollection{T}"/>.</typeparam>
    /// <typeparam name="T">Specifies the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="destination">Represents the unsafe collection that will receive the copied elements.</param>
    /// <param name="source">Represents the span containing the elements to be copied to the unsafe collection.</param>
    /// <exception cref="ArgumentException">Thrown when the source span and destination collection have different sizes.</exception>
    public static void CopyFrom<C, T>(this C destination, ReadOnlySpan<T> source)
        where C : IUnsafeCollection<T> where T : unmanaged
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
    /// <typeparam name="C">The type of the destination collection, which must implement <see cref="IUnsafeCollection{T}"/>.</typeparam>
    /// <typeparam name="T">Represents the type of elements being copied, which must be unmanaged.</typeparam>
    /// <param name="destination">The collection where elements will be copied to.</param>
    /// <param name="source">The span containing the elements to be copied.</param>
    /// <param name="sourceIndex">The starting index in the source span from which to begin copying.</param>
    /// <param name="destinationIndex">The starting index in the destination collection where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source span to the destination collection.</param>
    /// <exception cref="ArgumentException">Thrown when the specified range exceeds the bounds of the source span or destination collection.</exception>
    public static void CopyFrom<C, T>(this C destination, ReadOnlySpan<T> source, uint sourceIndex, uint destinationIndex, uint length)
        where C : IUnsafeCollection<T> where T : unmanaged
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

    /// <summary>
    /// Creates a new array containing all elements from the specified unsafe collection.
    /// </summary>
    /// <typeparam name="C">The type of the source collection, which must implement <see cref="IUnsafeCollection{T}"/>.</typeparam>
    /// <typeparam name="T">The type of elements contained in the collection. Must be an unmanaged type.</typeparam>
    /// <param name="source">The collection whose elements will be copied to the new array. Must not be null.</param>
    /// <returns>An array containing all elements from <paramref name="source"/> in their current order.</returns>
    public static T[] ToArray<C, T>(this C source)
        where C : IUnsafeCollection<T> where T : unmanaged
    {
        return new Span<T>(source.GetUnsafePtr(), source.Count).ToArray();
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> containing the elements of the specified unsafe collection.
    /// </summary>
    /// <typeparam name="C">The type of the source collection, which must implement <see cref="IUnsafeCollection{T}"/>.</typeparam>
    /// <typeparam name="T">The type of elements contained in the collection. Must be an unmanaged type.</typeparam>
    /// <param name="source">The unsafe collection whose elements are to be copied to the new list. Must not be null.</param>
    /// <returns>A <see cref="List{T}"/> containing all elements from <paramref name="source"/> in their original order.</returns>
    public static List<T> ToList<C, T>(this C source)
        where C : IUnsafeCollection<T> where T : unmanaged
    {
        var list = new List<T>(source.Count);
        var span = new Span<T>(source.GetUnsafePtr(), source.Count);
        span.CopyTo(CollectionsMarshal.AsSpan(list));
        return list;
    }
}