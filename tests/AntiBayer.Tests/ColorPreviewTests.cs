using System;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class ColorPreviewTests
{
    private static ushort[] Flat(int w, int h, BayerPattern pattern)
    {
        var frame = new ushort[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                frame[y * w + x] = Bayer.FilterAt(pattern, x, y) switch
                {
                    BayerFilter.Red => 200,
                    BayerFilter.Green1 => 100,
                    BayerFilter.Green2 => 80,
                    _ => 50,
                };
        return frame;
    }

    [Fact]
    public void Gleichmaessige_Flaeche_wird_grau_trotz_verschiedener_Filterwerte()
    {
        var frame = Flat(12, 12, BayerPattern.GBRG);
        var rgb = ColorPreview.ToRgb(frame, 12, 12, BayerPattern.GBRG, new PixelRect(2, 2, 8, 8), 255);
        Assert.Equal(3 * 8 * 8, rgb.Length);
        for (int i = 0; i < rgb.Length; i += 3)
        {
            Assert.Equal(rgb[i], rgb[i + 1]);
            Assert.Equal(rgb[i], rgb[i + 2]);
        }
    }

    [Fact]
    public void Zu_helles_Blau_an_einer_Stelle_wird_dort_blau_und_nicht_anderswo()
    {
        const int w = 16, h = 16;
        var frame = Flat(w, h, BayerPattern.RGGB);
        for (int y = 8; y < 12; y++)
            for (int x = 8; x < 12; x++)
                if (Bayer.FilterAt(BayerPattern.RGGB, x, y) == BayerFilter.Blue) frame[y * w + x] = 90;
        var rgb = ColorPreview.ToRgb(frame, w, h, BayerPattern.RGGB, new PixelRect(0, 0, w, h), 255);
        int Blue(int x, int y) => rgb[3 * (y * w + x) + 2];
        int Green(int x, int y) => rgb[3 * (y * w + x) + 1];
        Assert.True(Blue(10, 10) > Green(10, 10) + 20);
        Assert.True(Math.Abs(Blue(2, 2) - Green(2, 2)) < 20);
        Assert.True(Blue(10, 10) > Blue(2, 2));
    }

    [Fact]
    public void Ausschnitt_mit_ungeradem_Beginn_vertauscht_die_Farben_nicht()
    {
        var frame = Flat(12, 12, BayerPattern.GBRG);
        var rgb = ColorPreview.ToRgb(frame, 12, 12, BayerPattern.GBRG, new PixelRect(3, 3, 5, 5), 255);
        for (int i = 0; i < rgb.Length; i += 3)
        {
            Assert.InRange(Math.Abs(rgb[i] - rgb[i + 1]), 0, 1);
            Assert.InRange(Math.Abs(rgb[i + 2] - rgb[i + 1]), 0, 1);
        }
    }

    [Fact]
    public void Ohne_Muster_gibt_es_keine_Farbansicht()
    {
        Assert.Throws<ArgumentException>(() =>
            ColorPreview.ToRgb(new ushort[16], 4, 4, BayerPattern.None, new PixelRect(0, 0, 4, 4), 255));
    }

    [Fact]
    public void Bildrand_und_Skala_Null_stuerzen_nicht_ab()
    {
        var frame = Flat(4, 4, BayerPattern.BGGR);
        Assert.Equal(48, ColorPreview.ToRgb(frame, 4, 4, BayerPattern.BGGR, new PixelRect(0, 0, 4, 4), 255).Length);
        Assert.All(ColorPreview.ToRgb(frame, 4, 4, BayerPattern.BGGR, new PixelRect(0, 0, 4, 4), 0), b => Assert.Equal(0, b));
    }

    [Fact]
    public void Die_Versatzkorrektur_nimmt_den_Farbsaum_aus_der_Farbansicht()
    {
        const int w = 64, h = 32;
        const double shiftX = 0.6;
        var pattern = BayerPattern.GBRG;
        double Edge(double x) => 20000 + 25000 * (1 + Math.Tanh((x - 32) / 4)) / 2;
        ushort[] Scene(double shift)
        {
            var f = new ushort[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    f[y * w + x] = (ushort)Math.Round(Edge(Bayer.FilterAt(pattern, x, y) == BayerFilter.Red ? x + shift : x));
            return f;
        }
        var frame = Scene(shiftX);

        var corrected = (ushort[])frame.Clone();
        new ColorShiftCorrector(pattern, w, h, new ColorShift(shiftX, 0, 0, 0), 65535).Apply(frame, corrected, null);

        var region = new PixelRect(8, 8, 48, 16);
        var before = ColorPreview.ToRgb(frame, w, h, pattern, region, 65535);
        var after = ColorPreview.ToRgb(corrected, w, h, pattern, region, 65535);
        // At a steep edge the view shows a little colour even without any shift, because each colour is sampled at other places.
        var clean = ColorPreview.ToRgb(Scene(0), w, h, pattern, region, 65535);

        static int Fringe(byte[] rgb)
        {
            int worst = 0;
            for (int i = 0; i < rgb.Length; i += 3) worst = Math.Max(worst, Math.Abs(rgb[i] - rgb[i + 1]));
            return worst;
        }
        Assert.True(Fringe(before) >= Fringe(clean) + 3, $"vorher {Fringe(before)}, ohne Versatz {Fringe(clean)}");
        Assert.True(Fringe(after) <= Fringe(clean) + 1, $"nachher {Fringe(after)}, ohne Versatz {Fringe(clean)}");
    }
}
