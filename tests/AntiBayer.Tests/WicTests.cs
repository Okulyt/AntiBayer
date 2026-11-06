using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class WicTests
{
    internal static void Save(string path, BitmapSource bmp)
    {
        BitmapEncoder enc = path.EndsWith(".png") ? new PngBitmapEncoder() : new TiffBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    private static ushort Val(int x, int y, int max) => (ushort)((x * 37 + y * 11) % (max + 1));

    private static ushort Expected(int x, int y, int bits) =>
        bits == 8 ? Val(x, y, 255) : (ushort)(Val(x, y, 4095) * 16 + 7);

    private static byte[] Gray(int w, int h, int bits)
    {
        var b = new byte[w * h * (bits / 8)];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                ushort v = Expected(x, y, bits);
                if (bits == 8) b[i] = (byte)v;
                else { b[2 * i] = (byte)v; b[2 * i + 1] = (byte)(v >> 8); }
            }
        return b;
    }

    [Theory]
    [InlineData("png", 8)]
    [InlineData("png", 16)]
    [InlineData("tif", 8)]
    [InlineData("tif", 16)]
    public void Grau_Rundgang_liefert_identische_Pixel_und_Bittiefe(string ext, int bits)
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, "in." + ext), dst = Path.Combine(dir, "out." + ext);
        const int w = 13, h = 7;
        var fmt = bits == 8 ? PixelFormats.Gray8 : PixelFormats.Gray16;
        Save(src, BitmapSource.Create(w, h, 96, 96, fmt, null, Gray(w, h, bits), w * bits / 8));

        var frame = new ushort[w * h];
        using (var s = ImageFiles.Open(src))
        {
            Assert.Equal(bits, s.Info.BitDepth);
            Assert.Equal(1, s.Info.FrameCount);
            Assert.Equal((w, h), (s.Info.Width, s.Info.Height));
            s.ReadFrame(0, frame);
            using var sink = ImageFiles.CreateSink(s, dst);
            sink.WriteFrame(frame);
            sink.Complete();
        }
        using var back = ImageFiles.Open(dst);
        var again = new ushort[w * h];
        back.ReadFrame(0, again);
        Assert.Equal(bits, back.Info.BitDepth);
        Assert.Equal(frame, again);
        Assert.Equal(Expected(1, 0, bits), frame[1]);
        Assert.Equal(Expected(5, 3, bits), frame[3 * w + 5]);
    }

    [Fact]
    public void Weisswert_bei_12_Bit_linksbuendig_wird_erkannt()
    {
        string p = Path.Combine(TestData.TempDir(), "a.tif");
        var raw = new byte[4 * 4 * 2];
        for (int i = 0; i < 16; i++) { ushort v = (ushort)(i * 16); raw[2 * i] = (byte)v; raw[2 * i + 1] = (byte)(v >> 8); }
        Save(p, BitmapSource.Create(4, 4, 96, 96, PixelFormats.Gray16, null, raw, 8));
        using var s = ImageFiles.Open(p);
        Assert.Equal(65520, s.Info.WhiteLevel);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RGB_mit_gleichen_Kanaelen_gilt_als_einkanalig(bool alpha)
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, "in.png"), dst = Path.Combine(dir, "out.png");
        const int w = 5, h = 4;
        var fmt = alpha ? PixelFormats.Bgra32 : PixelFormats.Bgr24;
        int step = alpha ? 4 : 3;
        var raw = new byte[w * h * step];
        for (int i = 0; i < w * h; i++)
        {
            byte v = (byte)(i * 11);
            raw[i * step] = raw[i * step + 1] = raw[i * step + 2] = v;
            if (alpha) raw[i * step + 3] = 255;
        }
        Save(src, BitmapSource.Create(w, h, 96, 96, fmt, null, raw, w * step));

        var frame = new ushort[w * h];
        using (var s = ImageFiles.Open(src))
        {
            Assert.Equal(8, s.Info.BitDepth);
            s.ReadFrame(0, frame);
            Assert.Equal(11, frame[1]);
            frame[2] = 200;
            using var sink = ImageFiles.CreateSink(s, dst);
            sink.WriteFrame(frame);
            sink.Complete();
        }
        var dec = BitmapDecoder.Create(new Uri(dst), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.Equal(fmt, dec.Frames[0].Format);
        var o = new byte[w * h * step];
        dec.Frames[0].CopyPixels(o, w * step, 0);
        Assert.Equal(new byte[] { 200, 200, 200 }, o.Skip(2 * step).Take(3).ToArray());
        if (alpha) Assert.Equal(255, o[2 * step + 3]);
    }

    [Fact]
    public void Rgb48_mit_gleichen_Kanaelen_ist_16_Bit_und_bleibt_Rgb48()
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, "in.tif"), dst = Path.Combine(dir, "out.tif");
        const int w = 3, h = 2;
        var raw = new byte[w * h * 6];
        for (int i = 0; i < w * h; i++)
            for (int c = 0; c < 3; c++) { ushort v = (ushort)(1000 * (i + 1)); raw[i * 6 + 2 * c] = (byte)v; raw[i * 6 + 2 * c + 1] = (byte)(v >> 8); }
        Save(src, BitmapSource.Create(w, h, 96, 96, PixelFormats.Rgb48, null, raw, w * 6));
        var frame = new ushort[w * h];
        using (var s = ImageFiles.Open(src))
        {
            Assert.Equal(16, s.Info.BitDepth);
            s.ReadFrame(0, frame);
            Assert.Equal(2000, frame[1]);
            using var sink = ImageFiles.CreateSink(s, dst);
            sink.WriteFrame(frame);
            sink.Complete();
        }
        var dec = BitmapDecoder.Create(new Uri(dst), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.Equal(PixelFormats.Rgb48, dec.Frames[0].Format);
        using var back = ImageFiles.Open(dst);
        var again = new ushort[w * h];
        back.ReadFrame(0, again);
        Assert.Equal(frame, again);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Indexed8_mit_Graupalette_gilt_als_grau(bool inverted)
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, "in.png"), dst = Path.Combine(dir, "out.png");
        var pal = new BitmapPalette(Enumerable.Range(0, 256)
            .Select(i => { byte g = (byte)(inverted ? 255 - i : i); return Color.FromRgb(g, g, g); }).ToList());
        var raw = new byte[] { 0, 10, 20, 255 };
        Save(src, BitmapSource.Create(2, 2, 96, 96, PixelFormats.Indexed8, pal, raw, 2));
        var frame = new ushort[4];
        using (var s = ImageFiles.Open(src))
        {
            Assert.Equal(8, s.Info.BitDepth);
            s.ReadFrame(0, frame);
            ushort[] expected = inverted ? new ushort[] { 255, 245, 235, 0 } : new ushort[] { 0, 10, 20, 255 };
            Assert.Equal(expected, frame);
            frame[0] = 99;
            using var sink = ImageFiles.CreateSink(s, dst);
            sink.WriteFrame(frame);
            sink.Complete();
        }
        using var back = ImageFiles.Open(dst);
        var again = new ushort[4];
        back.ReadFrame(0, again);
        Assert.Equal(frame, again);
    }

    [Fact]
    public void Indexed8_mit_Farbpalette_wird_abgelehnt()
    {
        string p = Path.Combine(TestData.TempDir(), "c.png");
        var pal = new BitmapPalette(new[] { Colors.Red, Colors.Green });
        Save(p, BitmapSource.Create(2, 1, 96, 96, PixelFormats.Indexed8, pal, new byte[] { 0, 1 }, 2));
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
    }

    [Theory]
    [InlineData("png")]
    [InlineData("tif")]
    public void Echtes_Farbbild_wird_abgelehnt(string ext)
    {
        string p = Path.Combine(TestData.TempDir(), "c." + ext);
        var raw = new byte[] { 10, 20, 30, 10, 20, 30, 10, 20, 31, 10, 20, 30 };
        Save(p, BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgr24, null, raw, 6));
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
    }

    [Fact]
    public void Mehrseitiges_TIFF_wird_abgelehnt()
    {
        string p = Path.Combine(TestData.TempDir(), "m.tif");
        var enc = new TiffBitmapEncoder();
        for (int i = 0; i < 2; i++)
            enc.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2, 2, 96, 96, PixelFormats.Gray8, null, new byte[4], 2)));
        using (var fs = File.Create(p)) enc.Save(fs);
        var ex = Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
        Assert.Contains("Multi-page", ex.Message);
    }

    [Fact]
    public void Unbekannte_Endung_und_kaputte_Datei_liefern_UnsupportedImageException()
    {
        string dir = TestData.TempDir();
        string txt = Path.Combine(dir, "a.txt");
        File.WriteAllText(txt, "hello");
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(txt));
        foreach (var ext in new[] { "png", "tif", "ser" })
        {
            string bad = Path.Combine(dir, "bad." + ext);
            File.WriteAllText(bad, "this is not an image, just text");
            Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(bad));
        }
    }

    [Fact]
    public void Quelle_sperrt_Datei_nicht_zum_Lesen_und_laesst_sie_frei_nach_Dispose()
    {
        string p = Path.Combine(TestData.TempDir(), "l.png");
        Save(p, BitmapSource.Create(2, 2, 96, 96, PixelFormats.Gray8, null, new byte[4], 2));
        var s = ImageFiles.Open(p);
        using (var other = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read)) { }
        s.Dispose();
        File.Delete(p);
        Assert.False(File.Exists(p));
    }

    [Fact]
    public void Quelle_ist_von_anderem_Thread_lesbar()
    {
        string p = Path.Combine(TestData.TempDir(), "t.png");
        Save(p, BitmapSource.Create(2, 2, 96, 96, PixelFormats.Gray8, null, new byte[] { 1, 2, 3, 4 }, 2));
        using var s = ImageFiles.Open(p);
        var f = new ushort[4];
        var t = new System.Threading.Thread(() => s.ReadFrame(0, f));
        t.Start(); t.Join();
        Assert.Equal(new ushort[] { 1, 2, 3, 4 }, f);
    }

    [Fact]
    public void Indexed8_Sink_laesst_sich_auf_anderem_Thread_erzeugen_und_schreiben()
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, "in.png"), dst = Path.Combine(dir, "out.png");
        var pal = new BitmapPalette(Enumerable.Range(0, 256).Select(i => Color.FromRgb((byte)i, (byte)i, (byte)i)).ToList());
        Save(src, BitmapSource.Create(2, 2, 96, 96, PixelFormats.Indexed8, pal, new byte[] { 0, 10, 20, 255 }, 2));
        using var s = ImageFiles.Open(src);
        Exception? error = null;
        var t = new System.Threading.Thread(() =>
        {
            try
            {
                using var sink = ImageFiles.CreateSink(s, dst);
                sink.WriteFrame(new ushort[] { 1, 2, 3, 4 });
                sink.Complete();
            }
            catch (Exception e) { error = e; }
        });
        t.Start();
        t.Join();
        Assert.Null(error);
        Assert.True(File.Exists(dst));
    }

    [Fact]
    public void Ausgabe_ueber_die_Quelle_wird_abgelehnt()
    {
        string p = Path.Combine(TestData.TempDir(), "o.png");
        Save(p, BitmapSource.Create(2, 2, 96, 96, PixelFormats.Gray8, null, new byte[4], 2));
        using var s = ImageFiles.Open(p);
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.CreateSink(s, p));
    }
}
