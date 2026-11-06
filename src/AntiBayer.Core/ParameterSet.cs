using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiBayer.Core;

public enum SetCompatibility { Ok, PatternDiffers, BitDepthMismatch, CurveCountMismatch }

/// <summary>Confirmed and preview curve points per filter position.</summary>
public sealed class ParameterSet
{
    private readonly Dictionary<BayerFilter, SortedDictionary<ushort, ushort>> _confirmed = new();
    private readonly Dictionary<BayerFilter, CurvePoint> _preview = new();

    public BayerPattern Pattern { get; private set; }
    public int BitDepth { get; }
    public CaptureInfo Capture { get; set; } = new CaptureInfo();

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
        if (!_confirmed.TryGetValue(f, out var d)) _confirmed[f] = d = new SortedDictionary<ushort, ushort>();
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

    public bool HasConfirmedPoints => _confirmed.Values.Any(d => d.Count > 0);

    /// <summary>Keeps curves within the four Bayer patterns; to and from None they are cleared.</summary>
    public void ChangePattern(BayerPattern p)
    {
        if (p == Pattern) return;
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

    public IReadOnlyList<BayerFilter> NonMonotonicFilters(ushort whiteLevel) =>
        BuildConfirmedTables(whiteLevel).Where(kv => !kv.Value.IsMonotonic).Select(kv => kv.Key).ToList();

    public SetCompatibility CompatibleWith(int bitDepth, BayerPattern pattern)
    {
        if (bitDepth != BitDepth) return SetCompatibility.BitDepthMismatch;
        if ((pattern == BayerPattern.None) != (Pattern == BayerPattern.None)) return SetCompatibility.CurveCountMismatch;
        if (pattern != Pattern) return SetCompatibility.PatternDiffers;
        return SetCompatibility.Ok;
    }
}
