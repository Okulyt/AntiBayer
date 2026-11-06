using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiBayer.Core;

public readonly record struct CurvePoint(ushort Original, ushort New);

/// <summary>Piecewise-linear tone curve as lookup table: integer part plus fraction per input value.</summary>
public sealed class CurveTable
{
    private readonly ushort[] _floor;
    private readonly float[] _frac;
    private readonly ushort _white;

    public bool IsMonotonic { get; }

    private CurveTable(ushort[] floor, float[] frac, ushort white, bool monotonic)
    {
        _floor = floor; _frac = frac; _white = white; IsMonotonic = monotonic;
    }

    public static CurveTable Build(IEnumerable<CurvePoint> points, int bitDepth, ushort whiteLevel)
    {
        if (bitDepth != 8 && bitDepth != 16) throw new ArgumentOutOfRangeException(nameof(bitDepth));
        int size = 1 << bitDepth;
        if (whiteLevel >= size) throw new ArgumentOutOfRangeException(nameof(whiteLevel));

        // Points at or above the white level are dropped; the white level itself is forced to (white, white).
        var byOriginal = new SortedDictionary<int, int>();
        foreach (var p in points)
            if (p.Original < whiteLevel) byOriginal[p.Original] = Math.Min((int)p.New, (int)whiteLevel);
        byOriginal[whiteLevel] = whiteLevel;
        if (!byOriginal.ContainsKey(0)) byOriginal[0] = 0;

        var xs = byOriginal.Keys.ToArray();
        var ys = byOriginal.Values.ToArray();
        bool monotonic = true;
        for (int i = 1; i < ys.Length; i++)
            if (ys[i] < ys[i - 1]) { monotonic = false; break; }

        var floor = new ushort[size];
        var frac = new float[size];
        int seg = 0;
        for (int v = 0; v < size; v++)
        {
            if (v >= whiteLevel) { floor[v] = whiteLevel; continue; }
            while (xs[seg + 1] <= v) seg++;
            double a = xs[seg], b = xs[seg + 1], A = ys[seg], B = ys[seg + 1];
            double f = Math.Clamp(A + (v - a) * (B - A) / (b - a), 0, whiteLevel);
            double fl = Math.Floor(f);
            floor[v] = (ushort)fl;
            frac[v] = (float)(f - fl);
        }
        return new CurveTable(floor, frac, whiteLevel, monotonic);
    }

    private int Index(ushort v) => Math.Min((int)v, _floor.Length - 1);

    /// <summary>Rounded mapping without randomness (preview).</summary>
    public ushort Map(ushort v)
    {
        int i = Index(v);
        int r = _floor[i] + (_frac[i] >= 0.5f ? 1 : 0);
        return (ushort)Math.Min(r, (int)_white);
    }

    /// <summary>floor(f) + (random below frac(f) ? 1 : 0), limited to the white level.</summary>
    public ushort MapDithered(ushort v, Random r)
    {
        int i = Index(v);
        int o = _floor[i] + (r.NextDouble() < _frac[i] ? 1 : 0);
        return (ushort)Math.Min(o, (int)_white);
    }
}
