namespace Misaki.HighPerformance.Image;

public readonly struct AnimatedFrameResult
{
    public required ImageResult Image
    {
        get; init;
    }

    public int DelayInMs
    {
        get; init;
    }
}