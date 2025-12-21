using System.IO;

namespace Misaki.HighPerformance.Image;

public readonly struct ImageInfo
{
    public int Width
    {
        get; init;
    }

    public int Height
    {
        get; init;
    }

    public ColorComponents ColorComponents
    {
        get; init;
    }

    public int BitsPerChannel
    {
        get; init;
    }

    public static unsafe ImageInfo FromStream(Stream stream)
    {
        int width, height, comp;
        var context = new StbImage.stbi__context(stream);

        var is16Bit = StbImage.stbi__is_16_main(context) == 1;
        StbImage.stbi__rewind(context);

        var infoResult = StbImage.stbi__info_main(context, &width, &height, &comp);
        StbImage.stbi__rewind(context);

        if (infoResult == 0)
        {
            return default;
        }

        return new ImageInfo
        {
            Width = width,
            Height = height,
            ColorComponents = (ColorComponents)comp,
            BitsPerChannel = is16Bit ? 16 : 8
        };
    }
}
