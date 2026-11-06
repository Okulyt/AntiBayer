using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AntiBayer.Core.Imaging;

namespace AntiBayer.Core;

/// <summary>Reads capture settings from a FireCapture sidecar .txt and from the text fields of a SER header.</summary>
public static class CaptureInfoReader
{
    private static readonly Regex Number = new(@"^\s*[-+]?\d+(?:\.\d+)?", RegexOptions.Compiled);
    private static readonly Regex GainField = new(@"gain=([-+]?\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ExpField = new(@"exp=([-+]?\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>The same-named .txt next to the image wins per field; gaps are filled from the SER header fields.</summary>
    public static CaptureInfo ForFile(string imagePath, SerHeader? header)
    {
        CaptureInfo side = new();
        try
        {
            string txt = Path.ChangeExtension(imagePath, ".txt");
            if (File.Exists(txt))
                side = ParseSidecar(File.ReadAllText(txt, Encoding.Latin1));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // an unreadable sidecar is the same as none
        }

        if (header == null) return side;
        var ser = ParseSerFields(header.Observer, header.Instrument, header.Telescope);
        return new CaptureInfo(
            side.Camera ?? ser.Camera, side.Filter ?? ser.Filter, side.ExposureMs ?? ser.ExposureMs, side.Gain ?? ser.Gain,
            side.WRed ?? ser.WRed, side.WGreen ?? ser.WGreen, side.WBlue ?? ser.WBlue,
            side.TemperatureC ?? ser.TemperatureC, side.Note ?? ser.Note);
    }

    public static CaptureInfo ParseSidecar(string text)
    {
        string? camera = null, filter = null;
        double? exposure = null, gain = null, red = null, green = null, blue = null, temp = null;

        foreach (var raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Contains("Timestamps", StringComparison.OrdinalIgnoreCase)) break;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
            if (value.Length == 0) continue;

            if (key.Equals("Camera", StringComparison.OrdinalIgnoreCase)) camera ??= value;
            else if (key.Equals("Filter", StringComparison.OrdinalIgnoreCase)) filter ??= value;
            else if (key.Equals("Shutter", StringComparison.OrdinalIgnoreCase)) exposure ??= Exposure(value);
            else if (key.Equals("Gain", StringComparison.OrdinalIgnoreCase)) gain ??= Leading(value);
            else if (key.Equals("WRed", StringComparison.OrdinalIgnoreCase)) red ??= Leading(value);
            else if (key.Equals("WGreen", StringComparison.OrdinalIgnoreCase)) green ??= Leading(value);
            else if (key.Equals("WBlue", StringComparison.OrdinalIgnoreCase)) blue ??= Leading(value);
            else if (key.Contains("temp", StringComparison.OrdinalIgnoreCase)) temp ??= Leading(value);
        }
        return new CaptureInfo(camera, filter, exposure, gain, red, green, blue, temp);
    }

    /// <summary>FireCapture abuses the SER text fields: Instrument "VENDOR=Camera", Telescope "fps=..gain=..exp=..".</summary>
    public static CaptureInfo ParseSerFields(string observer, string instrument, string telescope)
    {
        string? camera = null;
        string ins = (instrument ?? "").Trim();
        if (ins.Length > 0)
        {
            int eq = ins.IndexOf('=');
            string name = (eq >= 0 ? ins[(eq + 1)..] : ins).Trim();
            if (name.Length > 0) camera = name;
        }

        double? gain = null, exposure = null;
        string tel = telescope ?? "";
        var g = GainField.Match(tel);
        if (g.Success) gain = Parse(g.Groups[1].Value);
        var e = ExpField.Match(tel);
        if (e.Success) exposure = Parse(e.Groups[1].Value);
        return new CaptureInfo(Camera: camera, ExposureMs: exposure, Gain: gain);
    }

    private static double? Parse(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double? Leading(string s)
    {
        var m = Number.Match(s);
        return m.Success ? Parse(m.Value.Trim()) : null;
    }

    private static double? Exposure(string s)
    {
        var v = Leading(s);
        if (v == null) return null;
        string rest = s.Trim().ToLowerInvariant();
        if (rest.EndsWith("ms")) return v;
        if (rest.EndsWith("us") || rest.EndsWith("\u00B5s")) return v / 1000.0;
        if (rest.EndsWith("s")) return v * 1000.0;
        return v;
    }
}
