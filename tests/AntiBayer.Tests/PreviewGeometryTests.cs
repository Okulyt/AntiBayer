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

    [Fact]
    public void Klick_im_gezoomten_Bild_trifft_das_Pixel()
    {
        // Box 100x100, image 10x10: scale 10, no margins.
        Assert.Equal((0, 0), PreviewGeometry.ZoomedClickToPixel(0, 0, 100, 100, 10, 10));
        Assert.Equal((9, 9), PreviewGeometry.ZoomedClickToPixel(99, 99, 100, 100, 10, 10));
        Assert.Equal((3, 7), PreviewGeometry.ZoomedClickToPixel(35, 79, 100, 100, 10, 10));
    }

    [Fact]
    public void Klick_auf_den_Rand_neben_dem_gezoomten_Bild_trifft_nichts()
    {
        // Box 100x60, image 10x10: scale 6, drawn 60x60 centred at x=20..79.
        Assert.Null(PreviewGeometry.ZoomedClickToPixel(19, 30, 100, 60, 10, 10));
        Assert.Null(PreviewGeometry.ZoomedClickToPixel(80, 30, 100, 60, 10, 10));
        Assert.Equal((0, 0), PreviewGeometry.ZoomedClickToPixel(20, 0, 100, 60, 10, 10));
        Assert.Equal((9, 9), PreviewGeometry.ZoomedClickToPixel(79, 59, 100, 60, 10, 10));
        Assert.Null(PreviewGeometry.ZoomedClickToPixel(-1, 0, 100, 60, 10, 10));
    }

    [Fact]
    public void Klick_bei_leerer_Box_trifft_nichts()
    {
        Assert.Null(PreviewGeometry.ZoomedClickToPixel(0, 0, 0, 0, 10, 10));
    }

    [Theory]
    [InlineData(400, 300, 100, 80)]
    [InlineData(333, 517, 64, 48)]
    [InlineData(300, 300, 7, 5)]
    [InlineData(650, 410, 128, 96)]
    public void PixelToZoomedRect_Mitte_fuehrt_mit_ZoomedClickToPixel_zum_selben_Pixel(int bw, int bh, int iw, int ih)
    {
        foreach (var (px, py) in new[] { (0, 0), (iw - 1, ih - 1), (iw / 2, ih / 3), (1, ih - 2) })
        {
            var r = PreviewGeometry.PixelToZoomedRect(px, py, bw, bh, iw, ih);
            Assert.NotNull(r);
            var back = PreviewGeometry.ZoomedClickToPixel(r.Value.X + r.Value.Width / 2, r.Value.Y + r.Value.Height / 2, bw, bh, iw, ih);
            Assert.Equal((px, py), back);
        }
    }

    [Theory]
    [InlineData(400, 300, 100, 80)]
    [InlineData(333, 517, 640, 480)]
    public void PixelToZoomedRect_erstes_und_letztes_Pixel_liegen_in_der_gezeichneten_Flaeche(int bw, int bh, int iw, int ih)
    {
        double scale = System.Math.Min((double)bw / iw, (double)bh / ih);
        int dw = (int)(iw * scale), dh = (int)(ih * scale);
        int left = (bw - dw) / 2, top = (bh - dh) / 2;
        var first = PreviewGeometry.PixelToZoomedRect(0, 0, bw, bh, iw, ih)!.Value;
        var last = PreviewGeometry.PixelToZoomedRect(iw - 1, ih - 1, bw, bh, iw, ih)!.Value;
        Assert.True(first.X >= left && first.Y >= top);
        Assert.True(last.X + last.Width <= left + dw && last.Y + last.Height <= top + dh);
        Assert.Null(PreviewGeometry.PixelToZoomedRect(iw, 0, bw, bh, iw, ih));
    }

    [Theory]
    [InlineData(100, 200, 98, 198)]
    [InlineData(101, 200, 98, 198)]
    [InlineData(100, 201, 98, 198)]
    [InlineData(101, 201, 98, 198)]
    public void Lupe_zeigt_drei_mal_drei_Zellen_mit_der_Zelle_des_Pixels_in_der_Mitte(int x, int y, int left, int top)
    {
        Assert.Equal(new PixelRect(left, top, 6, 6), PreviewGeometry.MagnifierRect(x, y, 1000, 1000));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(999, 999, 994, 994)]
    public void Lupe_bleibt_am_Bildrand_im_Bild(int x, int y, int left, int top)
    {
        Assert.Equal(new PixelRect(left, top, 6, 6), PreviewGeometry.MagnifierRect(x, y, 1000, 1000));
    }

    [Fact]
    public void Lupe_enthaelt_bei_ungerader_Bildbreite_auch_das_letzte_Pixel()
    {
        var r = PreviewGeometry.MagnifierRect(6, 6, 7, 7);
        Assert.True(r.X + r.Width > 6 && r.Y + r.Height > 6);
        Assert.True(r.X + r.Width <= 7 && r.Y + r.Height <= 7);
    }

    [Fact]
    public void Lupe_wird_bei_sehr_kleinem_Bild_nicht_groesser_als_das_Bild()
    {
        Assert.Equal(new PixelRect(0, 0, 4, 2), PreviewGeometry.MagnifierRect(3, 1, 4, 2));
    }

    [Fact]
    public void Klick_in_die_Lupe_trifft_jedes_gezeichnete_Pixel()
    {
        var region = new PixelRect(98, 198, 6, 6);
        var layout = PreviewGeometry.MagnifierLayout(187, 185, 6, 6);
        Assert.NotNull(layout);
        var (left, top, size) = layout.Value;
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 6; x++)
            {
                Assert.Equal((98 + x, 198 + y), PreviewGeometry.MagnifierClickToPixel(left + x * size, top + y * size, 187, 185, region));
                Assert.Equal((98 + x, 198 + y), PreviewGeometry.MagnifierClickToPixel(left + x * size + size - 1, top + y * size + size - 1, 187, 185, region));
            }
    }

    [Fact]
    public void Klick_neben_das_Lupenbild_trifft_nichts()
    {
        var region = new PixelRect(98, 198, 6, 6);
        var (left, top, size) = PreviewGeometry.MagnifierLayout(187, 185, 6, 6)!.Value;
        Assert.Null(PreviewGeometry.MagnifierClickToPixel(left - 1, top, 187, 185, region));
        Assert.Null(PreviewGeometry.MagnifierClickToPixel(left, top - 1, 187, 185, region));
        Assert.Null(PreviewGeometry.MagnifierClickToPixel(left + 6 * size, top, 187, 185, region));
        Assert.Null(PreviewGeometry.MagnifierClickToPixel(left, top + 6 * size, 187, 185, region));
    }

    [Fact]
    public void Lupe_in_zu_kleiner_Box_trifft_nichts()
    {
        Assert.Null(PreviewGeometry.MagnifierLayout(10, 10, 6, 6));
        Assert.Null(PreviewGeometry.MagnifierClickToPixel(5, 5, 10, 10, new PixelRect(0, 0, 6, 6)));
    }
}
