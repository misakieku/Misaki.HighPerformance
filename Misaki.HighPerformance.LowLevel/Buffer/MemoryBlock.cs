using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Represents an allocated memory block with metadata.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct MemoryBlock
{
    /// <summary>
    /// Pointer to the actual allocated memory.
    /// </summary>
    public void* Ptr
    {
        get;
    }

    /// <summary>
    /// Size of the allocated memory in bytes.
    /// </summary>
    public nuint Size
    {
        get;
    }

    /// <summary>
    /// Alignment of the allocated memory.
    /// </summary>
    public nuint Alignment
    {
        get;
    }

    /// <summary>
    /// Indicates whether this memory block is valid.
    /// </summary>
    public readonly bool IsValid => Ptr != null && Size > 0;

    /// <summary>
    /// Creates a new MemoryBlock with the specified parameters.
    /// </summary>
    /// <param name="ptr">Pointer to the allocated memory.</param>
    /// <param name="size">Size of the allocated memory.</param>
    /// <param name="alignment">Alignment of the allocated memory.</param>
    public MemoryBlock(void* ptr, nuint size, nuint alignment)
    {
        Ptr = ptr;
        Size = size;
        Alignment = alignment;
    }

    /// <summary>
    /// Creates an invalid MemoryBlock.
    /// </summary>
    public static MemoryBlock Invalid => new(null, 0, 0);

    public Span<T> AsSpan<T>()
        where T : unmanaged
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot create span from invalid MemoryBlock.");
        }

        return new Span<T>(Ptr, (int)(Size / SizeOf<T>()));
    }
}