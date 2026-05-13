using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct VirtualStack : IMemoryAllocator<VirtualStack, VirtualStack.CreationOptions>
{
    private const nuint _PAGE_SIZE = 64 * 1024;

    public struct CreationOptions
    {
        public nuint reserveCapacity;
    }

    public static VirtualStack Create(in CreationOptions opts)
    {
        return new VirtualStack(opts.reserveCapacity);
    }

    public readonly ref struct Scope : IDisposable
    {
        private readonly VirtualStack* _allocator;
        private readonly AllocationHandle _handle;
        private readonly nuint _originalOffset;

        public readonly AllocationHandle AllocationHandle => _handle;
        public readonly nuint OriginalOffset => _originalOffset;

        internal Scope(VirtualStack* allocator, AllocationHandle handle)
        {
            _allocator = allocator;
            _handle = handle;
            _originalOffset = allocator->_allocatedOffset;
        }

        public void Dispose()
        {
            if (_allocator != null)
            {
                _allocator->_allocatedOffset = _allocator->_allocatedOffset > _originalOffset ? _originalOffset : _allocator->_allocatedOffset;
            }
        }
    }

    private byte* _baseAddress;
    private nuint _reserveCapacity;
    private nuint _committedSize;
    private nuint _allocatedOffset;

    public readonly byte* Buffer => _baseAddress;
    public readonly nuint Reserved => _reserveCapacity;
    public readonly nuint Committed => _committedSize;
    public readonly nuint Allocated => _allocatedOffset;

    public VirtualStack(nuint reserveCapacity)
    {
        _reserveCapacity = (reserveCapacity + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);
        _committedSize = 0;
        _allocatedOffset = 0;

        _baseAddress = (byte*)MemoryUtility.Mmap(null, _reserveCapacity, VirtualAllocationFlags.Reserve);
    }

    /// <summary>
    /// Creates a new scope instance associated with the current stack context.
    /// </summary>
    /// <remarks>
    /// The instance of <see cref="VirtualStack"/> must be pinned or allocated on the native heap to ensure that the pointer remains valid for the lifetime of the scope.
    /// </remarks>
    /// <returns>A <see cref="Scope"/> object that represents a scope tied to this stack.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scope CreateScope(AllocationHandle handle)
    {
#if MHP_ENABLE_SAFETY_CHECKS
        if (_baseAddress == null)
        {
            throw new InvalidOperationException("Allocator must be initialized before creating a scope.");
        }
#endif
        return new Scope((VirtualStack*)Unsafe.AsPointer(ref this), handle);
    }

    /// <summary>
    /// Allocates a block of memory of the specified size and alignment.
    /// </summary>
    /// <remarks>
    /// This is not thread-safe. It is designed for single-threaded or thread-local contexts.
    /// </remarks>
    public void* Allocate(nuint size, nuint alignment, AllocationOption option = AllocationOption.None)
    {
        if (size == 0)
        {
            return null;
        }

        if ((alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        }

        // Align the requested offset
        var alignedOffset = (_allocatedOffset + alignment - 1) & ~(alignment - 1);
        var newAllocatedOffset = alignedOffset + size;

        if (newAllocatedOffset > _reserveCapacity)
        {
            return null;
        }

        if (newAllocatedOffset > _committedSize)
        {
            var sizeToCommit = newAllocatedOffset - _committedSize;

            // Align the commit size to the 64KB OS Page Size
            sizeToCommit = (sizeToCommit + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);

            var commitAddress = _baseAddress + _committedSize;
            var result = MemoryUtility.Mmap(commitAddress, sizeToCommit, VirtualAllocationFlags.Commit);

            if (result == null)
            {
                return null; // Out of physical RAM
            }

            _committedSize += sizeToCommit;
        }

        var userPtr = _baseAddress + alignedOffset;
        _allocatedOffset = newAllocatedOffset;

        if (option.HasOption(AllocationOption.Clear))
        {
            MemoryUtility.MemClear(userPtr, size);
        }

        return userPtr;
    }

    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
    {
        if (_baseAddress == null)
        {
            return null;
        }

        if (newSize < oldSize)
        {
            return ptr;
        }

        if (ptr == null)
        {
            return Allocate(newSize, alignment, allocationOption);
        }

        if ((byte*)ptr + oldSize == _baseAddress + _allocatedOffset)
        {
            var diff = newSize - oldSize;
            _allocatedOffset += diff;

            if (_allocatedOffset > _committedSize)
            {
                var sizeToCommit = _allocatedOffset - _committedSize;

                // Align the commit size to the 64KB OS Page Size
                sizeToCommit = (sizeToCommit + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);

                var commitAddress = _baseAddress + _committedSize;
                var result = MemoryUtility.Mmap(commitAddress, sizeToCommit, VirtualAllocationFlags.Commit);

                if (result == null)
                {
                    return null;
                }

                _committedSize += sizeToCommit;
            }

            if (allocationOption.HasOption(AllocationOption.Clear))
            {
                MemoryUtility.MemClear(_baseAddress + _allocatedOffset - diff, diff);
            }

            return ptr;
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemoryUtility.MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

        return newPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Free(void* ptr)
    {
    }

    /// <summary>
    /// Resets the internal offset to its initial position, keeping the committed physical memory intact for future reuse.
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
        var size = _reserveCapacity;

        _baseAddress = null;
        _allocatedOffset = 0;
        _committedSize = 0;
        _reserveCapacity = 0;

        MemoryUtility.Munmap(ptr, size);
    }
}
