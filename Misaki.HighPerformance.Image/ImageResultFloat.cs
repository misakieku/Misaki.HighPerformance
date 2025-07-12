using Misaki.HighPerformance.Image.Runtime;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Image;

public unsafe class ImageResultFloat : IDisposable
{
    private float* _buffer;

    public int Width
    {
        get; init;
    }

    public int Height
    {
        get; init;
    }

    public ColorComponents SourceComponent
    {
        get; init;
    }

    public ColorComponents Component
    {
        get; init;
    }

    public Span<byte> Data => new(_buffer, (int)(Width * Height * (uint)Component));

    internal static unsafe ImageResultFloat FromResult(float* result, int width, int height, ColorComponents comp,
        ColorComponents req_comp)
    {
        if (result == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        var image = new ImageResultFloat
        {
            Width = width,
            Height = height,
            SourceComponent = comp,
            Component = req_comp == ColorComponents.Default ? comp : req_comp
        };

        image._buffer = result;

        return image;
    }

    public static unsafe ImageResultFloat FromStream(Stream stream,
        ColorComponents requiredComponents = ColorComponents.Default)
    {
        int x, y, comp;

        var context = new StbImage.stbi__context(stream);
        var result = StbImage.stbi__loadf_main(context, &x, &y, &comp, (int)requiredComponents);

        return FromResult(result, x, y, (ColorComponents)comp, requiredComponents);
    }

    public static ImageResultFloat FromMemory(byte[] data,
        ColorComponents requiredComponents = ColorComponents.Default)
    {
        using (var stream = new MemoryStream(data))
        {
            return FromStream(stream, requiredComponents);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float* GetUnsafePtr()
    {
        return _buffer;
    }

    public void Dispose()
    {
        if (_buffer == null)
        {
            return;
        }

        CRuntime.free(_buffer);
        _buffer = null;
    }
}