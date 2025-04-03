using Misaki.HighPerformance.Unsafe.Buffer;
using Misaki.HighPerformance.Unsafe.Collections;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Unsafe.Services;

public static unsafe class AllocationManager
{
    private readonly struct AllocationInfo(void* ptr, nuint size)
    {
        public readonly void* ptr = ptr;
        public readonly nuint size = size;
    }

    private static DynamicArena _arena;
    private static bool _initialized;

    private static UnsafeQueue<AllocationInfo> _allocated;

    private static readonly Lock _lock = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void VerifyInitialization()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("The AllocationManager has not been initialized.");
        }
    }

    /// <summary>
    /// Initializes the AllocationManager with a specified initial size for the memory arena.
    /// </summary>
    /// <param name="initialSize">The initial size in bytes for the memory arena.</param>
    public static void Initialize(uint initialSize)
    {
        if (_initialized || initialSize <= 0)
        {
            return;
        }

        _arena = new DynamicArena(initialSize);
        _allocated = new UnsafeQueue<AllocationInfo>(16, Allocator.Persistent);

        _initialized = true;
    }

    internal static T* Allocate<T>(uint size, uint alignSize, Allocator allocator, AllocationOption allocationOption)
        where T : unmanaged
    {
        if (allocationOption == AllocationOption.UnTracked)
        {
            return (T*)AlignedAlloc(size, alignSize);
        }

        VerifyInitialization();

        lock (_lock)
        {
            switch (allocator)
            {
                case Allocator.Temp:
                    return (T*)_arena.Allocate(size * (uint)sizeof(T), alignSize, allocationOption);

                case Allocator.Persistent:
                    var allocationSize = size * (nuint)sizeof(T);
                    var buffer = (T*)AlignedAlloc(allocationSize, alignSize);
                    _allocated.Enqueue(new AllocationInfo(buffer, allocationSize));
                    return buffer;

                default:
                    throw new ArgumentOutOfRangeException(nameof(allocator), "Invalid allocator type.");
            }
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
        VerifyInitialization();
        _arena.Reset(clear);
    }

    /// <summary>
    /// Disposes of the AllocationManager, freeing all allocated memory and resources.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if there are still allocated buffers that have not been freed.</exception>
    public static void Dispose()
    {
        _arena.Dispose();

        nuint unfreedBytes = 0u;
        while (_allocated.TryDequeue(out var allocationInfo))
        {
            unfreedBytes += allocationInfo.size;
            AlignedFree(allocationInfo.ptr);
        }

        _allocated.Dispose();

        if (unfreedBytes > 0u)
        {
            throw new InvalidOperationException($"There are still {unfreedBytes} bytes allocated buffers. Please free them before disposing.");
        }
    }
}