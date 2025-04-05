using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Collections.Contracts;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Unsafe.Buffer;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct AllocationHandler : IAllocator
{
    public unsafe T* Allocate<T>(uint size, uint alignSize, AllocationOption allocationOption)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public unsafe T* Reallocate<T>(T* buffer, uint size, uint alignSize)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public unsafe void Free<T>(T* buffer, uint size, uint alignSize)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}