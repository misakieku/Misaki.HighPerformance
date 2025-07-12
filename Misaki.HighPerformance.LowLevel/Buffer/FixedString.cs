using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 32 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public unsafe struct FixedString32 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 30));
        }
    }

    public FixedString32(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 30)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString32.");
        }

        _buffer = (byte*)NativeMemory.Alloc(30);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 30);
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

        _buffer = (byte*)NativeMemory.Alloc(30);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString32(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 64 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct FixedString64 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 62));
        }
    }

    public FixedString64(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 62)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString64.");
        }

        _buffer = (byte*)NativeMemory.Alloc(62);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 62);
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

        _buffer = (byte*)NativeMemory.Alloc(62);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString64(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 128 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 128)]
public unsafe struct FixedString128 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 126));
        }
    }

    public FixedString128(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 126)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString128.");
        }

        _buffer = (byte*)NativeMemory.Alloc(126);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 126);
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

        _buffer = (byte*)NativeMemory.Alloc(126);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString128(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 256 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 256)]
public unsafe struct FixedString256 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 254));
        }
    }

    public FixedString256(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 254)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString256.");
        }

        _buffer = (byte*)NativeMemory.Alloc(254);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 254);
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

        _buffer = (byte*)NativeMemory.Alloc(254);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString256(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 512 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 512)]
public unsafe struct FixedString512 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 510));
        }
    }

    public FixedString512(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 510)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString512.");
        }

        _buffer = (byte*)NativeMemory.Alloc(510);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 510);
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

        _buffer = (byte*)NativeMemory.Alloc(510);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString512(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 1024 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1024)]
public unsafe struct FixedString1024 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 1022));
        }
    }

    public FixedString1024(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 1022)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString1024.");
        }

        _buffer = (byte*)NativeMemory.Alloc(1022);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 1022);
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

        _buffer = (byte*)NativeMemory.Alloc(1022);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString1024(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 2048 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 2048)]
public unsafe struct FixedString2048 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 2046));
        }
    }

    public FixedString2048(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 2046)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString2048.");
        }

        _buffer = (byte*)NativeMemory.Alloc(2046);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 2046);
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

        _buffer = (byte*)NativeMemory.Alloc(2046);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString2048(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

/// <summary>
/// Represents a heap allocated fixed-size UTF-8 encoded string of length 4096 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public unsafe struct FixedString4096 : IDisposable
{
    private ushort _length;
    private byte* _buffer;

    public ushort Length => _length;
    public string Value
    {
        readonly get
        {
            return Encoding.UTF8.GetString(_buffer, _length);
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

            _length = (ushort)Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer, 4094));
        }
    }

    public FixedString4096(ReadOnlySpan<char> input)
    {
        var maxBytes = Encoding.UTF8.GetByteCount(input);
        if (maxBytes > 4094)
        {
            throw new ArgumentException("Input string is too long to fit in FixedString4096.");
        }

        _buffer = (byte*)NativeMemory.Alloc(4094);

        fixed (char* inputPtr = input)
        {
            var actualByteCount = Encoding.UTF8.GetBytes(inputPtr, input.Length, _buffer, 4094);
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

        _buffer = (byte*)NativeMemory.Alloc(4094);
        _length = (ushort)input.Length;

        fixed (byte* inputPtr = input)
        {
            SystemUnsfae.CopyBlockUnaligned(_buffer, inputPtr, _length);
        }
    }

    public FixedString4096(byte* input, ushort length)
        : this(new ReadOnlySpan<byte>(input, length))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return new(_buffer, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePointer()
    {
        return _buffer;
    }
    
    public override string ToString()
    {
        return Value;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);

            _length = 0;
            _buffer = null;
        }
    }
}

