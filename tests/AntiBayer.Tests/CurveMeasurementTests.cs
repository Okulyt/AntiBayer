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

    // Brightness per 2x2 cell column; response differs per filter.
    private static ushort[] Gradient(int w, int h, BayerPattern pattern, double lumMin, double lumMax, bool is16)
    {
        var f = new ushort[w * h];
        int cols = w / 2;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double lum = lumMin + (x / 2) * (lumMax - lumMin) / (cols - 1);
                double k = is16 ? 0.0000001 : 0.0001;
                double v = Bayer.FilterAt(pattern, x, y) switch
                {
                    BayerFilter.Red => lum * 1.08 - k * lum * lum,
                    BayerFilter.Blue => lum * 0.92,
                    _ => lum,
                };
                f[y * w + x] = (ushort)Math.Round(v);
            }
        return f;
    }

    /// <summary>Per band (5 equally filled by brightness) and filter: |mean(filter - cell mean)|, plus the band's mean reference.</summary>
    private static List<(double[] Dev, double Ref)> Bands(ushort[] frame, int w, int h, BayerPattern p)
    {
        var cells = new List<(double Ref, double[] Diff)>();
        for (int y = 0; y + 1 < h; y += 2)
            for (int x = 0; x + 1 < w; x += 2)
            {
                var v = new double[4];
                v[(int)Bayer.FilterAt(p, x, y)] = frame[y * w + x];
                v[(int)Bayer.FilterAt(p, x + 1, y)] = frame[y * w + x + 1];
                v[(int)Bayer.FilterAt(p, x, y + 1)] = frame[(y + 1) * w + x];
                v[(int)Bayer.FilterAt(p, x + 1, y + 1)] = frame[(y + 1) * w + x + 1];
                double r = v.Average();
                cells.Add((r, v.Select(a => a - r).ToArray()));
            }
        // The brightest 10 % lie beyond the last measured point (mean of the top class) and are extrapolated towards white.
        var sorted = cells.OrderBy(c => c.Ref).Take(cells.Count * 9 / 10).ToList();
        var result = new List<(double[], double)>();
        for (int b = 0; b < 5; b++)
        {
            var part = sorted.Skip(b * sorted.Count / 5).Take((b + 1) * sorted.Count / 5 - b * sorted.Count / 5).ToList();
            var dev = Enumerable.Range(0, 4).Select(k => Math.Abs(part.Average(c => c.Diff[k]))).ToArray();
            result.Add((dev, part.Average(c => c.Ref)));
        }
        return result;
    }

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
    public void Uebersteuerte_Zellen_zaehlen_nicht_und_aendern_die_Punkte_nicht()
    {
        var pattern = BayerPattern.RGGB;
        int w = 200;
        var clean = Gradient(w, 196, pattern, 20, 220, false);
        var cleanSrc = new FakeSource(w, 196, 8, 3, 255, _ => clean);
        var expected = CurveMeasurement.Measure(cleanSrc, pattern);

        var full = Gradient(w, 200, pattern, 20, 220, false);
        var spiked = (ushort[])full.Clone();
        for (int x = 0; x < w; x += 2) spiked[x] = 255; // red of the cells in the two top cell rows, one frame only
        for (int x = 0; x < w; x += 2) spiked[2 * w + x] = 255;
        var src = new FakeSource(w, 200, 8, 3, 255, i => i == 1 ? spiked : full);
        var m = CurveMeasurement.Measure(src, pattern);

        Assert.Equal(100 * 98, m.CellsUsed);
        Assert.Equal(100 * 98, expected.CellsUsed);
        foreach (var f in new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue })
            Assert.Equal(expected.Points[f].ToArray(), m.Points[f].ToArray());
    }

    [Fact]
    public void Gleichmaessiges_Bild_wirft_nicht_und_gibt_hoechstens_einen_Punkt_je_Filter()
    {
        var frame = Enumerable.Repeat((ushort)100, 40 * 40).ToArray();
        var src = new FakeSource(40, 40, 8, 5, 255, _ => frame);
        var m = CurveMeasurement.Measure(src, BayerPattern.RGGB);
        Assert.Equal(400, m.CellsUsed);
        foreach (var list in m.Points.Values) Assert.True(list.Count <= 1);
    }

    [Fact]
    public void Zu_wenige_Zellen_ergeben_leere_Listen()
    {
        var frame = Gradient(18, 10, BayerPattern.RGGB, 20, 220, false); // 45 cells
        var src = new FakeSource(18, 10, 8, 5, 255, _ => frame);
        var m = CurveMeasurement.Measure(src, BayerPattern.RGGB);
        Assert.Equal(45, m.CellsUsed);
        Assert.Equal(4, m.Points.Count);
        Assert.All(m.Points.Values, l => Assert.Empty(l));
    }

    [Fact]
    public void Ungerade_Breite_und_Hoehe_laufen_und_zaehlen_nur_ganze_Zellen()
    {
        var frame = Gradient(32, 26, BayerPattern.RGGB, 20, 220, false);
        // 31 x 25 image: reuse the first 31 columns of each row
        var odd = new ushort[31 * 25];
        for (int y = 0; y < 25; y++) Array.Copy(frame, y * 32, odd, y * 31, 31);
        var src = new FakeSource(31, 25, 8, 5, 255, _ => odd);
        var m = CurveMeasurement.Measure(src, BayerPattern.RGGB);
        Assert.Equal(15 * 12, m.CellsUsed);
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
}
