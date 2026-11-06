using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using AntiBayer.Core.Series;
using Xunit;

namespace AntiBayer.Tests;

public class SeriesRunnerTests
{
    private sealed class SyncProgress : IProgress<SeriesProgress>
    {
        private readonly Action<SeriesProgress> _a;
        public SyncProgress(Action<SeriesProgress> a) => _a = a;
        public void Report(SeriesProgress value) => _a(value);
    }

    private sealed class FailingSink : IFrameSink
    {
        private readonly IFrameSink _inner;
        public FailingSink(IFrameSink inner) => _inner = inner;
        public void WriteFrame(ReadOnlySpan<ushort> frame)
        {
            _inner.WriteFrame(frame);
            throw new IOException("disk full", unchecked((int)0x80070070));
        }
        public void Complete() => _inner.Complete();
        public void Dispose() => _inner.Dispose();
    }

    private static SeriesJob Job(IReadOnlyList<string> files, string outDir, ParameterSet? set = null, int seed = 7) =>
        new(files, outDir, "BA_", "_k", set ?? DitherSet(), BayerPattern.RGGB, seed, false);

    private static ParameterSet DitherSet()
    {
        var set = new ParameterSet(BayerPattern.RGGB, 8);
        set.SetConfirmed(BayerFilter.Red, new CurvePoint(100, 121));
        set.SetConfirmed(BayerFilter.Blue, new CurvePoint(100, 119));
        set.SetConfirmed(BayerFilter.Green1, new CurvePoint(60, 71));
        return set;
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string[] Leftovers(string dir) =>
        Directory.GetFiles(dir, "*.tmp*", SearchOption.AllDirectories);

    [Fact]
    public void Serie_ist_mit_gleichem_Seed_reproduzierbar()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser", w: 16, h: 16);
        string o1 = Path.Combine(dir, "o1"), o2 = Path.Combine(dir, "o2"), o3 = Path.Combine(dir, "o3");
        SeriesRunner.Run(Job(new[] { a }, o1, seed: 7), new[] { a }, null, CancellationToken.None);
        SeriesRunner.Run(Job(new[] { a }, o2, seed: 7), new[] { a }, null, CancellationToken.None);
        SeriesRunner.Run(Job(new[] { a }, o3, seed: 8), new[] { a }, null, CancellationToken.None);
        Assert.Equal(File.ReadAllBytes(Path.Combine(o1, "BA_a_k.ser")), File.ReadAllBytes(Path.Combine(o2, "BA_a_k.ser")));
        Assert.NotEqual(File.ReadAllBytes(Path.Combine(o1, "BA_a_k.ser")), File.ReadAllBytes(Path.Combine(o3, "BA_a_k.ser")));
    }

    [Fact]
    public void Serie_erhaelt_Frameanzahl_Kopf_und_Anhang()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser", frames: 5);
        string o = Path.Combine(dir, "o");
        var report = SeriesRunner.Run(Job(new[] { a }, o), new[] { a }, null, CancellationToken.None);
        string outFile = Path.Combine(o, "BA_a_k.ser");
        Assert.Equal(new[] { a }, report.Completed);
        var src = File.ReadAllBytes(a);
        var dst = File.ReadAllBytes(outFile);
        Assert.Equal(src.Length, dst.Length);
        Assert.Equal(src.Take(178), dst.Take(178));
        Assert.Equal(src.TakeLast(40), dst.TakeLast(40));
        using var s = new SerSource(outFile);
        Assert.Equal(5, s.Info.FrameCount);
        Assert.True(s.HasTimestampTrailer);
    }

    [Fact]
    public void Serie_nutzt_nur_bestaetigte_Punkte()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser", w: 16, h: 16);
        string o1 = Path.Combine(dir, "o1"), o2 = Path.Combine(dir, "o2");
        SeriesRunner.Run(Job(new[] { a }, o1), new[] { a }, null, CancellationToken.None);
        var withPreview = DitherSet();
        withPreview.SetPreview(BayerFilter.Red, new CurvePoint(50, 200));
        SeriesRunner.Run(Job(new[] { a }, o2, withPreview), new[] { a }, null, CancellationToken.None);
        Assert.Equal(Hash(Path.Combine(o1, "BA_a_k.ser")), Hash(Path.Combine(o2, "BA_a_k.ser")));
    }

    [Fact]
    public void Original_bleibt_unveraendert()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser");
        string before = Hash(a);
        DateTime stamp = File.GetLastWriteTimeUtc(a);
        SeriesRunner.Run(Job(new[] { a }, Path.Combine(dir, "o")), new[] { a }, null, CancellationToken.None);
        Assert.Equal(before, Hash(a));
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(a));
    }

    [Fact]
    public void Serie_aus_Einzelbildern_funktioniert_und_meldet_Fortschritt()
    {
        string dir = TestData.TempDir();
        string p1 = PreflightTests.MakePng(dir, "1.png"), p2 = PreflightTests.MakePng(dir, "2.png");
        var seen = new List<SeriesProgress>();
        var report = SeriesRunner.Run(Job(new[] { p1, p2 }, Path.Combine(dir, "o")), new[] { p1, p2 },
            new SyncProgress(seen.Add), CancellationToken.None);
        Assert.Equal(2, report.Completed.Count);
        Assert.True(File.Exists(Path.Combine(dir, "o", "BA_1_k.png")));
        Assert.True(File.Exists(Path.Combine(dir, "o", "BA_2_k.png")));
        Assert.Contains(seen, p => p.FileIndex == 1 && p.FileCount == 2);
        Assert.Empty(Leftovers(dir));
    }

    [Fact]
    public void Sidecar_wird_mit_neuem_Namen_kopiert()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser");
        File.WriteAllText(Path.Combine(dir, "a.txt"), "FireCapture log");
        string o = Path.Combine(dir, "o");
        var report = SeriesRunner.Run(Job(new[] { a }, o), new[] { a }, null, CancellationToken.None);
        Assert.Equal("FireCapture log", File.ReadAllText(Path.Combine(o, "BA_a_k.txt")));
        Assert.Empty(report.Failed);
    }

    [Fact]
    public void Sidecar_Fehler_ist_Hinweis_Bild_bleibt_Completed()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser");
        File.WriteAllText(Path.Combine(dir, "a.txt"), "log");
        string o = Path.Combine(dir, "o");
        Directory.CreateDirectory(o);
        File.WriteAllText(Path.Combine(o, "BA_a_k.txt"), "other");
        var report = SeriesRunner.Run(Job(new[] { a }, o), new[] { a }, null, CancellationToken.None);
        Assert.Equal(new[] { a }, report.Completed);
        var f = Assert.Single(report.Failed);
        Assert.Equal(a, f.File);
        Assert.StartsWith("sidecar:", f.Reason);
        Assert.Equal("other", File.ReadAllText(Path.Combine(o, "BA_a_k.txt")));
    }

    [Fact]
    public void Name_behaelt_Praefix_und_Suffix_bei_Kollision()
    {
        string dir = TestData.TempDir();
        string o = Path.Combine(dir, "o");
        Directory.CreateDirectory(o);
        File.WriteAllText(Path.Combine(o, "BA_x_k.ser"), "");
        Assert.Equal(Path.Combine(o, "BA_x_k_1.ser"), OutputNaming.Build(Path.Combine(dir, "x.ser"), o, "BA_", "_k"));
        File.WriteAllText(Path.Combine(o, "BA_x_k_1.ser"), "");
        Assert.Equal(Path.Combine(o, "BA_x_k_2.ser"), OutputNaming.Build(Path.Combine(dir, "x.ser"), o, "BA_", "_k"));
        Assert.Equal(Path.Combine(o, "BA_y_k.png"), OutputNaming.Build(Path.Combine(dir, "y.png"), o, "BA_", "_k"));
    }

    [Fact]
    public void Gesperrte_Datei_wird_berichtet_Lauf_geht_weiter()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser"), b = PreflightTests.MakeSer(dir, "b.ser");
        string o = Path.Combine(dir, "o");
        FileStream? lockStream = null;
        var report = SeriesRunner.Run(Job(new[] { a, b }, o), new[] { a, b }, null, CancellationToken.None,
            (src, tmp) =>
            {
                if (lockStream == null)
                {
                    string final = tmp.Replace(".tmp", "");
                    lockStream = new FileStream(final, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                }
                return ImageFiles.CreateSink(src, tmp);
            });
        lockStream?.Dispose();
        Assert.Equal(new[] { b }, report.Completed);
        Assert.Equal(a, Assert.Single(report.Failed).File);
        Assert.Empty(Leftovers(o));
        Assert.False(report.Cancelled);
    }

    [Fact]
    public void Abbruch_hinterlaesst_nichts_unter_Endnamen()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser"), b = PreflightTests.MakeSer(dir, "b.ser");
        string o = Path.Combine(dir, "o");
        using var cts = new CancellationTokenSource();
        var report = SeriesRunner.Run(Job(new[] { a, b }, o), new[] { a, b },
            new SyncProgress(p => { if (p.Frame >= 1) cts.Cancel(); }), cts.Token);
        Assert.True(report.Cancelled);
        Assert.Empty(report.Completed);
        Assert.Equal(new[] { a, b }, report.Skipped);
        Assert.Empty(Directory.GetFiles(o));
    }

    [Fact]
    public void Datentraeger_voll_stoppt_Lauf()
    {
        string dir = TestData.TempDir();
        var files = new[] { "a.ser", "b.ser", "c.ser" }.Select(n => PreflightTests.MakeSer(dir, n)).ToArray();
        string o = Path.Combine(dir, "o");
        var report = SeriesRunner.Run(Job(files, o), files, null, CancellationToken.None,
            (src, tmp) => new FailingSink(ImageFiles.CreateSink(src, tmp)));
        Assert.True(report.StoppedDiskFull);
        Assert.Empty(report.Completed);
        Assert.Equal(files[0], Assert.Single(report.Failed).File);
        Assert.Equal(new[] { files[1], files[2] }, report.Skipped);
        Assert.Empty(Directory.GetFiles(o));
    }

    [Fact]
    public void Unerwartete_Ausnahme_einer_Datei_wird_berichtet_Lauf_geht_weiter()
    {
        string dir = TestData.TempDir();
        string a = PreflightTests.MakeSer(dir, "a.ser"), b = PreflightTests.MakeSer(dir, "b.ser");
        string o = Path.Combine(dir, "o");
        int calls = 0;
        var report = SeriesRunner.Run(Job(new[] { a, b }, o), new[] { a, b }, null, CancellationToken.None,
            (src, tmp) => calls++ == 0
                ? throw new System.Runtime.InteropServices.COMException("wic broke")
                : ImageFiles.CreateSink(src, tmp));
        Assert.Equal(new[] { b }, report.Completed);
        Assert.Equal(a, Assert.Single(report.Failed).File);
        Assert.False(report.StoppedDiskFull);
        Assert.Empty(Leftovers(o));
    }

    [Fact]
    public void Datentraeger_voll_in_InnerException_stoppt_Lauf()
    {
        string dir = TestData.TempDir();
        var files = new[] { "a.ser", "b.ser" }.Select(n => PreflightTests.MakeSer(dir, n)).ToArray();
        string o = Path.Combine(dir, "o");
        var report = SeriesRunner.Run(Job(files, o), files, null, CancellationToken.None,
            (src, tmp) => throw new System.Runtime.InteropServices.COMException("wrapped",
                new IOException("disk full", unchecked((int)0x80070070))));
        Assert.True(report.StoppedDiskFull);
        Assert.Equal(files[1], Assert.Single(report.Skipped));
    }

    [Fact]
    public void Nicht_lesbare_Datei_wird_berichtet_Lauf_geht_weiter()
    {
        string dir = TestData.TempDir();
        string bad = Path.Combine(dir, "bad.ser");
        File.WriteAllText(bad, "nope");
        string b = PreflightTests.MakeSer(dir, "b.ser");
        var report = SeriesRunner.Run(Job(new[] { bad, b }, Path.Combine(dir, "o")), new[] { bad, b }, null, CancellationToken.None);
        Assert.Equal(new[] { b }, report.Completed);
        Assert.Equal(bad, Assert.Single(report.Failed).File);
    }
}
