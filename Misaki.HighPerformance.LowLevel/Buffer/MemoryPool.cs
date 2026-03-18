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

    private static void* Allocate(void* pAllocator, nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        return ((T*)pAllocator)->Allocate(size, alignment, allocationOption);
    }

    private static void* Reallocate(void* pAllocator, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        if (ptr == null)
        {
            return Allocate(pAllocator, newSize, alignment, allocationOption, pHandle);
        }

        MemoryHandle newHandle;
        var newPtr = Allocate(pAllocator, newSize, alignment, allocationOption, &newHandle);
        if (newPtr == null)
        {
            return null;
        }

        MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));
        Free(pAllocator, ptr, *pHandle);

        *pHandle = newHandle;
        return newPtr;
    }

    private static void Free(void* pAllocator, void* ptr, MemoryHandle handle)
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