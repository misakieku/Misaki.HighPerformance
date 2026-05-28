using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Jobs;

[StructLayout(LayoutKind.Explicit)]
public class SPMCQueue<T>
{
    [FieldOffset(0)]
    private readonly T[] _queue;
    [FieldOffset(8)]
    private readonly int _mask;

    [FieldOffset(64)]
    private int _head;
    
    [FieldOffset(128)]
    private int _tail;

    public bool IsEmpty => Volatile.Read(ref _tail) - Volatile.Read(ref _head) <= 0;

    /// <summary>
    /// Initializes a new instance of the SPMCQueue class with the specified capacity.
    /// </summary>
    /// <remarks>
    /// This queue will not resize when it reaches capacity.
    /// </remarks>
    /// <param name="capacity">The capacity of the queue.</param>
    public SPMCQueue(int capacity)
    {
        var powerOfTwoCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)capacity);
        _queue = new T[powerOfTwoCapacity];
        _mask = powerOfTwoCapacity - 1;
    }

    /// <summary>
    /// Tries to push an item onto the queue.
    /// </summary>
    /// <param name="item">The item to push.</param>
    /// <returns>True if the item was pushed successfully; otherwise, false.</returns>
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

    /// <summary>
    /// Trys to pop an item from the queue.
    /// </summary>
    /// <param name="item">The item to pop.</param>
    /// <returns>True if an item was popped successfully; otherwise, false.</returns>
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
            Volatile.Write(ref _tail, head + 1);
            return true;
        }

        Volatile.Write(ref _tail, head + 1);
        item = default;
        return false;
    }

    /// <summary>
    /// Trys to steal an item from the queue.
    /// </summary>
    /// <param name="item">The item to steal.</param>
    /// <returns>True if an item was stolen successfully; otherwise, false.</returns>
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
