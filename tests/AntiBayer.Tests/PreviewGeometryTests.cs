using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class PreviewGeometryTests
{
    [Fact]
    public void Vorschau_beginnt_auf_geraden_Koordinaten()
    {
        foreach (var (iw, ih) in new[] { (136, 116), (137, 117), (640, 480), (31, 17) })
            foreach (var (ww, wh) in new[] { (68, 60), (67, 59), (200, 200), (2, 2) })
                for (int cx = 0; cx < iw; cx += 7)
                    for (int cy = 0; cy < ih; cy += 5)
                    {
                        var r = PreviewGeometry.Compute(cx, cy, ww, wh, iw, ih);
                        Assert.Equal(0, r.X % 2);
                        Assert.Equal(0, r.Y % 2);
                        Assert.True(r.X >= 0 && r.Y >= 0);
                        Assert.True(r.Width > 0 && r.Height > 0);
                        Assert.True(r.X + r.Width <= iw);
                        Assert.True(r.Y + r.Height <= ih);
                    }
    }

    [Fact]
    public void Vorschau_groesser_als_ROI_wird_begrenzt()
    {
        Assert.Equal(new PixelRect(0, 0, 136, 116), PreviewGeometry.Compute(0, 0, 200, 200, 136, 116));
        var r = PreviewGeometry.Compute(135, 115, 68, 60, 136, 116);
        Assert.True(r.X >= 0 && r.Y >= 0 && r.X + r.Width <= 136 && r.Y + r.Height <= 116);
        Assert.Equal(68, r.Width);
        Assert.Equal(60, r.Height);
    }

    [Fact]
    public void Vorschau_zentriert_wenn_moeglich()
    {
        var r = PreviewGeometry.Compute(100, 80, 40, 20, 640, 480);
        Assert.Equal(new PixelRect(80, 70, 40, 20), r);
    }
}
