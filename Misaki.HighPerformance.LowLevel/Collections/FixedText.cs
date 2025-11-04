using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Collections;

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 32 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 32 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText32"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public unsafe struct FixedText32
{
    private ushort _length;
    private fixed byte _buffer[30];

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
            if (maxBytes > 30)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText32.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 30));
            }
        }
    }

    public FixedText32(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 30)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText32.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 30);
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
        if (input.Length > 30)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 64 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText64"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct FixedText64
{
    private ushort _length;
    private fixed byte _buffer[62];

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
            if (maxBytes > 62)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText64.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 62));
            }
        }
    }

    public FixedText64(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 62)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText64.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 62);
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
        if (input.Length > 62)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 128 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText128"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 128)]
public unsafe struct FixedText128
{
    private ushort _length;
    private fixed byte _buffer[126];

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
            if (maxBytes > 126)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText128.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 126));
            }
        }
    }

    public FixedText128(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 126)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText128.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 126);
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
        if (input.Length > 126)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 256 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText256"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 256)]
public unsafe struct FixedText256
{
    private ushort _length;
    private fixed byte _buffer[254];

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
            if (maxBytes > 254)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText256.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 254));
            }
        }
    }

    public FixedText256(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 254)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText256.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 254);
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
        if (input.Length > 254)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 512 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText512"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 512)]
public unsafe struct FixedText512
{
    private ushort _length;
    private fixed byte _buffer[510];

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
            if (maxBytes > 510)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText512.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 510));
            }
        }
    }

    public FixedText512(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 510)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText512.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 510);
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
        if (input.Length > 510)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 1024 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText1024"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 1024)]
public unsafe struct FixedText1024
{
    private ushort _length;
    private fixed byte _buffer[1022];

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
            if (maxBytes > 1022)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText1024.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 1022));
            }
        }
    }

    public FixedText1024(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 1022)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText1024.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 1022);
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
        if (input.Length > 1022)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 2048 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText2048"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 2048)]
public unsafe struct FixedText2048
{
    private ushort _length;
    private fixed byte _buffer[2046];

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
            if (maxBytes > 2046)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText2048.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 2046));
            }
        }
    }

    public FixedText2048(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 2046)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText2048.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 2046);
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
        if (input.Length > 2046)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 4096 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedText4096"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public unsafe struct FixedText4096
{
    private ushort _length;
    private fixed byte _buffer[4094];

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
            if (maxBytes > 4094)
            {
                throw new ArgumentException("Input string is too long to fit in FixedText4096.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 4094));
            }
        }
    }

    public FixedText4096(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 4094)
        {
            throw new ArgumentException("Input string is too long to fit in FixedText4096.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 4094);
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
        if (input.Length > 4094)
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
    public byte* GetUnsafePointer()
    {
        fixed (byte* ptr = _buffer)
        {
            return ptr;
        }
    }

    public override string ToString()
    {
        return Value;
    }
}

