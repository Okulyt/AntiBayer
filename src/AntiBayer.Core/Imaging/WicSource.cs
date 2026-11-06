using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AntiBayer.Core.Imaging;

/// <summary>
/// Single-channel TIFF/PNG via WIC. The pixels are copied at construction, so the source does not
/// hold the file and does not depend on the thread that created it.
/// </summary>
public sealed class WicSource : IFrameSource
{
    private readonly ushort[] _pixels;

    public ImageInfo Info { get; }

    internal PixelFormat PixelFormat { get; }
    internal BitmapPalette? Palette { get; }

    public WicSource(string path)
    {
        BitmapSource frame;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count > 1)
                throw new UnsupportedImageException("Multi-page TIFF files are not supported.");
            frame = decoder.Frames[0];
            frame.Freeze();
        }
        catch (Exception e) when (e is NotSupportedException or FileFormatException or InvalidOperationException
                                      or ArgumentException or COMException or OverflowException)
        {
            throw new UnsupportedImageException("The file could not be read as a TIFF or PNG image.");
        }

        int w = frame.PixelWidth, h = frame.PixelHeight;
        PixelFormat = frame.Format;
        Palette = frame.Palette;
        _pixels = new ushort[w * h];
        int bitDepth = Extract(frame, _pixels);

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var format = ext == ".png" ? ImageFormat.Png : ImageFormat.Tiff;
        Info = new ImageInfo(path, format, w, h, bitDepth, 1,
            WhiteLevel.Detect(_pixels, bitDepth, bitDepth), null, CaptureInfoReader.ForFile(path, null), null);
    }

    private static UnsupportedImageException Colour() =>
        new("Colour images are not supported. AntiBayer needs single-channel raw data (or RGB with identical channels).");

    private static int Extract(BitmapSource frame, ushort[] target)
    {
        int w = frame.PixelWidth, h = frame.PixelHeight;
        var f = frame.Format;
        int bpp = f.BitsPerPixel;
        if (bpp % 8 != 0)
            throw new UnsupportedImageException("This pixel format is not supported.");
        int stride = (w * bpp + 7) / 8;
        var raw = new byte[stride * h];
        frame.CopyPixels(raw, stride, 0);

        if (f == PixelFormats.Gray8)
        {
            for (int i = 0; i < target.Length; i++) target[i] = raw[i];
            return 8;
        }
        if (f == PixelFormats.Gray16)
        {
            for (int i = 0; i < target.Length; i++) target[i] = (ushort)(raw[2 * i] | (raw[2 * i + 1] << 8));
            return 16;
        }
        if (f == PixelFormats.Indexed8)
        {
            var pal = frame.Palette;
            if (pal == null) throw Colour();
            var grey = new byte[256];
            for (int i = 0; i < pal.Colors.Count; i++)
            {
                var c = pal.Colors[i];
                if (c.R != c.G || c.G != c.B) throw Colour();
                grey[i] = c.R;
            }
            for (int i = 0; i < target.Length; i++) target[i] = grey[raw[i]];
            return 8;
        }
        if (f == PixelFormats.Bgr24 || f == PixelFormats.Bgr32 || f == PixelFormats.Bgra32)
        {
            int step = bpp / 8;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int o = y * stride + x * step;
                    if (raw[o] != raw[o + 1] || raw[o + 1] != raw[o + 2]) throw Colour();
                    target[y * w + x] = raw[o];
                }
            return 8;
        }
        if (f == PixelFormats.Rgb48 || f == PixelFormats.Rgba64)
        {
            int step = bpp / 8;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int o = y * stride + x * step;
                    int r = raw[o] | (raw[o + 1] << 8), g = raw[o + 2] | (raw[o + 3] << 8), b = raw[o + 4] | (raw[o + 5] << 8);
                    if (r != g || g != b) throw Colour();
                    target[y * w + x] = (ushort)r;
                }
            return 16;
        }
        throw new UnsupportedImageException("This pixel format is not supported.");
    }

    public void ReadFrame(int index, ushort[] target)
    {
        if (target.Length < _pixels.Length) throw new ArgumentException("Target is too small for a frame.", nameof(target));
        if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
        Array.Copy(_pixels, target, _pixels.Length);
    }

    public void Dispose() { }
}
