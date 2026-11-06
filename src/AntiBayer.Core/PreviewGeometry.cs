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

    /// <summary>
    /// Image pixel under a click into a box that shows the image zoomed, centred and with kept proportions
    /// (PictureBoxSizeMode.Zoom); null when the click lies beside the drawn image.
    /// </summary>
    public static (int X, int Y)? ZoomedClickToPixel(int clickX, int clickY, int boxWidth, int boxHeight,
        int imageWidth, int imageHeight)
    {
        if (boxWidth <= 0 || boxHeight <= 0 || imageWidth <= 0 || imageHeight <= 0) return null;
        double scale = Math.Min((double)boxWidth / imageWidth, (double)boxHeight / imageHeight);
        int drawnWidth = (int)(imageWidth * scale), drawnHeight = (int)(imageHeight * scale);
        int left = (boxWidth - drawnWidth) / 2, top = (boxHeight - drawnHeight) / 2;
        int dx = clickX - left, dy = clickY - top;
        if (dx < 0 || dy < 0 || dx >= drawnWidth || dy >= drawnHeight) return null;
        return (Math.Min((int)(dx / scale), imageWidth - 1), Math.Min((int)(dy / scale), imageHeight - 1));
    }
}
