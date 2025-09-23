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
                throw new ArgumentException("Input string is too long to fit in FixedString32.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 30));
            }
        }
    }

    public FixedString32(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 30)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString32.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 30);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString32(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString32(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString32(ReadOnlySpan<byte> input)
    {
        if (input.Length > 30)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString32.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString32(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 64 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString64"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct FixedString64
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
                throw new ArgumentException("Input string is too long to fit in FixedString64.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 62));
            }
        }
    }

    public FixedString64(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 62)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString64.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 62);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString64(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString64(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString64(ReadOnlySpan<byte> input)
    {
        if (input.Length > 62)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString64.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString64(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 128 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString128"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 128)]
public unsafe struct FixedString128
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
                throw new ArgumentException("Input string is too long to fit in FixedString128.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 126));
            }
        }
    }

    public FixedString128(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 126)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString128.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 126);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString128(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString128(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString128(ReadOnlySpan<byte> input)
    {
        if (input.Length > 126)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString128.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString128(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 256 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString256"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 256)]
public unsafe struct FixedString256
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
                throw new ArgumentException("Input string is too long to fit in FixedString256.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 254));
            }
        }
    }

    public FixedString256(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 254)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString256.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 254);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString256(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString256(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString256(ReadOnlySpan<byte> input)
    {
        if (input.Length > 254)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString256.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString256(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 512 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString512"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 512)]
public unsafe struct FixedString512
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
                throw new ArgumentException("Input string is too long to fit in FixedString512.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 510));
            }
        }
    }

    public FixedString512(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 510)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString512.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 510);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString512(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString512(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString512(ReadOnlySpan<byte> input)
    {
        if (input.Length > 510)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString512.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString512(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 1024 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString1024"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 1024)]
public unsafe struct FixedString1024
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
                throw new ArgumentException("Input string is too long to fit in FixedString1024.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 1022));
            }
        }
    }

    public FixedString1024(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 1022)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString1024.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 1022);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString1024(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString1024(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString1024(ReadOnlySpan<byte> input)
    {
        if (input.Length > 1022)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString1024.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString1024(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 2048 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString2048"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 2048)]
public unsafe struct FixedString2048
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
                throw new ArgumentException("Input string is too long to fit in FixedString2048.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 2046));
            }
        }
    }

    public FixedString2048(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 2046)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString2048.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 2046);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString2048(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString2048(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString2048(ReadOnlySpan<byte> input)
    {
        if (input.Length > 2046)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString2048.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString2048(byte* input, ushort length)
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
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 4096 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString4096"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public unsafe struct FixedString4096
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
                throw new ArgumentException("Input string is too long to fit in FixedString4096.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 4094));
            }
        }
    }

    public FixedString4096(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 4094)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString4096.");
        }

        fixed (char* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, bufferPtr, 4094);
            _length = (ushort)actualByteCount;
        }
    }

    public FixedString4096(string input)
        : this(input.AsSpan())
    {
    }

    public FixedString4096(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedString4096(ReadOnlySpan<byte> input)
    {
        if (input.Length > 4094)
        {
            throw new ArgumentException("Input byte array is too long to fit in FixedString4096.");
        }

        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        fixed (byte* bufferPtr = _buffer)
        {
            Unsafe.CopyBlockUnaligned(bufferPtr, inputPtr, _length);
        }
    }

    public FixedString4096(byte* input, ushort length)
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

