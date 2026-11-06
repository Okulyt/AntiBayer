using System.Collections.Generic;
using System.Linq;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class HistogramTests
{
    [Fact]
    public void Zaehlt_je_Filterposition_getrennt()
    {
        // RGGB, 4x2: row 0 = R G R G, row 1 = G B G B
        var frame = new ushort[] { 10, 20, 10, 20, 30, 40, 30, 40 };
        var h = Histogram.Compute(frame, 4, 2, BayerPattern.RGGB, 8);
        Assert.Equal(2, h.Of(BayerFilter.Red)[10]);
        Assert.Equal(2, h.Of(BayerFilter.Green1)[20]);
        Assert.Equal(2, h.Of(BayerFilter.Green2)[30]);
        Assert.Equal(2, h.Of(BayerFilter.Blue)[40]);
        Assert.Equal(0, h.Of(BayerFilter.Green1)[30]);
        Assert.Equal(8, h.Of(BayerFilter.Red).Sum() + h.Of(BayerFilter.Green1).Sum()
            + h.Of(BayerFilter.Green2).Sum() + h.Of(BayerFilter.Blue).Sum());
        Assert.Equal(8, h.Of(BayerFilter.All).Sum());
        Assert.Equal(2, h.Max(BayerFilter.Blue));
        Assert.Equal(2, h.MaxAll);
        Assert.Equal(256, h.Of(BayerFilter.Red).Length);
    }

    [Fact]
    public void Sechzehn_Bit_faellt_in_256_Klassen()
    {
        var frame = new ushort[] { 65520, 0, 255, 256 };
        var h = Histogram.Compute(frame, 2, 2, BayerPattern.None, 16);
        var all = h.Of(BayerFilter.All);
        Assert.Equal(256, all.Length);
        Assert.Equal(1, all[255]);
        Assert.Equal(2, all[0]);
        Assert.Equal(1, all[1]);
    }

    [Fact]
    public void Transform_verschiebt_Werte()
    {
        var frame = new ushort[] { 100, 100, 100, 100 };
        var h = Histogram.Compute(frame, 2, 2, BayerPattern.None, 8);
        var identity = CurveTable.Build(new[] { new CurvePoint(0, 0) }, 8, 255);
        var same = h.Transform(new Dictionary<BayerFilter, CurveTable> { [BayerFilter.All] = identity });
        Assert.Equal(h.Of(BayerFilter.All), same.Of(BayerFilter.All));

        var shift = CurveTable.Build(new[] { new CurvePoint(100, 150) }, 8, 255);
        var moved = h.Transform(new Dictionary<BayerFilter, CurveTable> { [BayerFilter.All] = shift });
        Assert.Equal(4, moved.Of(BayerFilter.All)[150]);
        Assert.Equal(0, moved.Of(BayerFilter.All)[100]);
        Assert.Equal(4, h.Of(BayerFilter.All)[100]); // original unchanged
    }

    [Fact]
    public void Balkenhoehe_kleines_Maximum_ohne_Division_durch_null()
    {
        Assert.Equal(100, Histogram.BarHeight(5, 10, 200));
        Assert.Equal(0, Histogram.BarHeight(0, 0, 200));
        Assert.Equal(200, Histogram.BarHeight(3, 3, 200));
        Assert.Equal(66, Histogram.BarHeight(2, 3, 100));
        Assert.Equal(200, Histogram.BarHeight(9, 3, 200)); // never above pixels
    }

    [Fact]
    public void Zaehlt_grosses_Bild_schnell_und_vollstaendig()
    {
        var frame = new ushort[3856 * 2180];
        for (int i = 0; i < frame.Length; i++) frame[i] = (ushort)(i * 7);
        var h = Histogram.Compute(frame, 3856, 2180, BayerPattern.GRBG, 16);
        Assert.Equal(frame.Length, h.Of(BayerFilter.All).Sum());
    }
}
