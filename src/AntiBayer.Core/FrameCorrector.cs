using System;
using System.Collections.Generic;

namespace AntiBayer.Core;

/// <summary>Applies the per-filter tone curves to a frame.</summary>
public sealed class FrameCorrector
{
    // Row-major 2x2 cell: (0,0), (1,0), (0,1), (1,1).
    private readonly CurveTable[] _cell = new CurveTable[4];

    public FrameCorrector(BayerPattern pattern, IReadOnlyDictionary<BayerFilter, CurveTable> tables)
    {
        for (int i = 0; i < 4; i++)
        {
            var filter = Bayer.FilterAt(pattern, i & 1, i >> 1);
            if (!tables.TryGetValue(filter, out var t))
                throw new ArgumentException($"No curve for filter {filter}.", nameof(tables));
            _cell[i] = t;
        }
    }

    /// <summary>With dither null the mapping is rounded and deterministic (preview); otherwise dithered in pixel order.</summary>
    public void Apply(ReadOnlySpan<ushort> src, Span<ushort> dst, int width, int height, Random? dither)
    {
        int n = width * height;
        if (src.Length < n || dst.Length < n) throw new ArgumentException("Frame is too small.");

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            var even = _cell[(y & 1) << 1];
            var odd = _cell[((y & 1) << 1) | 1];
            for (int x = 0; x < width; x++)
            {
                var t = (x & 1) == 0 ? even : odd;
                ushort v = src[row + x];
                dst[row + x] = dither == null ? t.Map(v) : t.MapDithered(v, dither);
            }
        }
    }
}
