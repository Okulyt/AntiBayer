using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AntiBayer.Core;
using AntiBayer.Core.Series;
using Xunit;

namespace AntiBayer.Tests;

public class PreflightTests
{
    internal static ParameterSet Set(BayerPattern p = BayerPattern.RGGB, int depth = 8)
    {
        var set = new ParameterSet(p, depth);
        set.SetConfirmed(p == BayerPattern.None ? BayerFilter.All : BayerFilter.Red,
            new CurvePoint(100, depth == 8 ? (ushort)120 : (ushort)30000));
        return set;
    }

    internal static string MakeSer(string dir, string name, int frames = 3, int depth = 8, int colorId = 8, int w = 8, int h = 8)
    {
        string path = Path.Combine(dir, name);
        int mod = depth == 8 ? 200 : 40000;
        TestData.WriteSer(path, w, h, depth, colorId, frames,
            (f, x, y) => (ushort)((f * 31 + x * 7 + y * 13) % mod));
        return path;
    }

    internal static string MakePng(string dir, string name, int bits = 8)
    {
        string path = Path.Combine(dir, name);
        var fmt = bits == 8 ? PixelFormats.Gray8 : PixelFormats.Gray16;
        WicTests.Save(path, BitmapSource.Create(4, 4, 96, 96, fmt, null, new byte[16 * bits / 8], 4 * bits / 8));
        return path;
    }

    internal static SeriesJob Job(IReadOnlyList<string> files, string outDir, ParameterSet? set = null,
        BayerPattern pattern = BayerPattern.RGGB) =>
        new(files, outDir, "BA_", "_k", set ?? Set(pattern), pattern, 7, false);

    private static long Plenty(string _) => long.MaxValue;

    [Fact]
    public void Preflight_lehnt_Ausgabe_gleich_Quelle_ab()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser");
        var r = Preflight.Check(Job(new[] { a }, dir), Plenty);
        Assert.Empty(r.Runnable);
        Assert.Contains(r.Issues, i => i.File == null && i.Excludes);
    }

    [Fact]
    public void Preflight_akzeptiert_passende_Dateien_und_sperrt_keine()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser"), b = MakeSer(dir, "b.ser");
        var r = Preflight.Check(Job(new[] { a, b }, Path.Combine(dir, "out")), Plenty);
        Assert.Equal(new[] { a, b }, r.Runnable);
        Assert.Empty(r.Issues);
        File.Delete(a);
        File.Delete(b);
        Assert.False(File.Exists(a));
    }

    [Fact]
    public void Preflight_meldet_andere_Bittiefe_Dateityp_Farbe_und_abgebrochene_SER()
    {
        string dir = TestData.TempDir();
        string good = MakeSer(dir, "good.ser");
        string deep = MakeSer(dir, "deep.ser", depth: 16);
        string png = MakePng(dir, "p.png");
        string colour = MakeSer(dir, "colour.ser", colorId: 100);
        string cut = MakeSer(dir, "cut.ser");
        using (var fs = new FileStream(cut, FileMode.Open)) fs.SetLength(fs.Length - 24 - 10); // trailer plus part of the last frame

        var r = Preflight.Check(Job(new[] { good, deep, png, colour, cut }, Path.Combine(dir, "out")), Plenty);

        Assert.Equal(new[] { good }, r.Runnable);
        foreach (var f in new[] { deep, png, colour, cut })
            Assert.Contains(r.Issues, i => i.File == f && i.Excludes);
        File.Delete(cut);
        File.Delete(colour);
        File.Delete(deep);
    }

    [Fact]
    public void Preflight_schliesst_Dateien_aus_wenn_Bittiefe_nicht_zum_Parametersatz_passt()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser", depth: 16), b = MakeSer(dir, "b.ser", depth: 16);
        var r = Preflight.Check(Job(new[] { a, b }, Path.Combine(dir, "out"), Set(depth: 8)), Plenty);
        Assert.Empty(r.Runnable);
        Assert.All(new[] { a, b }, f => Assert.Contains(r.Issues, i => i.File == f && i.Excludes));
    }

    [Fact]
    public void Preflight_warnt_bei_ColorID_ungleich_Auswahl_ohne_Ausschluss()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser", colorId: 8); // RGGB
        var r = Preflight.Check(Job(new[] { a }, Path.Combine(dir, "out"), pattern: BayerPattern.GBRG), Plenty);
        Assert.Equal(new[] { a }, r.Runnable);
        var issue = Assert.Single(r.Issues);
        Assert.Equal(a, issue.File);
        Assert.False(issue.Excludes);
    }

    [Fact]
    public void Preflight_meldet_Kurvenanzahl_Konflikt_global()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser", colorId: 0);
        var r = Preflight.Check(Job(new[] { a }, Path.Combine(dir, "out"), Set(BayerPattern.None), BayerPattern.RGGB), Plenty);
        Assert.Empty(r.Runnable);
        Assert.Contains(r.Issues, i => i.File == null && i.Excludes);
    }

    [Fact]
    public void Preflight_meldet_zu_wenig_Speicherplatz()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser");
        var r = Preflight.Check(Job(new[] { a }, Path.Combine(dir, "out")), _ => 10);
        Assert.Empty(r.Runnable);
        Assert.Contains(r.Issues, i => i.File == null && i.Excludes);
    }

    [Fact]
    public void Preflight_prueft_bei_fehlendem_Ausgabeordner_den_naechsten_vorhandenen_Vorfahren()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser");
        string? asked = null;
        var r = Preflight.Check(Job(new[] { a }, Path.Combine(dir, "x", "y")), p => { asked = p; return long.MaxValue; });
        Assert.Equal(new[] { a }, r.Runnable);
        Assert.Equal(Path.GetFullPath(dir).TrimEnd('\\'), Path.GetFullPath(asked!).TrimEnd('\\'));
    }

    [Fact]
    public void Dateiliste_mit_Nichtbild_wird_gemeldet()
    {
        string dir = TestData.TempDir();
        string a = MakeSer(dir, "a.ser");
        string txt = Path.Combine(dir, "notes.txt");
        File.WriteAllText(txt, "hi");
        var r = Preflight.Check(Job(new[] { a, txt }, Path.Combine(dir, "out")), Plenty);
        Assert.Equal(new[] { a }, r.Runnable);
        Assert.Contains(r.Issues, i => i.File == txt && i.Excludes);
    }
}
