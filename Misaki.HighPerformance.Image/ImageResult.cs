using Misaki.HighPerformance.Image.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Image;

public unsafe class ImageResult : IDisposable
{
    private byte* _buffer;

    public uint Width
    {
        get; init;
    }

    public uint Height
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

    internal void SetData(byte* data)
    {
        CRuntime.free(_buffer);
        _buffer = data;
    }

    internal static unsafe ImageResult FromResult(byte* result, uint width, uint height, ColorComponents comp,
        ColorComponents req_comp)
    {
        if (result == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        var image = new ImageResult
        {
            Width = width,
            Height = height,
            SourceComponent = comp,
            Component = req_comp == ColorComponents.Default ? comp : req_comp
        };

        image._buffer = result;

        return image;
    }

    public static unsafe ImageResult FromStream(Stream stream,
        ColorComponents requiredComponents = ColorComponents.Default)
    {
        int x, y, comp;

        var context = new StbImage.stbi__context(stream);
        var result = StbImage.stbi__load_and_postprocess_8bit(context, &x, &y, &comp, (int)requiredComponents);

        return FromResult(result, (uint)x, (uint)y, (ColorComponents)comp, requiredComponents);
    }

    public static ImageResult FromMemory(byte[] data, ColorComponents requiredComponents = ColorComponents.Default)
    {
        using var stream = new MemoryStream(data);
        return FromStream(stream, requiredComponents);
    }

    public static IEnumerable<AnimatedFrameResult> AnimatedGifFramesFromStream(Stream stream, ColorComponents requiredComponents = ColorComponents.Default)
    {
        return new AnimatedGifEnumerable(stream, requiredComponents);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetUnsafePtr()
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