using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class CpsFileTests
{
    private static string Fixture => Path.Combine(AppContext.BaseDirectory, "Fixtures", "old-8bit.cps");

    private static string TempFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AntiBayerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "test.cps");
    }

    [Fact]
    public void Alte_Datei_laedt_nur_bestaetigte_Punkte()
    {
        var s = CpsFile.Load(Fixture);
        Assert.Equal(new[] { new CurvePoint(100, 120) }, s.Confirmed(BayerFilter.Red));
        Assert.Empty(s.Confirmed(BayerFilter.Green1));
        Assert.Empty(s.Confirmed(BayerFilter.Green2));
        Assert.Empty(s.Confirmed(BayerFilter.Blue));
        Assert.Equal(8, s.BitDepth);
        Assert.False(s.PatternKnown);
        Assert.Equal(BayerPattern.RGGB, s.Pattern);
        Assert.Equal(new CaptureInfo(), s.Capture);
    }

    [Fact]
    public void Alte_Tabellen_werden_nicht_uebernommen()
    {
        var s = CpsFile.Load(Fixture);
        var t = s.BuildConfirmedTables(255)[BayerFilter.Red];
        Assert.True(t.Map(101) > 120);
        Assert.Equal(120, t.Map(100));
    }

    [Fact]
    public void Alte_Datei_ohne_Muster_passt_zu_jedem_Bayer_Muster()
    {
        var s = CpsFile.Load(Fixture);
        foreach (var p in new[] { BayerPattern.RGGB, BayerPattern.GRBG, BayerPattern.GBRG, BayerPattern.BGGR })
            Assert.Equal(SetCompatibility.Ok, s.CompatibleWith(8, p));
        Assert.Equal(SetCompatibility.CurveCountMismatch, s.CompatibleWith(8, BayerPattern.None));
        Assert.Equal(SetCompatibility.BitDepthMismatch, s.CompatibleWith(16, BayerPattern.GRBG));
    }

    [Fact]
    public void Bekanntes_Muster_weicht_weiterhin_ab()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        Assert.True(s.PatternKnown);
        Assert.Equal(SetCompatibility.PatternDiffers, s.CompatibleWith(8, BayerPattern.GRBG));
    }

    [Fact]
    public void Neu_Rundgang_mit_Muster_und_Aufnahmedaten()
    {
        var s = new ParameterSet(BayerPattern.GRBG, 16);
        s.SetConfirmed(BayerFilter.Red, new CurvePoint(1000, 1200));
        s.SetConfirmed(BayerFilter.Red, new CurvePoint(30000, 31000));
        s.SetConfirmed(BayerFilter.Green1, new CurvePoint(5, 7));
        s.SetConfirmed(BayerFilter.Blue, new CurvePoint(65000, 64000));
        s.Capture = new CaptureInfo("QHY5III462C", "IR742", 12.45, 95.5, 52.0, 1.0, 87.25, -10.5,
            "Folie <A> & \"B\" 'ü' ß\nzweite Zeile");
        var path = TempFile();
        CpsFile.Save(path, s);
        var r = CpsFile.Load(path);

        Assert.Equal(BayerPattern.GRBG, r.Pattern);
        Assert.True(r.PatternKnown);
        Assert.Equal(16, r.BitDepth);
        Assert.Equal(s.Capture, r.Capture);
        foreach (var f in new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue })
            Assert.Equal(s.Confirmed(f), r.Confirmed(f));
        Assert.Equal(2, r.Confirmed(BayerFilter.Red).Count);
    }

    [Fact]
    public void Neu_Rundgang_ohne_Muster_und_ohne_Aufnahmedaten()
    {
        var s = new ParameterSet(BayerPattern.None, 8);
        s.SetConfirmed(BayerFilter.All, new CurvePoint(100, 90));
        var path = TempFile();
        CpsFile.Save(path, s);
        var r = CpsFile.Load(path);
        Assert.Equal(BayerPattern.None, r.Pattern);
        Assert.Equal(new[] { new CurvePoint(100, 90) }, r.Confirmed(BayerFilter.All));
        Assert.Equal(new CaptureInfo(), r.Capture);
    }

    [Fact]
    public void Zahlen_werden_invariant_geschrieben()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.Capture = new CaptureInfo(ExposureMs: 12.5);
        var path = TempFile();
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CpsFile.Save(path, s);
            Assert.Contains("12.5", File.ReadAllText(path));
            Assert.Equal(12.5, CpsFile.Load(path).Capture.ExposureMs);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void Neue_Datei_schreibt_keine_Vorschaupunkte()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 120));
        s.SetPreview(BayerFilter.Green1, new CurvePoint(50, 60));
        var path = TempFile();
        CpsFile.Save(path, s);
        var r = CpsFile.Load(path);
        Assert.Empty(r.Confirmed(BayerFilter.Green1));
        Assert.Equal(50, r.BuildPreviewTables(255)[BayerFilter.Green1].Map(50));
        var root = System.Xml.Linq.XDocument.Load(path).Root!;
        Assert.Equal("AntiBayerParameterSet", root.Name.LocalName);
        Assert.Equal("2", (string?)root.Attribute("version"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("das ist kein XML")]
    [InlineData("<Fremd><x/></Fremd>")]
    [InlineData("<AntiBayerParameterSet version=\"2\"><BitDepth>12</BitDepth></AntiBayerParameterSet>")]
    [InlineData("<AntiBayerParameterSet version=\"9\"><BitDepth>8</BitDepth><Pattern>RGGB</Pattern></AntiBayerParameterSet>")]
    public void Kaputte_Datei_wirft_CpsFormatException(string content)
    {
        var path = TempFile();
        File.WriteAllText(path, content);
        Assert.Throws<CpsFormatException>(() => CpsFile.Load(path));
    }

    [Fact]
    public void Fehlende_Datei_wirft_IO_Fehler()
    {
        Assert.ThrowsAny<IOException>(() => CpsFile.Load(Path.Combine(Path.GetTempPath(), "gibt-es-nicht-" + Guid.NewGuid().ToString("N") + ".cps")));
    }

    [Fact]
    public void Ein_neuer_Wert_mit_Nachkommastellen_ueberlebt_den_Rundgang()
    {
        var set = new ParameterSet(BayerPattern.GBRG, 8);
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 97.25));
        string path = Path.Combine(TestData.TempDir(), "a.cps");
        CpsFile.Save(path, set);
        Assert.Equal(97.25, System.Linq.Enumerable.Single(CpsFile.Load(path).Confirmed(BayerFilter.Red)).New);
    }

    [Theory]
    [InlineData("256")]
    [InlineData("255.5")]
    [InlineData("-1")]
    [InlineData("NaN")]
    [InlineData("1e2")]
    public void Ein_ungueltiger_neuer_Wert_wird_abgelehnt(string value)
    {
        var set = new ParameterSet(BayerPattern.GBRG, 8);
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 97));
        string path = Path.Combine(TestData.TempDir(), "a.cps");
        CpsFile.Save(path, set);
        File.WriteAllText(path, File.ReadAllText(path).Replace("new=\"97\"", "new=\"" + value + "\""));
        Assert.Throws<CpsFormatException>(() => CpsFile.Load(path));
    }
}

public class CpsAltdateiAnwendungTests
{
    [Fact]
    public void Alte_Datei_laesst_sich_laden_auf_aktuelles_Muster_umstellen_und_neu_speichern()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "old-8bit.cps");
        var loaded = CpsFile.Load(path);
        Assert.False(loaded.PatternKnown);
        Assert.Equal(SetCompatibility.Ok, loaded.CompatibleWith(8, BayerPattern.GRBG));

        loaded.ChangePattern(BayerPattern.GRBG);
        loaded.PatternKnown = true;
        loaded.Capture = new CaptureInfo(Camera: "Cam", Note: "nach Aufnahme");

        var tmp = Path.Combine(Path.GetTempPath(), "cps_" + Guid.NewGuid().ToString("N") + ".cps");
        try
        {
            CpsFile.Save(tmp, loaded);
            var again = CpsFile.Load(tmp);
            Assert.True(again.PatternKnown);
            Assert.Equal(BayerPattern.GRBG, again.Pattern);
            Assert.Equal("nach Aufnahme", again.Capture.Note);
            foreach (var f in new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue })
                Assert.Equal(loaded.Confirmed(f), again.Confirmed(f));
        }
        finally { File.Delete(tmp); }
    }

    [Theory]
    [InlineData("aufnahme.ser")]
    [InlineData("bild.tif")]
    [InlineData("ohne_endung")]
    [InlineData("werte.cps.txt")]
    public void Speichern_lehnt_andere_Endung_als_cps_ab(string name)
    {
        var dir = Path.Combine(Path.GetTempPath(), "AntiBayerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, "Original");
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        Assert.Throws<ArgumentException>(() => CpsFile.Save(path, s));
        Assert.Equal("Original", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(dir));
    }

    [Fact]
    public void Speichern_nimmt_cps_in_beliebiger_Schreibweise()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AntiBayerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "werte.CPS");
        CpsFile.Save(path, new ParameterSet(BayerPattern.RGGB, 8));
        Assert.True(File.Exists(path));
    }
}
