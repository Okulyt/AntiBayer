using System.IO;

namespace AntiBayer.Core.Series;

public static class OutputNaming
{
    /// <summary>prefix + name + suffix + extension; on collision a counter is added after the suffix (_1, _2, ...).</summary>
    public static string Build(string input, string folder, string prefix, string suffix)
    {
        string stem = Path.GetFileNameWithoutExtension(input);
        string ext = Path.GetExtension(input);
        string baseName = prefix + stem + suffix;
        string candidate = Path.Combine(folder, baseName + ext);
        for (int n = 1; File.Exists(candidate) || Directory.Exists(candidate); n++)
            candidate = Path.Combine(folder, $"{baseName}_{n}{ext}");
        return candidate;
    }
}
