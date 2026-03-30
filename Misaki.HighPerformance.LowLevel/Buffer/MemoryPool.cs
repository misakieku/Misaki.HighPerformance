using Misaki.HighPerformance.LowLevel.Utilities;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct MemoryPool<T, TOpts> : IDisposable
    where T : unmanaged, IMemoryAllocator<T, TOpts>
{
    private T* _pAllocator;
    private AllocationHandle _allocationHandle;

    public readonly ref T Allocator => ref *_pAllocator;
    public readonly AllocationHandle AllocationHandle => _allocationHandle;

    public MemoryPool(in TOpts opts)
    {
        _pAllocator = (T*)Malloc((nuint)sizeof(T));
        *_pAllocator = T.Create(opts);

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
        return ((T*)pAllocator)->Allocate(size, alignment, allocationOption);
    }

    private static void* Reallocate(void* pAllocator, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle* pHandle
#endif
        )
    {
        if (ptr == null)
        {
            return Allocate(pAllocator, newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
                , pHandle
#endif
                );
        }

        var newPtr = Allocate(pAllocator, newSize, alignment, allocationOption
#if MHP_ENABLE_SAFETY_CHECKS
            , pHandle
#endif
            );
        if (newPtr == null)
        {
            return null;
        }

        MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));
        Free(pAllocator, ptr
#if MHP_ENABLE_SAFETY_CHECKS
            , *pHandle
#endif
            );

        return newPtr;
    }

    private static void Free(void* pAllocator, void* ptr
#if MHP_ENABLE_SAFETY_CHECKS
        , MemoryHandle handle
#endif
        )
    {
        ((T*)pAllocator)->Free(ptr);
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