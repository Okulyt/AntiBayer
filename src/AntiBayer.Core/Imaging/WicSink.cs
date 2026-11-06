using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AntiBayer.Core.Imaging;

/// <summary>Writes one frame as lossless PNG or TIFF in the pixel format of the source.</summary>
public sealed class WicSink : IFrameSink
{
    private readonly WicSource _source;
    private readonly FileStream _out;
    private readonly BitmapEncoder _encoder;
    private bool _written, _completed;

    public WicSink(WicSource source, string targetPath)
    {
        _source = source;
        if (string.Equals(Path.GetFullPath(targetPath), Path.GetFullPath(source.Info.Path), StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedImageException("The output must not overwrite the source file.");
        _encoder = Path.GetExtension(targetPath).ToLowerInvariant() switch
        {
            ".png" => new PngBitmapEncoder(),
            ".tif" or ".tiff" => new TiffBitmapEncoder { Compression = TiffCompressOption.None },
            _ => throw new UnsupportedImageException("The output file must be .tif, .tiff or .png."),
        };
        _out = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void WriteFrame(ReadOnlySpan<ushort> frame)
    {
        int w = _source.Info.Width, h = _source.Info.Height, n = w * h;
        if (frame.Length < n) throw new ArgumentException("Frame is too small.", nameof(frame));
        if (_written) throw new InvalidOperationException("Too many frames written.");

        var f = _source.PixelFormat;
        int bpp = f.BitsPerPixel, step = bpp / 8, stride = (w * bpp + 7) / 8;
        var raw = new byte[stride * h];
        BitmapPalette? palette = null;

        if (f == PixelFormats.Gray16)
        {
            for (int i = 0; i < n; i++) { raw[2 * i] = (byte)frame[i]; raw[2 * i + 1] = (byte)(frame[i] >> 8); }
        }
        else if (f == PixelFormats.Rgb48 || f == PixelFormats.Rgba64)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ushort v = frame[y * w + x];
                    int o = y * stride + x * step;
                    for (int c = 0; c < 3; c++) { raw[o + 2 * c] = (byte)v; raw[o + 2 * c + 1] = (byte)(v >> 8); }
                    if (step == 8) { raw[o + 6] = 0xFF; raw[o + 7] = 0xFF; }
                }
        }
        else if (f == PixelFormats.Gray8 || f == PixelFormats.Indexed8)
        {
            for (int i = 0; i < n; i++) raw[i] = (byte)frame[i];
            if (f == PixelFormats.Indexed8)
            {
                // A non-identity grey palette cannot hold arbitrary corrected values; write plain grey then.
                if (IsIdentityPalette(_source.Palette)) palette = _source.Palette;
                else f = PixelFormats.Gray8;
            }
        }
        else
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte v = (byte)frame[y * w + x];
                    int o = y * stride + x * step;
                    raw[o] = raw[o + 1] = raw[o + 2] = v;
                    if (step == 4) raw[o + 3] = 0xFF;
                }
        }

        var bmp = BitmapSource.Create(w, h, 96, 96, f, palette, raw, stride);
        _encoder.Frames.Add(BitmapFrame.Create(bmp));
        _written = true;
    }

    private static bool IsIdentityPalette(BitmapPalette? p)
    {
        if (p == null || p.Colors.Count != 256) return false;
        for (int i = 0; i < 256; i++) if (p.Colors[i].R != i) return false;
        return true;
    }

    public void Complete()
    {
        if (!_written) throw new InvalidOperationException("Expected 1 frame but 0 were written.");
        if (_completed) return;
        _encoder.Save(_out);
        _out.Flush();
        _completed = true;
    }

    public void Dispose() => _out.Dispose();
}
