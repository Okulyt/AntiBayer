using System;
using System.IO;
using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class AppConfigTests
{
    private static string TempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "AntiBayerCfg_" + Guid.NewGuid().ToString("N"));
        return d;
    }

    [Fact]
    public void Fehlende_Datei_liefert_Standard_statt_null()
    {
        var cfg = AppConfig.Load(Path.Combine(TempDir(), "gibt-es-nicht.config"));
        Assert.NotNull(cfg);
        Assert.Null(cfg.LastFolder);
        Assert.Equal(BayerPattern.RGGB, cfg.LastPattern);
    }

    [Theory]
    [InlineData("das ist kein json")]
    [InlineData("")]
    [InlineData("{\"LastPattern\":\"Quatsch\",\"LastFolder\":5}")]
    [InlineData("null")]
    public void Kaputte_Datei_liefert_Standard(string content)
    {
        var dir = TempDir();
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "a.config");
            File.WriteAllText(path, content);
            var cfg = AppConfig.Load(path);
            Assert.NotNull(cfg);
            Assert.Null(cfg.LastFolder);
            Assert.Equal(BayerPattern.RGGB, cfg.LastPattern);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Rundgang_legt_Ordner_an()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "tief", "a.config");
            new AppConfig { LastFolder = @"C:\Bilder", LastPattern = BayerPattern.BGGR }.Save(path);
            var cfg = AppConfig.Load(path);
            Assert.Equal(@"C:\Bilder", cfg.LastFolder);
            Assert.Equal(BayerPattern.BGGR, cfg.LastPattern);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Standardpfad_liegt_unter_AppData_AntiBayer()
    {
        Assert.EndsWith(Path.Combine("AntiBayer", "AntiBayer.config"), AppConfig.DefaultPath);
    }
}
