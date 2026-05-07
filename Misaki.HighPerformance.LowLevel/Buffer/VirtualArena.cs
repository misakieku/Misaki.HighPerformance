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

    private int _allocationLock;

    public readonly byte* Buffer => _baseAddress;
    public readonly nuint Reserved => _reserveCapacity;
    public readonly nuint Committed => _committedSize;
    public readonly nuint Allocated => _allocatedOffset;

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
        if (_baseAddress == null || size == 0)
        {
            return null;
        }

        var spinWait = new SpinWait();

        while (true)
        {
            var currentOffset = Volatile.Read(ref _allocatedOffset);

            // Align the requested offset
            var alignedOffset = (currentOffset + alignment - 1) & ~(alignment - 1);
            var newOffset = alignedOffset + size;

            if (newOffset > _reserveCapacity)
            {
                return null;
            }

            if (newOffset <= Volatile.Read(ref _committedSize))
            {
                // Try to atomically claim this space. 
                if (Interlocked.CompareExchange(ref _allocatedOffset, newOffset, currentOffset) == currentOffset)
                {
                    var ptr = _baseAddress + alignedOffset;

                    if (allocationOption.HasOption(AllocationOption.Clear))
                    {
                        MemClear(ptr, size);
                    }

                    return ptr;
                }

                spinWait.SpinOnce();
                continue;
            }

            var lockWait = new SpinWait();
            while (Interlocked.CompareExchange(ref _allocationLock, 1, 0) != 0)
            {
                lockWait.SpinOnce();
            }

            try
            {
                // DOUBLE-CHECK: Did another thread commit enough memory while we were waiting for the lock?
                var currentCommitted = _committedSize;
                if (newOffset > currentCommitted)
                {
                    var sizeToCommit = newOffset - currentCommitted;
                    sizeToCommit = (sizeToCommit + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);

                    var commitAddress = _baseAddress + currentCommitted;
                    var result = Mmap(commitAddress, sizeToCommit, VirtualAllocationFlags.Commit);

                    if (result == null)
                    {
                        return null;
                    }

                    Volatile.Write(ref _committedSize, currentCommitted + sizeToCommit);
                }
            }
            finally
            {
                Volatile.Write(ref _allocationLock, 0);
            }

            // We committed the memory (or realized someone else did).
            // Loop back up to try the lock-free allocation again!
        }
    }

    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
    {
        if (_baseAddress == null || newSize == 0)
        {
            return null;
        }

        if (newSize <= oldSize)
        {
            return ptr;
        }

        if (ptr == null)
        {
            return Allocate(newSize, alignment, allocationOption);
        }

        var additionalSize = newSize - oldSize;
        var currentOffset = Volatile.Read(ref _allocatedOffset);

        // Fast-path: Check if it's the last allocated block
        if ((byte*)ptr + oldSize == _baseAddress + currentOffset)
        {
            var newOffset = currentOffset + additionalSize;

            // Check if we need to commit more physical memory
            if (newOffset > Volatile.Read(ref _committedSize))
            {
                var spinWait = new SpinWait();
                while (Interlocked.CompareExchange(ref _allocationLock, 1, 0) != 0)
                {
                    spinWait.SpinOnce();
                }

                try
                {
                    // DOUBLE CHECK: Did another thread commit the memory while we waited?
                    var currentCommitted = _committedSize;
                    if (newOffset > currentCommitted)
                    {
                        var sizeToCommit = newOffset - currentCommitted;
                        sizeToCommit = (sizeToCommit + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);

                        var commitAddress = _baseAddress + currentCommitted;
                        var result = Mmap(commitAddress, sizeToCommit, VirtualAllocationFlags.Commit);

                        if (result == null)
                        {
                            return null; // OOM or mapping failure
                        }

                        Volatile.Write(ref _committedSize, currentCommitted + sizeToCommit);
                    }
                }
                finally
                {
                    Volatile.Write(ref _allocationLock, 0);
                }
            }

            // Try to atomically extend the block
            if (Interlocked.CompareExchange(ref _allocatedOffset, newOffset, currentOffset) == currentOffset)
            {
                // Safe to clear: we own the space between oldSize and newOffset
                if (allocationOption.HasOption(AllocationOption.Clear) && additionalSize > 0)
                {
                    MemClear((byte*)ptr + oldSize, additionalSize);
                }

                return ptr;
            }
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemCpy(newPtr, ptr, oldSize);
        return newPtr;
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
        if (_baseAddress == null)
        {
            return;
        }

        var ptr = _baseAddress;

        _baseAddress = null;
        _allocatedOffset = 0;
        _committedSize = 0;
        _reserveCapacity = 0;

        Munmap(ptr, _reserveCapacity);
    }
}
