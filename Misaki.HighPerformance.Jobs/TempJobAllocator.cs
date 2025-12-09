using System.Runtime.CompilerServices;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Misaki.HighPerformance.Jobs;

public unsafe struct TempJobAllocator : IAllocator, IDisposable
{
    private const int _FRAME_LATENCY = 4;
    private const uint _ARENA_SIZE = 1024 * 1024; // 1 MB

    private DynamicArena* _pArena;
    private int _currentFrameIndex;
    private fixed int _allocationsPerFrame[_FRAME_LATENCY];

    private MemoryHandle _memoryHandle;
    private AllocationHandle _handle;

    public readonly AllocationHandle Handle => _handle;

    internal void Init()
    {
        var memoryHandle = default(MemoryHandle);

        _pArena = (DynamicArena*)AllocationManager.HeapAlloc((nuint)(sizeof(DynamicArena) * _FRAME_LATENCY), MemoryUtility.AlignOf<DynamicArena>(), AllocationOption.Clear, &memoryHandle);
        _currentFrameIndex = 0;
        _memoryHandle = memoryHandle;

        for (int i = 0; i < _FRAME_LATENCY; i++)
        {
            _pArena[i].Initialize(_ARENA_SIZE);
            _allocationsPerFrame[i] = 0;
        }

        _handle = new(Unsafe.AsPointer(ref this), &Allocate, &Reallocate, &Free);
    }

    private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        var selfPtr = (TempJobAllocator*)instance;
        var pCurrentArena = selfPtr->_pArena + selfPtr->_currentFrameIndex;
        var ptr = pCurrentArena->Allocate(size, alignment, allocationOption);
        if (ptr == null)
        {
            *pHandle = MemoryHandle.Invalid;
            return null;
        }

        Interlocked.Increment(ref selfPtr->_allocationsPerFrame[selfPtr->_currentFrameIndex]);
        *pHandle = AllocationManager.GetMagicHandle();
        return ptr;
    }

    private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption, MemoryHandle* pHandle)
    {
        if (ptr == null)
        {
            return Allocate(instance, newSize, alignment, allocationOption, pHandle);
        }

        var selfPtr = (TempJobAllocator*)instance;
        var pCurrentArena = selfPtr->_pArena + selfPtr->_currentFrameIndex;
        var newPtr = pCurrentArena->Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemoryUtility.MemCpy(ptr, newPtr,Math.Min(oldSize, newSize));

        return newPtr;
    }

    private static void Free(void* instance, void* ptr, MemoryHandle pHandle)
    {
        // The arena allocator does not free individual blocks, as it manages memory in chunks.
        var selfPtr = (TempJobAllocator*)instance;
        Interlocked.Decrement(ref selfPtr->_allocationsPerFrame[selfPtr->_currentFrameIndex]);
    }

    public int AdvanceFrame()
    {
        var allocations = Interlocked.Exchange(ref _allocationsPerFrame[_currentFrameIndex], 0);

        _currentFrameIndex = (_currentFrameIndex + 1) % _FRAME_LATENCY;
        var pCurrentArena = _pArena + _currentFrameIndex;
        pCurrentArena->Reset();

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
