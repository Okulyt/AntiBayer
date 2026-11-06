using System;
using System.Collections.Generic;

namespace AntiBayer.Core;

public enum BayerPattern { None, RGGB, GRBG, GBRG, BGGR }

public enum BayerFilter { Red = 0, Green1 = 1, Green2 = 2, Blue = 3, All = 4 }

public static class Bayer
{
    // Row-major 2x2 cell per pattern: (0,0), (1,0), (0,1), (1,1).
    private static readonly BayerFilter[][] Cells =
    {
        new[] { BayerFilter.All, BayerFilter.All, BayerFilter.All, BayerFilter.All },
        new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue },
        new[] { BayerFilter.Green1, BayerFilter.Red, BayerFilter.Blue, BayerFilter.Green2 },
        new[] { BayerFilter.Green1, BayerFilter.Blue, BayerFilter.Red, BayerFilter.Green2 },
        new[] { BayerFilter.Blue, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Red },
    };

    public static BayerFilter FilterAt(BayerPattern p, int x, int y)
        => Cells[(int)p][((y & 1) << 1) | (x & 1)];

    /// <summary>Maps a SER ColorID to a pattern; null for unsupported IDs.</summary>
    public static BayerPattern? FromSerColorId(int colorId) => colorId switch
    {
        0 => BayerPattern.None,
        8 => BayerPattern.RGGB,
        9 => BayerPattern.GRBG,
        10 => BayerPattern.GBRG,
        11 => BayerPattern.BGGR,
        _ => null,
    };

    /// <summary>
    /// Mean per filter position over a 3x3 window around (x, y). The window is shifted
    /// to lie fully inside the image; for images smaller than 3 pixels it shrinks.
    /// </summary>
    public static IReadOnlyDictionary<BayerFilter, double> NeighbourhoodMean(
        ushort[] frame, int width, int height, BayerPattern p, int x, int y)
    {
        int w = Math.Min(3, width), h = Math.Min(3, height);
        int x0 = Math.Clamp(x - 1, 0, width - w);
        int y0 = Math.Clamp(y - 1, 0, height - h);

        var sum = new double[5];
        var count = new int[5];
        for (int yy = y0; yy < y0 + h; yy++)
            for (int xx = x0; xx < x0 + w; xx++)
            {
                int k = (int)FilterAt(p, xx, yy);
                sum[k] += frame[yy * width + xx];
                count[k]++;
            }

        var result = new Dictionary<BayerFilter, double>();
        for (int k = 0; k < 5; k++)
            if (count[k] > 0) result[(BayerFilter)k] = sum[k] / count[k];
        return result;
    }
}
