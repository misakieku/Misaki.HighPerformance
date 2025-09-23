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

    public bool TryGetElement(int slotIndex, int generation, [MaybeNullWhen(false)] out T value)
    {
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            value = default;
            return false;
        }

        ref var slot = ref _data[slotIndex];
        if (slot.generation != generation)
        {
            value = default;
            return false;
        }

        value = slot.value;
        return true;
    }

    public T GetElementAt(int slotIndex, int generation)
    {
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index is out of range.");
        }

        ref var slot = ref _data[slotIndex];
        if (!slot.isValid || slot.generation != generation)
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied.");
        }

        return slot.value;
    }

    public ref T GetElementReferenceAt(int slotIndex, int generation, out bool exist)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        ref var slot = ref _data[slotIndex];

        if (!slot.isValid || slot.generation != generation)
        {
            exist = false;
            return ref Unsafe.NullRef<T>();
        }

        exist = true;
        return ref slot.value!;
    }

    public void UpdateElement(int slotIndex, int generation, T newValue)
    {
        if (slotIndex < 0 || slotIndex >= Volatile.Read(ref _capacity))
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index is out of range.");
        }

        ref var slot = ref _data[slotIndex];
        if (!slot.isValid || slot.generation != generation)
        {
            throw new InvalidOperationException($"Slot {slotIndex} is not occupied or generation mismatch.");
        }

        slot.value = newValue;
    }

    public void Clear()
    {
        _count = 0;

        _data.AsSpan().Clear();
        _freeSlots.Clear();
    }
}