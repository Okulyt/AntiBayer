using System;
using System.Collections.Generic;

namespace AntiBayer.Core;

/// <summary>
/// Per-filter histogram with 256 classes (8 bit: value; 16 bit: value / 256).
/// </summary>
public sealed class Histogram
{
    public const int Classes = 256;

    private readonly long[][] _bins; // indexed by BayerFilter
    private readonly int _bitDepth;
    private readonly BayerPattern _pattern;

    private Histogram(long[][] bins, int bitDepth, BayerPattern pattern)
    {
        _bins = bins; _bitDepth = bitDepth; _pattern = pattern;
    }

    private static int ClassOf(int value, int bitDepth)
        => bitDepth == 16 ? value >> 8 : Math.Min(value, Classes - 1);

    public static Histogram Compute(ushort[] frame, int w, int h, BayerPattern p, int bitDepth)
    {
        if (bitDepth != 8 && bitDepth != 16) throw new ArgumentOutOfRangeException(nameof(bitDepth));
        var bins = NewBins();
        // Cell table instead of Bayer.FilterAt per pixel: (y & 1) * 2 + (x & 1).
        var cell = new long[4][];
        for (int i = 0; i < 4; i++)
            cell[i] = bins[(int)Bayer.FilterAt(p, i & 1, i >> 1)];
        var all = bins[(int)BayerFilter.All];
        bool plain = p == BayerPattern.None;

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            var even = cell[(y & 1) << 1];
            var odd = cell[((y & 1) << 1) | 1];
            for (int x = 0; x < w; x++)
            {
                int c = ClassOf(frame[row + x], bitDepth);
                ((x & 1) == 0 ? even : odd)[c]++;
                if (!plain) all[c]++;
            }
        }
        return new Histogram(bins, bitDepth, p);
    }

    private static long[][] NewBins()
    {
        var bins = new long[5][];
        for (int i = 0; i < 5; i++) bins[i] = new long[Classes];
        return bins;
    }

    public long[] Of(BayerFilter f) => _bins[(int)f];

    public long Max(BayerFilter f)
    {
        long m = 0;
        foreach (var c in _bins[(int)f]) if (c > m) m = c;
        return m;
    }

    /// <summary>Maximum over the channel histograms (R, G1, G2, B; for unfiltered data: All).</summary>
    public long MaxAll
    {
        get
        {
            if (_pattern == BayerPattern.None) return Max(BayerFilter.All);
            long m = 0;
            foreach (var f in new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue })
                m = Math.Max(m, Max(f));
            return m;
        }
    }

    /// <summary>
    /// Histogram after applying the curves. With 256 classes the mapping works on the class
    /// centre value (16 bit: class * 256 + 128), not on each individual value. Filters without
    /// a table are copied unchanged.
    /// </summary>
    public Histogram Transform(IReadOnlyDictionary<BayerFilter, CurveTable> tables)
    {
        var bins = NewBins();
        for (int f = 0; f < 5; f++)
        {
            var src = _bins[f];
            if (!tables.TryGetValue((BayerFilter)f, out var table))
            {
                Array.Copy(src, bins[f], Classes);
                continue;
            }
            for (int c = 0; c < Classes; c++)
            {
                if (src[c] == 0) continue;
                int centre = _bitDepth == 16 ? c * 256 + 128 : c;
                int mapped = table.Map((ushort)centre);
                bins[f][ClassOf(mapped, _bitDepth)] += src[c];
            }
        }
        return new Histogram(bins, _bitDepth, _pattern);
    }

    /// <summary>Bar height in pixels; 0 for an empty histogram, never above <paramref name="pixels"/>.</summary>
    public static int BarHeight(long count, long max, int pixels)
    {
        if (max <= 0 || count <= 0 || pixels <= 0) return 0;
        return (int)Math.Min(pixels, count * pixels / max);
    }
}
