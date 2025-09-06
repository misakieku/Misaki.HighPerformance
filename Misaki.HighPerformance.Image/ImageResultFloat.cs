using Misaki.HighPerformance.Image.Runtime;
using System;
using System.IO;

namespace Misaki.HighPerformance.Image;

public unsafe class ImageResultFloat : IDisposable
{
    public float* Data
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

    internal static unsafe ImageResultFloat FromResult(float* result, uint width, uint height, ColorComponents comp,
        ColorComponents req_comp)
    {
        if (result == null)
        {
            throw new InvalidOperationException(StbImage.stbi__g_failure_reason);
        }

        var image = new ImageResultFloat
        {
            Data = result,
            Width = width,
            Height = height,
            SourceComp = comp,
            Comp = req_comp == ColorComponents.Default ? comp : req_comp
        };

        return image;
    }

    public static unsafe ImageResultFloat FromStream(Stream stream,
        ColorComponents requiredComponents = ColorComponents.Default)
    {
        int x, y, comp;

        var context = new StbImage.stbi__context(stream);

        var result = StbImage.stbi__loadf_main(context, &x, &y, &comp, (int)requiredComponents);

        return FromResult(result, (uint)x, (uint)y, (ColorComponents)comp, requiredComponents);
    }

    public static ImageResultFloat FromMemory(byte[] data,
        ColorComponents requiredComponents = ColorComponents.Default)
    {
        using (var stream = new MemoryStream(data))
        {
            return FromStream(stream, requiredComponents);
        }
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
