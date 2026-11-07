using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AntiBayer.Core;

namespace AntiBayer;

/// <summary>Turns 8-bit display data into bitmaps. Bitmaps serve the display only, never as a pixel data source.</summary>
public static class FrameBitmap
{
    /// <summary>Grey 24 bpp bitmap. Every call creates a new bitmap; the caller disposes the old one.</summary>
    public static Bitmap ToBitmap(byte[] pixels, int width, int height) =>
        Create(width, height, (y, row) =>
        {
            int source = y * width;
            for (int x = 0; x < width; x++)
            {
                byte v = pixels[source + x];
                row[3 * x] = v;
                row[3 * x + 1] = v;
                row[3 * x + 2] = v;
            }
        });

    /// <summary>Colour bitmap from RGB bytes per pixel.</summary>
    public static Bitmap ToColorBitmap(byte[] rgb, int width, int height) =>
        Create(width, height, (y, row) =>
        {
            int source = 3 * y * width;
            for (int x = 0; x < width; x++)
            {
                row[3 * x] = rgb[source + 3 * x + 2];
                row[3 * x + 1] = rgb[source + 3 * x + 1];
                row[3 * x + 2] = rgb[source + 3 * x];
            }
        });

    /// <summary>
    /// Like <see cref="ToBitmap"/>, but tints the pixels of one filter position in its colour. x and y are
    /// counted from the crop origin, which lies on even image coordinates and so keeps the Bayer phase.
    /// </summary>
    public static Bitmap ToMarkedBitmap(byte[] pixels, int width, int height, BayerPattern pattern, BayerFilter mark) =>
        Create(width, height, (y, row) =>
        {
            int source = y * width;
            for (int x = 0; x < width; x++)
            {
                byte v = pixels[source + x];
                byte half = (byte)(v / 2), raised = (byte)Math.Min(255, v + 10);
                bool marked = Core.Bayer.FilterAt(pattern, x, y) == mark;
                row[3 * x] = !marked ? v : mark == BayerFilter.Blue ? raised : half;
                row[3 * x + 1] = !marked ? v : mark is BayerFilter.Green1 or BayerFilter.Green2 ? raised : half;
                row[3 * x + 2] = !marked ? v : mark is BayerFilter.Red or BayerFilter.All ? raised : half;
            }
        });

    private static Bitmap Create(int width, int height, Action<int, byte[]> fillBgrRow)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            var row = new byte[3 * width];
            for (int y = 0; y < height; y++)
            {
                fillBgrRow(y, row);
                Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * data.Stride), row.Length);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }
}

/// <summary>PictureBox with a settable interpolation mode; whole pixels stay aligned to the zoom grid.</summary>
public class PictureBoxWithInterpolationMode : PictureBox
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public InterpolationMode InterpolationMode { get; set; }

    protected override void OnPaint(PaintEventArgs paintEventArgs)
    {
        paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
        paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        base.OnPaint(paintEventArgs);
    }
}
