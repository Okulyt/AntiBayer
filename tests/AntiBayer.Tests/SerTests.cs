using System;
using System.IO;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class SerTests
{
    private static ushort Px(int f, int x, int y) => (ushort)(((f * 31 + x * 7 + y * 13) * 257 + 3) & 0xFFFF);
    private static ushort Px8(int f, int x, int y) => (ushort)((f * 31 + x * 7 + y * 13) & 0xFF);

    private static byte[] RoundTrip(string src, string dst, bool swap = false, Func<ushort[], ushort[]>? change = null)
    {
        using var source = new SerSource(src, swap);
        using (var sink = new SerSink(source, dst))
        {
            var buf = new ushort[source.Info.Width * source.Info.Height];
            for (int i = 0; i < source.Info.FrameCount; i++)
            {
                source.ReadFrame(i, buf);
                sink.WriteFrame(change != null ? change(buf) : buf);
            }
            sink.Complete();
        }
        return File.ReadAllBytes(dst);
    }

    [Fact] public void Liest_FireCapture_Kopf()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser");
        TestData.WriteSer(p, 4, 2, 16, 10, 2, (f, x, y) => (ushort)(0x1230 + x + 16 * y + f), littleEndianFlag: 0);
        using var s = new SerSource(p);
        Assert.Equal(BayerPattern.GBRG, s.Info.SuggestedPattern);
        Assert.Equal(16, s.Info.BitDepth);
        Assert.Equal(2, s.Info.FrameCount);
        Assert.Equal("offset=0", s.Header.Observer);
        Assert.Equal("Camera", s.Header.Instrument);
        Assert.Equal("Scope", s.Header.Telescope);
        Assert.True(s.HasTimestampTrailer);
        var buf = new ushort[8];
        s.ReadFrame(1, buf);
        Assert.Equal(0x1230 + 3 + 16 + 1, buf[7]);
        Assert.Equal(0x1230 + 1 + 1, buf[1]);
    }

    [Theory]
    [InlineData(8, false)] [InlineData(8, true)] [InlineData(16, false)] [InlineData(16, true)]
    public void Rundgang_Identitaet_bytegleich(int depth, bool trailer)
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 5, 3, depth, 8, 4, depth == 8 ? Px8 : Px, trailer);
        Assert.Equal(File.ReadAllBytes(p), RoundTrip(p, o));
    }

    [Fact] public void Bytes_tauschen_aendert_nur_Interpretation()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 2, 1, 16, 0, 1, (f, x, y) => 0x1234);
        using (var s = new SerSource(p, swapBytes: true))
        {
            var buf = new ushort[2];
            s.ReadFrame(0, buf);
            Assert.Equal(0x3412, buf[0]);
        }
        Assert.Equal(File.ReadAllBytes(p), RoundTrip(p, o, swap: true));
    }

    [Fact] public void Kopf_und_Anhang_bleiben_bei_Korrektur_gleich()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        int n = 3;
        TestData.WriteSer(p, 4, 4, 16, 9, n, Px);
        var orig = File.ReadAllBytes(p);
        var res = RoundTrip(p, o, change: a => { var c = (ushort[])a.Clone(); for (int i = 0; i < c.Length; i++) c[i] = (ushort)(c[i] / 2); return c; });
        Assert.Equal(orig.Length, res.Length);
        Assert.Equal(orig[..178], res[..178]);
        Assert.Equal(orig[^(8 * n)..], res[^(8 * n)..]);
        Assert.NotEqual(orig[178..^(8 * n)], res[178..^(8 * n)]);
    }

    [Theory] [InlineData(100)] [InlineData(101)]
    public void Farb_SER_wird_abgelehnt(int colorId)
    {
        string p = Path.Combine(TestData.TempDir(), "a.ser");
        TestData.WriteSer(p, 4, 4, 8, colorId, 1, Px8);
        var ex = Assert.Throws<UnsupportedImageException>(() => new SerSource(p));
        Assert.Contains("Colour", ex.Message);
    }

    [Fact] public void Abgebrochene_SER_oeffnet_vollstaendige_Frames()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser");
        TestData.WriteSer(p, 4, 4, 8, 8, 10, Px8, trailer: false);
        using (var fs = new FileStream(p, FileMode.Open)) fs.SetLength(178 + 16 * 7 + 8);
        using var s = new SerSource(p);
        Assert.Equal(7, s.Info.FrameCount);
        Assert.NotNull(s.Info.Warning);
        Assert.False(s.HasTimestampTrailer);
        Assert.True(s.IsTruncated);
    }

    [Fact] public void SerSink_lehnt_abgebrochene_Quelle_ab()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 4, 4, 8, 8, 10, Px8, trailer: false);
        using (var fs = new FileStream(p, FileMode.Open)) fs.SetLength(178 + 16 * 7 + 8);
        using var s = new SerSource(p);
        Assert.Throws<UnsupportedImageException>(() => new SerSink(s, o));
        Assert.False(File.Exists(o));
    }

    [Fact] public void Offset_ueber_2_GB_ohne_Ueberlauf()
    {
        var bytes = new byte[178];
        System.Text.Encoding.ASCII.GetBytes("LUCAM-RECORDER").CopyTo(bytes, 0);
        BitConverter.GetBytes(0).CopyTo(bytes, 18);
        BitConverter.GetBytes(1920).CopyTo(bytes, 26);
        BitConverter.GetBytes(1080).CopyTo(bytes, 30);
        BitConverter.GetBytes(16).CopyTo(bytes, 34);
        BitConverter.GetBytes(3000).CopyTo(bytes, 38);
        var h = SerHeader.Parse(bytes);
        Assert.Equal(178 + 2000L * 4147200, h.FrameOffset(2000));
        Assert.True(h.FrameOffset(2000) > int.MaxValue);
        Assert.Equal(3000L * 4147200, h.DataBytes);
    }

    [Fact] public void Original_bleibt_lesbar_waehrend_Quelle_offen()
    {
        string p = Path.Combine(TestData.TempDir(), "a.ser");
        TestData.WriteSer(p, 4, 4, 8, 8, 2, Px8);
        var before = File.ReadAllBytes(p);
        using var s = new SerSource(p);
        using (var other = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.Equal(before.Length, other.Length);
        Assert.Throws<IOException>(() => new FileStream(p, FileMode.Open, FileAccess.Write, FileShare.Read));
        Assert.Equal(before, File.ReadAllBytes(p));
    }

    [Fact] public void SerSink_ueberschreibt_Quelle_nicht()
    {
        string p = Path.Combine(TestData.TempDir(), "a.ser");
        TestData.WriteSer(p, 4, 4, 8, 8, 2, Px8);
        using var s = new SerSource(p);
        Assert.Throws<UnsupportedImageException>(() => new SerSink(s, p));
    }

    [Fact] public void Echte_Datei_Gegenprobe()
    {
        string? sample = Environment.GetEnvironmentVariable("ANTIBAYER_SAMPLE");
        if (string.IsNullOrEmpty(sample) || !File.Exists(sample)) return; // skipped without the variable
        string o = Path.Combine(TestData.TempDir(), "out.ser");
        var orig = File.ReadAllBytes(sample);
        using (var s = new SerSource(sample))
        {
            Assert.Equal(65520, s.Info.WhiteLevel);
            Assert.Equal(BayerPattern.GBRG, s.Info.SuggestedPattern);
            Assert.Equal(490, s.Info.FrameCount);
        }
        Assert.Equal(orig, RoundTrip(sample, o));
    }

    private static void AppendBytes(string path, byte[] extra)
    {
        using var fs = new FileStream(path, FileMode.Append, FileAccess.Write);
        fs.Write(extra);
    }

    [Fact] public void Unvollstaendiger_Zeitstempel_Anhang_bleibt_bytegleich_erhalten()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 4, 4, 8, 8, 5, Px8);
        using (var fs = new FileStream(p, FileMode.Open)) fs.SetLength(fs.Length - 13); // 27 of 40 trailer bytes
        var orig = File.ReadAllBytes(p);
        var res = RoundTrip(p, o);
        Assert.Equal(orig, res);
    }

    [Fact] public void Bytes_nach_dem_Anhang_bleiben_bytegleich_erhalten()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 4, 4, 16, 8, 4, Px);
        AppendBytes(p, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });
        var orig = File.ReadAllBytes(p);
        var res = RoundTrip(p, o);
        Assert.Equal(orig, res);
    }

    [Fact] public void Zusaetzliche_Frames_ueber_die_Kopfangabe_hinaus_bleiben_erhalten()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 4, 4, 8, 8, 3, Px8, trailer: false);
        var extra = new byte[32];
        for (int i = 0; i < extra.Length; i++) extra[i] = (byte)(200 + i);
        AppendBytes(p, extra); // two more frames, no trailer
        var orig = File.ReadAllBytes(p);
        Assert.Equal(orig, RoundTrip(p, o));
    }

    [Fact] public void Langer_Rest_ueber_mehrere_Kopierblöcke_bleibt_bytegleich()
    {
        string d = TestData.TempDir(), p = Path.Combine(d, "a.ser"), o = Path.Combine(d, "b.ser");
        TestData.WriteSer(p, 2, 2, 8, 8, 2, Px8);
        var extra = new byte[300_000];
        for (int i = 0; i < extra.Length; i++) extra[i] = (byte)(i * 7);
        AppendBytes(p, extra);
        var orig = File.ReadAllBytes(p);
        Assert.Equal(orig, RoundTrip(p, o));
    }
}
