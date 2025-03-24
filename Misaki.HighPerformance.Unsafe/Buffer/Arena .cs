using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Unsafe.Buffer;

/// <summary>
/// A memory management structure that allocates and resets memory blocks with specified alignment.
/// </summary>
public unsafe struct Arena : IDisposable
{
    private void* _buffer;
    private ulong _size;
    private ulong _offset;

    private bool _disposed;

    public Arena(ulong size)
    {
        _buffer = Marshal.AllocHGlobal((IntPtr)size).ToPointer();
        _size = size;
        _offset = 0;
    }

    /// <summary>
    /// Allocates a block of memory of a specified size with a given alignment. Returns a pointer to the allocated
    /// memory or null if allocation fails.
    /// </summary>
    /// <param name="size">Specifies the amount of memory to allocate in bytes.</param>
    /// <param name="alignSize">Defines the alignment requirement for the allocated memory.</param>
    /// <returns>A pointer to the allocated memory block or null if the allocation cannot be fulfilled.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void* Allocate(ulong size, uint alignSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var offset = (_offset + alignSize - 1) & ~(alignSize - 1);
        if (offset + size > _size)
        {
            return null;
        }

        _offset = offset + size;
        var ptr = (byte*)_buffer + offset;
        MemClear(ptr, (uint)size);

        return ptr;
    }

    /// <summary>
    /// Resets the arena, optionally clearing the allocated memory.
    /// </summary>
    /// <param name="clear">If true, the allocated memory will be cleared; otherwise, it will not be cleared.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void Reset(bool clear = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (clear)
        {
            MemClear(_buffer, (uint)_size);
        }

        _offset = 0;
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal((IntPtr)_buffer);

        _buffer = null;
        _size = 0;
        _offset = 0;

        _disposed = true;
    }
}