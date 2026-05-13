using System.Runtime.InteropServices;

namespace Misaki.HighPerformance;

/// <summary>
/// A synchronization primitive optimized for many readers and rare writers.
/// Readers never block writers and never block each other.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct SeqLock
{
    [FieldOffset(0)]
    private ulong _sequence;

    public void EnterWrite()
    {
        var spinner = new SpinWait();
        while (true)
        {
            var seq = Volatile.Read(ref _sequence);
            if ((seq & 1) == 0 && Interlocked.CompareExchange(ref _sequence, seq + 1, seq) == seq)
            {
                break;
            }

            spinner.SpinOnce();
        }
    }

    public void ExitWrite()
    {
        Volatile.Write(ref _sequence, _sequence + 1);
    }

    public ulong BeginRead()
    {
        var spinner = new SpinWait();
        while (true)
        {
            var seq = Volatile.Read(ref _sequence);

            // If sequence is even, no writer is currently mutating data
            if ((seq & 1) == 0)
            {
                // Ensure the sequence read doesn't get reordered AFTER the upcoming data reads
                Thread.MemoryBarrier();
                return seq;
            }

            spinner.SpinOnce();
        }
    }

    public bool EndRead(ulong seq)
    {
        Thread.MemoryBarrier();
        return Volatile.Read(ref _sequence) == seq;
    }
}
