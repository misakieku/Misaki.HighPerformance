using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A thread-safe memory management structure that reserves a large virtual address space and commits physical memory on demand as allocations are made.
/// </summary>
public unsafe struct VirtualArena : IMemoryAllocator<VirtualArena, VirtualArena.CreationOptions>
{
    public struct CreationOptions
    {
        public nuint reserveCapacity;
    }
    public static VirtualArena Create(in CreationOptions opts)
    {
        return new VirtualArena(opts.reserveCapacity);
    }

    private const nuint _PAGE_SIZE = 64 * 1024;

    private byte* _baseAddress;
    private nuint _reserveCapacity;
    private nuint _committedSize;
    private nuint _allocatedOffset;

    private volatile int _allocationLock;

    public VirtualArena(nuint reserveCapacity)
    {
        _reserveCapacity = (reserveCapacity + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);
        _committedSize = 0;
        _allocatedOffset = 0;

        _baseAddress = (byte*)Mmap(null, _reserveCapacity, VirtualAllocationFlags.Reserve);

        if (_baseAddress == null)
        {
            throw new OutOfMemoryException("Failed to reserve virtual address space.");
        }
    }

    /// <summary>
    /// Allocates a block of memory of the specified size and alignment, using the given allocation options.
    /// </summary>
    /// <remarks>
    /// This is thread safe.
    /// </remarks>
    /// <param name="size">The number of bytes to allocate. Must be greater than zero and less than or equal to the reserved capacity.</param>
    /// <param name="alignment">The alignment, in bytes, for the allocated memory block. Must be a power of two.</param>
    /// <param name="allocationOption">The allocation options that control allocation behavior.</param>
    /// <returns>A pointer to the allocated memory block if the allocation succeeds, otherwise null.</returns>
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption)
    {
        while (Interlocked.CompareExchange(ref _allocationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        void* ptr;

        try
        {
            // Align the requested offset
            var alignedOffset = (_allocatedOffset + alignment - 1) & ~(alignment - 1);
            var newAllocatedOffset = alignedOffset + size;

            if (newAllocatedOffset > _reserveCapacity)
            {
                return null; // Out of reserved space
            }

            if (newAllocatedOffset > _committedSize)
            {
                var sizeToCommit = newAllocatedOffset - _committedSize;

                // Align the commit size to the 64KB OS Page Size
                sizeToCommit = (sizeToCommit + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);

                var commitAddress = _baseAddress + _committedSize;
                var result = Mmap(commitAddress, sizeToCommit, VirtualAllocationFlags.Commit);

                if (result == null)
                {
                    return null; // Out of physical RAM
                }

                _committedSize += sizeToCommit;
            }

            ptr = _baseAddress + alignedOffset;
            _allocatedOffset = newAllocatedOffset;
        }
        finally
        {
            Interlocked.Exchange(ref _allocationLock, 0);
        }

        if (allocationOption.HasFlag(AllocationOption.Clear))
        {
            MemClear(ptr, size);
        }

        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Free(void* ptr)
    {
    }

    /// <summary>
    /// Resets the arena.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _allocatedOffset = 0;
    }

    public void Dispose()
    {
        if (_baseAddress != null)
        {
            Munmap(_baseAddress, _reserveCapacity);

            _baseAddress = null;
            _reserveCapacity = 0;
            _committedSize = 0;
            _allocatedOffset = 0;
        }
    }
}
