using System;
using System.Collections.Generic;
using AntiBayer.Core.Imaging;

namespace AntiBayer.Core;

public sealed record MeasuredCurves(
    IReadOnlyDictionary<BayerFilter, IReadOnlyList<CurvePoint>> Points, int FramesUsed, long SamplesUsed);

/// <summary>Derives per-filter curve points from the series itself.</summary>
public static class CurveMeasurement
{
    public const int MaxFrames = 40;
    public const int Bins = 12;
    public const int MinSamplesPerPoint = 200;
    private const int Margin = 2;

    private static readonly BayerFilter[] Filters =
        { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue };

    /// <summary>
    /// Compares every pixel with the [1 2 1]x[1 2 1] mean around it, frame by frame. That mean holds every filter in its
    /// proper share and is centred on the pixel, so a one-sided slope such as the limb does not look like colour; single
    /// frames keep image drift from blurring brightness levels that are rare in the series.
    /// </summary>
    public static MeasuredCurves Measure(IFrameSource source, BayerPattern pattern)
    {
        if (pattern == BayerPattern.None)
            throw new ArgumentException("Curves can only be measured for a Bayer pattern.", nameof(pattern));

        var info = source.Info;
        int w = info.Width, h = info.Height, white = info.WhiteLevel;
        int frames = Math.Min(info.FrameCount, MaxFrames);

        var points = new Dictionary<BayerFilter, List<CurvePoint>>();
        foreach (var f in Filters) points[f] = new List<CurvePoint>();
        if (frames <= 0 || w <= 2 * Margin || h <= 2 * Margin) return Result(points, Math.Max(frames, 0), 0);

        var filterAt = new int[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                filterAt[y * w + x] = (int)Bayer.FilterAt(pattern, x, y);

        var buffer = new ushort[w * h];
        var row = new double[w * h];
        var values = new double[w * h];
        var boxRow = new double[w * h];
        var classOf = new double[w * h];
        var count = new long[4, Bins];
        var sumValue = new double[4, Bins];
        var sumRef = new double[4, Bins];
        double lo = 0, scale = 0;
        long samples = 0;

        for (int i = 0; i < frames; i++)
        {
            source.ReadFrame((int)((long)i * info.FrameCount / frames), buffer);
            if (i == 0) (lo, scale) = ClassRange(buffer);

            for (int p = 0; p < buffer.Length; p++) values[p] = buffer[p];
            for (int y = 0; y < h; y++)
            {
                GreenField.RunningMean(values, y * w, 1, w, boxRow);
                for (int x = 1; x < w - 1; x++)
                {
                    int p = y * w + x;
                    row[p] = (buffer[p - 1] + 2.0 * buffer[p] + buffer[p + 1]) / 4;
                }
            }
            // The class comes from a plain 5x5 mean: any pixel weighs the same in it, so its noise moves
            // the pixel and the reference alike and does not flatten the curve.
            for (int x = 0; x < w; x++) GreenField.RunningMean(boxRow, x, w, h, classOf);

            for (int y = Margin; y < h - Margin; y++)
                for (int x = Margin; x < w - Margin; x++)
                {
                    int p = y * w + x;
                    if (AnyClipped(buffer, p, w, white)) continue;
                    double reference = (row[p - w] + 2 * row[p] + row[p + w]) / 4;
                    int bin = Math.Clamp((int)((classOf[p] - lo) * scale), 0, Bins - 1);
                    int k = filterAt[p];
                    count[k, bin]++;
                    sumValue[k, bin] += buffer[p];
                    sumRef[k, bin] += reference;
                    samples++;
                }
        }

        foreach (var f in Filters) EmitPoints(points[f], (int)f, white, count, sumValue, sumRef);
        return Result(points, frames, samples);
    }

    private static bool AnyClipped(ushort[] frame, int p, int w, int white)
    {
        for (int q = p - w; q <= p + w; q += w)
            if (frame[q - 1] >= white || frame[q] >= white || frame[q + 1] >= white) return true;
        return false;
    }

    /// <summary>The range of the classes, from the first frame; the outermost values do not count, so hot pixels cannot stretch it.</summary>
    private static (double Low, double Scale) ClassRange(ushort[] frame)
    {
        var sorted = (ushort[])frame.Clone();
        Array.Sort(sorted);
        int skip = sorted.Length / 10000;
        double low = sorted[skip], high = sorted[sorted.Length - 1 - skip];
        return (low, high > low ? Bins / (high - low) : 0);
    }

    /// <summary>Walks the classes from dark to bright, collects until enough samples are there and emits one point.</summary>
    private static void EmitPoints(List<CurvePoint> list, int k, int white, long[,] count, double[,] sumValue, double[,] sumRef)
    {
        long n = 0;
        double value = 0, reference = 0;
        for (int b = 0; b < Bins; b++)
        {
            n += count[k, b];
            value += sumValue[k, b];
            reference += sumRef[k, b];
            if (n < MinSamplesPerPoint) continue;

            double meanValue = value / n, meanRef = reference / n;
            n = 0; value = 0; reference = 0;

            // Original is a whole grey value. The curve rises by about 1, so its rounding is carried over to New.
            long original = (long)Math.Round(meanValue, MidpointRounding.AwayFromZero);
            double newValue = Math.Round(Math.Clamp(meanRef + (original - meanValue), 0, white), 2);
            if (original == 0 || original >= white) continue;
            if (list.Count > 0 && (original <= list[^1].Original || newValue < list[^1].New)) continue;
            list.Add(new CurvePoint((ushort)original, newValue));
        }
    }

    private static MeasuredCurves Result(Dictionary<BayerFilter, List<CurvePoint>> points, int frames, long samples)
    {
        var dict = new Dictionary<BayerFilter, IReadOnlyList<CurvePoint>>();
        foreach (var kv in points) dict[kv.Key] = kv.Value;
        return new MeasuredCurves(dict, frames, samples);
    }
}
