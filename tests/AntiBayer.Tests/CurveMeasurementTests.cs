using System;
using System.Collections.Generic;
using System.Linq;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class CurveMeasurementTests
{
    private sealed class FakeSource : IFrameSource
    {
        private readonly Func<int, ushort[]> _frame;
        public List<int> Reads { get; } = new();
        public ImageInfo Info { get; }

        public FakeSource(int w, int h, int depth, int frames, ushort white, Func<int, ushort[]> frame)
        {
            _frame = frame;
            Info = new ImageInfo("fake", ImageFormat.Ser, w, h, depth, frames, white, null, new CaptureInfo(), null);
        }

        public void ReadFrame(int index, ushort[] target)
        {
            Reads.Add(index);
            _frame(index).CopyTo(target, 0);
        }

        public void Dispose() { }
    }

    // Smooth rise from left to right; the response differs per filter.
    private static ushort[] Gradient(int w, int h, BayerPattern pattern, double lumMin, double lumMax, bool is16)
    {
        var f = new ushort[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                f[y * w + x] = (ushort)Math.Round(Response(Bayer.FilterAt(pattern, x, y), lumMin + x * (lumMax - lumMin) / (w - 1), is16));
        return f;
    }

    private static double Response(BayerFilter f, double lum, bool is16)
    {
        double k = is16 ? 0.0000001 : 0.0001;
        return f switch
        {
            BayerFilter.Red => lum * 1.08 - k * lum * lum,
            BayerFilter.Blue => lum * 0.92,
            _ => lum,
        };
    }

    /// <summary>The value centred on the pixel, with an equal share of every filter.</summary>
    private static double Centred(ushort[] f, int w, int p) =>
        (4.0 * f[p] + 2.0 * (f[p - 1] + f[p + 1] + f[p - w] + f[p + w]) + f[p - w - 1] + f[p - w + 1] + f[p + w - 1] + f[p + w + 1]) / 16;

    /// <summary>Per band (5 equally filled by brightness) and filter: |mean(filter - centred value)|, plus the band's mean reference.</summary>
    private static List<(double[] Dev, double Ref)> Bands(ushort[] frame, int w, int h, BayerPattern p)
    {
        var samples = new List<(double Ref, int Filter, double Diff)>();
        for (int y = 2; y < h - 2; y++)
            for (int x = 2; x < w - 2; x++)
            {
                double r = Centred(frame, w, y * w + x);
                samples.Add((r, (int)Bayer.FilterAt(p, x, y), frame[y * w + x] - r));
            }
        // The brightest 10 % lie beyond the last measured point (mean of the top class) and are extrapolated towards white.
        var sorted = samples.OrderBy(c => c.Ref).Take(samples.Count * 9 / 10).ToList();
        var result = new List<(double[], double)>();
        for (int b = 0; b < 5; b++)
        {
            var part = sorted.Skip(b * sorted.Count / 5).Take((b + 1) * sorted.Count / 5 - b * sorted.Count / 5).ToList();
            var dev = Enumerable.Range(0, 4).Select(k => Math.Abs(part.Where(c => c.Filter == k).Average(c => c.Diff))).ToArray();
            result.Add((dev, part.Average(c => c.Ref)));
        }
        return result;
    }

    private static double[] Noise(int seed, int n, double sigma)
    {
        var r = new Random(seed);
        var v = new double[n];
        for (int i = 0; i < n; i++)
            v[i] = sigma * Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble());
        return v;
    }

    private static readonly BayerFilter[] AllFilters =
        { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue };

    private static void KorrigiertSenktAbweichung(int w, int h, int depth, ushort white, double lumMax, double limitAfter, bool relative)
    {
        var pattern = BayerPattern.GBRG;
        var frame = Gradient(w, h, pattern, depth == 8 ? 20 : 1000, lumMax, depth == 16);
        var src = new FakeSource(w, h, depth, 5, white, _ => frame);
        var m = CurveMeasurement.Measure(src, pattern);

        var set = new ParameterSet(pattern, depth);
        set.ReplaceConfirmed(m.Points);
        var corrector = new FrameCorrector(pattern, set.BuildConfirmedTables(white));
        var corrected = new ushort[w * h];
        corrector.Apply(frame, corrected, w, h, new Random(1));

        var before = Bands(frame, w, h, pattern);
        var after = Bands(corrected, w, h, pattern);
        double Scale(int b) => relative ? before[b].Ref / 100.0 : 1.0; // relative: deviation in percent of the cell mean
        double beforeMax = Enumerable.Range(0, 5).Max(b => before[b].Dev.Max() / Scale(b));
        Assert.True(beforeMax > limitAfter * 5, $"vorher nur {beforeMax}");
        for (int b = 0; b < 5; b++)
            Assert.True(after[b].Dev.Max() / (relative ? after[b].Ref / 100.0 : 1.0) < limitAfter,
                $"Band {b}: {after[b].Dev.Max() / (relative ? after[b].Ref / 100.0 : 1.0)}");
    }

    [Fact]
    public void Gemessene_Kurven_senken_die_Filterabweichung_bei_8_Bit()
        => KorrigiertSenktAbweichung(200, 100, 8, 255, 220, 1.0, false);

    [Fact]
    public void Gemessene_Kurven_senken_die_Filterabweichung_bei_16_Bit_unter_0_1_Prozent()
        => KorrigiertSenktAbweichung(200, 100, 16, 65535, 56000, 0.1, true);

    [Fact]
    public void Bei_1000_Frames_werden_genau_40_gleichmaessig_verteilte_gelesen()
    {
        var frame = Gradient(200, 100, BayerPattern.RGGB, 20, 220, false);
        var src = new FakeSource(200, 100, 8, 1000, 255, _ => frame);
        var m = CurveMeasurement.Measure(src, BayerPattern.RGGB);
        Assert.Equal(40, src.Reads.Distinct().Count());
        Assert.Equal(40, src.Reads.Count);
        Assert.Equal(0, src.Reads.First());
        Assert.Equal(975, src.Reads.Last());
        Assert.Equal(40, m.FramesUsed);
    }

    [Fact]
    public void Bei_3_Frames_werden_alle_drei_gelesen()
    {
        var frame = Gradient(200, 100, BayerPattern.RGGB, 20, 220, false);
        var src = new FakeSource(200, 100, 8, 3, 255, _ => frame);
        var m = CurveMeasurement.Measure(src, BayerPattern.RGGB);
        Assert.Equal(new[] { 0, 1, 2 }, src.Reads.ToArray());
        Assert.Equal(3, m.FramesUsed);
    }

    [Fact]
    public void Uebersteuerte_Pixel_zaehlen_nicht_und_verschieben_die_Punkte_nicht()
    {
        var pattern = BayerPattern.RGGB;
        int w = 200, h = 200;
        var clean = Gradient(w, h, pattern, 20, 220, false);
        var expected = CurveMeasurement.Measure(new FakeSource(w, h, 8, 3, 255, _ => clean), pattern);

        var spiked = (ushort[])clean.Clone();
        for (int x = 3; x < w; x += 4) spiked[10 * w + x] = 255; // one frame only, all over a row
        var m = CurveMeasurement.Measure(new FakeSource(w, h, 8, 3, 255, i => i == 1 ? spiked : clean), pattern);

        Assert.True(m.SamplesUsed < expected.SamplesUsed);
        foreach (var f in AllFilters)
        {
            Assert.Equal(expected.Points[f].Count, m.Points[f].Count);
            for (int i = 0; i < m.Points[f].Count; i++)
            {
                double want = expected.Points[f][i].New - expected.Points[f][i].Original;
                Assert.InRange(m.Points[f][i].New - m.Points[f][i].Original, want - 0.3, want + 0.3);
            }
        }
    }

    [Fact]
    public void Gleichmaessiges_Bild_wirft_nicht_und_gibt_hoechstens_einen_Punkt_je_Filter()
    {
        var frame = Enumerable.Repeat((ushort)100, 40 * 40).ToArray();
        var m = CurveMeasurement.Measure(new FakeSource(40, 40, 8, 5, 255, _ => frame), BayerPattern.RGGB);
        Assert.Equal(36 * 36 * 5, m.SamplesUsed);
        foreach (var list in m.Points.Values) Assert.True(list.Count <= 1);
    }

    [Fact]
    public void Zu_wenige_Pixel_ergeben_leere_Listen()
    {
        var frame = Gradient(18, 10, BayerPattern.RGGB, 20, 220, false);
        var m = CurveMeasurement.Measure(new FakeSource(18, 10, 8, 5, 255, _ => frame), BayerPattern.RGGB);
        Assert.Equal(14 * 6 * 5, m.SamplesUsed);
        Assert.Equal(4, m.Points.Count);
        Assert.All(m.Points.Values, l => Assert.Empty(l));
    }

    [Fact]
    public void Ungerade_Breite_und_Hoehe_laufen()
    {
        var odd = Gradient(31, 25, BayerPattern.RGGB, 20, 220, false);
        var m = CurveMeasurement.Measure(new FakeSource(31, 25, 8, 5, 255, _ => odd), BayerPattern.RGGB);
        Assert.Equal(27 * 21 * 5, m.SamplesUsed);
    }

    [Fact]
    public void Muster_None_wirft()
    {
        var src = new FakeSource(40, 40, 8, 1, 255, _ => new ushort[1600]);
        Assert.Throws<ArgumentException>(() => CurveMeasurement.Measure(src, BayerPattern.None));
    }

    [Theory]
    [InlineData(BayerPattern.GBRG)]
    [InlineData(BayerPattern.RGGB)]
    public void Punkte_landen_bei_der_richtigen_Farbe(BayerPattern pattern)
    {
        int w = 40, h = 40;
        var frame = new ushort[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                frame[y * w + x] = Bayer.FilterAt(pattern, x, y) switch
                {
                    BayerFilter.Red => 120,
                    BayerFilter.Blue => 80,
                    _ => 100,
                };
        var src = new FakeSource(w, h, 8, 2, 255, _ => frame);
        var m = CurveMeasurement.Measure(src, pattern);
        Assert.Equal(120, m.Points[BayerFilter.Red].Single().Original);
        Assert.Equal(80, m.Points[BayerFilter.Blue].Single().Original);
        Assert.Equal(100, m.Points[BayerFilter.Green1].Single().Original);
        Assert.Equal(100, m.Points[BayerFilter.Green2].Single().Original);
        Assert.Equal(100, m.Points[BayerFilter.Red].Single().New);
    }

    // ---- Reference on the pixel, frame by frame ----

    [Fact]
    public void Reine_Helligkeitsrampe_ergibt_keinen_Farbunterschied()
    {
        // All filters answer alike. The 2x2 cell mean lies half a pixel off every filter and would see colour here.
        int w = 200, h = 100;
        var frame = new ushort[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                frame[y * w + x] = (ushort)(1000 + 150 * (x + y));
        var m = CurveMeasurement.Measure(new FakeSource(w, h, 16, 3, 65535, _ => frame), BayerPattern.GBRG);

        foreach (var f in AllFilters)
        {
            Assert.NotEmpty(m.Points[f]);
            Assert.All(m.Points[f], p => Assert.InRange(p.New - p.Original, -2.0, 2.0));
        }
    }

    [Fact]
    public void Wandernder_Sonnenrand_wird_je_Einzelframe_richtig_gemessen()
    {
        // A steep edge that moves between frames: a mean frame would mix the brightnesses of different places.
        int w = 200, h = 100;
        var pattern = BayerPattern.GBRG;
        ushort[] Frame(int i)
        {
            var f = new ushort[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double t = Math.Clamp((x - 40 - 3.0 * i) / 40.0, 0, 1);
                    f[y * w + x] = (ushort)Math.Round(Response(Bayer.FilterAt(pattern, x, y), 500 + 39500 * t, true));
                }
            return f;
        }
        var m = CurveMeasurement.Measure(new FakeSource(w, h, 16, 30, 65535, Frame), pattern);

        foreach (var f in AllFilters)
        {
            Assert.NotEmpty(m.Points[f]);
            Assert.All(m.Points[f], p => Assert.InRange(p.Original, Response(f, p.New, true) - 150, Response(f, p.New, true) + 150));
        }
    }

    [Fact]
    public void Stuetzpunkte_bei_8_Bit_bleiben_ungerundet()
    {
        int w = 200, h = 100;
        var pattern = BayerPattern.GBRG;
        ushort[] Frame(int i)
        {
            var noise = Noise(i, w * h, 1.0);
            var f = new ushort[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double factor = Bayer.FilterAt(pattern, x, y) switch { BayerFilter.Red => 1.03, BayerFilter.Blue => 0.97, _ => 1.0 };
                    f[y * w + x] = (ushort)Math.Round((40 + x * 180.0 / (w - 1)) * factor + noise[y * w + x]);
                }
            return f;
        }
        var m = CurveMeasurement.Measure(new FakeSource(w, h, 8, 10, 255, Frame), pattern);

        var red = m.Points[BayerFilter.Red];
        Assert.True(red.Count >= 6);
        Assert.Contains(red, p => p.New != Math.Round(p.New));
        Assert.All(red, p => Assert.InRange(p.New, p.Original / 1.03 - 0.2, p.Original / 1.03 + 0.2));
    }

    [Fact]
    public void Rauschen_flacht_die_Kurve_nicht_ab()
    {
        // All filters alike: the curve must stay the identity. A class formed from the pixel's own noise would flatten it.
        int w = 200, h = 200;
        ushort[] Frame(int i)
        {
            var noise = Noise(i, w * h, 8.0);
            var f = new ushort[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    f[y * w + x] = (ushort)Math.Clamp(Math.Round(180 + 25 * Math.Sin(x / 12.0) + noise[y * w + x]), 0, 254);
            return f;
        }
        var m = CurveMeasurement.Measure(new FakeSource(w, h, 8, 20, 255, Frame), BayerPattern.GBRG);

        foreach (var f in AllFilters)
        {
            var pts = m.Points[f];
            Assert.True(pts.Count >= 4);
            double slope = (pts[^1].New - pts[0].New) / (pts[^1].Original - pts[0].Original);
            Assert.InRange(slope, 0.985, 1.015);
        }
    }
}
