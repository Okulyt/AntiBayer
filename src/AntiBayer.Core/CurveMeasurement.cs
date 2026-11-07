using System;
using System.Collections.Generic;
using AntiBayer.Core.Imaging;

namespace AntiBayer.Core;

public sealed record MeasuredCurves(
    IReadOnlyDictionary<BayerFilter, IReadOnlyList<CurvePoint>> Points, int FramesUsed, int CellsUsed);

/// <summary>Derives per-filter curve points from the series itself: each filter is compared with the mean of its 2x2 cell.</summary>
public static class CurveMeasurement
{
    public const int MaxFrames = 40;
    public const int Bins = 12;
    public const int MinCellsPerPoint = 100;

    private static readonly BayerFilter[] Filters =
        { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue };

    public static MeasuredCurves Measure(IFrameSource source, BayerPattern pattern)
    {
        if (pattern == BayerPattern.None)
            throw new ArgumentException("Curves can only be measured for a Bayer pattern.", nameof(pattern));

        var info = source.Info;
        int w = info.Width, h = info.Height;
        int white = info.WhiteLevel;
        int frames = Math.Min(info.FrameCount, MaxFrames);

        var points = new Dictionary<BayerFilter, List<CurvePoint>>();
        foreach (var f in Filters) points[f] = new List<CurvePoint>();

        int cw = w / 2, ch = h / 2;
        if (frames <= 0 || cw == 0 || ch == 0)
            return Result(points, Math.Max(frames, 0), 0);

        var sum = new double[w * h];
        var over = new bool[w * h];
        var buffer = new ushort[w * h];
        for (int i = 0; i < frames; i++)
        {
            source.ReadFrame((int)((long)i * info.FrameCount / frames), buffer);
            for (int p = 0; p < sum.Length; p++)
            {
                ushort v = buffer[p];
                sum[p] += v;
                if (v >= white) over[p] = true;
            }
        }

        // Cells without a clipped pixel: reference and mean per filter.
        var refs = new List<double>();
        var perFilter = new List<double>(); // four values per cell, indexed by (int)BayerFilter
        for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                int x0 = cx * 2, y0 = cy * 2;
                int i00 = y0 * w + x0, i10 = i00 + 1, i01 = i00 + w, i11 = i01 + 1;
                if (over[i00] || over[i10] || over[i01] || over[i11]) continue;
                double[] m = new double[4];
                m[(int)Bayer.FilterAt(pattern, x0, y0)] = sum[i00] / frames;
                m[(int)Bayer.FilterAt(pattern, x0 + 1, y0)] = sum[i10] / frames;
                m[(int)Bayer.FilterAt(pattern, x0, y0 + 1)] = sum[i01] / frames;
                m[(int)Bayer.FilterAt(pattern, x0 + 1, y0 + 1)] = sum[i11] / frames;
                refs.Add((m[0] + m[1] + m[2] + m[3]) / 4);
                perFilter.AddRange(m);
            }

        int cells = refs.Count;
        if (cells < MinCellsPerPoint) return Result(points, frames, cells);

        double min = double.MaxValue, max = double.MinValue;
        foreach (var r in refs) { if (r < min) min = r; if (r > max) max = r; }

        var binCount = new int[Bins];
        var binRef = new double[Bins];
        var binFilter = new double[Bins, 4];
        for (int c = 0; c < cells; c++)
        {
            int b = max > min ? Math.Min(Bins - 1, (int)((refs[c] - min) / (max - min) * Bins)) : 0;
            binCount[b]++;
            binRef[b] += refs[c];
            for (int k = 0; k < 4; k++) binFilter[b, k] += perFilter[c * 4 + k];
        }

        var cRef = 0.0;
        var cCount = 0;
        var cFilter = new double[4];
        for (int b = 0; b < Bins; b++)
        {
            cCount += binCount[b];
            cRef += binRef[b];
            for (int k = 0; k < 4; k++) cFilter[k] += binFilter[b, k];
            if (cCount < MinCellsPerPoint) continue;

            long newValue = (long)Math.Round(cRef / cCount, MidpointRounding.AwayFromZero);
            foreach (var f in Filters)
            {
                long orig = (long)Math.Round(cFilter[(int)f] / cCount, MidpointRounding.AwayFromZero);
                if (orig == 0 || orig >= white || newValue > ushort.MaxValue) continue;
                var list = points[f];
                if (list.Count > 0 && (orig <= list[^1].Original || newValue < list[^1].New)) continue;
                list.Add(new CurvePoint((ushort)orig, (ushort)newValue));
            }
            cRef = 0; cCount = 0;
            Array.Clear(cFilter);
        }
        return Result(points, frames, cells);
    }

    private static MeasuredCurves Result(Dictionary<BayerFilter, List<CurvePoint>> points, int frames, int cells)
    {
        var dict = new Dictionary<BayerFilter, IReadOnlyList<CurvePoint>>();
        foreach (var kv in points) dict[kv.Key] = kv.Value;
        return new MeasuredCurves(dict, frames, cells);
    }
}
