using Misaki.HighPerformance.LowLevel.Utilities;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A Two-Level Segregated Fit (TLSF) memory allocator.
/// Guarantees O(1) allocation and deallocation with very low fragmentation.
/// Note: This is a single-threaded allocator. Wrap it in a lock for thread safety.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct TLSF : IMemoryAllocator<TLSF, TLSF.CreationOptions>
{
    public struct CreationOptions
    {
        public nuint alignment;
        public nuint initialChunkSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryChunk
    {
        public MemoryChunk* next;
        public byte* memory;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct BlockHeader
    {
        [FieldOffset(0)]
        public BlockHeader* prevPhysBlock;
        [FieldOffset(8)]
        public nuint sizeAndFlags;

        [FieldOffset(16)]
        public BlockHeader* nextFree;
        [FieldOffset(24)]
        public BlockHeader* prevFree;

        public readonly bool IsFree => (sizeAndFlags & 1) != 0;
        public readonly bool IsPrevFree => (sizeAndFlags & 2) != 0;
        public readonly nuint Size => sizeAndFlags & ~3u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSizeAndFlags(nuint size, bool isFree, bool isPrevFree)
        {
            sizeAndFlags = size | (isFree ? 1u : 0u) | (isPrevFree ? 2u : 0u);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFree(bool isFree)
        {
            if (isFree)
            {
                sizeAndFlags |= 1u;
            }
            else
            {
                sizeAndFlags &= ~1u;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPrevFree(bool isPrevFree)
        {
            if (isPrevFree)
            {
                sizeAndFlags |= 2u;
            }
            else
            {
                sizeAndFlags &= ~2u;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BlockHeader* GetNextPhysBlock()
        {
            return (BlockHeader*)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)) + Size);
        }
    }

    private ulong _flBitmap;
    private uint* _slBitmaps;
    private BlockHeader** _blocks;
    private MemoryChunk* _chunks;
    private readonly nuint _alignment;
    private readonly nuint _chunkSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TLSF Create(in CreationOptions opts)
    {
        return new TLSF(opts.alignment, opts.initialChunkSize);
    }

    public TLSF (nuint alignment, nuint chunkSize)
    {
        alignment = alignment == 0 ? 16 : alignment;
        if (alignment < 16)
        {
            alignment = 16;
        }

        if ((alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of 2");
        }

        _alignment = alignment;
        _chunkSize = chunkSize == 0 ? 64 * 1024 : chunkSize;

        var slSize = 64 * (nuint)sizeof(uint);
        var blocksSize = 64 * 32 * (nuint)sizeof(BlockHeader*);
        _slBitmaps = (uint*)Calloc(slSize);
        _blocks = (BlockHeader**)Calloc(blocksSize);

        AddChunk(_chunkSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MappingInsert(nuint size, out int fli, out int sli)
    {
        var fl = BitOperations.Log2(size);
        sli = (int)((size ^ (1ul << fl)) >> (fl - 5));
        fli = fl - 5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MappingSearch(nuint size, out int fli, out int sli)
    {
        var shift = BitOperations.Log2(size) - 5;
        var mappingSize = size + (nuint)((1ul << shift) - 1);
        var fl = BitOperations.Log2(mappingSize);
        sli = (int)((mappingSize ^ (1ul << fl)) >> (fl - 5));
        fli = fl - 5;
    }

    private void AddChunk(nuint size)
    {
        var totalSize = size + (nuint)sizeof(MemoryChunk);
        var mem = (byte*)AlignedAlloc(totalSize, _alignment);

        MemoryChunk* chunk = (MemoryChunk*)mem;
        chunk->next = _chunks;
        chunk->memory = mem;
        _chunks = chunk;

        var blockMem = mem + sizeof(MemoryChunk);
        var offset = (nuint)blockMem % _alignment;
        if (offset != 0)
        {
            blockMem += (_alignment - offset);
        }

        var usableSize = totalSize - (nuint)(blockMem - mem);
        usableSize &= ~15u; // Align usable size to 16
        usableSize -= 16; // Room for sentinel

        BlockHeader* block = (BlockHeader*)blockMem;
        block->SetSizeAndFlags(usableSize, true, false);
        block->prevPhysBlock = null;

        BlockHeader* sentinel = block->GetNextPhysBlock();
        sentinel->SetSizeAndFlags(0, false, true); // Sentinel is marked as allocated so it's never coalesced
        sentinel->prevPhysBlock = block;

        InsertFreeBlock(block);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertFreeBlock(BlockHeader* block)
    {
        MappingInsert(block->Size, out var fli, out var sli);

        BlockHeader* head = _blocks[fli * 32 + sli];
        block->nextFree = head;
        block->prevFree = null;
        if (head != null)
        {
            head->prevFree = block;
        }
        _blocks[fli * 32 + sli] = block;

        _slBitmaps[fli] |= (1u << sli);
        _flBitmap |= (1ul << fli);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFreeBlock(BlockHeader* block)
    {
        MappingInsert(block->Size, out var fli, out var sli);
        RemoveFreeBlock(block, fli, sli);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFreeBlock(BlockHeader* block, int fli, int sli)
    {
        if (block->prevFree != null)
        {
            block->prevFree->nextFree = block->nextFree;
        }
        else
        {
            _blocks[fli * 32 + sli] = block->nextFree;
            if (block->nextFree == null)
            {
                _slBitmaps[fli] &= ~(1u << sli);
                if (_slBitmaps[fli] == 0)
                {
                    _flBitmap &= ~(1ul << fli);
                }
            }
        }

        if (block->nextFree != null)
        {
            block->nextFree->prevFree = block->prevFree;
        }
    }

    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (size == 0)
        {
            return null;
        }

        // TODO: Use Front Splitting to better handle alignment requirements

        if (alignment != 0 && (alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentException("Alignment must be a power of two.");
        }

        var totalSize = size + 16; // 16 bytes for header overhead
        totalSize = (totalSize + 15) & ~15u;
        if (totalSize < 32)
        {
            totalSize = 32;
        }

        MappingSearch(totalSize, out var fli, out var sli);

        var slMap = _slBitmaps[fli] & (~0u << sli);
        int blockFli, blockSli;

        if (slMap != 0)
        {
            blockFli = fli;
            blockSli = BitOperations.TrailingZeroCount(slMap);
        }
        else
        {
            var flMap = _flBitmap & (~0ul << (fli + 1));
            if (flMap == 0)
            {
                AddChunk(Math.Max(_chunkSize, totalSize * 2));
                MappingSearch(totalSize, out fli, out sli);
                slMap = _slBitmaps[fli] & (~0u << sli);
                if (slMap != 0)
                {
                    blockFli = fli;
                    blockSli = BitOperations.TrailingZeroCount(slMap);
                }
                else
                {
                    flMap = _flBitmap & (~0ul << (fli + 1));
                    blockFli = BitOperations.TrailingZeroCount(flMap);
                    blockSli = BitOperations.TrailingZeroCount(_slBitmaps[blockFli]);
                }
            }
            else
            {
                blockFli = BitOperations.TrailingZeroCount(flMap);
                blockSli = BitOperations.TrailingZeroCount(_slBitmaps[blockFli]);
            }
        }

        BlockHeader* block = _blocks[blockFli * 32 + blockSli];
        RemoveFreeBlock(block, blockFli, blockSli);

        var blockSize = block->Size;
        var remainSize = blockSize - totalSize;
        if (remainSize >= 32)
        {
            BlockHeader* remainBlock = (BlockHeader*)((byte*)block + totalSize);
            remainBlock->SetSizeAndFlags(remainSize, true, false);
            remainBlock->prevPhysBlock = block;

            BlockHeader* nextPhys = remainBlock->GetNextPhysBlock();
            if (nextPhys != null)
            {
                nextPhys->prevPhysBlock = remainBlock;
            }

            block->SetSizeAndFlags(totalSize, false, block->IsPrevFree);
            InsertFreeBlock(remainBlock);

            nextPhys->SetPrevFree(true);
        }
        else
        {
            block->SetSizeAndFlags(blockSize, false, block->IsPrevFree);
            BlockHeader* nextPhys = block->GetNextPhysBlock();
            if (nextPhys != null)
            {
                nextPhys->SetPrevFree(false);
            }
        }

        void* userPtr = (byte*)block + 16;
        if (allocationOption.HasFlag(AllocationOption.Clear))
        {
            MemClear(userPtr, block->Size - 16);
        }

        return userPtr;
    }

    public void Free(void* ptr)
    {
        if (ptr == null)
        {
            return;
        }

        BlockHeader* block = (BlockHeader*)((byte*)ptr - 16);
        block->SetFree(true);

        BlockHeader* prev = block->IsPrevFree ? block->prevPhysBlock : null;
        BlockHeader* next = block->GetNextPhysBlock();

        if (next->IsFree)
        {
            RemoveFreeBlock(next);
            block->SetSizeAndFlags(block->Size + next->Size, true, block->IsPrevFree);

            BlockHeader* nextNext = block->GetNextPhysBlock();
            if (nextNext != null)
            {
                nextNext->prevPhysBlock = block;
            }
        }

        if (prev != null)
        {
            RemoveFreeBlock(prev);
            prev->SetSizeAndFlags(prev->Size + block->Size, true, prev->IsPrevFree);
            block = prev;

            BlockHeader* nextNext = block->GetNextPhysBlock();
            if (nextNext != null)
            {
                nextNext->prevPhysBlock = block;
            }
        }

        InsertFreeBlock(block);

        BlockHeader* finalNext = block->GetNextPhysBlock();
        if (finalNext != null)
        {
            finalNext->SetPrevFree(true);
        }
    }

    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption = AllocationOption.None)
    {
        if (ptr == null)
        {
            return Allocate(newSize, alignment, allocationOption);
        }

        if (newSize == 0)
        {
            Free(ptr);
            return null;
        }

        BlockHeader* block = (BlockHeader*)((byte*)ptr - 16);
        var currentTotalSize = block->Size;
        var neededTotalSize = newSize + 16;
        neededTotalSize = (neededTotalSize + 15) & ~15u;
        if (neededTotalSize < 32)
        {
            neededTotalSize = 32;
        }

        if (currentTotalSize >= neededTotalSize)
        {
            var remainSize = currentTotalSize - neededTotalSize;
            if (remainSize >= 32)
            {
                BlockHeader* remainBlock = (BlockHeader*)((byte*)block + neededTotalSize);
                remainBlock->SetSizeAndFlags(remainSize, true, false);
                remainBlock->prevPhysBlock = block;

                BlockHeader* nextPhys = remainBlock->GetNextPhysBlock();
                if (nextPhys != null)
                {
                    nextPhys->prevPhysBlock = remainBlock;
                }

                block->SetSizeAndFlags(neededTotalSize, false, block->IsPrevFree);
                InsertFreeBlock(remainBlock);

                nextPhys->SetPrevFree(true);
            }
            return ptr;
        }

        BlockHeader* next = block->GetNextPhysBlock();
        if (next->IsFree && (currentTotalSize + next->Size) >= neededTotalSize)
        {
            RemoveFreeBlock(next);

            var combinedSize = currentTotalSize + next->Size;
            block->SetSizeAndFlags(combinedSize, false, block->IsPrevFree);

            BlockHeader* nextNext = block->GetNextPhysBlock();
            if (nextNext != null)
            {
                nextNext->prevPhysBlock = block;
            }

            var remainSize = combinedSize - neededTotalSize;
            if (remainSize >= 32)
            {
                BlockHeader* remainBlock = (BlockHeader*)((byte*)block + neededTotalSize);
                remainBlock->SetSizeAndFlags(remainSize, true, false);
                remainBlock->prevPhysBlock = block;

                BlockHeader* nextPhys = remainBlock->GetNextPhysBlock();
                if (nextPhys != null)
                {
                    nextPhys->prevPhysBlock = remainBlock;
                }

                block->SetSizeAndFlags(neededTotalSize, false, block->IsPrevFree);
                InsertFreeBlock(remainBlock);

                nextPhys->SetPrevFree(true);
            }
            else
            {
                if (nextNext != null)
                {
                    nextNext->SetPrevFree(false);
                }
            }

            return ptr;
        }

        var newPtr = Allocate(newSize, alignment, allocationOption);
        if (newPtr != null)
        {
            var copySize = oldSize < newSize ? oldSize : newSize;
            MemCpy(newPtr, ptr, copySize);
            Free(ptr);
        }
        return newPtr;
    }

    public void Dispose()
    {
        if (_blocks != null)
        {
            MemoryUtility.Free(_blocks);
            _blocks = null;
        }

        if (_slBitmaps != null)
        {
            MemoryUtility.Free(_slBitmaps);
            _slBitmaps = null;
        }

        MemoryChunk* chunk = _chunks;
        _chunks = null;

        while (chunk != null)
        {
            MemoryChunk* next = chunk->next;
            AlignedFree(chunk->memory);
            chunk = next;
        }
    }
}
