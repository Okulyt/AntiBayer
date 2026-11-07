using System;

namespace AntiBayer.Core;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

public static class PreviewGeometry
{
    /// <summary>Side of the magnifier in real pixels: 3 x 3 Bayer cells of 2 x 2 pixels.</summary>
    public const int MagnifierSize = 6;

    /// <summary>Free border of the magnifier box, where the ticks are drawn.</summary>
    public const int MagnifierMargin = 7;

    /// <summary>
    /// The magnifier crop: the 2 x 2 cell of the pixel in the middle, one cell around it, shifted inward at
    /// the image edge. It always contains the pixel, even in an image of odd size.
    /// </summary>
    public static PixelRect MagnifierRect(int pixelX, int pixelY, int imageWidth, int imageHeight)
    {
        int w = Math.Min(MagnifierSize, imageWidth);
        int h = Math.Min(MagnifierSize, imageHeight);
        int x = Math.Clamp((pixelX & ~1) - 2, 0, imageWidth - w);
        int y = Math.Clamp((pixelY & ~1) - 2, 0, imageHeight - h);
        return new PixelRect(x, y, w, h);
    }

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

    /// <summary>Where the magnifier draws its crop in its box: whole-numbered pixel size, centred, with room for the ticks. Null when the box is too small.</summary>
    public static (int Left, int Top, int PixelSize)? MagnifierLayout(int boxWidth, int boxHeight, int regionWidth, int regionHeight)
    {
        if (regionWidth <= 0 || regionHeight <= 0) return null;
        int size = (Math.Min(boxWidth, boxHeight) - 2 * MagnifierMargin) / Math.Max(regionWidth, regionHeight);
        if (size <= 0) return null;
        return ((boxWidth - size * regionWidth) / 2, (boxHeight - size * regionHeight) / 2, size);
    }

    /// <summary>Image pixel under a click into the magnifier; null beside the drawn crop.</summary>
    public static (int X, int Y)? MagnifierClickToPixel(int clickX, int clickY, int boxWidth, int boxHeight, PixelRect region)
    {
        if (MagnifierLayout(boxWidth, boxHeight, region.Width, region.Height) is not (int left, int top, int size)) return null;
        int dx = clickX - left, dy = clickY - top;
        if (dx < 0 || dy < 0 || dx >= size * region.Width || dy >= size * region.Height) return null;
        return (region.X + dx / size, region.Y + dy / size);
    }

    /// <summary>Rectangle of one image pixel inside a zoomed box; inverse of ZoomedClickToPixel. Null when the pixel is not drawn.</summary>
    public static PixelRect? PixelToZoomedRect(int pixelX, int pixelY, int boxWidth, int boxHeight,
        int imageWidth, int imageHeight)
    {
        if (boxWidth <= 0 || boxHeight <= 0 || imageWidth <= 0 || imageHeight <= 0) return null;
        if (pixelX < 0 || pixelY < 0 || pixelX >= imageWidth || pixelY >= imageHeight) return null;
        double scale = Math.Min((double)boxWidth / imageWidth, (double)boxHeight / imageHeight);
        int drawnWidth = (int)(imageWidth * scale), drawnHeight = (int)(imageHeight * scale);
        int left = (boxWidth - drawnWidth) / 2, top = (boxHeight - drawnHeight) / 2;
        int x0 = left + (int)Math.Ceiling(pixelX * scale), x1 = left + (int)Math.Ceiling((pixelX + 1) * scale);
        int y0 = top + (int)Math.Ceiling(pixelY * scale), y1 = top + (int)Math.Ceiling((pixelY + 1) * scale);
        x0 = Math.Min(x0, left + drawnWidth - 1); y0 = Math.Min(y0, top + drawnHeight - 1);
        x1 = Math.Min(x1, left + drawnWidth); y1 = Math.Min(y1, top + drawnHeight);
        return new PixelRect(x0, y0, Math.Max(1, x1 - x0), Math.Max(1, y1 - y0));
    }
}
