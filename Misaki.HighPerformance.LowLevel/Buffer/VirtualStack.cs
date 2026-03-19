using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct VirtualStack : IMemoryAllocator<VirtualStack, VirtualStack.CreationOpts>
{
    private const nuint _PAGE_SIZE = 64 * 1024;

    public struct CreationOpts
    {
        public nuint reserveCapacity;
    }

    public static VirtualStack Create(in CreationOpts opts)
    {
        return new VirtualStack(opts.reserveCapacity);
    }

    public readonly ref struct Scope : IDisposable
    {
        private readonly VirtualStack* _allocator;
        private readonly AllocationHandle _handle;
        private readonly nuint _originalOffset;

        public readonly AllocationHandle AllocationHandle => _handle;

        internal Scope(VirtualStack* allocator, AllocationHandle handle)
        {
            _allocator = allocator;
            _handle = handle;
            _originalOffset = allocator->_allocatedOffset;
#if ENABLE_SAFETY_CHECKS
            _allocator->_activeScopeCount++;
#endif
        }

        public void Dispose()
        {
            if (_allocator != null)
            {
                _allocator->_allocatedOffset = _allocator->_allocatedOffset > _originalOffset ? _originalOffset : _allocator->_allocatedOffset;
#if ENABLE_SAFETY_CHECKS
                _allocator->_activeScopeCount--;
#endif
            }
        }
    }

    private byte* _baseAddress;
    private nuint _reserveCapacity;
    private nuint _committedSize;
    private nuint _allocatedOffset;
#if ENABLE_SAFETY_CHECKS
    private uint _activeScopeCount;
#endif

    internal readonly byte* Buffer => _baseAddress;
    internal nuint Offset
    {
        readonly get => _allocatedOffset;
        set => _allocatedOffset = value;
    }

    public VirtualStack(nuint reserveCapacity)
    {
        _reserveCapacity = (reserveCapacity + _PAGE_SIZE - 1) & ~(_PAGE_SIZE - 1);
        _committedSize = 0;
        _allocatedOffset = 0;

        _baseAddress = (byte*)Mmap(null, _reserveCapacity, VirtualAllocationFlags.Reserve);

#if ENABLE_SAFETY_CHECKS
        _activeScopeCount = 0;
#endif
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
#if ENABLE_SAFETY_CHECKS
        if (_activeScopeCount == 0)
        {
            throw new InvalidOperationException("Allocations can only be made within an active memory scope.");
        }
#endif

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

        var userPtr = _baseAddress + alignedOffset;
        _allocatedOffset = newAllocatedOffset;

        if (option.HasFlag(AllocationOption.Clear))
        {
            MemClear(userPtr, size);
        }

        return userPtr;
    }

    /// <summary>
    /// Resets the internal offset to its initial position, keeping the committed physical memory intact for future reuse.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _allocatedOffset = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Free(void* ptr)
    {
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
