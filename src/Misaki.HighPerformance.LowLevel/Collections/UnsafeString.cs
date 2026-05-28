using Misaki.HighPerformance.LowLevel.Buffer;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Collections;

public unsafe struct UnsafeString : IDisposable
{
    private UnsafeArray<char> _chars;

    public readonly int Length => _chars.Length;

    public readonly char this[int index] => _chars[index];

    public UnsafeString(ReadOnlySpan<char> span, AllocationHandle handle)
    {
        _chars = new UnsafeArray<char>(span.Length, handle);
        span.CopyTo(_chars.AsSpan());
    }

    public UnsafeString(UnsafeString other)
    {
        _chars = new UnsafeArray<char>(other.Length, other._chars.AllocationHandle);
        other.AsSpan().CopyTo(_chars.AsSpan());
    }

    public readonly UnsafeString Copy(AllocationHandle handle = default)
    {
        handle = handle.IsValid ? handle : _chars.AllocationHandle;
        var clone = new UnsafeString(AsSpan(), handle);
        return clone;
    }

    public readonly ReadOnlySpan<char> AsSpan()
    {
        return _chars.AsSpan();
    }

    public readonly void* GetUnsafePtr()
    {
        return _chars.GetUnsafePtr();
    }

    public readonly override string ToString()
    {
        return new string(_chars.AsSpan());
    }

    public void Dispose()
    {
        _chars.Dispose();
    }
}


public unsafe struct UnsafeText : IDisposable
{
    private UnsafeArray<byte> _chars;

    public readonly int Length => _chars.Length;

    public readonly byte this[int index] => _chars[index];

    public UnsafeText(ReadOnlySpan<byte> span, AllocationHandle handle)
    {
        _chars = new UnsafeArray<byte>(span.Length, handle);
        span.CopyTo(_chars.AsSpan());
    }

    public UnsafeText(UnsafeText other)
    {
        _chars = new UnsafeArray<byte>(other.Length, other._chars.AllocationHandle);
        other.AsSpan().CopyTo(_chars.AsSpan());
    }

    public readonly UnsafeText Copy(AllocationHandle handle = default)
    {
        handle = handle.IsValid ? handle : _chars.AllocationHandle;
        var clone = new UnsafeText(AsSpan(), handle);
        return clone;
    }

    public readonly ReadOnlySpan<byte> AsSpan()
    {
        return _chars.AsSpan();
    }

    public readonly void* GetUnsafePtr()
    {
        return _chars.GetUnsafePtr();
    }

    public readonly override string ToString()
    {
        return Encoding.UTF8.GetString(_chars.AsSpan());
    }

    public void Dispose()
    {
        _chars.Dispose();
    }
}
