using System;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class CurveTableTests
{
    [Fact]
    public void Abschnitt_startet_beim_neuen_Grauwert()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(100, 120) }, 8, 255);
        Assert.Equal(120, t.Map(100));
        Assert.True(t.Map(101) > 120);
        Assert.Equal(255, t.Map(255));
    }

    [Fact]
    public void Sechzehn_Bit_ohne_Haenger_und_begrenzt_auf_Weisswert()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(30000, 31000) }, 16, 65520);
        Assert.Equal(31000, t.Map(30000));
        Assert.Equal(65520, t.Map(65520));
        Assert.Equal(65520, t.Map(65535));
    }

    [Fact]
    public void Stuetzpunkt_ueber_Weisswert_wird_ignoriert_und_Weisswert_erzwungen()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(65535, 60000), new CurvePoint(65520, 1000) }, 16, 65520);
        Assert.Equal(65520, t.Map(65520));
        Assert.Equal(65520, t.Map(65535));
        Assert.Equal(32760, t.Map(32760)); // identity between (0,0) and (white,white)
    }

    [Fact]
    public void Null_ist_setzbar()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(0, 10) }, 8, 255);
        Assert.Equal(10, t.Map(0));
        Assert.Equal(255, t.Map(255));
    }

    [Fact]
    public void Dither_trifft_im_Mittel_die_Kurve()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(100, 105) }, 8, 255);
        var r = new Random(1);
        double sum = 0;
        const int n = 100000;
        for (int i = 0; i < n; i++) sum += t.MapDithered(50, r);
        Assert.InRange(sum / n, 52.49, 52.51);
    }

    [Fact]
    public void Dither_ueberschreitet_Weisswert_nie()
    {
        var t = CurveTable.Build(new[] { new CurvePoint(200, 250) }, 8, 250);
        var r = new Random(2);
        for (int v = 0; v < 256; v++)
            for (int k = 0; k < 200; k++)
                Assert.True(t.MapDithered((ushort)v, r) <= 250);
    }

    [Fact]
    public void Identitaet_aendert_keinen_Wert()
    {
        var t = CurveTable.Build(Array.Empty<CurvePoint>(), 16, 65535);
        var r = new Random(3);
        for (int v = 0; v < 65536; v += 17)
        {
            Assert.Equal(v, t.Map((ushort)v));
            Assert.Equal(v, t.MapDithered((ushort)v, r));
        }
        Assert.Equal(65535, t.Map(65535));
        Assert.Equal(65535, t.MapDithered(65535, r));
    }

    [Fact]
    public void Nicht_monoton_wird_gemeldet()
    {
        var bad = CurveTable.Build(new[] { new CurvePoint(100, 200), new CurvePoint(150, 120) }, 8, 255);
        var good = CurveTable.Build(new[] { new CurvePoint(100, 120), new CurvePoint(150, 160) }, 8, 255);
        Assert.False(bad.IsMonotonic);
        Assert.True(good.IsMonotonic);
    }
}
