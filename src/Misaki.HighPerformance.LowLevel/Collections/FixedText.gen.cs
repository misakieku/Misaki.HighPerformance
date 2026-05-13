using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 32 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public unsafe struct FixedText32
{
    public const int MAX_LENGTH = 30;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText32.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText32(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText32.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText32(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText32(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText32(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText32.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText32(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct FixedText64
{
    public const int MAX_LENGTH = 62;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText64.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText64(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText64.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText64(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText64(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText64(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText64.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText64(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 128)]
public unsafe struct FixedText128
{
    public const int MAX_LENGTH = 126;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText128.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText128(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText128.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText128(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText128(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText128(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText128.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText128(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 256)]
public unsafe struct FixedText256
{
    public const int MAX_LENGTH = 254;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText256.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText256(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText256.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText256(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText256(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText256(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText256.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText256(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 512)]
public unsafe struct FixedText512
{
    public const int MAX_LENGTH = 510;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText512.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText512(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText512.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText512(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText512(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText512(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText512.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText512(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 1024)]
public unsafe struct FixedText1024
{
    public const int MAX_LENGTH = 1022;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText1024.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText1024(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText1024.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText1024(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText1024(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText1024(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText1024.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText1024(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 2048)]
public unsafe struct FixedText2048
{
    public const int MAX_LENGTH = 2046;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText2048.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText2048(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText2048.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText2048(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText2048(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText2048(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText2048.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText2048(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
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
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public unsafe struct FixedText4096
{
    public const int MAX_LENGTH = 4094;

    private ushort _length;
    private fixed byte _buffer[MAX_LENGTH];

    public readonly ushort Length => _length;
    public string Value
    {
        get
        {
            fixed (byte* bufferPtr = _buffer)
            {
                return Encoding.UTF8.GetString(bufferPtr, _length);
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _length = 0;
                return;
            }

            var maxBytes = Encoding.UTF8.GetByteCount(value);
            if (maxBytes > MAX_LENGTH)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText4096.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, MAX_LENGTH));
            }
        }
    }

    public FixedText4096(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > MAX_LENGTH)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText4096.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, MAX_LENGTH);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedText4096(string input)
        : this(input.AsSpan())
    {
    }

    public FixedText4096(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedText4096(ReadOnlySpan<byte> input)
    {
        if (input.Length > MAX_LENGTH)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedText4096.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedText4096(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        fixed (byte* ptr = _buffer)
        {
            return new(ptr, _length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)((ushort*)Unsafe.AsPointer(in this) + 1);
    }

    public override string ToString()
    {
        return Value;
    }
}

