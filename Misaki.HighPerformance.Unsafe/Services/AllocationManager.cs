using Misaki.HighPerformance.Unsafe.Buffer;
using Misaki.HighPerformance.Unsafe.Collections;

#if DEBUG
using System.Diagnostics;
#endif

namespace Misaki.HighPerformance.Unsafe.Services;

internal readonly struct AllocationInfo
{
    public readonly nuint Size
    {
        get;
        init;
    }

#if DEBUG
    public readonly StackTrace StackTrace
    {
        get;
        init;
    }
#endif
}

public static unsafe class AllocationManager
{
    private const uint _DEFAULT_ARENA_SIZE = 512 * 1024; // 512 KB

    private static DynamicArena _arena;
    private static bool _initialized;

    private static Dictionary<IntPtr, AllocationInfo> _allocated = null!;

    private static readonly Lock _lock = new();

    /// <summary>
    /// Initializes the AllocationManager with a specified initial size for the memory arena.
    /// </summary>
    /// <param name="initialSize">The initial size in bytes for the memory arena.</param>
    public static void Initialize(uint initialSize = _DEFAULT_ARENA_SIZE)
    {
        if (_initialized || initialSize <= 0)
        {
            return;
        }

        _arena = new DynamicArena(initialSize);
        _allocated = new(32);

        _initialized = true;
    }

    internal static T* Allocate<T>(uint size, uint alignSize, Allocator allocator, AllocationOption allocationOption)
        where T : unmanaged
    {
        if (allocationOption.HasFlag(AllocationOption.UnTracked))
        {
            return (T*)AlignedAlloc(size, alignSize);
        }

        if (!_initialized)
        {
            Initialize();
        }

        lock (_lock)
        {
            T* buffer;
            switch (allocator)
            {
                case Allocator.Temp:
                    buffer = (T*)_arena.Allocate(size * (uint)sizeof(T), alignSize, allocationOption);
                    break;

                case Allocator.Persistent:
                    var allocationSize = size * (nuint)sizeof(T);
                    buffer = (T*)AlignedAlloc(allocationSize, alignSize);
                    _allocated[(IntPtr)buffer] = new AllocationInfo
                    {
                        Size = allocationSize,
#if DEBUG
                        StackTrace = new StackTrace(true)
#endif
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(allocator), "Invalid allocator type.");
            }

            if (allocationOption.HasFlag(AllocationOption.Clear))
            {
                MemClear(buffer, size * (uint)sizeof(T));
            }

            return buffer;
        }
    }

    internal static T* Realloc<T>(T* buffer, uint size, uint alignSize, Allocator allocator)
        where T : unmanaged
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The AllocationManager has not been initialized.");
        }

        lock (_lock)
        {
            T* newBuffer;
            switch (allocator)
            {
                case Allocator.Temp:
                    newBuffer = (T*)_arena.Allocate(size * (uint)sizeof(T), alignSize, AllocationOption.None);
                    break;

                case Allocator.Persistent:
                    var allocationSize = size * (nuint)sizeof(T);
                    newBuffer = (T*)AlignedRealloc(buffer, allocationSize, alignSize);

                    // If the allocation map can not find the old value, it means that it was a untracked allocation
                    if (_allocated.Remove((IntPtr)buffer))
                    {
                        _allocated[(IntPtr)newBuffer] = new AllocationInfo
                        {
                            Size = allocationSize,
#if DEBUG
                            StackTrace = new StackTrace(true)
#endif
                        };
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(allocator), "Invalid allocator type.");
            }

            return newBuffer;
        }
    }

    internal static void Free(void* ptr, Allocator allocator)
    {
        lock (_lock)
        {
            if (allocator == Allocator.Persistent)
            {
                AlignedFree(ptr);
            }
        }
    }

    /// <summary>
    /// Resets the memory arena, optionally clearing the allocated memory.
    /// </summary>
    /// <param name="clear">If true, the allocated memory will be cleared; otherwise, it will not be cleared.</param>
    public static void Reset(bool clear = false)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The AllocationManager has not been initialized.");
        }

        _arena.Reset(clear);
    }

    /// <summary>
    /// Disposes of the AllocationManager, freeing all allocated memory and resources.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if there are still allocated buffers that have not been freed.</exception>
    public static void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _arena.Dispose();

        nuint unfreeBytes = 0u;
        foreach (var pair in _allocated)
        {
            unfreeBytes += pair.Value.Size;
            AlignedFree((void*)pair.Key);
        }

        if (unfreeBytes > 0u)
        {
            throw new MemoryLeakException([.. _allocated.Values]);
        }

        _allocated.Clear();
    }
}