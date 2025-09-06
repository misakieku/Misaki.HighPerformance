using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Represents a stack allocated fixed-size UTF-8 encoded string of length 32 bytes.
/// </summary>
/// <remarks>
/// This struct is designed to hold data on the stack. Every copy of this struct causes a copy of the underlying data.
/// If you need a heap allocated fixed-size UTF-8 encoded string of length 32 bytes, consider using <see cref="Misaki.HighPerformance.Unsafe.Buffer.FixedString32"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public unsafe struct FixedStackString32
{
    private ushort _length;
    private fixed byte _buffer[30];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString32.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 30));
            }
        }
    }

    public FixedStackString32(ReadOnlySpan<char> input)
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

    public FixedStackString32(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString32(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString32(ReadOnlySpan<byte> input)
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

    public FixedStackString32(byte* input, ushort length)
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
public unsafe struct FixedStackString64
{
    private ushort _length;
    private fixed byte _buffer[62];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString64.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 62));
            }
        }
    }

    public FixedStackString64(ReadOnlySpan<char> input)
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

    public FixedStackString64(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString64(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString64(ReadOnlySpan<byte> input)
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

    public FixedStackString64(byte* input, ushort length)
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
public unsafe struct FixedStackString128
{
    private ushort _length;
    private fixed byte _buffer[126];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString128.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 126));
            }
        }
    }

    public FixedStackString128(ReadOnlySpan<char> input)
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

    public FixedStackString128(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString128(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString128(ReadOnlySpan<byte> input)
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

    public FixedStackString128(byte* input, ushort length)
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
public unsafe struct FixedStackString256
{
    private ushort _length;
    private fixed byte _buffer[254];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString256.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 254));
            }
        }
    }

    public FixedStackString256(ReadOnlySpan<char> input)
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

    public FixedStackString256(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString256(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString256(ReadOnlySpan<byte> input)
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

    public FixedStackString256(byte* input, ushort length)
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
public unsafe struct FixedStackString512
{
    private ushort _length;
    private fixed byte _buffer[510];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString512.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 510));
            }
        }
    }

    public FixedStackString512(ReadOnlySpan<char> input)
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

    public FixedStackString512(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString512(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString512(ReadOnlySpan<byte> input)
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

    public FixedStackString512(byte* input, ushort length)
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
public unsafe struct FixedStackString1024
{
    private ushort _length;
    private fixed byte _buffer[1022];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString1024.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 1022));
            }
        }
    }

    public FixedStackString1024(ReadOnlySpan<char> input)
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

    public FixedStackString1024(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString1024(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString1024(ReadOnlySpan<byte> input)
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

    public FixedStackString1024(byte* input, ushort length)
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
public unsafe struct FixedStackString2048
{
    private ushort _length;
    private fixed byte _buffer[2046];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString2048.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 2046));
            }
        }
    }

    public FixedStackString2048(ReadOnlySpan<char> input)
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

    public FixedStackString2048(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString2048(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString2048(ReadOnlySpan<byte> input)
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

    public FixedStackString2048(byte* input, ushort length)
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
public unsafe struct FixedStackString4096
{
    private ushort _length;
    private fixed byte _buffer[4094];

    public ushort Length => _length;
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
                throw new ArgumentException("Input string is too long to fit in FixedStackString4096.");
            }

            fixed (byte* bufferPtr = _buffer)
            {
                _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(bufferPtr, 4094));
            }
        }
    }

    public FixedStackString4096(ReadOnlySpan<char> input)
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

    public FixedStackString4096(string input)
        : this(input.AsSpan())
    {
    }

    public FixedStackString4096(char* input, ushort length)
        : this(new Span<char>(input, length))
    {
    }

    public FixedStackString4096(ReadOnlySpan<byte> input)
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

    public FixedStackString4096(byte* input, ushort length)
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

