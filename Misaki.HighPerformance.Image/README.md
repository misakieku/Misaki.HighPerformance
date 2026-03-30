# Misaki.HighPerformance.Image

STB-based image loading and translation helpers for C#.

This package focuses on practical image decoding with low-level control over memory ownership and result handling.

## What it includes

- image loading from streams and memory
- animated GIF frame enumeration
- image metadata and result wrappers
- runtime helpers and native memory statistics
- translation layers for STB image formats

## Highlights

- allocates image data in unmanaged memory
- exposes width, height, and component information alongside the pixel buffer
- useful when you need simple decoding without a heavy graphics dependency
- supports animated frame workflows in addition to single image loads

## Main types

- `StbImage`
- `ImageResult`
- `ImageInfo`
- `AnimatedGifEnumerator`
- `AnimatedFrameResult`
- `ImageResultFloat`
- `ColorComponents`

## Example

```csharp
using Misaki.HighPerformance.Image;

using var stream = File.OpenRead("image.png");
using ImageResult image = ImageResult.FromStream(stream);

Span<byte> pixels = image.AsSpan();
```

## Package reference

```bash
dotnet add package Misaki.HighPerformance.Image
```

## Notes

This project targets `net10.0` and uses unsafe code for image memory handling.
