using System;
using System.Collections.Generic;

namespace AntiBayer.Core;

public sealed record CaptureInfo(
    string? Camera = null, string? Filter = null, double? ExposureMs = null, double? Gain = null,
    double? WRed = null, double? WGreen = null, double? WBlue = null, double? TemperatureC = null,
    string? Note = null)
{
    /// <summary>Names the fields set on both sides that differ; Note is ignored.</summary>
    public IReadOnlyList<string> DifferencesTo(CaptureInfo other)
    {
        var d = new List<string>();
        Str(d, nameof(Camera), Camera, other.Camera);
        Str(d, nameof(Filter), Filter, other.Filter);
        Num(d, nameof(ExposureMs), ExposureMs, other.ExposureMs);
        Num(d, nameof(Gain), Gain, other.Gain);
        Num(d, nameof(WRed), WRed, other.WRed);
        Num(d, nameof(WGreen), WGreen, other.WGreen);
        Num(d, nameof(WBlue), WBlue, other.WBlue);
        Num(d, nameof(TemperatureC), TemperatureC, other.TemperatureC);
        return d;
    }

    private static void Str(List<string> d, string name, string? a, string? b)
    {
        if (a != null && b != null && !string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) d.Add(name);
    }

    private static void Num(List<string> d, string name, double? a, double? b)
    {
        if (a.HasValue && b.HasValue && a.Value != b.Value) d.Add(name);
    }
}
