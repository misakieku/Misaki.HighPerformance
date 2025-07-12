using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A memory management structure that allocates and resets memory blocks with specified alignment.
/// </summary>
public unsafe struct Arena : IDisposable
{
    private byte* _buffer;
    private nuint _size;
    private nuint _offset;

    private bool _disposed;

    public Arena(nuint size)
    {
        Initialize(size);
    }

    public void Initialize(nuint size)
    {
        if (_buffer != null)
        {
            return;
        }

        _buffer = (byte*)Malloc(size);
        _size = size;
        _offset = 0;
    }

    /// <summary>
    /// Allocates a block of memory of a specified size with a given alignment. Returns a pointer to the allocated
    /// memory or null if allocation fails.
    /// You don't need to free the memory manually, it will be freed when the arena is disposed.
    /// </summary>
    /// <param name="size">Specifies the amount of memory to allocate in bytes.</param>
    /// <param name="alignment">Defines the alignment requirement for the allocated memory.</param>
    /// <param name="allocationOption">The option when allocating memory.</param>
    /// <returns>A pointer to the allocated memory block or null if the allocation cannot be fulfilled.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DynamicArena));
        }

        var offset = _offset + alignment - 1 & ~(alignment - 1);
        if (offset + size > _size)
        {
            return null;
        }

        _offset = offset + size;
        var ptr = _buffer + offset;

        if (allocationOption.HasFlag(AllocationOption.Clear))
        {
            MemClear(ptr, size);
        }

        return ptr;
    }

    /// <summary>
    /// Resets the arena, optionally clearing the allocated memory.
    /// </summary>
    /// <param name="clear">If true, the allocated memory will be cleared; otherwise, it will not be cleared.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void Reset()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DynamicArena));
        }

        _offset = 0;
    }

    public void Dispose()
    {
        Free(_buffer);

        _buffer = null;
        _size = 0;
        _offset = 0;

        _disposed = true;
    }
}