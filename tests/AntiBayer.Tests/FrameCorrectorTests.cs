using System;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class FrameCorrectorTests
{
    private static ParameterSet GbrgSet()
    {
        var set = new ParameterSet(BayerPattern.GBRG, 8);
        set.SetConfirmed(BayerFilter.Green1, new CurvePoint(100, 110));
        set.SetConfirmed(BayerFilter.Blue, new CurvePoint(100, 130));
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 120));
        set.SetConfirmed(BayerFilter.Green2, new CurvePoint(100, 140));
        return set;
    }

    [Fact]
    public void Jede_Filterposition_nutzt_eigene_Kurve()
    {
        var corrector = new FrameCorrector(BayerPattern.GBRG, GbrgSet().BuildConfirmedTables(255));
        var src = new ushort[16];
        Array.Fill(src, (ushort)100);
        var dst = new ushort[16];
        corrector.Apply(src, dst, 4, 4, null);

        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                ushort expected = Bayer.FilterAt(BayerPattern.GBRG, x, y) switch
                {
                    BayerFilter.Green1 => 110,
                    BayerFilter.Blue => 130,
                    BayerFilter.Red => 120,
                    _ => 140,
                };
                Assert.Equal(expected, dst[y * 4 + x]);
            }
        Assert.Equal(110, dst[0]);
        Assert.Equal(130, dst[1]);
        Assert.Equal(120, dst[4]);
        Assert.Equal(140, dst[5]);
    }

    [Fact]
    public void Ohne_Muster_nutzt_alle_Pixel_die_All_Kurve()
    {
        var set = new ParameterSet(BayerPattern.None, 8);
        set.SetConfirmed(BayerFilter.All, new CurvePoint(100, 120));
        var corrector = new FrameCorrector(BayerPattern.None, set.BuildConfirmedTables(255));
        var src = new ushort[9];
        Array.Fill(src, (ushort)100);
        var dst = new ushort[9];
        corrector.Apply(src, dst, 3, 3, null);
        Assert.All(dst, v => Assert.Equal(120, v));
    }

    [Fact]
    public void Vorschau_ohne_Dither_ist_deterministisch()
    {
        var set = new ParameterSet(BayerPattern.RGGB, 8);
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 121));
        var corrector = new FrameCorrector(BayerPattern.RGGB, set.BuildConfirmedTables(255));
        var src = new ushort[64];
        for (int i = 0; i < src.Length; i++) src[i] = (ushort)(90 + i % 20);
        var a = new ushort[64];
        var b = new ushort[64];
        corrector.Apply(src, a, 8, 8, null);
        corrector.Apply(src, b, 8, 8, null);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Dither_mit_gleichem_Seed_ist_reproduzierbar()
    {
        var set = new ParameterSet(BayerPattern.RGGB, 8);
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 121));
        set.SetConfirmed(BayerFilter.Blue, new CurvePoint(100, 119));
        var corrector = new FrameCorrector(BayerPattern.RGGB, set.BuildConfirmedTables(255));
        var src = new ushort[1024];
        for (int i = 0; i < src.Length; i++) src[i] = (ushort)(50 + i % 100);
        var a = new ushort[1024];
        var b = new ushort[1024];
        var c = new ushort[1024];
        corrector.Apply(src, a, 32, 32, new Random(5));
        corrector.Apply(src, b, 32, 32, new Random(5));
        corrector.Apply(src, c, 32, 32, new Random(6));
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void CountChanged_zaehlt_nur_die_genannte_Filterposition()
    {
        var before = CurveTable.Build(new CurvePoint[0], 8, 255);
        var after = CurveTable.Build(new[] { new CurvePoint(100, 110) }, 8, 255);
        var frame = new ushort[8 * 4];
        Array.Fill(frame, (ushort)100);
        // RGGB in an 8x4 frame: 8 pixels per filter position, 32 without pattern
        Assert.Equal(8, FrameCorrector.CountChanged(frame, 8, 4, BayerPattern.RGGB, BayerFilter.Red, before, after));
        Assert.Equal(8, FrameCorrector.CountChanged(frame, 8, 4, BayerPattern.RGGB, BayerFilter.Blue, before, after));
        Assert.Equal(32, FrameCorrector.CountChanged(frame, 8, 4, BayerPattern.None, BayerFilter.All, before, after));
        frame[0] = 0; // red pixel that both tables map to 0
        Assert.Equal(7, FrameCorrector.CountChanged(frame, 8, 4, BayerPattern.RGGB, BayerFilter.Red, before, after));
    }

    [Fact]
    public void CountChanged_ist_0_bei_gleichen_Tabellen()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(100, 110) }, 8, 255);
        var frame = new ushort[64];
        Array.Fill(frame, (ushort)100);
        Assert.Equal(0, FrameCorrector.CountChanged(frame, 8, 8, BayerPattern.RGGB, BayerFilter.Green1, t, t));
    }

    private static FrameCorrector OnlyRedLowered()
    {
        var tables = new System.Collections.Generic.Dictionary<BayerFilter, CurveTable>();
        foreach (var f in new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue })
            tables[f] = CurveTable.Build(f == BayerFilter.Red ? new[] { new CurvePoint(100, 90) } : Array.Empty<CurvePoint>(), 8, 255);
        return new FrameCorrector(BayerPattern.RGGB, tables);
    }

    [Fact]
    public void Ausschnitt_nutzt_die_Filterposition_im_ganzen_Bild()
    {
        var frame = new ushort[8 * 8];
        Array.Fill(frame, (ushort)100);
        // Rot liegt bei RGGB auf geraden Koordinaten: (2,2) ist die erste Stelle des Ausschnitts.
        var even = OnlyRedLowered().ApplyRegion(frame, 8, new PixelRect(2, 2, 2, 2));
        Assert.Equal(new ushort[] { 90, 100, 100, 100 }, even);
        // Ein ungerader Beginn (Bild ungerader Breite) darf die Farben nicht vertauschen.
        var odd = OnlyRedLowered().ApplyRegion(frame, 8, new PixelRect(1, 1, 2, 2));
        Assert.Equal(new ushort[] { 100, 100, 100, 90 }, odd);
    }

    [Fact]
    public void Ausschnitt_entspricht_der_Korrektur_des_ganzen_Bildes()
    {
        var frame = new ushort[8 * 8];
        for (int i = 0; i < frame.Length; i++) frame[i] = (ushort)(60 + i * 2);
        var whole = new ushort[frame.Length];
        OnlyRedLowered().Apply(frame, whole, 8, 8, null);
        var crop = OnlyRedLowered().ApplyRegion(frame, 8, new PixelRect(2, 4, 3, 2));
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
                Assert.Equal(whole[(4 + y) * 8 + 2 + x], crop[y * 3 + x]);
    }
}
