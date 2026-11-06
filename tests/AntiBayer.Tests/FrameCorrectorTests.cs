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
}
