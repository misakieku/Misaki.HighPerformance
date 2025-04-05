namespace Misaki.HighPerformance.Unsafe.Collections.Contracts;

internal unsafe interface IAllocator : IDisposable
{
    public T* Allocate<T>(uint size, uint alignSize, AllocationOption allocationOption)
        where T : unmanaged;

    public T* Reallocate<T>(T* buffer, uint size, uint alignSize)
        where T : unmanaged;

    public void Free<T>(T* buffer, uint size, uint alignSize)
        where T : unmanaged;
}