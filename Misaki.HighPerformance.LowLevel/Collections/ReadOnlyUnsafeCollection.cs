using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// Provides a read-only, unsafe view over a contiguous region of unmanaged memory as an array of elements of type T.
/// Enables efficient, low-level access to memory without copying or additional safety checks.
/// </summary>
/// <remarks>
/// This read only collection does not own the memory it points to. The user is responsible for ensuring the memory remains valid for the lifetime of this structure.
/// The goal of this struc is similar to <see cref="ReadOnlySpan{T}"/>, but it can be used in contexts where spans are not allowed, such as fields in structs and shared across threads.
/// </remarks>
/// <typeparam name="T">The type of elements in the collection. Must be an unmanaged type.</typeparam>
public readonly unsafe struct ReadOnlyUnsafeCollection<T> : IEnumerable<T>
    where T : unmanaged
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ReadOnlyUnsafeCollection<T> _collection;
        private int _index;

        public readonly T Current => _collection[_index];
        readonly object IEnumerator.Current => Current;

        public Enumerator(ref readonly ReadOnlyUnsafeCollection<T> array)
        {
            _collection = array;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _collection.Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }
    }

    private readonly T* _buffer;
    private readonly int _count;

    public int Count => _count;
    public int Length => _count;

    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndexBounds(index);
            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public ref readonly T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndexBounds((int)index);
            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(in this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public ReadOnlyUnsafeCollection(T* buffer, int count)
    {
        _buffer = buffer;
        _count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("ENABLE_SAFETY_CHECKS")]
    private readonly void CheckIndexBounds(int index)
    {
        if (index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }
    }

    /// <summary>
    /// Returns a read-only span that represents the valid elements in the underlying buffer.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> containing the elements of the buffer up to the current count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsSpan()
    {
        return new ReadOnlySpan<T>(_buffer, _count);
    }

    /// <summary>
    /// Reinterprets the underlying collection as a read-only collection of a different unmanaged type without copying the data.
    /// </summary>
    /// <typeparam name="U">The unmanaged type to reinterpret the collection elements as.</typeparam>
    /// <returns>A new ReadOnlyUnsafeCollection<U> that provides a read-only view of the same memory, interpreted as elements of type U.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the total size of the underlying collection is not a multiple of the size of type U, making the reinterpretation invalid.</exception>
    public ReadOnlyUnsafeCollection<U> Reinterpret<U>()
        where U : unmanaged
    {
        var totalSize = Count * sizeof(T);
        if (totalSize % sizeof(U) != 0)
        {
            throw new InvalidOperationException("Cannot reinterpret collection: size mismatch.");
        }

        var newCount = totalSize / sizeof(U);
        return new ReadOnlyUnsafeCollection<U>((U*)_buffer, newCount);
    }

    /// <summary>
    /// Returns an unsafe pointer to the underlying buffer of the collection, allowing for low-level access to the memory.
    /// </summary>
    /// <returns>The pointer to the first element of the collection's buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* GetUnsafePtr()
    {
        return _buffer;
    }
}
