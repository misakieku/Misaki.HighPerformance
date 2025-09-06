using Misaki.HighPerformance.Image.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace Misaki.HighPerformance.Image;

public unsafe class ImageResult : IDisposable
{
    public byte* Data
    {
        get; init;
    }

    public uint Width
    {
        get; init;
    }

    public uint Height
    {
        get; init;
    }

    public ColorComponents SourceComp
    {
        get; init;
    }

    public ColorComponents Comp
    {
        get; init;
    }

    public ulong Size
    {
        get
        {
            if (Data == null)
            {
                return 0;
            }

            return (ulong)(Width * Height * (int)Comp);
        }
    }

    internal static unsafe ImageResult FromResult(byte* result, uint width, uint height, ColorComponents comp,
        ColorComponents req_comp)
    {
        if (result == null)
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

        var image = new ImageResult
        {
            Data = result,
            Width = width,
            Height = height,
            SourceComp = comp,
            Comp = req_comp == ColorComponents.Default ? comp : req_comp
        };

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

    public static IEnumerable<AnimatedFrameResult> AnimatedGifFramesFromStream(Stream stream,
        ColorComponents requiredComponents = ColorComponents.Default)
    {
        return new AnimatedGifEnumerable(stream, requiredComponents);
    }

    public Span<byte> AsSpan()
    {
        if (Data == null)
        {
            return Span<byte>.Empty;
        }

        return new Span<byte>(Data, (int)Size);
    }

    public void Dispose()
    {
        CRuntime.free(Data);
    }
}
