using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct MemoryPool<TAllocator, TOpts> : IDisposable
    where TAllocator : unmanaged, IMemoryAllocator<TAllocator, TOpts>
{
    private TAllocator* _pAllocator;
    private AllocationHandle _allocationHandle;

    public readonly ref TAllocator Allocator => ref Unsafe.AsRef<TAllocator>(_pAllocator);
    public readonly AllocationHandle AllocationHandle => _allocationHandle;

    public MemoryPool(in TOpts opts)
    {
        _pAllocator = (TAllocator*)Malloc((nuint)sizeof(TAllocator));
        *_pAllocator = TAllocator.Create(opts);

        _allocationHandle = new AllocationHandle
        {
            State = _pAllocator,
            Alloc = &Allocate,
            Realloc = &Reallocate,
            Free = &Free,
            IsValid = null
        };
    }

    private static void* Allocate(void* pAllocator, nuint size, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle* pHandle
#endif
        )
    {
        var ptr =  ((TAllocator*)pAllocator)->Allocate(size, alignment, allocationOption);
#if MHP_ENABLE_SAFETY_CHECKS
        if (ptr != null)
        {
            *pHandle = AllocationManager.AddAllocation(ptr, size);
        }
#endif
        return ptr;
    }

    private static void* Reallocate(void* pAllocator, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle* pHandle
#endif
        )
    {
        var newPtr = ((TAllocator*)pAllocator)->Reallocate(ptr, oldSize, newSize, alignment, allocationOption);
#if MHP_ENABLE_SAFETY_CHECKS
        if (ptr == null && newPtr != null)
        {
            *pHandle = AllocationManager.AddAllocation(newPtr, newSize);
        }
        else
        {
            if (newPtr == null)
            {
                AllocationManager.RemoveAllocation(*pHandle);
            }
            else
            {
                AllocationManager.UpdateAllocation(*pHandle, newPtr, newSize);
            }
        }
#endif
        return newPtr;
    }

    private static void Free(void* pAllocator, void* ptr
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle handle
#endif
        )
    {
        ((TAllocator*)pAllocator)->Free(ptr);
#if MHP_ENABLE_SAFETY_CHECKS
        AllocationManager.RemoveAllocation(handle);
#endif
    }

    public void Dispose()
    {
        if (_pAllocator == null)
        {
            return;
        }

        _pAllocator->Dispose();

        MemoryUtility.Free(_pAllocator);

        _pAllocator = null;
        _allocationHandle = default;
    }
}