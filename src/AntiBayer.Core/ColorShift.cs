using System;
using System.Collections.Generic;
using AntiBayer.Core.Imaging;

namespace AntiBayer.Core;

/// <summary>
/// How far red and blue lie off the green grid, in pixels: red at (x, y) shows what green shows at (x + RedX, y + RedY).
/// Tone curves cannot correct this, because at a steep edge it depends on the place and not on the value.
/// </summary>
public sealed record ColorShift(double RedX, double RedY, double BlueX, double BlueY)
{
    /// <summary>From this size on the correction is offered; below it the series is left alone.</summary>
    public const double SuggestFrom = 0.1;

    public double Largest => Math.Max(Math.Sqrt(RedX * RedX + RedY * RedY), Math.Sqrt(BlueX * BlueX + BlueY * BlueY));

    public bool WorthCorrecting => Largest >= SuggestFrom;
}

/// <summary>Green at every position and its 5x5 mean, from which the local slope of the image is taken.</summary>
internal sealed class GreenField
{
    private readonly int _w, _h;
    private readonly BayerFilter[] _filter;
    private readonly double[] _green, _tmp;

    /// <summary>The 5x5 mean of green.</summary>
    public double[] Smooth { get; }

    public GreenField(BayerPattern pattern, int width, int height)
    {
        _w = width; _h = height;
        int n = width * height;
        _filter = new BayerFilter[n];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _filter[y * width + x] = Bayer.FilterAt(pattern, x, y);
        _green = new double[n];
        _tmp = new double[n];
        Smooth = new double[n];
    }

    public BayerFilter FilterAt(int index) => _filter[index];

    public void Update(ReadOnlySpan<ushort> frame)
    {
        for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                int p = y * _w + x;
                if (_filter[p] is BayerFilter.Green1 or BayerFilter.Green2) { _green[p] = frame[p]; continue; }
                double sum = 0; int count = 0;
                if (x > 0) { sum += frame[p - 1]; count++; }
                if (x < _w - 1) { sum += frame[p + 1]; count++; }
                if (y > 0) { sum += frame[p - _w]; count++; }
                if (y < _h - 1) { sum += frame[p + _w]; count++; }
                _green[p] = count > 0 ? sum / count : frame[p];
            }

        // A running sum: this runs for every frame of a series.
        for (int y = 0; y < _h; y++)
            RunningMean(_green, y * _w, 1, _w, _tmp);
        for (int x = 0; x < _w; x++)
            RunningMean(_tmp, x, _w, _h, Smooth);
    }

    /// <summary>Mean over up to 5 values along one row or column; the window is shortened at the ends.</summary>
    private static void RunningMean(double[] from, int start, int step, int length, double[] to)
    {
        double sum = 0;
        int a = 0, b = -1;
        for (int i = 0; i < length; i++)
        {
            int newB = Math.Min(length - 1, i + 2), newA = Math.Max(0, i - 2);
            while (b < newB) sum += from[start + ++b * step];
            while (a < newA) sum -= from[start + a++ * step];
            to[start + i * step] = sum / (b - a + 1);
        }
    }
}

/// <summary>Measures the colour shift of a series on the curve-corrected frames.</summary>
public static class ColorShiftMeasurement
{
    public const int MaxFrames = 60;
    private const int Margin = 2;
    private const int Terms = 4;

    /// <returns>The shift, or null when the frames hold too little structure to tell.</returns>
    public static ColorShift? Measure(IFrameSource source, BayerPattern pattern, IReadOnlyDictionary<BayerFilter, CurveTable> tables)
    {
        if (pattern == BayerPattern.None)
            throw new ArgumentException("A colour shift can only be measured for a Bayer pattern.", nameof(pattern));

        var info = source.Info;
        int w = info.Width, h = info.Height, white = info.WhiteLevel;
        int frames = Math.Min(info.FrameCount, MaxFrames);
        if (frames <= 0 || w <= 2 * Margin || h <= 2 * Margin) return null;

        var corrector = new FrameCorrector(pattern, tables);
        var field = new GreenField(pattern, w, h);
        var raw = new ushort[w * h];
        var curved = new ushort[w * h];
        var greenAt = new double[w * h];
        var normal = new Dictionary<BayerFilter, double[,]> { [BayerFilter.Red] = new double[Terms, Terms], [BayerFilter.Blue] = new double[Terms, Terms] };
        var right = new Dictionary<BayerFilter, double[]> { [BayerFilter.Red] = new double[Terms], [BayerFilter.Blue] = new double[Terms] };
        var row = new double[Terms];

        for (int i = 0; i < frames; i++)
        {
            source.ReadFrame((int)((long)i * info.FrameCount / frames), raw);
            corrector.Apply(raw, curved, w, h, null);
            field.Update(curved);
            var s = field.Smooth;
            for (int y = Margin; y < h - Margin; y++)
                for (int x = Margin; x < w - Margin; x++)
                {
                    int p = y * w + x;
                    var f = field.FilterAt(p);
                    if (f is not (BayerFilter.Red or BayerFilter.Blue) || raw[p] >= white) continue;
                    double green = (curved[p - 1] + curved[p + 1] + curved[p - w] + curved[p + w]) / 4.0;
                    double d = curved[p] - green;
                    row[0] = 1;
                    row[1] = (s[p + 2] - s[p - 2]) / 4;
                    row[2] = (s[p + 2 * w] - s[p - 2 * w]) / 4;
                    // Blur of the channel is a separate term, so that it does not leak into the shift.
                    row[3] = (s[p + 2] + s[p - 2] + s[p + 2 * w] + s[p - 2 * w] - 4 * s[p]) / 4;
                    var a = normal[f]; var b = right[f];
                    for (int m = 0; m < Terms; m++)
                    {
                        b[m] += row[m] * d;
                        for (int n = 0; n < Terms; n++) a[m, n] += row[m] * row[n];
                    }
                }
        }

        var red = Solve(normal[BayerFilter.Red], right[BayerFilter.Red]);
        var blue = Solve(normal[BayerFilter.Blue], right[BayerFilter.Blue]);
        if (red == null || blue == null) return null;
        return new ColorShift(red[1], red[2], blue[1], blue[2]);
    }

    /// <summary>Gaussian elimination with pivoting; null when the system has no clear solution, as with an image without structure.</summary>
    private static double[]? Solve(double[,] a, double[] b)
    {
        int n = b.Length;
        var m = (double[,])a.Clone();
        var r = (double[])b.Clone();
        double scale = 0;
        for (int i = 1; i < n; i++) scale = Math.Max(scale, Math.Abs(m[i, i]));
        if (scale <= 0) return null;

        for (int p = 0; p < n; p++)
        {
            int best = p;
            for (int q = p + 1; q < n; q++) if (Math.Abs(m[q, p]) > Math.Abs(m[best, p])) best = q;
            if (Math.Abs(m[best, p]) <= scale * 1e-9) return null;
            if (best != p)
            {
                for (int c = 0; c < n; c++) (m[p, c], m[best, c]) = (m[best, c], m[p, c]);
                (r[p], r[best]) = (r[best], r[p]);
            }
            for (int q = p + 1; q < n; q++)
            {
                double f = m[q, p] / m[p, p];
                for (int c = p; c < n; c++) m[q, c] -= f * m[p, c];
                r[q] -= f * r[p];
            }
        }
        var s = new double[n];
        for (int p = n - 1; p >= 0; p--)
        {
            double v = r[p];
            for (int c = p + 1; c < n; c++) v -= m[p, c] * s[c];
            s[p] = v / m[p, p];
        }
        return s;
    }
}

/// <summary>Takes the measured shift out of red and blue after the curves. Green, clipped pixels and the outer two rows stay as they are.</summary>
public sealed class ColorShiftCorrector
{
    private const int Margin = 2;
    private readonly int _w, _h;
    private readonly ushort _white;
    private readonly ColorShift _shift;
    private readonly GreenField _field;

    public ColorShiftCorrector(BayerPattern pattern, int width, int height, ColorShift shift, ushort whiteLevel)
    {
        _w = width; _h = height; _white = whiteLevel; _shift = shift;
        _field = new GreenField(pattern, width, height);
    }

    /// <summary>With dither null the result is rounded and deterministic (preview); otherwise dithered.</summary>
    /// <param name="raw">The frame before the curves, to recognise clipped pixels.</param>
    /// <param name="curved">The frame after the curves; corrected in place.</param>
    public void Apply(ReadOnlySpan<ushort> raw, Span<ushort> curved, Random? dither)
    {
        if (_w <= 2 * Margin || _h <= 2 * Margin) return;
        _field.Update(curved);
        var s = _field.Smooth;
        for (int y = Margin; y < _h - Margin; y++)
            for (int x = Margin; x < _w - Margin; x++)
            {
                int p = y * _w + x;
                var f = _field.FilterAt(p);
                double sx, sy;
                if (f == BayerFilter.Red) { sx = _shift.RedX; sy = _shift.RedY; }
                else if (f == BayerFilter.Blue) { sx = _shift.BlueX; sy = _shift.BlueY; }
                else continue;
                if (raw[p] >= _white) continue;

                double delta = sx * (s[p + 2] - s[p - 2]) / 4 + sy * (s[p + 2 * _w] - s[p - 2 * _w]) / 4;
                double v = Math.Clamp(curved[p] - delta, 0, _white);
                double fl = Math.Floor(v);
                bool up = dither != null ? dither.NextDouble() < v - fl : v - fl >= 0.5;
                curved[p] = (ushort)Math.Min(_white, fl + (up ? 1 : 0));
            }
    }
}
