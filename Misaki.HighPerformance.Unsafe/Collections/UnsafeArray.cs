using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Unsafe.Collections;

public unsafe struct UnsafeArray<T> : IUnsafeCollection<T>, IEnumerable<T>
    where T : unmanaged
{
    private struct Enumerator : IEnumerator<T>
    {
        private UnsafeArray<T> _collection;
        private int _index;
        private T _value;

        public Enumerator(ref UnsafeArray<T> collection)
        {
            _collection = collection;
            _index = -1;
            _value = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            if (_index < _collection.Size)
            {
                _value = UnsafeUtilities.ReadArrayElement<T>(_collection.Buffer, _index);
                return true;
            }

            _value = default;
            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        // Let NativeArray indexer check for out of range.
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _value;
            }
        }

        object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Current;
            }
        }

        public void Dispose()
        {
        }
    }

    private T* _buffer;
    private int _size;

    public readonly T* Buffer => _buffer;
    public readonly int Size => _size;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref UnsafeUtilities.ReadArrayElementRef<T>(_buffer, index);
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(ref this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public UnsafeArray(int size, AllocationType allocationType)
    {
        _size = size;
        _buffer = (T*)NativeMemory.AlignedAlloc((nuint)(size * sizeof(T)), AlignOf<T>());

        if (allocationType == AllocationType.Clear)
        {
            Clear();
        }
    }

    public void ReAlloc(int newSize)
    {
        if (newSize == _size)
        {
            return;
        }

        _buffer = (T*)NativeMemory.AlignedRealloc(_buffer, (nuint)(newSize * sizeof(T)), AlignOf<T>());
        _size = newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        MemClear(_buffer, (uint)(_size * sizeof(T)));
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(_buffer);

        _buffer = null;
        _size = 0;
    }
}

