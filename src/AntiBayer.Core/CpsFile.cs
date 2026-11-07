using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AntiBayer.Core;

public sealed class CpsFormatException : Exception
{
    public CpsFormatException(string message) : base(message) { }
    public CpsFormatException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Reads legacy (DataContract) and current .cps parameter sets and writes the current format.
/// Both are read through XDocument by local name, so the old XML namespaces do not matter.
/// </summary>
public static class CpsFile
{
    private const string NewRoot = "AntiBayerParameterSet";
    private const string OldRoot = "ConversionParameterSet";
    private const int CurrentVersion = 2;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static ParameterSet Load(string path)
    {
        XDocument doc;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            doc = XDocument.Load(fs);
        }
        catch (XmlException ex)
        {
            throw new CpsFormatException("The file is not a valid parameter set (no readable XML).", ex);
        }

        var root = doc.Root ?? throw new CpsFormatException("The file is empty.");
        return root.Name.LocalName switch
        {
            NewRoot => LoadCurrent(root),
            OldRoot => LoadLegacy(root),
            _ => throw new CpsFormatException("The file is not an AntiBayer parameter set."),
        };
    }

    public static void Save(string path, ParameterSet set)
    {
        if (!string.Equals(Path.GetExtension(path), ".cps", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A parameter set can only be saved as a .cps file.", nameof(path));

        var capture = new XElement("Capture");
        var c = set.Capture;
        AddText(capture, "Camera", c.Camera);
        AddText(capture, "Filter", c.Filter);
        AddNum(capture, "ExposureMs", c.ExposureMs);
        AddNum(capture, "Gain", c.Gain);
        AddNum(capture, "WRed", c.WRed);
        AddNum(capture, "WGreen", c.WGreen);
        AddNum(capture, "WBlue", c.WBlue);
        AddNum(capture, "TemperatureC", c.TemperatureC);
        AddText(capture, "Note", c.Note);

        var curves = new XElement("Curves");
        var filters = set.Pattern == BayerPattern.None
            ? new[] { BayerFilter.All }
            : new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue };
        foreach (var f in filters)
        {
            var curve = new XElement("Curve", new XAttribute("filter", f.ToString()));
            foreach (var p in set.Confirmed(f))
                curve.Add(new XElement("Point",
                    new XAttribute("original", p.Original.ToString(Inv)),
                    new XAttribute("new", p.New.ToString(Inv))));
            curves.Add(curve);
        }

        var root = new XElement(NewRoot,
            new XAttribute("version", CurrentVersion.ToString(Inv)),
            new XElement("Pattern", set.Pattern.ToString()),
            new XElement("BitDepth", set.BitDepth.ToString(Inv)),
            capture, curves);
        if (set.MeasuredShift is ColorShift shift)
        {
            var element = new XElement("ColorShift", new XAttribute("enabled", set.ShiftEnabled ? "true" : "false"));
            AddNum(element, "RedX", shift.RedX);
            AddNum(element, "RedY", shift.RedY);
            AddNum(element, "BlueX", shift.BlueX);
            AddNum(element, "BlueY", shift.BlueY);
            root.Add(element);
        }
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

        var tmp = path + ".tmp";
        try
        {
            doc.Save(tmp);
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    private static void AddText(XElement parent, string name, string? value)
    {
        if (value == null) return;
        var clean = new string(value.Where(ch => XmlConvert.IsXmlChar(ch) || char.IsSurrogate(ch)).ToArray());
        parent.Add(new XElement(name, clean));
    }

    private static void AddNum(XElement parent, string name, double? value)
    {
        if (value.HasValue) parent.Add(new XElement(name, value.Value.ToString("R", Inv)));
    }

    // ---- current format -------------------------------------------------------------

    private static ParameterSet LoadCurrent(XElement root)
    {
        if ((string?)root.Attribute("version") != CurrentVersion.ToString(Inv))
            throw new CpsFormatException("Unsupported parameter set version.");

        var pattern = ParsePattern(Child(root, "Pattern"));
        int depth = ParseDepth(Child(root, "BitDepth"));
        var set = new ParameterSet(pattern, depth);

        var cap = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Capture");
        if (cap != null)
        {
            set.Capture = new CaptureInfo(
                Text(cap, "Camera"), Text(cap, "Filter"), Num(cap, "ExposureMs"), Num(cap, "Gain"),
                Num(cap, "WRed"), Num(cap, "WGreen"), Num(cap, "WBlue"), Num(cap, "TemperatureC"),
                Text(cap, "Note"));
        }

        int max = (1 << depth) - 1;
        var curves = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Curves");
        foreach (var curve in curves?.Elements().Where(e => e.Name.LocalName == "Curve") ?? Enumerable.Empty<XElement>())
        {
            if (!Enum.TryParse<BayerFilter>((string?)curve.Attribute("filter"), out var f) || !Enum.IsDefined(f))
                throw new CpsFormatException("Unknown filter in curve.");
            foreach (var pt in curve.Elements().Where(e => e.Name.LocalName == "Point"))
            {
                var p = new CurvePoint(ParseValue((string?)pt.Attribute("original"), max),
                                       ParseValue((string?)pt.Attribute("new"), max));
                AddPoint(set, f, p);
            }
        }

        // Optional: files without it simply have no colour shift.
        var shift = root.Elements().FirstOrDefault(e => e.Name.LocalName == "ColorShift");
        if (shift != null)
        {
            if (pattern == BayerPattern.None)
                throw new CpsFormatException("A colour shift needs a Bayer pattern.");
            set.MeasuredShift = new ColorShift(Required(shift, "RedX"), Required(shift, "RedY"), Required(shift, "BlueX"), Required(shift, "BlueY"));
            set.ShiftEnabled = string.Equals((string?)shift.Attribute("enabled"), "true", StringComparison.OrdinalIgnoreCase);
        }
        return set;
    }

    private static double Required(XElement parent, string name) =>
        Num(parent, name) ?? throw new CpsFormatException($"Missing element {name}.");

    private static string Child(XElement parent, string name) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value
        ?? throw new CpsFormatException($"Missing element {name}.");

    private static string? Text(XElement parent, string name) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;

    private static double? Num(XElement parent, string name)
    {
        var t = Text(parent, name);
        if (t == null) return null;
        if (!double.TryParse(t, NumberStyles.Float, Inv, out var v) || !double.IsFinite(v))
            throw new CpsFormatException($"Invalid number in {name}.");
        return v;
    }

    private static BayerPattern ParsePattern(string text) =>
        Enum.TryParse<BayerPattern>(text, out var p) && Enum.IsDefined(p) && !int.TryParse(text, out _)
            ? p
            : throw new CpsFormatException("Unknown Bayer pattern.");

    private static int ParseDepth(string text) =>
        text == "8" ? 8 : text == "16" ? 16 : throw new CpsFormatException("Unsupported bit depth.");

    private static ushort ParseValue(string? text, int max)
    {
        if (!int.TryParse(text, NumberStyles.None, Inv, out var v) || v > max)
            throw new CpsFormatException("Curve point out of range.");
        return (ushort)v;
    }

    private static void AddPoint(ParameterSet set, BayerFilter f, CurvePoint p)
    {
        try { set.SetConfirmed(f, p); }
        catch (ArgumentException ex) { throw new CpsFormatException("Curve does not match the pattern.", ex); }
    }

    // ---- legacy format --------------------------------------------------------------

    private static ParameterSet LoadLegacy(XElement root)
    {
        var sets = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Sets")
            ?? throw new CpsFormatException("Legacy file without Sets.");

        var filters = new List<(BayerFilter Filter, XElement Value)>();
        foreach (var kv in sets.Elements())
        {
            var key = kv.Elements().FirstOrDefault(e => e.Name.LocalName == "Key")?.Value;
            var value = kv.Elements().FirstOrDefault(e => e.Name.LocalName == "Value");
            if (value == null || !TryLegacyFilter(key, out var f))
                throw new CpsFormatException("Unreadable legacy curve set.");
            filters.Add((f, value));
        }

        BayerPattern pattern;
        if (filters.Count == 1 && filters[0].Filter == BayerFilter.All) pattern = BayerPattern.None;
        else if (filters.Count == 4 && filters.Select(x => x.Filter).Distinct().Count() == 4
                 && filters.All(x => x.Filter != BayerFilter.All)) pattern = BayerPattern.RGGB;
        else throw new CpsFormatException("Legacy file has an unexpected number of curves.");

        int maxGrey = 255;
        var first = Text(filters[0].Value, "MaxGreyLevel");
        if (first != null && !int.TryParse(first, NumberStyles.None, Inv, out maxGrey))
            throw new CpsFormatException("Invalid MaxGreyLevel.");
        int depth = maxGrey == 255 ? 8 : 16;

        // Legacy files do not store the pattern; RGGB is only a placeholder.
        var set = new ParameterSet(pattern, depth) { PatternKnown = false };
        int limit = (1 << depth) - 1;
        foreach (var (f, value) in filters)
        {
            foreach (var sp in value.Descendants().Where(e => e.Name.LocalName == "StrongPoint"))
            {
                if (string.Equals(Text(sp, "PreviewStrongPoint"), "true", StringComparison.OrdinalIgnoreCase)) continue;
                var p = new CurvePoint(ParseValue(Text(sp, "OriginalGreyValue"), limit),
                                       ParseValue(Text(sp, "NewGreyValue"), limit));
                // The automatic end points were added by the old program, not by the user.
                if (p.Original == 0 && p.New == 0) continue;
                if (p.Original == maxGrey && p.New == maxGrey) continue;
                AddPoint(set, f, p);
            }
        }
        return set;
    }

    private static bool TryLegacyFilter(string? key, out BayerFilter f)
    {
        switch (key)
        {
            case "red": f = BayerFilter.Red; return true;
            case "green1": f = BayerFilter.Green1; return true;
            case "green2": f = BayerFilter.Green2; return true;
            case "blue": f = BayerFilter.Blue; return true;
            case "None": f = BayerFilter.All; return true;
            default: f = default; return false;
        }
    }
}
