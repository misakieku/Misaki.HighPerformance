using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Helpers;

public static unsafe class UnsafeUtilities
{
    /// <summary>
    /// Converts a pointer to a reference of a specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the reference to be created from the pointer.</typeparam>
    /// <param name="ptr">Represents the memory address to be converted into a reference.</param>
    /// <returns>Returns a reference of the specified type pointing to the given memory address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AsRef<T>(void* ptr)
        where T : unmanaged
    {
        return ref *(T*)ptr;
    }

    /// <summary>
    /// Returns the address of a specified variable in memory.
    /// </summary>
    /// <typeparam name="T">Represents the type of the variable whose address is being retrieved.</typeparam>
    /// <param name="value">The variable whose memory address is to be obtained.</param>
    /// <returns>A pointer to the memory address of the specified variable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AddressOf<T>(ref T value)
        where T : unmanaged
    {
        return Unsafe.AsPointer(ref value);
    }

    /// <summary>
    /// Reads an element from an unmanaged array at a specified index using a pointer.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the unmanaged array.</typeparam>
    /// <param name="ptr">Points to the start of the unmanaged array from which the element is read.</param>
    /// <param name="index">Indicates the position of the element to be accessed within the array.</param>
    /// <returns>Returns a pointer to the element located at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* ReadArrayElementUnsafe<T>(void* ptr, int index)
        where T : unmanaged
    {
        return (T*)((byte*)ptr + index * sizeof(T));
    }

    /// <summary>
    /// Reads an element from an unmanaged array at a specified index using a pointer.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the unmanaged array.</typeparam>
    /// <param name="ptr">Points to the start of the unmanaged array from which the element is read.</param>
    /// <param name="index">Indicates the position of the element to be accessed within the array.</param>
    /// <returns>Returns a pointer to the element located at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* ReadArrayElementUnsafe<T>(void* ptr, uint index)
        where T : unmanaged
    {
        return (T*)((byte*)ptr + index * sizeof(T));
    }

    /// <summary>
    /// Reads an element from an unmanaged array using a pointer and index, returning a reference to the element.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the elements in the unmanaged array.</typeparam>
    /// <param name="ptr">Points to the start of the unmanaged array from which the element is read.</param>
    /// <param name="index">Indicates the position of the element to be accessed in the array.</param>
    /// <returns>A reference to the specified element in the unmanaged array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T ReadArrayElementRef<T>(void* ptr, int index)
        where T : unmanaged
    {
        return ref AsRef<T>(ReadArrayElementUnsafe<T>(ptr, index));
    }

    /// <summary>
    /// Reads an element from an unmanaged array using a pointer and index, returning a reference to the element.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the elements in the unmanaged array.</typeparam>
    /// <param name="ptr">Points to the start of the unmanaged array from which the element is read.</param>
    /// <param name="index">Indicates the position of the element to be accessed in the array.</param>
    /// <returns>A reference to the specified element in the unmanaged array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T ReadArrayElementRef<T>(void* ptr, uint index)
        where T : unmanaged
    {
        return ref AsRef<T>(ReadArrayElementUnsafe<T>(ptr, index));
    }

    /// <summary>
    /// Reads an element from an array at a specified index using a pointer to the array.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the elements in the array, which must be unmanaged.</typeparam>
    /// <param name="ptr">Points to the start of the array from which an element will be read.</param>
    /// <param name="index">Indicates the position of the element to be accessed within the array.</param>
    /// <returns>The element located at the specified index in the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadArrayElement<T>(void* ptr, int index)
        where T : unmanaged
    {
        return *ReadArrayElementUnsafe<T>(ptr, index);
    }

    /// <summary>
    /// Reads an element from an array at a specified index using a pointer to the array.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the elements in the array, which must be unmanaged.</typeparam>
    /// <param name="ptr">Points to the start of the array from which an element will be read.</param>
    /// <param name="index">Indicates the position of the element to be accessed within the array.</param>
    /// <returns>The element located at the specified index in the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadArrayElement<T>(void* ptr, uint index)
        where T : unmanaged
    {
        return *ReadArrayElementUnsafe<T>(ptr, index);
    }

    /// <summary>
    /// Writes a value to a specified index of an unmanaged array using a pointer.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the value being written to the array, which must be an unmanaged type.</typeparam>
    /// <param name="ptr">Points to the beginning of the unmanaged array where the value will be written.</param>
    /// <param name="index">Indicates the position in the array where the value should be stored.</param>
    /// <param name="value">Represents the value to be written to the specified index of the array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArrayElement<T>(void* ptr, int index, T value)
        where T : unmanaged
    {
        *ReadArrayElementUnsafe<T>(ptr, index) = value;
    }

    /// <summary>
    /// Writes a value to a specified index of an unmanaged array using a pointer.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the value being written to the array, which must be an unmanaged type.</typeparam>
    /// <param name="ptr">Points to the beginning of the unmanaged array where the value will be written.</param>
    /// <param name="index">Indicates the position in the array where the value should be stored.</param>
    /// <param name="value">Represents the value to be written to the specified index of the array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArrayElement<T>(void* ptr, uint index, T value)
        where T : unmanaged
    {
        *ReadArrayElementUnsafe<T>(ptr, index) = value;
    }

    /// <summary>
    /// Converts an UnsafeArray of one unmanaged type to another unmanaged type without copying the elements.
    /// </summary>
    /// <typeparam name="TIn">Represents the type of elements in the input array.</typeparam>
    /// <typeparam name="TOut">Represents the type of elements in the output array.</typeparam>
    /// <param name="array">The input array containing elements of the specified input type.</param>
    /// <returns>An UnsafeArray containing elements of the specified output type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnsafeArray<TOut> CastArray<TIn, TOut>(UnsafeArray<TIn> array)
        where TIn : unmanaged where TOut : unmanaged
    {
        return new UnsafeArray<TOut>((TOut*)array.GetUnsafePtr(), array.Count * sizeof(TIn) / sizeof(TOut));
    }
}