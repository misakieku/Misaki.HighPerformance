using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 32 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 32 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString32"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public unsafe struct FixedString32
{
    public const int MAX_LENGTH = 15;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString32.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString32(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString32(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString32.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString32(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 64 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 64 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString64"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct FixedString64
{
    public const int MAX_LENGTH = 31;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString64.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString64(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString64(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString64.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString64(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 128 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 128 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString128"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 128)]
public unsafe struct FixedString128
{
    public const int MAX_LENGTH = 63;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString128.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString128(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString128(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString128.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString128(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 256 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 256 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString256"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 256)]
public unsafe struct FixedString256
{
    public const int MAX_LENGTH = 127;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString256.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString256(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString256(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString256.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString256(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 512 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 512 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString512"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 512)]
public unsafe struct FixedString512
{
    public const int MAX_LENGTH = 255;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString512.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString512(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString512(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString512.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString512(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 1024 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 1024 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString1024"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 1024)]
public unsafe struct FixedString1024
{
    public const int MAX_LENGTH = 511;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString1024.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString1024(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString1024(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString1024.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString1024(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 2048 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 2048 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString2048"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 2048)]
public unsafe struct FixedString2048
{
    public const int MAX_LENGTH = 1023;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString2048.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString2048(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString2048(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString2048.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString2048(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 4096 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 4096 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString4096"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public unsafe struct FixedString4096
{
    public const int MAX_LENGTH = 2047;

    private ushort _length;
    private fixed char _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (char* bufferPtr = _buffer)
            {
                return new string(bufferPtr, 0, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedString4096.");
            }

            _length = (ushort)value.Length;
            fixed (char* bufferPtr = _buffer)
            fixed (char* valuePtr = value)
            {
                Unsafe.CopyBlockUnaligned(bufferPtr, valuePtr, (uint)(_length * sizeof(char)));
            }
        }
    }

    public FixedString4096(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString4096(ReadOnlySpan<char> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString4096.");
        }

        _length = (ushort)input.Length;

        fixed (char* inputPtr = input)
        fixed (char* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, (uint)(_length * sizeof(char)));
        }
    }

    public FixedString4096(char* input, ushort length)
        : this(new ReadOnlySpan<char>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (char* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* GetUnsafePtr()
    {
        return (char*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

