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
    public const int RegionBins = 6;
    public const int MinSamplesPerPoint = 200;

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

        EmitPoints(points, white, binCount, binRef, binFilter, MinCellsPerPoint);
        return Result(points, frames, cells);
    }

    /// <summary>Walks the classes from dark to bright, collects until enough samples are there and emits one point per filter.</summary>
    private static void EmitPoints(Dictionary<BayerFilter, List<CurvePoint>> points, int white,
        int[] binCount, double[] binRef, double[,] binFilter, int minCount)
    {
        var cRef = 0.0;
        var cCount = 0;
        var cFilter = new double[4];
        for (int b = 0; b < binCount.Length; b++)
        {
            cCount += binCount[b];
            cRef += binRef[b];
            for (int k = 0; k < 4; k++) cFilter[k] += binFilter[b, k];
            if (cCount < minCount) continue;

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
    }

    /// <summary>
    /// Measures within a rectangle: per frame and 2x2 cell one sample (cell mean L, smoothed brightness S over 3x3 cells),
    /// classes by S, so a dark patch is measured against its own brightness and frames may drift.
    /// </summary>
    public static MeasuredCurves MeasureRegion(IFrameSource source, BayerPattern pattern, PixelRect region)
    {
        if (pattern == BayerPattern.None)
            throw new ArgumentException("Curves can only be measured for a Bayer pattern.", nameof(pattern));

        var info = source.Info;
        int w = info.Width, h = info.Height;
        int white = info.WhiteLevel;
        int frames = Math.Min(info.FrameCount, MaxFrames);

        var points = new Dictionary<BayerFilter, List<CurvePoint>>();
        foreach (var f in Filters) points[f] = new List<CurvePoint>();

        int imgCw = w / 2, imgCh = h / 2;
        int cx0 = Math.Max(0, (region.X + 1) / 2), cy0 = Math.Max(0, (region.Y + 1) / 2);
        int cx1 = Math.Min(imgCw, (region.X + region.Width) / 2), cy1 = Math.Min(imgCh, (region.Y + region.Height) / 2);
        if (frames <= 0 || cx1 <= cx0 || cy1 <= cy0)
            return Result(points, Math.Max(frames, 0), 0);

        // Window of cells: region plus one cell around it, limited to the image.
        int wx0 = Math.Max(0, cx0 - 1), wy0 = Math.Max(0, cy0 - 1);
        int wx1 = Math.Min(imgCw, cx1 + 1), wy1 = Math.Min(imgCh, cy1 + 1);
        int ww = wx1 - wx0, wh = wy1 - wy0;

        var buffer = new ushort[w * h];
        var lum = new double[ww * wh];
        var ok = new bool[ww * wh];
        var sL = new List<double>();
        var sS = new List<double>();
        var sF = new List<double>(); // four values per sample, indexed by (int)BayerFilter

        for (int i = 0; i < frames; i++)
        {
            source.ReadFrame((int)((long)i * info.FrameCount / frames), buffer);
            for (int cy = 0; cy < wh; cy++)
                for (int cx = 0; cx < ww; cx++)
                {
                    int x0 = (wx0 + cx) * 2, y0 = (wy0 + cy) * 2;
                    int i00 = y0 * w + x0, i01 = i00 + w;
                    ushort a = buffer[i00], b = buffer[i00 + 1], c = buffer[i01], d = buffer[i01 + 1];
                    int k = cy * ww + cx;
                    ok[k] = a < white && b < white && c < white && d < white;
                    lum[k] = (a + b + c + d) / 4.0;
                }
            for (int cy = cy0; cy < cy1; cy++)
                for (int cx = cx0; cx < cx1; cx++)
                {
                    int k = (cy - wy0) * ww + (cx - wx0);
                    if (!ok[k]) continue;
                    double acc = 0; int n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = cx + dx - wx0, ny = cy + dy - wy0;
                            if (nx < 0 || ny < 0 || nx >= ww || ny >= wh) continue;
                            int nk = ny * ww + nx;
                            if (!ok[nk]) continue;
                            acc += lum[nk]; n++;
                        }
                    int px = cx * 2, py = cy * 2, p0 = py * w + px;
                    var m = new double[4];
                    m[(int)Bayer.FilterAt(pattern, px, py)] = buffer[p0];
                    m[(int)Bayer.FilterAt(pattern, px + 1, py)] = buffer[p0 + 1];
                    m[(int)Bayer.FilterAt(pattern, px, py + 1)] = buffer[p0 + w];
                    m[(int)Bayer.FilterAt(pattern, px + 1, py + 1)] = buffer[p0 + w + 1];
                    sL.Add(lum[k]);
                    sS.Add(acc / n);
                    sF.AddRange(m);
                }
        }

        int samples = sL.Count;
        if (samples < MinSamplesPerPoint) return Result(points, frames, samples);

        double min = double.MaxValue, max = double.MinValue;
        foreach (var s in sS) { if (s < min) min = s; if (s > max) max = s; }

        var binCount = new int[RegionBins];
        var binRef = new double[RegionBins];
        var binFilter = new double[RegionBins, 4];
        for (int c = 0; c < samples; c++)
        {
            int b = max > min ? Math.Min(RegionBins - 1, (int)((sS[c] - min) / (max - min) * RegionBins)) : 0;
            binCount[b]++;
            binRef[b] += sL[c];
            for (int k = 0; k < 4; k++) binFilter[b, k] += sF[c * 4 + k];
        }
        EmitPoints(points, white, binCount, binRef, binFilter, MinSamplesPerPoint);
        return Result(points, frames, samples);
    }

    private static MeasuredCurves Result(Dictionary<BayerFilter, List<CurvePoint>> points, int frames, int cells)
    {
        var dict = new Dictionary<BayerFilter, IReadOnlyList<CurvePoint>>();
        foreach (var kv in points) dict[kv.Key] = kv.Value;
        return new MeasuredCurves(dict, frames, cells);
    }
}
