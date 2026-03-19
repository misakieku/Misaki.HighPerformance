using System.Runtime.CompilerServices;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Misaki.HighPerformance.Jobs;

public unsafe struct TempJobAllocator : IAllocator, IDisposable
{
    private const int _FRAME_LATENCY = 4;
    private const int _MAGIC_ID = -559038737;

    private VirtualArena* _pArena;
    private int _currentFrameCount;
    private int _currentFrameIndex;
    private fixed int _allocationsPerFrame[_FRAME_LATENCY];

    private MemoryHandle _memoryHandle;
    private AllocationHandle _handle;

    public readonly AllocationHandle Handle => _handle;

    public void Initialize(nuint capacity)
    {
        var memoryHandle = default(MemoryHandle);

        _pArena = (VirtualArena*)AllocationManager.HeapAlloc((nuint)(sizeof(VirtualArena) * _FRAME_LATENCY), MemoryUtility.AlignOf<VirtualArena>(), AllocationOption.Clear, &memoryHandle);
        _currentFrameCount = 0;
        _currentFrameIndex = 0;
        _memoryHandle = memoryHandle;

        for (int i = 0; i < _FRAME_LATENCY; i++)
        {
            _pArena[i] = new VirtualArena(capacity);
            _allocationsPerFrame[i] = 0;
        }

        _handle = new AllocationHandle
        {
            State = Unsafe.AsPointer(ref this),
            Alloc = &Allocate,
            Realloc = &Reallocate,
            Free = &Free,
            IsValid = &IsValid,
        };
    }

    private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        var pSelf = (TempJobAllocator*)instance;
        var pCurrentArena = pSelf->_pArena + pSelf->_currentFrameIndex;
        var ptr = pCurrentArena->Allocate(size, alignment, allocationOption);
        if (ptr == null)
        {
            *pHandle = MemoryHandle.Invalid;
            return null;
        }

        Interlocked.Increment(ref pSelf->_allocationsPerFrame[pSelf->_currentFrameIndex]);
        *pHandle = new MemoryHandle(_MAGIC_ID, pSelf->_currentFrameCount);
        return ptr;
    }

    private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        if (ptr == null)
        {
            return Allocate(instance, newSize, alignment, allocationOption, pHandle);
        }

        var pSelf = (TempJobAllocator*)instance;
        var pCurrentArena = pSelf->_pArena + pSelf->_currentFrameIndex;
        var newPtr = pCurrentArena->Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemoryUtility.MemCpy(ptr, newPtr,Math.Min(oldSize, newSize));

        return newPtr;
    }

    private static void Free(void* instance, void* ptr, MemoryHandle handle)
    {
        // The arena allocator does not free individual blocks, as it manages memory in chunks.
        var pSelf = (TempJobAllocator*)instance;
        Interlocked.Decrement(ref pSelf->_allocationsPerFrame[pSelf->_currentFrameIndex]);
    }

    private static bool IsValid(void* instance, MemoryHandle handle)
    {
        var pSelf = (TempJobAllocator*)instance;
        return handle.id == _MAGIC_ID && handle.generation > pSelf->_currentFrameCount - _FRAME_LATENCY;
    }

    public int AdvanceFrame()
    {
        var allocations = Interlocked.Exchange(ref _allocationsPerFrame[_currentFrameIndex], 0);

        _currentFrameCount++;
        _currentFrameIndex = _currentFrameCount % _FRAME_LATENCY;

        (_pArena + _currentFrameIndex)->Reset();

        return allocations;
    }

    public void Dispose()
    {
        for (int i = 0; i < _FRAME_LATENCY; i++)
        {
            _pArena[i].Dispose();
        }

        AllocationManager.HeapFree(_pArena, _memoryHandle);
    }
}
