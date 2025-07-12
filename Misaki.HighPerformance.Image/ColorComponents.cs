namespace Misaki.HighPerformance.Image
{
#if !STBSHARP_INTERNAL
    public
#else
	internal
#endif
    enum ColorComponents
    {
        Default,
        R,
        RA,
        RGB,
        RGBA
    }
}