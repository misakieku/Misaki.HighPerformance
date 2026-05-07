using Misaki.HighPerformance.LowLevel.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct VirtualMemoryBlock : IDisposable
{
    private byte* _baseAddress;
    private nuint _size;
    private nuint _committed;

    public VirtualMemoryBlock(nuint size)
    {
        _baseAddress = (byte*)MemoryUtility.Mmap(null, size, VirtualAllocationFlags.Reserve);
        _size = size;
        _committed = 0;
    }

    public void Dispose()
    {
        if (_baseAddress == null)
        {
            return;
        }

        var addr = _baseAddress;
        _baseAddress = null;
        _size = 0;
        _committed = 0;

        MemoryUtility.Munmap(addr, _size);
    }
}
