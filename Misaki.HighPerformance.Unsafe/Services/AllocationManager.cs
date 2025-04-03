using Misaki.HighPerformance.Unsafe.Buffer;
using Misaki.HighPerformance.Unsafe.Collections;

namespace Misaki.HighPerformance.Unsafe.Services;

public static unsafe class AllocationManager
{
    private static DynamicArena _arena;
    private static bool _initialized;

    private static readonly Lock _lock = new();

    public static void Initialize(uint initialSize)
    {
        _arena = new DynamicArena(initialSize);
        _initialized = true;
    }

    internal static T* Allocate<T>(uint size, uint alignSize, Allocator allocator, AllocationOption allocationType)
        where T : unmanaged
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The AllocationManager has not been initialized.");
        }

        lock (_lock)
        {
            return allocator switch
            {
                Allocator.Temp => (T*)_arena.Allocate(size * (uint)sizeof(T), alignSize, allocationType),
                Allocator.Persistent => (T*)AlignedAlloc((nuint)(size * sizeof(T)), alignSize),
                _ => throw new ArgumentOutOfRangeException(nameof(allocator), "Invalid allocator type."),
            };
        }
    }

    internal static void Free(void* ptr, Allocator allocator)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The AllocationManager has not been initialized.");
        }

        lock (_lock)
        {
            if (allocator == Allocator.Persistent)
            {
                AlignedFree(ptr);
            }
        }
    }

    public static void Reset(bool clear = false)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The AllocationManager has not been initialized.");
        }

        _arena.Reset(clear);
    }

    public static void Dispose()
    {
        _arena.Dispose();
    }
}