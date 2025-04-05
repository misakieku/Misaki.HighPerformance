#define UNSAFE_COLLECTION_CHECK

using Misaki.HighPerformance.Unsafe.Collections;
#if UNSAFE_COLLECTION_CHECK
#if DEBUG
using System.Diagnostics;
#endif
#endif

namespace Misaki.HighPerformance.Unsafe.Buffer;

// TODO: Custom allocator
public static unsafe class AllocationManager
{
    private const uint _DEFAULT_ARENA_SIZE = 512 * 1024; // 512 KB

    private static DynamicArena _arena;
    private static bool _initialized;

#if UNSAFE_COLLECTION_CHECK
    private static Dictionary<IntPtr, MemoryLeakExceptionInfo> _allocated = null!;
#endif

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
#if UNSAFE_COLLECTION_CHECK
        _allocated = new Dictionary<nint, MemoryLeakExceptionInfo>(32);
#endif

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

#if UNSAFE_COLLECTION_CHECK
                    _allocated[(IntPtr)buffer] = new MemoryLeakExceptionInfo
                    {
                        Size = allocationSize,
#if DEBUG
                        StackTrace = new StackTrace(true)
#endif
                    };
#endif

                    if (allocationOption.HasFlag(AllocationOption.Clear))
                    {
                        MemClear(buffer, allocationSize);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(allocator), "Invalid allocator type.");
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

#if UNSAFE_COLLECTION_CHECK
                    // If the allocation map can not find the old value, it means that it was a untracked allocation
                    if (_allocated.Remove((IntPtr)buffer))
                    {
                        _allocated[(IntPtr)newBuffer] = new MemoryLeakExceptionInfo
                        {
                            Size = allocationSize,
#if DEBUG
                            StackTrace = new StackTrace(true)
#endif
                        };
                    }
#endif
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
#if UNSAFE_COLLECTION_CHECK
                _allocated.Remove((IntPtr)ptr);
#endif
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
#if UNSAFE_COLLECTION_CHECK
    /// <exception cref="MemoryLeakException">Thrown if there are still allocated buffers that have not been freed.</exception>
#endif
    public static void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _arena.Dispose();

#if UNSAFE_COLLECTION_CHECK
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
#endif

        _initialized = false;
    }
}