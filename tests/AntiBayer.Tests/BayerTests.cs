using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class BayerTests
{
    [Theory]
    [InlineData(BayerPattern.RGGB, BayerFilter.Red,    BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue)]
    [InlineData(BayerPattern.GRBG, BayerFilter.Green1, BayerFilter.Red,    BayerFilter.Blue,   BayerFilter.Green2)]
    [InlineData(BayerPattern.GBRG, BayerFilter.Green1, BayerFilter.Blue,   BayerFilter.Red,    BayerFilter.Green2)]
    [InlineData(BayerPattern.BGGR, BayerFilter.Blue,   BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Red)]
    public void FilterAt_liefert_Muster(BayerPattern p, BayerFilter a, BayerFilter b, BayerFilter c, BayerFilter d)
    {
        Assert.Equal(a, Bayer.FilterAt(p, 0, 0));
        Assert.Equal(b, Bayer.FilterAt(p, 1, 0));
        Assert.Equal(c, Bayer.FilterAt(p, 0, 1));
        Assert.Equal(d, Bayer.FilterAt(p, 1, 1));
        Assert.Equal(a, Bayer.FilterAt(p, 2, 2));
    }

    [Fact]
    public void FilterAt_None_ist_All()
    {
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                Assert.Equal(BayerFilter.All, Bayer.FilterAt(BayerPattern.None, x, y));
    }

    [Theory]
    [InlineData(8, BayerPattern.RGGB)]
    [InlineData(9, BayerPattern.GRBG)]
    [InlineData(10, BayerPattern.GBRG)]
    [InlineData(11, BayerPattern.BGGR)]
    [InlineData(0, BayerPattern.None)]
    public void FromSerColorId_bekannte_Werte(int id, BayerPattern expected)
    {
        Assert.Equal(expected, Bayer.FromSerColorId(id));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(12)]
    [InlineData(-1)]
    public void FromSerColorId_unbekannt_ist_null(int id)
    {
        Assert.Null(Bayer.FromSerColorId(id));
    }

    private static ushort[] Frame4x4()
    {
        var f = new ushort[16];
        for (int i = 0; i < 16; i++) f[i] = (ushort)(i * 10);
        return f;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    public void Mittel_am_Rand_enthaelt_alle_Positionen(int x, int y)
    {
        var f = Frame4x4();
        var m = Bayer.NeighbourhoodMean(f, 4, 4, BayerPattern.RGGB, x, y);

        Assert.Equal(4, m.Count);
        // Fenster ist auf (0..2) bzw. (1..3) verschoben; unabhängig berechneter Erwartungswert.
        int x0 = x == 0 ? 0 : 1, y0 = y == 0 ? 0 : 1;
        var sum = new double[4]; var n = new int[4];
        for (int yy = y0; yy < y0 + 3; yy++)
            for (int xx = x0; xx < x0 + 3; xx++)
            {
                int k = (yy % 2) * 2 + (xx % 2);
                sum[k] += f[yy * 4 + xx]; n[k]++;
            }
        Assert.Equal(sum[0] / n[0], m[BayerFilter.Red], 6);
        Assert.Equal(sum[1] / n[1], m[BayerFilter.Green1], 6);
        Assert.Equal(sum[2] / n[2], m[BayerFilter.Green2], 6);
        Assert.Equal(sum[3] / n[3], m[BayerFilter.Blue], 6);
    }

    [Fact]
    public void Mittel_Ecke_0_0_hat_feste_Werte()
    {
        var m = Bayer.NeighbourhoodMean(Frame4x4(), 4, 4, BayerPattern.RGGB, 0, 0);
        // Fenster x0..2, y0..2: Red (0,0),(2,0),(0,2),(2,2) = 0,20,80,100 -> 50
        Assert.Equal(50.0, m[BayerFilter.Red], 6);
        // Green1 (1,0),(1,2) = 10,90 -> 50
        Assert.Equal(50.0, m[BayerFilter.Green1], 6);
        // Green2 (0,1),(2,1) = 40,60 -> 50
        Assert.Equal(50.0, m[BayerFilter.Green2], 6);
        // Blue (1,1) = 50
        Assert.Equal(50.0, m[BayerFilter.Blue], 6);
    }

    [Fact]
    public void Mittel_bei_None_liefert_nur_All()
    {
        var m = Bayer.NeighbourhoodMean(Frame4x4(), 4, 4, BayerPattern.None, 1, 1);
        Assert.Single(m);
        // Fenster 0..2: Mittel aller 9 Werte = 50
        Assert.Equal(50.0, m[BayerFilter.All], 6);
    }

    [Fact]
    public void Mittel_kleines_Bild_laesst_fehlende_Positionen_weg()
    {
        var f = new ushort[] { 10, 20 }; // 2x1
        var m = Bayer.NeighbourhoodMean(f, 2, 1, BayerPattern.RGGB, 0, 0);
        Assert.Equal(2, m.Count);
        Assert.Equal(10.0, m[BayerFilter.Red], 6);
        Assert.Equal(20.0, m[BayerFilter.Green1], 6);
        Assert.False(m.ContainsKey(BayerFilter.Blue));
    }
}
