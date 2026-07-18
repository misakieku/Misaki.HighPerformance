using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct MemoryPool<TAllocator, TOpts> : IDisposable
    where TAllocator : unmanaged, IMemoryAllocator<TAllocator, TOpts>
{
    private TAllocator* _pAllocator;
    private AllocationHandle.Allocator* _pHandleAllocator;
    private AllocationHandle _allocationHandle;

    public readonly ref TAllocator Allocator => ref Unsafe.AsRef<TAllocator>(_pAllocator);
    public readonly AllocationHandle AllocationHandle => _allocationHandle;

    public MemoryPool(in TOpts opts)
    {
        var allocator = TAllocator.Create(opts);

        _pAllocator = (TAllocator*)NativeMemory.Alloc((nuint)sizeof(TAllocator));
        *_pAllocator = allocator;

        _pHandleAllocator = (AllocationHandle.Allocator*)NativeMemory.Alloc((nuint)sizeof(AllocationHandle.Allocator));
        *_pHandleAllocator = new AllocationHandle.Allocator
        {
            state = _pAllocator,
            alloc = &Allocate,
            realloc = &Reallocate,
            free = &Free
        };

        _allocationHandle = new AllocationHandle(_pHandleAllocator);
    }

    private static void* Allocate(void* pAllocator, nuint size, nuint alignment, AllocationOption allocationOption)
    {
        return ((TAllocator*)pAllocator)->Allocate(size, alignment, allocationOption);
    }

    private static void* Reallocate(void* pAllocator, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
    {
        return ((TAllocator*)pAllocator)->Reallocate(ptr, oldSize, newSize, alignment, allocationOption);
    }

    private static void Free(void* pAllocator, void* ptr)
    {
        ((TAllocator*)pAllocator)->Free(ptr);
    }

    public void Dispose()
    {
        if (_pAllocator == null)
        {
            return;
        }

        _pAllocator->Dispose();

        NativeMemory.Free(_pHandleAllocator);
        NativeMemory.Free(_pAllocator);

        _pAllocator = null;
        _pHandleAllocator = null;
        _allocationHandle = default;
    }
}
