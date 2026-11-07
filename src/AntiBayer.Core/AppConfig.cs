using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AntiBayer.Core;

/// <summary>Small per-user settings: last folder and last Bayer pattern. A missing or damaged file gives the defaults.</summary>
public sealed class AppConfig
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string? LastFolder { get; set; }
    public BayerPattern LastPattern { get; set; } = BayerPattern.RGGB;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AntiBayer", "AntiBayer.config");

    public static AppConfig Load(string path)
    {
        try
        {
            var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), Options);
            if (cfg == null || !Enum.IsDefined(cfg.LastPattern)) return new AppConfig();
            return cfg;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException
                                      or NotSupportedException or ArgumentException)
        {
            // A missing or unreadable settings file is not an error for the user: start with the defaults.
            return new AppConfig();
        }
    }

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = path + ".tmp";
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Options));
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
