using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Collections;

public class SlotMap<T> : IEnumerable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly SlotMap<T> _slotMap;
        private int _currentIndex;

        public Enumerator(SlotMap<T> slotMap)
        {
            _slotMap = slotMap;
            _currentIndex = 0;
        }

        public readonly T Current => _slotMap._data[_currentIndex];
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (++_currentIndex < _slotMap._capacity)
            {
                if (_slotMap._isOccupiedBits[_currentIndex])
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset() => _currentIndex = 0;

        public void Dispose()
        {
        }
    }

    private T[] _data;
    private int[] _generations;
    private readonly BitArray _isOccupiedBits;
    private readonly Queue<int> _freeSlots;

    private int _count;
    private int _capacity;

    public int Count => _count;
    public int Capacity => _capacity;

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public SlotMap(int initialCapacity = 16)
    {
        _capacity = initialCapacity;

        _data = new T[initialCapacity];
        _generations = new int[initialCapacity];
        _isOccupiedBits = new BitArray(initialCapacity);
        _freeSlots = new(initialCapacity);

        Add(default!, out _);
    }

    private void Resize()
    {
        var newCapacity = _capacity * 2;

        Array.Resize(ref _data, newCapacity);
        Array.Resize(ref _generations, newCapacity);

        _isOccupiedBits.Length = newCapacity;
        _freeSlots.EnsureCapacity(newCapacity);

        _capacity = newCapacity;
    }

    public int Add(T item, out int generation)
    {
        if (_count >= _capacity)
        {
            Resize();
        }

        int slotIndex;
        if (_freeSlots.Count == 0)
        {
            slotIndex = _count;
        }
        else
        {
            slotIndex = _freeSlots.Dequeue();
        }

        _data[slotIndex] = item;
        _isOccupiedBits[slotIndex] = true;

        _count++;

        generation = _generations[slotIndex];
        return slotIndex;
    }

    public bool Contains(int slotIndex, int generation)
    {
        if (slotIndex <= 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            return false;
        }

        if (_isOccupiedBits[slotIndex] && _generations[slotIndex] == generation)
        {
            return true;
        }

        return false;
    }

    public bool Remove(int slotIndex, int generation)
    {
        if (!Contains(slotIndex, generation))
        {
            return false;
        }

        _generations[slotIndex]++;
        _isOccupiedBits[slotIndex] = false;

        _freeSlots.Enqueue(slotIndex);
        _count--;

        return true;
    }

    public bool TryGetElement(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        if (!Contains(slotIndex, generation))
        {
            value = default;
            return false;
        }

        value = _data[slotIndex];
        return true;
    }

    public T GetElementAt(int slotIndex, int generation)
    {
        if (!Contains(slotIndex, generation))
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        return _data[slotIndex];
    }

    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (!Contains(slotIndex, generation))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        exist = true;
        return ref _data[slotIndex];
    }

    public bool UpdateElement(int slotIndex, int generation, T newValue)
    {
        if (!Contains(slotIndex, generation))
        {
            return false;
        }

        _data[slotIndex] = newValue;
        return true;
    }

    public void Clear()
    {
        _count = 0;

        _data.AsSpan().Clear();
        _freeSlots.Clear();

        Add(default!, out _);
    }

    public Span<T> AsSpan()
    {
        // Skip the first element at index 0
        return _data.AsSpan(1, _count);
    }
}
