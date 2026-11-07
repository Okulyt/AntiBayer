using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AntiBayer.Core.Imaging;

namespace AntiBayer.Core.Series;

public static class Preflight
{
    /// <summary>Reads only the headers of all files. freeBytes (path -> free bytes) is replaceable for tests.</summary>
    public static PreflightResult Check(SeriesJob job, Func<string, long>? freeBytes = null)
    {
        var issues = new List<PreflightIssue>();
        var none = new PreflightResult(Array.Empty<string>(), issues);

        if (job.Files.Count == 0)
        {
            issues.Add(new PreflightIssue(null, "No files were selected.", true));
            return none;
        }

        string outFull = NormalizeFolder(job.OutputFolder);
        if (job.Files.Any(f => string.Equals(NormalizeFolder(Path.GetDirectoryName(Path.GetFullPath(f)) ?? ""), outFull,
                StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new PreflightIssue(null, "The output folder must not be the folder of a source file.", true));
            return none;
        }

        if (job.Set.CompatibleWith(job.Set.BitDepth, job.Pattern) == SetCompatibility.CurveCountMismatch)
        {
            issues.Add(new PreflightIssue(null,
                "The parameter set has a different number of curves than the selected Bayer pattern needs.", true));
            return none;
        }

        var runnable = new List<string>();
        ImageFormat? refFormat = null;
        int refDepth = 0;

        foreach (var file in job.Files)
        {
            if (!ImageFiles.IsSupportedExtension(file))
            {
                issues.Add(new PreflightIssue(file, "This is not a supported image file (.ser, .tif, .tiff, .png, .fit, .fits, .fts).", true));
                continue;
            }

            ImageInfo info;
            bool truncated;
            try
            {
                using var source = ImageFiles.Open(file, job.SwapBytes);
                info = source.Info;
                truncated = source is SerSource ser && ser.IsTruncated;
            }
            catch (Exception e) when (e is UnsupportedImageException or IOException or UnauthorizedAccessException
                                          or ArgumentException or NotSupportedException)
            {
                issues.Add(new PreflightIssue(file, "The file cannot be read: " + e.Message, true));
                continue;
            }

            if (truncated)
            {
                issues.Add(new PreflightIssue(file, "The SER file is incomplete and cannot be saved.", true));
                continue;
            }
            refFormat ??= info.Format;
            if (refDepth == 0) refDepth = info.BitDepth;
            if (info.Format != refFormat)
            {
                issues.Add(new PreflightIssue(file, "A different file type than the first file.", true));
                continue;
            }
            if (info.BitDepth != refDepth)
            {
                issues.Add(new PreflightIssue(file, "A different bit depth than the first file.", true));
                continue;
            }
            if (info.BitDepth != job.Set.BitDepth)
            {
                issues.Add(new PreflightIssue(file,
                    $"The file has {info.BitDepth} bit but the parameter set is for {job.Set.BitDepth} bit.", true));
                continue;
            }

            runnable.Add(file);
            if (info.SuggestedPattern is { } suggested && suggested != job.Pattern)
                issues.Add(new PreflightIssue(file,
                    $"The file says its pattern is {suggested}; the selected pattern {job.Pattern} is used.", false));
        }

        if (runnable.Count > 0)
        {
            long needed = 0;
            foreach (var f in runnable)
            {
                try { needed += new FileInfo(f).Length; } catch (IOException) { }
            }
            long? free = FreeSpace(job.OutputFolder, freeBytes);
            if (free is { } fb && fb < needed)
            {
                issues.Add(new PreflightIssue(null,
                    $"Not enough free disk space: about {needed / (1024 * 1024)} MB are needed, {fb / (1024 * 1024)} MB are free.", true));
                return new PreflightResult(Array.Empty<string>(), issues);
            }
        }

        return new PreflightResult(runnable, issues);
    }

    private static string NormalizeFolder(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static long? FreeSpace(string outputFolder, Func<string, long>? freeBytes)
    {
        string? probe = Path.GetFullPath(outputFolder);
        while (probe != null && !Directory.Exists(probe)) probe = Path.GetDirectoryName(probe);
        if (probe == null) return null;
        try
        {
            if (freeBytes != null) return freeBytes(probe);
            return new DriveInfo(Path.GetPathRoot(probe)!).AvailableFreeSpace;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return null; // unknown (e.g. network path): do not block
        }
    }
}
