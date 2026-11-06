using System;
using System.Collections.Generic;

namespace AntiBayer.Core;

/// <summary>Mean squared difference to the neighbours of one filter position, per neighbour direction.</summary>
public sealed record FilterVariance(
    double Horizontal, double Vertical, double Diagonal1, double Diagonal2, double Cross, double X, double Circle);

public static class Variances
{
    /// <summary>
    /// Evaluates every pixel that has all eight neighbours, grouped by filter position. The result holds each
    /// filter of the pattern; a filter without an evaluated pixel (image edge below 3 pixels) gets zeros.
    /// </summary>
    public static IReadOnlyDictionary<BayerFilter, FilterVariance> Compute(ushort[] pixels, int width, int height, BayerPattern pattern)
    {
        var horizontal = new double[5];
        var vertical = new double[5];
        var diagonal1 = new double[5];
        var diagonal2 = new double[5];
        var count = new long[5];

        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                int k = (int)Bayer.FilterAt(pattern, x, y);
                int i = y * width + x;
                double c = pixels[i];
                horizontal[k] += Sq(c, pixels[i - 1]) + Sq(c, pixels[i + 1]);
                vertical[k] += Sq(c, pixels[i - width]) + Sq(c, pixels[i + width]);
                diagonal1[k] += Sq(c, pixels[i - width - 1]) + Sq(c, pixels[i + width + 1]);
                diagonal2[k] += Sq(c, pixels[i + width - 1]) + Sq(c, pixels[i - width + 1]);
                count[k]++;
            }

        var filters = pattern == BayerPattern.None
            ? new[] { BayerFilter.All }
            : new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue };
        var result = new Dictionary<BayerFilter, FilterVariance>();
        foreach (var f in filters)
        {
            int k = (int)f;
            double n = Math.Max(count[k], 1);
            double h = horizontal[k], v = vertical[k], d1 = diagonal1[k], d2 = diagonal2[k];
            result[f] = new FilterVariance(
                h / (2 * n), v / (2 * n), d1 / (2 * n), d2 / (2 * n),
                (h + v) / (4 * n), (d1 + d2) / (4 * n), (h + v + d1 + d2) / (8 * n));
        }
        return result;
    }

    private static double Sq(double a, double b) => (a - b) * (a - b);
}
