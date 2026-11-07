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
