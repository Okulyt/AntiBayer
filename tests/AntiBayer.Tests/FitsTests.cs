using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class FitsTests
{
    private static string Card(string text) => text.PadRight(80);

    private static string KV(string key, string value) => Card(key.PadRight(8) + "= " + value.PadLeft(20));

    /// <summary>Builds a synthetic FITS file. data: already encoded big-endian bytes.</summary>
    private static byte[] Build(int bitpix, int w, int h, byte[] data, IEnumerable<string>? extra = null,
        double? bzero = null, int naxis = 2, byte[]? tail = null)
    {
        var cards = new List<string> { KV("SIMPLE", "T"), KV("BITPIX", bitpix.ToString()), KV("NAXIS", naxis.ToString()),
            KV("NAXIS1", w.ToString()), KV("NAXIS2", h.ToString()) };
        if (naxis == 3) cards.Add(KV("NAXIS3", "3"));
        if (bzero != null) cards.Add(KV("BZERO", bzero.Value.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture)));
        if (extra != null) cards.AddRange(extra);
        cards.Add(Card("END"));
        var head = Encoding.ASCII.GetBytes(string.Concat(cards));
        int headLen = (head.Length + 2879) / 2880 * 2880;
        var ms = new MemoryStream();
        ms.Write(head);
        ms.Write(Encoding.ASCII.GetBytes(new string(' ', headLen - head.Length)));
        ms.Write(data);
        ms.Write(new byte[(2880 - data.Length % 2880) % 2880]);
        if (tail != null) ms.Write(tail);
        return ms.ToArray();
    }

    private static byte[] Enc16(int w, int h, Func<int, int, int> stored)
    {
        var b = new byte[w * h * 2];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int v = stored(x, y), i = y * w + x;
                b[2 * i] = (byte)(v >> 8); b[2 * i + 1] = (byte)v;
            }
        return b;
    }

    private static byte[] RoundTrip(byte[] fits, string name, out ushort[] values, out ImageInfo info)
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, name), dst = Path.Combine(dir, "out" + Path.GetExtension(name));
        File.WriteAllBytes(src, fits);
        using var s = ImageFiles.Open(src);
        info = s.Info;
        values = new ushort[s.Info.Width * s.Info.Height];
        s.ReadFrame(0, values);
        using (var sink = ImageFiles.CreateSink(s, dst))
        {
            sink.WriteFrame(values);
            sink.Complete();
        }
        return File.ReadAllBytes(dst);
    }

    [Theory]
    [InlineData(".fit")]
    [InlineData(".fits")]
    [InlineData(".FTS")]
    public void Endungen_werden_unterstuetzt(string ext) =>
        Assert.True(ImageFiles.IsSupportedExtension("a" + ext));

    [Fact]
    public void BITPIX_8_Rundgang_ist_bytegleich()
    {
        var data = new byte[7 * 5];
        for (int i = 0; i < data.Length; i++) data[i] = (byte)(i * 7);
        data[0] = 0; data[1] = 255;
        var fits = Build(8, 7, 5, data);
        var back = RoundTrip(fits, "a.fits", out var v, out var info);
        Assert.Equal(fits, back);
        Assert.Equal(ImageFormat.Fits, info.Format);
        Assert.Equal(8, info.BitDepth);
        Assert.Equal((ushort)255, info.WhiteLevel);
        Assert.Equal(0, v[0]);
        Assert.Equal(255, v[1]);
    }

    [Fact]
    public void BITPIX_16_mit_BZERO_32768_Rundgang_ist_bytegleich_und_Werte_stimmen()
    {
        // stored = value - 32768; value 0 -> -32768, 65535 -> 32767
        int Value(int x, int y) => x == 0 && y == 0 ? 0 : x == 1 && y == 0 ? 65535 : (x * 4099 + y * 313) % 65536;
        var fits = Build(16, 6, 4, Enc16(6, 4, (x, y) => (Value(x, y) - 32768) & 0xFFFF), bzero: 32768);
        var back = RoundTrip(fits, "a.fit", out var v, out var info);
        Assert.Equal(fits, back);
        Assert.Equal(16, info.BitDepth);
        Assert.Equal(0, v[0]);
        Assert.Equal(65535, v[1]);
        Assert.Equal(Value(3, 2), v[2 * 6 + 3]);
        Assert.Equal((ushort)65535, info.WhiteLevel);
    }

    [Fact]
    public void Header_bleibt_erhalten()
    {
        var extra = new[] { Card("COMMENT synthetic test file"), Card("HISTORY made by test"),
            Card("OBJECT  = 'Jupiter '           / target") };
        // a trailing IMAGE extension HDU after the data
        var ext = new List<string> { KV("XTENSION", "'IMAGE   '"), KV("BITPIX", "8"), KV("NAXIS", "0"), Card("END") };
        var tail = new byte[2880];
        var extBytes = Encoding.ASCII.GetBytes(string.Concat(ext));
        Array.Copy(extBytes, tail, extBytes.Length);
        for (int i = extBytes.Length; i < tail.Length; i++) tail[i] = (byte)' ';
        var fits = Build(16, 4, 4, Enc16(4, 4, (x, y) => (x + y) * 100), extra, tail: tail);
        var back = RoundTrip(fits, "h.fits", out _, out _);
        Assert.Equal(fits, back);
        Assert.Contains("HISTORY made by test", Encoding.ASCII.GetString(back));
        Assert.Contains("XTENSION", Encoding.ASCII.GetString(back, back.Length - 2880, 2880));
    }

    [Fact]
    public void BITPIX_16_ohne_BZERO_mit_positiven_Werten_rundlauffaehig()
    {
        var fits = Build(16, 5, 3, Enc16(5, 3, (x, y) => x * 1000 + y * 7 + (x == 4 && y == 2 ? 20000 : 0)));
        var back = RoundTrip(fits, "p.fits", out var v, out var info);
        Assert.Equal(fits, back);
        Assert.Equal(1000, v[1]);
        Assert.Equal(4 * 1000 + 14 + 20000, v[2 * 5 + 4]);
        Assert.Equal((ushort)32767, info.WhiteLevel);
    }

    [Fact]
    public void BITPIX_16_ohne_BZERO_mit_negativen_Werten_ist_nicht_unterstuetzt()
    {
        var fits = Build(16, 2, 2, Enc16(2, 2, (x, y) => x == 1 ? 0xFFFF : 5));
        string p = Path.Combine(TestData.TempDir(), "n.fits");
        File.WriteAllBytes(p, fits);
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
    }

    [Fact]
    public void NAXIS_3_ist_nicht_unterstuetzt()
    {
        string p = Path.Combine(TestData.TempDir(), "c.fits");
        File.WriteAllBytes(p, Build(16, 2, 2, new byte[2 * 2 * 2 * 3], naxis: 3));
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
    }

    [Theory]
    [InlineData(-32)]
    [InlineData(32)]
    public void Gleitkomma_und_32_Bit_sind_nicht_unterstuetzt(int bitpix)
    {
        string p = Path.Combine(TestData.TempDir(), "f.fits");
        File.WriteAllBytes(p, Build(bitpix, 2, 2, new byte[16]));
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
    }

    [Fact]
    public void Abgeschnittene_Datei_ist_nicht_unterstuetzt()
    {
        var fits = Build(16, 100, 100, new byte[20000]);
        string p = Path.Combine(TestData.TempDir(), "t.fits");
        File.WriteAllBytes(p, fits[..(2880 + 1000)]);
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open(p));
    }

    [Fact]
    public void Zeilenfolge_und_Padding_auf_2880_Byte_stimmen()
    {
        // 100 x 30 x 2 = 6000 bytes -> padded to 8640; the value encodes the file position
        int w = 100, h = 30;
        var fits = Build(16, w, h, Enc16(w, h, (x, y) => y * 100 + x), bzero: null);
        Assert.Equal(0, fits.Length % 2880);
        var back = RoundTrip(fits, "r.fits", out var v, out _);
        Assert.Equal(0, back.Length % 2880);
        Assert.Equal(fits, back);
        Assert.Equal(0, v[0]);
        Assert.Equal(1 * 100 + 5, v[1 * w + 5]);       // second file row stays second
        Assert.Equal(29 * 100 + 99, v[w * h - 1]);
        // padding after the data is zero
        for (int i = 2880 + w * h * 2; i < back.Length; i++) Assert.Equal(0, back[i]);
    }

    [Fact]
    public void Ausgabe_darf_die_Quelle_nicht_ueberschreiben()
    {
        string p = Path.Combine(TestData.TempDir(), "s.fits");
        File.WriteAllBytes(p, Build(8, 2, 2, new byte[4]));
        using var s = ImageFiles.Open(p);
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.CreateSink(s, p));
    }
}
