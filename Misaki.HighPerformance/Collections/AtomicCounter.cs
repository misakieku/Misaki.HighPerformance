namespace Misaki.HighPerformance.Collections;

public struct AtomicCounter32
{
    private int _value;

    public readonly int Value => _value;

    public AtomicCounter32(int initialValue = 0)
    {
        _value = initialValue;
    }

    public int Add(int value = 1)
    {
        return Interlocked.Add(ref _value, value);
    }

    public int Subtract(int value = 1)
    {
        return Add(-value);
    }

    public void Reset(int value = 0)
    {
        Interlocked.Exchange(ref _value, value);
    }
}

public struct AtomicCounter64
{
    private long _value;

    public readonly long Value => _value;

    public AtomicCounter64(long initialValue = 0L)
    {
        _value = initialValue;
    }

    public long Add(long value = 1L)
    {
        return Interlocked.Add(ref _value, value);
    }

    public long Subtract(long value = 1L)
    {
        return Add(-value);
    }

    public void Reset(long value = 0L)
    {
        Interlocked.Exchange(ref _value, value);
    }
}