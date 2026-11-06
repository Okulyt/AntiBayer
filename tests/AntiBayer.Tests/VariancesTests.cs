using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class VariancesTests
{
    private static ushort[] Checker(int w, int h, ushort a, ushort b)
    {
        var f = new ushort[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                f[y * w + x] = ((x + y) & 1) == 0 ? a : b;
        return f;
    }

    [Theory]
    [InlineData(BayerPattern.RGGB)]
    [InlineData(BayerPattern.None)]
    public void Gleichmaessiges_Bild_ergibt_0(BayerPattern p)
    {
        var f = new ushort[8 * 6];
        System.Array.Fill(f, (ushort)1234);
        var r = Variances.Compute(f, 8, 6, p);
        Assert.NotEmpty(r);
        foreach (var v in r.Values)
        {
            Assert.Equal(0, v.Horizontal);
            Assert.Equal(0, v.Vertical);
            Assert.Equal(0, v.Diagonal1);
            Assert.Equal(0, v.Diagonal2);
            Assert.Equal(0, v.Cross);
            Assert.Equal(0, v.X);
            Assert.Equal(0, v.Circle);
        }
    }

    [Theory]
    [InlineData(BayerPattern.RGGB)]
    [InlineData(BayerPattern.GRBG)]
    [InlineData(BayerPattern.GBRG)]
    [InlineData(BayerPattern.BGGR)]
    public void Schachbrett_ergibt_berechnete_Varianz(BayerPattern p)
    {
        const ushort a = 100, b = 40100;
        double d = 40000.0 * 40000.0;
        var r = Variances.Compute(Checker(10, 10, a, b), 10, 10, p);
        Assert.Equal(new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue }, r.Keys);
        foreach (var v in r.Values)
        {
            Assert.Equal(d, v.Horizontal);    // both horizontal neighbours differ by 40000
            Assert.Equal(d, v.Vertical);
            Assert.Equal(0, v.Diagonal1);     // diagonal neighbours have the same value
            Assert.Equal(0, v.Diagonal2);
            Assert.Equal(d, v.Cross);
            Assert.Equal(0, v.X);
            Assert.Equal(d / 2, v.Circle);    // 4 of 8 neighbours differ
        }
    }

    [Fact]
    public void None_liefert_nur_All()
    {
        var r = Variances.Compute(Checker(5, 5, 0, 10), 5, 5, BayerPattern.None);
        Assert.Equal(new[] { BayerFilter.All }, r.Keys);
        Assert.Equal(100, r[BayerFilter.All].Horizontal);
    }

    [Fact]
    public void Position_wird_getrennt_gewertet()
    {
        // 3x3, RGGB: the only interior pixel (1,1) is Blue, so only Blue has values.
        var f = new ushort[] { 0, 0, 0, 0, 5, 0, 0, 0, 0 };
        var r = Variances.Compute(f, 3, 3, BayerPattern.RGGB);
        Assert.Equal(25, r[BayerFilter.Blue].Circle);
        Assert.Equal(0, r[BayerFilter.Red].Circle);
        Assert.Equal(0, r[BayerFilter.Green1].Circle);
        Assert.Equal(0, r[BayerFilter.Green2].Circle);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 5)]
    [InlineData(5, 2)]
    public void Bild_unter_3_Pixel_wirft_nicht_und_liefert_0(int w, int h)
    {
        var r = Variances.Compute(new ushort[w * h], w, h, BayerPattern.RGGB);
        Assert.Equal(4, r.Count);
        foreach (var v in r.Values)
        {
            Assert.Equal(0, v.Circle);
            Assert.False(double.IsNaN(v.Horizontal));
        }
    }
}
