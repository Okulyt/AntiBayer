using System.Linq;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class ParameterSetTests
{
    [Fact]
    public void Vorschaupunkte_erreichen_bestaetigte_Tabellen_nicht()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.SetPreview(BayerFilter.Red, new CurvePoint(100, 150));
        Assert.Equal(100, s.BuildConfirmedTables(255)[BayerFilter.Red].Map(100));
        Assert.Equal(150, s.BuildPreviewTables(255)[BayerFilter.Red].Map(100));
        Assert.False(s.HasConfirmedPoints);
    }

    [Fact]
    public void Vorschaupunkt_gleichen_Werts_zerstoert_bestaetigten_nicht()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 120));
        s.SetPreview(BayerFilter.Red, new CurvePoint(100, 150));
        Assert.Equal(150, s.BuildPreviewTables(255)[BayerFilter.Red].Map(100));
        s.ClearPreview();
        Assert.Contains(new CurvePoint(100, 120), s.Confirmed(BayerFilter.Red));
        Assert.Equal(120, s.BuildPreviewTables(255)[BayerFilter.Red].Map(100));
    }

    [Fact]
    public void Loeschen_wirkt_sofort()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.SetConfirmed(BayerFilter.Blue, new CurvePoint(100, 120));
        Assert.Equal(120, s.BuildConfirmedTables(255)[BayerFilter.Blue].Map(100));
        s.RemoveConfirmed(BayerFilter.Blue, 100);
        Assert.Equal(100, s.BuildConfirmedTables(255)[BayerFilter.Blue].Map(100));
        Assert.False(s.HasConfirmedPoints);
    }

    [Fact]
    public void Nicht_monoton_wird_gemeldet()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.SetConfirmed(BayerFilter.Green2, new CurvePoint(100, 200));
        s.SetConfirmed(BayerFilter.Green2, new CurvePoint(150, 120));
        Assert.Equal(new[] { BayerFilter.Green2 }, s.NonMonotonicFilters(255).ToArray());
    }

    [Fact]
    public void Filtermenge_haengt_vom_Muster_ab()
    {
        var none = new ParameterSet(BayerPattern.None, 8).BuildConfirmedTables(255);
        Assert.Equal(new[] { BayerFilter.All }, none.Keys.ToArray());
        var rggb = new ParameterSet(BayerPattern.RGGB, 8).BuildConfirmedTables(255);
        Assert.Equal(4, rggb.Count);
        Assert.DoesNotContain(BayerFilter.All, rggb.Keys);
    }

    [Fact]
    public void ChangePattern_behaelt_Kurven_zwischen_Bayer_Mustern_und_leert_bei_None()
    {
        var s = new ParameterSet(BayerPattern.RGGB, 8);
        s.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 120));
        s.ChangePattern(BayerPattern.GBRG);
        Assert.Equal(BayerPattern.GBRG, s.Pattern);
        Assert.True(s.HasConfirmedPoints);
        s.ChangePattern(BayerPattern.None);
        Assert.False(s.HasConfirmedPoints);
        s.SetConfirmed(BayerFilter.All, new CurvePoint(10, 20));
        s.ChangePattern(BayerPattern.RGGB);
        Assert.False(s.HasConfirmedPoints);
    }

    [Fact]
    public void CaptureInfo_nennt_nur_beidseitig_gesetzte_abweichende_Felder()
    {
        var a = new CaptureInfo(Camera: "QHY", ExposureMs: 5, Gain: 10, Note: "x");
        var b = new CaptureInfo(Camera: "QHY", ExposureMs: 6, WRed: 1.0, Note: "y");
        Assert.Equal(new[] { "ExposureMs" }, a.DifferencesTo(b).ToArray());
    }

    [Theory]
    [InlineData(16, BayerPattern.RGGB, 8, BayerPattern.RGGB, SetCompatibility.BitDepthMismatch)]
    [InlineData(8, BayerPattern.None, 8, BayerPattern.RGGB, SetCompatibility.CurveCountMismatch)]
    [InlineData(8, BayerPattern.RGGB, 8, BayerPattern.None, SetCompatibility.CurveCountMismatch)]
    [InlineData(8, BayerPattern.GBRG, 8, BayerPattern.RGGB, SetCompatibility.PatternDiffers)]
    [InlineData(8, BayerPattern.RGGB, 8, BayerPattern.RGGB, SetCompatibility.Ok)]
    public void Kompatibilitaet(int setDepth, BayerPattern setPattern, int depth, BayerPattern pattern, SetCompatibility expected)
    {
        var s = new ParameterSet(setPattern, setDepth);
        Assert.Equal(expected, s.CompatibleWith(depth, pattern));
    }
}
