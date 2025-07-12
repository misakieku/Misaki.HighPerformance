using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.LowLevel.Buffer;

// TODO: Implement a pool for UnsafeArray<T>.
public unsafe static class UnsafeArrayPool
{
    public static UnsafeArray<T> Rent<T>(int minimalSize)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public static void Return<T>(UnsafeArray<T> array)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }
}