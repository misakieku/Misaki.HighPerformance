using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Misaki.HighPerformance.Jobs;

public class SPMCQueue<T>
{
    private readonly T[] _queue;
    private readonly int _mask;

    private int _head;
    private int _tail;

    public bool IsEmpty => Volatile.Read(ref _tail) - Volatile.Read(ref _head) <= 0;

    public SPMCQueue(int capacity)
    {
        _queue = new T[(int)BitOperations.RoundUpToPowerOf2((uint)capacity)];
        _mask = capacity - 1;
    }

    public bool TryPush(T item)
    {
        var tail = _tail;

        if (tail - Volatile.Read(ref _head) >= _queue.Length)
        {
            return false;
        }

        _queue[tail & _mask] = item;

        Volatile.Write(ref _tail, tail + 1);

        return true;
    }

    public bool TryPop([MaybeNullWhen(false)] out T? item)
    {
        var tail = _tail - 1;
        Volatile.Write(ref _tail, tail);

        Interlocked.MemoryBarrier();

        var head = Volatile.Read(ref _head);
        var size = tail - head;

        if (size < 0)
        {
            Volatile.Write(ref _tail, head);
            item = default;
            return false;
        }

        item = _queue[tail & _mask];

        if (size > 0)
        {
            return true;
        }

        if (Interlocked.CompareExchange(ref _head, head + 1, head) == head)
        {
            return true;
        }

        Volatile.Write(ref _tail, head + 1);
        item = default;
        return false;
    }

    public bool TrySteal([MaybeNullWhen(false)] out T? item)
    {
        var head = Volatile.Read(ref _head);
        var tail = Volatile.Read(ref _tail);

        if (tail - head <= 0)
        {
            item = default;
            return false;
        }

        item = _queue[head & _mask];
        return Interlocked.CompareExchange(ref _head, head + 1, head) == head;
    }
}
