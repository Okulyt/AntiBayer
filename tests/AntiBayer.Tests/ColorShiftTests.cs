using System;
using System.Collections.Generic;
using System.Linq;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class ColorShiftTests
{
    private const int W = 120, H = 100;
    private const BayerPattern Pattern = BayerPattern.GBRG;

    private sealed class FakeSource : IFrameSource
    {
        private readonly ushort[] _frame;
        public ImageInfo Info { get; }

        public FakeSource(ushort[] frame, int frames = 3, int depth = 16, ushort white = 65535, int w = W, int h = H)
        {
            _frame = frame;
            Info = new ImageInfo("fake", ImageFormat.Ser, w, h, depth, frames, white, null, new CaptureInfo(), null);
        }

        public void ReadFrame(int index, ushort[] target) => _frame.CopyTo(target, 0);
        public void Dispose() { }
    }

    private static double Scene(double x, double y) =>
        30000 + 9000 * Math.Sin(x * 0.21) + 7000 * Math.Cos(y * 0.17) + 4000 * Math.Sin((x + y) * 0.11);

    /// <summary>Red shows the scene at x + rx, y + ry and blue at x + bx, y + by; green is on the grid.</summary>
    private static ushort[] Frame(double rx, double ry, double bx, double by)
    {
        var f = new ushort[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double v = Bayer.FilterAt(Pattern, x, y) switch
                {
                    BayerFilter.Red => Scene(x + rx, y + ry),
                    BayerFilter.Blue => Scene(x + bx, y + by),
                    _ => Scene(x, y),
                };
                f[y * W + x] = (ushort)Math.Round(v);
            }
        return f;
    }

    private static Dictionary<BayerFilter, CurveTable> Identity() =>
        new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue }
            .ToDictionary(f => f, _ => CurveTable.Build(Array.Empty<CurvePoint>(), 16, 65535));

    private static ColorShift Measure(ushort[] frame) =>
        ColorShiftMeasurement.Measure(new FakeSource(frame), Pattern, Identity())!;

    [Fact]
    public void Messung_findet_den_Versatz_von_Rot_und_Blau()
    {
        var s = Measure(Frame(0.5, -0.3, -0.2, 0.4));
        Assert.InRange(s.RedX, 0.45, 0.55);
        Assert.InRange(s.RedY, -0.35, -0.25);
        Assert.InRange(s.BlueX, -0.25, -0.15);
        Assert.InRange(s.BlueY, 0.35, 0.45);
    }

    [Fact]
    public void Ohne_Versatz_wird_nichts_vorgeschlagen()
    {
        var s = Measure(Frame(0, 0, 0, 0));
        Assert.True(s.Largest < 0.02);
        Assert.False(s.WorthCorrecting);
    }

    [Fact]
    public void Ab_der_Schwelle_wird_die_Korrektur_vorgeschlagen()
    {
        Assert.True(new ColorShift(0.1, 0, 0, 0).WorthCorrecting);
        Assert.True(new ColorShift(0, 0, 0.08, 0.08).WorthCorrecting);
        Assert.False(new ColorShift(0.09, 0, 0, -0.09).WorthCorrecting);
    }

    [Fact]
    public void Messung_ohne_Bildinhalt_liefert_null()
    {
        var flat = Enumerable.Repeat((ushort)1000, W * H).ToArray();
        Assert.Null(ColorShiftMeasurement.Measure(new FakeSource(flat), Pattern, Identity()));
    }

    private static double RmsAgainstScene(ushort[] f, BayerFilter filter)
    {
        double sum = 0; int n = 0;
        for (int y = 8; y < H - 8; y++)
            for (int x = 8; x < W - 8; x++)
                if (Bayer.FilterAt(Pattern, x, y) == filter)
                {
                    double d = f[y * W + x] - Scene(x, y);
                    sum += d * d; n++;
                }
        return Math.Sqrt(sum / n);
    }

    [Fact]
    public void Korrektur_bringt_Rot_und_Blau_auf_das_Gitter_und_laesst_Gruen_unveraendert()
    {
        var raw = Frame(0.6, -0.4, -0.5, 0.3);
        var curved = (ushort[])raw.Clone();
        var shift = Measure(raw);

        new ColorShiftCorrector(Pattern, W, H, shift, 65535).Apply(raw, curved, null);

        Assert.True(RmsAgainstScene(curved, BayerFilter.Red) < RmsAgainstScene(raw, BayerFilter.Red) / 5);
        Assert.True(RmsAgainstScene(curved, BayerFilter.Blue) < RmsAgainstScene(raw, BayerFilter.Blue) / 5);
        for (int p = 0; p < raw.Length; p++)
            if (Bayer.FilterAt(Pattern, p % W, p / W) is BayerFilter.Green1 or BayerFilter.Green2)
                Assert.Equal(raw[p], curved[p]);
    }

    [Fact]
    public void Uebersteuerte_Pixel_bleiben_unveraendert()
    {
        var raw = Frame(0.6, 0, 0.6, 0);
        int red = Enumerable.Range(0, raw.Length).First(p => p % W > 10 && p / W > 10 && Bayer.FilterAt(Pattern, p % W, p / W) == BayerFilter.Red);
        raw[red] = 65535;
        var curved = (ushort[])raw.Clone();

        new ColorShiftCorrector(Pattern, W, H, new ColorShift(0.6, 0, 0.6, 0), 65535).Apply(raw, curved, null);

        Assert.Equal((ushort)65535, curved[red]);
    }

    [Fact]
    public void Der_Rand_bleibt_unveraendert_und_nichts_ueberschreitet_die_Weissgrenze()
    {
        var raw = Frame(0.6, 0.6, 0.6, 0.6);
        var curved = (ushort[])raw.Clone();

        new ColorShiftCorrector(Pattern, W, H, new ColorShift(5, 5, 5, 5), 40000).Apply(raw, curved, null);

        for (int x = 0; x < W; x++)
        {
            Assert.Equal(raw[x], curved[x]);
            Assert.Equal(raw[(H - 1) * W + x], curved[(H - 1) * W + x]);
        }
        Assert.All(Enumerable.Range(2 * W + 2, 100), p =>
        {
            if (raw[p] < 40000) Assert.True(curved[p] <= 40000);
        });
    }

    [Fact]
    public void Ohne_Zufall_ist_die_Korrektur_reproduzierbar_und_der_Zufall_bleibt_im_Mittel_gleich()
    {
        var raw = Frame(0.33, 0.21, 0.13, 0.27);
        var shift = new ColorShift(0.33, 0.21, 0.13, 0.27);
        var a = (ushort[])raw.Clone(); var b = (ushort[])raw.Clone();
        var corrector = new ColorShiftCorrector(Pattern, W, H, shift, 65535);
        corrector.Apply(raw, a, null);
        corrector.Apply(raw, b, null);
        Assert.Equal(a, b);

        var d = (ushort[])raw.Clone();
        corrector.Apply(raw, d, new Random(3));
        Assert.All(Enumerable.Range(0, raw.Length), p => Assert.InRange(d[p] - a[p], -1, 1));
    }

    [Fact]
    public void Die_Steigung_stimmt_bis_nahe_an_den_Rand()
    {
        // A clean slope in x: every red pixel moves by the same amount. The outermost columns have fewer green neighbours, so they are left out.
        var raw = new ushort[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                raw[y * W + x] = (ushort)(10000 + 100 * x);
        var curved = (ushort[])raw.Clone();

        new ColorShiftCorrector(Pattern, W, H, new ColorShift(1, 0, 0, 0), 65535).Apply(raw, curved, null);

        for (int y = 6; y < H - 6; y++)
            for (int x = 6; x < W - 6; x++)
                if (Bayer.FilterAt(Pattern, x, y) == BayerFilter.Red)
                    Assert.Equal(raw[y * W + x] - 100, curved[y * W + x]);
    }

    [Fact]
    public void Ein_Rahmen_kleiner_als_das_Filterfenster_bleibt_unveraendert()
    {
        var raw = new ushort[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var curved = (ushort[])raw.Clone();
        new ColorShiftCorrector(Pattern, 3, 3, new ColorShift(1, 1, 1, 1), 65535).Apply(raw, curved, null);
        Assert.Equal(raw, curved);
    }

    // ---- Parameter set ----

    [Fact]
    public void Der_Parametersatz_wendet_den_Versatz_nur_an_wenn_er_eingeschaltet_ist()
    {
        var set = new ParameterSet(Pattern, 16);
        Assert.Null(set.ActiveShift);
        set.MeasuredShift = new ColorShift(0.5, 0, 0, 0);
        Assert.Null(set.ActiveShift);
        set.ShiftEnabled = true;
        Assert.Equal(set.MeasuredShift, set.ActiveShift);
    }

    [Fact]
    public void Neue_Kurven_und_ein_neues_Muster_verwerfen_den_Versatz()
    {
        ParameterSet Fresh() => new(Pattern, 16) { MeasuredShift = new ColorShift(0.5, 0, 0, 0), ShiftEnabled = true };

        var a = Fresh();
        a.ReplaceConfirmed(new Dictionary<BayerFilter, IReadOnlyList<CurvePoint>>());
        Assert.Null(a.MeasuredShift);
        Assert.False(a.ShiftEnabled);

        var b = Fresh();
        b.ChangePattern(BayerPattern.RGGB);
        Assert.Null(b.MeasuredShift);
        Assert.False(b.ShiftEnabled);

        var c = Fresh();
        c.ChangePattern(Pattern);
        Assert.NotNull(c.MeasuredShift);
    }

    [Fact]
    public void Eine_Messung_im_Ausschnitt_behaelt_den_Versatz()
    {
        var set = new ParameterSet(Pattern, 16) { MeasuredShift = new ColorShift(0.5, 0, 0, 0), ShiftEnabled = true };
        set.MergeMeasured(new Dictionary<BayerFilter, IReadOnlyList<CurvePoint>>
        {
            [BayerFilter.Red] = new[] { new CurvePoint(100, 90) },
        });
        Assert.True(set.ShiftEnabled);
    }

    [Fact]
    public void Der_Versatz_ueberlebt_den_Rundgang_durch_die_Parameterdatei()
    {
        var set = new ParameterSet(Pattern, 16) { MeasuredShift = new ColorShift(0.123456, -0.5, 0.25, 0.75), ShiftEnabled = true };
        string path = System.IO.Path.Combine(TestData.TempDir(), "a.cps");
        CpsFile.Save(path, set);
        var loaded = CpsFile.Load(path);
        Assert.Equal(set.MeasuredShift, loaded.MeasuredShift);
        Assert.True(loaded.ShiftEnabled);
    }

    [Fact]
    public void Eine_Parameterdatei_ohne_Versatz_laedt_mit_ausgeschaltetem_Versatz()
    {
        string path = System.IO.Path.Combine(TestData.TempDir(), "a.cps");
        CpsFile.Save(path, new ParameterSet(Pattern, 16));
        Assert.DoesNotContain("ColorShift", System.IO.File.ReadAllText(path));
        var loaded = CpsFile.Load(path);
        Assert.Null(loaded.MeasuredShift);
        Assert.False(loaded.ShiftEnabled);
    }

    [Fact]
    public void Ein_unvollstaendiger_Versatz_in_der_Parameterdatei_wird_abgelehnt()
    {
        string path = System.IO.Path.Combine(TestData.TempDir(), "a.cps");
        CpsFile.Save(path, new ParameterSet(Pattern, 16) { MeasuredShift = new ColorShift(1, 2, 3, 4) });
        System.IO.File.WriteAllText(path, System.IO.File.ReadAllText(path).Replace("<RedY>2</RedY>", ""));
        Assert.Throws<CpsFormatException>(() => CpsFile.Load(path));
    }

    // ---- Measure here: surface brightness ----

    private static ushort[] SpotOnSurface(int w, int h, double surface, double dark, int spotRight)
    {
        var f = new ushort[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double lum = x < spotRight ? dark + (surface - dark) * x / spotRight : surface;
                double factor = Bayer.FilterAt(Pattern, x, y) switch
                {
                    BayerFilter.Red => 1.05,
                    BayerFilter.Blue => 0.95,
                    _ => 1.0,
                };
                f[y * w + x] = (ushort)Math.Round(lum * factor);
            }
        return f;
    }

    [Fact]
    public void Flaechenhelligkeit_ist_die_Mitte_der_hellen_Haelfte()
    {
        var frame = SpotOnSurface(W, H, 40000, 1000, W / 4);
        double surface = CurveMeasurement.SurfaceLevel(new FakeSource(frame));
        Assert.InRange(surface, 38000, 42000);
    }

    [Fact]
    public void Flaechenhelligkeit_ignoriert_einzelne_heisse_Pixel()
    {
        var frame = SpotOnSurface(W, H, 20000, 1000, W / 4);
        frame[5] = 65000; frame[77] = 65000;
        double surface = CurveMeasurement.SurfaceLevel(new FakeSource(frame));
        Assert.InRange(surface, 19000, 21000);
    }

    [Fact]
    public void Messung_im_Ausschnitt_laesst_die_Flaechenhelligkeit_aus()
    {
        var frame = SpotOnSurface(W, H, 40000, 1000, W / 2);
        var measured = CurveMeasurement.MeasureRegionBelowSurface(
            new FakeSource(frame), Pattern, new PixelRect(0, 0, W, H));

        var points = measured.Points[BayerFilter.Red];
        Assert.NotEmpty(points);
        Assert.All(points, p => Assert.True(p.New < 40000 * CurveMeasurement.SurfaceFraction + 500));
    }

    [Fact]
    public void Ein_Ausschnitt_nur_auf_der_Flaeche_liefert_keine_Punkte()
    {
        var frame = SpotOnSurface(W, H, 40000, 1000, W / 4);
        var measured = CurveMeasurement.MeasureRegionBelowSurface(
            new FakeSource(frame), Pattern, new PixelRect(W / 2, 0, W / 2, H));

        Assert.All(measured.Points.Values, l => Assert.Empty(l));
        Assert.True(measured.CellsUsed > 0);
    }

    [Fact]
    public void Ein_Ausschnitt_auf_der_Flaeche_aendert_die_globale_Kurve_dort_nicht()
    {
        var frame = SpotOnSurface(W, H, 40000, 1000, W / 2);
        var source = new FakeSource(frame);
        var set = new ParameterSet(Pattern, 16);
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(41000, 39500));

        set.MergeMeasured(CurveMeasurement.MeasureRegionBelowSurface(source, Pattern, new PixelRect(0, 0, W, H)).Points);

        Assert.Contains(set.Confirmed(BayerFilter.Red), p => p.Original == 41000 && p.New == 39500);
    }
}
