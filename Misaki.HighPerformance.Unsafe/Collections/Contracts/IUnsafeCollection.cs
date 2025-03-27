namespace Misaki.HighPerformance.Unsafe.Collections.Contracts;

public unsafe interface IUnsafeCollection<T> : IDisposable
    where T : unmanaged
{
    public T* Buffer
    {
        get;
    }

    public int Size
    {
        get;
    }

    public ref T this[int index] { get; }

    public void Clear();
    public void ReAlloc(int newSize);
}

