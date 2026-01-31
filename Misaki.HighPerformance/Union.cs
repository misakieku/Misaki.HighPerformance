using System.Runtime.InteropServices;

namespace Misaki.HighPerformance;

[StructLayout(LayoutKind.Explicit)]
public struct Union<T0, T1>
    where T0 : unmanaged
    where T1 : unmanaged
{
    [FieldOffset(0)]
    public T0 v0;

    [FieldOffset(0)]
    public T1 v1;
}