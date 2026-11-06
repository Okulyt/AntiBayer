using System;

namespace AntiBayer.Core;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

public static class PreviewGeometry
{
    /// <summary>
    /// Rectangle of the wanted size centred on the given point, limited to the image
    /// and starting on even coordinates so the Bayer phase is preserved.
    /// </summary>
    public static PixelRect Compute(int centerX, int centerY, int wantedWidth, int wantedHeight,
        int imageWidth, int imageHeight)
    {
        int w = Math.Min(wantedWidth, imageWidth);
        int h = Math.Min(wantedHeight, imageHeight);
        int x = Math.Clamp(centerX - w / 2, 0, imageWidth - w) & ~1;
        int y = Math.Clamp(centerY - h / 2, 0, imageHeight - h) & ~1;
        return new PixelRect(x, y, w, h);
    }
}
