using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiBayer.Core;

public enum SetCompatibility { Ok, PatternDiffers, BitDepthMismatch, CurveCountMismatch }

/// <summary>Confirmed and preview curve points per filter position.</summary>
public sealed class ParameterSet
{
    private readonly Dictionary<BayerFilter, SortedDictionary<ushort, double>> _confirmed = new();
    private readonly Dictionary<BayerFilter, CurvePoint> _preview = new();

    public BayerPattern Pattern { get; private set; }
    public int BitDepth { get; }
    public CaptureInfo Capture { get; set; } = new CaptureInfo();
    /// <summary>False for legacy files that did not store the Bayer pattern; Pattern is then only a placeholder.</summary>
    public bool PatternKnown { get; set; } = true;
    /// <summary>The colour shift measured for this series, if any.</summary>
    public ColorShift? MeasuredShift { get; set; }
    /// <summary>Off by default: the shift is only taken out when the user asks for it.</summary>
    public bool ShiftEnabled { get; set; }
    public ColorShift? ActiveShift => ShiftEnabled ? MeasuredShift : null;

    public void ClearShift()
    {
        MeasuredShift = null;
        ShiftEnabled = false;
    }

    public ParameterSet(BayerPattern pattern, int bitDepth)
    {
        Pattern = pattern;
        BitDepth = bitDepth;
    }

    private static BayerFilter[] FiltersOf(BayerPattern p) => p == BayerPattern.None
        ? new[] { BayerFilter.All }
        : new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue };

    private void Check(BayerFilter f)
    {
        if (!FiltersOf(Pattern).Contains(f))
            throw new ArgumentException($"Filter {f} does not exist for pattern {Pattern}.", nameof(f));
    }

    public IReadOnlyList<CurvePoint> Confirmed(BayerFilter f) =>
        _confirmed.TryGetValue(f, out var d)
            ? d.Select(kv => new CurvePoint(kv.Key, kv.Value)).ToList()
            : new List<CurvePoint>();

    public void SetConfirmed(BayerFilter f, CurvePoint p)
    {
        Check(f);
        if (!_confirmed.TryGetValue(f, out var d)) _confirmed[f] = d = new SortedDictionary<ushort, double>();
        d[p.Original] = p.New;
    }

    public void RemoveConfirmed(BayerFilter f, ushort original)
    {
        if (_confirmed.TryGetValue(f, out var d)) d.Remove(original);
    }

    public void SetPreview(BayerFilter f, CurvePoint p)
    {
        Check(f);
        _preview[f] = p;
    }

    public void ClearPreview() => _preview.Clear();

    /// <summary>Discards all confirmed points, the preview and the colour shift measured on them, then sets the given points.</summary>
    public void ReplaceConfirmed(IReadOnlyDictionary<BayerFilter, IReadOnlyList<CurvePoint>> points)
    {
        foreach (var f in points.Keys) Check(f);
        _confirmed.Clear();
        _preview.Clear();
        ClearShift();
        foreach (var kv in points)
            foreach (var p in kv.Value)
                SetConfirmed(kv.Key, p);
    }

    public CurvePoint? PreviewPoint(BayerFilter f) => _preview.TryGetValue(f, out var q) ? q : null;

    /// <summary>True when a preview point is set; it is not part of the output.</summary>
    public bool HasPreviewPoints => _preview.Count > 0;

    public bool HasConfirmedPoints => _confirmed.Values.Any(d => d.Count > 0);

    /// <summary>Keeps curves within the four Bayer patterns; to and from None they are cleared.</summary>
    public void ChangePattern(BayerPattern p)
    {
        if (p == Pattern) return;
        ClearShift(); // the shift belongs to the positions of red and blue
        if (p == BayerPattern.None || Pattern == BayerPattern.None)
        {
            _confirmed.Clear();
            _preview.Clear();
        }
        Pattern = p;
    }

    public IReadOnlyDictionary<BayerFilter, CurveTable> BuildConfirmedTables(ushort whiteLevel) =>
        FiltersOf(Pattern).ToDictionary(f => f, f => CurveTable.Build(Confirmed(f), BitDepth, whiteLevel));

    public IReadOnlyDictionary<BayerFilter, CurveTable> BuildPreviewTables(ushort whiteLevel) =>
        FiltersOf(Pattern).ToDictionary(f => f, f =>
        {
            var pts = Confirmed(f).ToList();
            if (_preview.TryGetValue(f, out var q))
            {
                pts.RemoveAll(c => c.Original == q.Original);
                pts.Add(q);
            }
            return CurveTable.Build(pts, BitDepth, whiteLevel);
        });

    /// <summary>What the confirmed curve of this filter makes of a value; the preview point plays no part.</summary>
    public ushort ConfirmedCurveValue(BayerFilter f, ushort original, ushort whiteLevel) =>
        CurveTable.Build(Confirmed(f), BitDepth, whiteLevel).Map(original);

    public IReadOnlyList<BayerFilter> NonMonotonicFilters(ushort whiteLevel) =>
        BuildConfirmedTables(whiteLevel).Where(kv => !kv.Value.IsMonotonic).Select(kv => kv.Key).ToList();

    public SetCompatibility CompatibleWith(int bitDepth, BayerPattern pattern)
    {
        if (bitDepth != BitDepth) return SetCompatibility.BitDepthMismatch;
        if ((pattern == BayerPattern.None) != (Pattern == BayerPattern.None)) return SetCompatibility.CurveCountMismatch;
        if (pattern != Pattern && PatternKnown) return SetCompatibility.PatternDiffers;
        return SetCompatibility.Ok;
    }
}
