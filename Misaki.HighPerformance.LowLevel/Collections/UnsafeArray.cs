using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Collections;

internal class UnsafeArrayDebugView<T>
    where T : unmanaged
{
    private readonly UnsafeArray<T> _array;

    public UnsafeArrayDebugView(UnsafeArray<T> array)
    {
        _array = array;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var count = _array.Count;
            var result = new T[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = _array[i];
            }

            return result;
        }
    }
}

/// <summary>
/// A structure for managing an array of unmanaged types with unsafe memory operations.
/// </summary>
/// <typeparam name="T">Represents a type that can be stored in an unmanaged memory context.</typeparam>
[DebuggerTypeProxy(typeof(UnsafeArrayDebugView<>))]
public unsafe struct UnsafeArray<T> : IUnsafeCollection<T>
    where T : unmanaged
{
    public ref struct Enumerator
    {
        private ref UnsafeArray<T> _collection;
        private int _index;

        public readonly ref T Current => ref _collection._buffer[_index];

        public Enumerator(ref UnsafeArray<T> collection)
        {
            _collection = ref collection;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _collection._count;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    private T* _buffer;
    private int _count;
#if MHP_ENABLE_SAFETY_CHECKS
    private MemoryHandle _memoryHandle;
#endif
    private AllocationHandle _allocationHandle;

    internal readonly AllocationHandle AllocationHandle => _allocationHandle;

    public readonly int Count => _count;
    public readonly int Length => _count;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndexBounds(index);
            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public readonly ref T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndexBounds((int)index);
            return ref UnsafeUtility.ReadArrayElementRef<T>(_buffer, index);
        }
    }

    public readonly bool IsCreated
    {
        get
        {
#if MHP_ENABLE_SAFETY_CHECKS
            if (_buffer != null)
            {
                return _memoryHandle.IsValid;
            }

            return false;
#else
            return _buffer != null;
#endif
        }
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a default size of 1 and a persistent allocation handle.
    /// </summary>
    public UnsafeArray()
        : this(1, AllocationHandle.Persistent)
    {
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation handle.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="handle">A reference to an AllocationHandle that manages the memory allocation for the array.</param>
    /// <param name="allocationOption">Specifies how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    public UnsafeArray(int count, AllocationHandle handle, AllocationOption allocationOption = AllocationOption.None)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (handle.Alloc == null)
        {
            throw new InvalidOperationException("Target allocation handle does not support allocation.");
        }

        _buffer = (T*)handle.Alloc(handle.State, (nuint)(count * sizeof(T)), AlignOf<T>(), allocationOption);
#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle = MemoryHandle.Create(_buffer, (nuint)(count * sizeof(T)));
#endif
        _allocationHandle = handle;
        _count = count;
    }

    /// <summary>
    /// Initializes a new instance of UnsafeArray with a specified number of elements and an allocation type.
    /// </summary>
    /// <param name="count">Specifies the number of elements to allocate in the array, which must be greater than zero.</param>
    /// <param name="allocator">Specifies the allocator to use for memory allocation, which determines the memory management strategy.</param>
    /// <param name="allocationOption">Determines how the memory should be allocated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified number of elements is less than or equal to zero.</exception>
    [Obsolete("Use AllocationHandle instead.")]
    public UnsafeArray(int count, Allocator allocator, AllocationOption allocationOption = AllocationOption.None)
        : this(count, AllocationManager.GetAllocationHandle(allocator), allocationOption)
    {
    }

    /// <summary>
    /// Initializes an UnsafeArray with a pointer to a buffer and a count of elements. This does not copy the data.
    /// </summary>
    /// <param name="buffer">A pointer to the memory location that holds the elements of the array.</param>
    /// <param name="count">The total size of the data.</param>
    /// <remarks>
    /// When using this constructor, the user is responsible for managing the memory pointed to by the buffer.
    /// Disposing of the UnsafeArray does not free the memory and only release the reference. The memory should be freed manually when no longer needed.
    /// Use <see cref="UnsafeArray(int, Allocator, AllocationOption)"/> constructor and <see cref="MemCpy(void*, void*, nuint)"/> if you are not sure what you are doing.
    /// </remarks>
    public UnsafeArray(T* buffer, int count)
    {
        _buffer = buffer;
        _count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void ThrowIfNotCreated()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("The UnsafeArray is not created.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("MHP_ENABLE_SAFETY_CHECKS")]
    private readonly void CheckIndexBounds(int index)
    {
        ThrowIfNotCreated();
        if (index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }
    }

    /// <summary>
    /// Returns a read-only view of the current collection.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyUnsafeCollection{T}"/> that provides a read-only view of the elements in the current collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlyUnsafeCollection<T> AsReadOnly()
    {
        return new ReadOnlyUnsafeCollection<T>(_buffer, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    /// <inheritdoc/>
    public void Resize(int newSize, AllocationOption option = AllocationOption.None)
    {
        ThrowIfNotCreated();

        if (_allocationHandle.Realloc == null)
        {
            throw new InvalidOperationException("Target allocation handle does not support reallocation.");
        }

        if (newSize == Count)
        {
            return;
        }

        var elemSize = SizeOf<T>();
        _buffer = (T*)_allocationHandle.Realloc(_allocationHandle.State, _buffer, (nuint)Count * elemSize, (nuint)newSize * elemSize, AlignOf<T>(), option);
        _count = newSize;
#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle.Update(_buffer, (nuint)newSize * elemSize);
#endif
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Clear()
    {
        ThrowIfNotCreated();
        MemClear(_buffer, (nuint)(Count * sizeof(T)));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetUnsafePtr()
    {
        ThrowIfNotCreated();
        return _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan()
    {
        ThrowIfNotCreated();
        return new Span<T>(_buffer, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan(int start, int length)
    {
        ThrowIfNotCreated();
        return new Span<T>(_buffer + start, length);
    }

    /// <summary>
    /// Reinterprets the underlying buffer as an array of a different unmanaged type without copying the data.
    /// </summary>
    /// <remarks>
    /// The returned UnsafeArray<U> shares the same memory as the original array, and does not own the memory.
    /// </remarks>
    /// <typeparam name="U">The unmanaged type to reinterpret the buffer as.</typeparam>
    /// <returns>An UnsafeArray<U> that views the same memory as the original array, but as elements of type U.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the total size of the buffer in bytes is not a multiple of the size of type U.</exception>
    public readonly UnsafeArray<U> Reinterpret<U>()
        where U : unmanaged
    {
        ThrowIfNotCreated();

        var totalSize = (nuint)(Count * sizeof(T));
        if (totalSize % (nuint)sizeof(U) != 0)
        {
            throw new InvalidOperationException("Cannot reinterpret array: size mismatch.");
        }

        var newCount = (int)(totalSize / (nuint)sizeof(U));
        return new UnsafeArray<U>((U*)_buffer, newCount);
    }

    /// <summary>
    /// Copies elements from a source UnsafeCollection to a destination Span, ensuring both have the same size.
    /// </summary>
    /// <param name="destination">Represents the target span where elements are copied to.</param>
    public readonly void CopyTo(Span<T> destination)
    {
        var size = Math.Min(destination.Length, Count);
        fixed (T* pDest = destination)
        {
            MemCpy(pDest, _buffer, (uint)(size * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies a range of elements from a source collection to a destination span, ensuring both are adequately sized.
    /// </summary>
    /// <param name="destination">The span where the elements will be copied to.</param>
    /// <param name="sourceIndex">The starting index in the source collection for the copy operation.</param>
    /// <param name="destinationIndex">The starting index in the destination span where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source to the destination.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified range exceeds the bounds of the source collection or destination span.</exception>
    public readonly void CopyTo(Span<T> destination, int sourceIndex, int destinationIndex, int length)
    {
        if (sourceIndex + length > _count || destinationIndex + length > destination.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Source collection or destination span is too small for the specified range.");
        }

        fixed (T* pDest = destination)
        {
            MemCpy(pDest + destinationIndex, _buffer + sourceIndex, (nuint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies elements from a source span to a destination unsafe collection, ensuring both have the same size.
    /// </summary>
    /// <param name="source">Represents the span containing the elements to be copied to the unsafe collection.</param>
    public void CopyFrom(ReadOnlySpan<T> source)
    {
        if (_count < source.Length)
        {
            Resize(source.Length);
        }

        fixed (T* pSrc = source)
        {
            MemCpy(_buffer, pSrc, (nuint)(source.Length * sizeof(T)));
        }
    }

    /// <summary>
    /// Copies a specified range of elements from a source span to a destination collection.
    /// </summary>
    /// <param name="source">The span containing the elements to be copied.</param>
    /// <param name="sourceIndex">The starting index in the source span from which to begin copying.</param>
    /// <param name="destinationIndex">The starting index in the destination collection where the elements will be placed.</param>
    /// <param name="length">The number of elements to copy from the source span to the destination collection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified range exceeds the bounds of the source span or destination collection.</exception>
    public void CopyFrom(ReadOnlySpan<T> source, int sourceIndex, int destinationIndex, int length)
    {
        if (sourceIndex + length > source.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Source span or destination collection is too small for the specified range.");
        }

        if (destinationIndex + length > _count)
        {
            Resize(destinationIndex + length);
        }

        fixed (T* pSrc = source)
        {
            MemCpy(_buffer + destinationIndex, pSrc + sourceIndex, (nuint)(length * sizeof(T)));
        }
    }

    /// <summary>
    /// Creates a new array containing all elements.
    /// </summary>
    /// <returns>An array containing all elements.</returns>
    public readonly T[] ToArray()
    {
        return new Span<T>(_buffer, _count).ToArray();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!IsCreated)
        {
            UnsafeCollectionUtility.ReportDoubleFree<UnsafeArray<T>>(_buffer);
            return;
        }

        if (_allocationHandle.Free != null)
        {
            _allocationHandle.Free(_allocationHandle.State, _buffer);
        }

#if MHP_ENABLE_SAFETY_CHECKS
        _memoryHandle.Dispose();
#endif

        _buffer = null;
        _count = 0;
    }

    public static implicit operator ReadOnlyUnsafeCollection<T>(UnsafeArray<T> array)
    {
        return array.AsReadOnly();
    }

    public static implicit operator Span<T>(UnsafeArray<T> array)
    {
        return array.AsSpan();
    }
}
