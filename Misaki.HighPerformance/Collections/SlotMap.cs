
using System.Collections;

namespace Misaki.HighPerformance.LowLevel.Collections;

public class SlotMap<T> : IEnumerable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly SlotMap<T> _slotMap;
        private int _currentIndex;

        public Enumerator(SlotMap<T> slotMap)
        {
            _slotMap = slotMap;
            _currentIndex = -1;
        }

        public readonly T Current => _slotMap._data[_currentIndex].value;
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (++_currentIndex < _slotMap._capacity)
            {
                if (_slotMap._data[_currentIndex].isValid)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset() => _currentIndex = -1;

        public void Dispose()
        {
        }
    }

    private struct SlotData
    {
        public T value;
        public int generation;
        public bool isValid;
    }

    private SlotData[] _data;
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

        _data = new SlotData[initialCapacity];
        _freeSlots = new(initialCapacity);
    }

    private void Resize()
    {
        var newCapacity = _capacity * 2;

        Array.Resize(ref _data, newCapacity);
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

        ref var slot = ref _data[slotIndex];
        slot.value = item;
        slot.isValid = true;
        generation = slot.generation;

        _count++;

        return slotIndex;
    }

    public bool Remove(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            return false;
        }

        ref var slot = ref _data[slotIndex];
        if (slot.generation != generation)
        {
            return false;
        }

        slot.generation++;
        slot.isValid = false;

        _freeSlots.Enqueue(slotIndex);
        _count--;

        return true;
    }

    public bool Contain(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            return false;
        }

        ref var slot = ref _data[slotIndex];

        if (slot.isValid && slot.generation == generation)
        {
            return true;
        }

        return false;
    }

    public ref T GetElementAt(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index is out of range.");
        }

        ref var slot = ref _data[slotIndex];
        if (slot.generation != generation)
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied.");
        }

        return ref slot.value;
    }

    public void Clear()
    {
        _count = 0;

        _data.AsSpan().Clear();
        _freeSlots.Clear();
    }
}