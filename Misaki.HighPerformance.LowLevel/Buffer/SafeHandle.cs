namespace Misaki.HighPerformance.LowLevel.Buffer;

public unsafe struct SafeHandle
{
    private const nuint _ALIGNMENT = 16u;

    public int valid;

    public static nuint GetAlignWithHeader(nuint baseAlign)
    {
        return Math.Max(_ALIGNMENT, baseAlign);
    }

    public static nuint GetPaddedHeaderSize(nuint baseAlign)
    {
        var headerBaseSize = (nuint)sizeof(SafeHandle);
        var dataAlignment = Math.Max(_ALIGNMENT, baseAlign);
        return (headerBaseSize + (dataAlignment - 1u)) & ~(dataAlignment - 1u);
    }

    public static SafeHandle* GetSafeHandle(void* ptr, nuint baseAlign)
    {
        if (ptr == null)
        {
            return null;
        }

        var alignedHeaderSize = GetPaddedHeaderSize(baseAlign);
        return (SafeHandle*)((byte*)ptr - alignedHeaderSize);
    }
}
