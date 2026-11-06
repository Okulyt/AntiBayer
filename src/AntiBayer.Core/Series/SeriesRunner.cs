using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AntiBayer.Core.Imaging;

namespace AntiBayer.Core.Series;

public static class SeriesRunner
{
    private sealed class DiskFullException : Exception
    {
        public DiskFullException(Exception inner) : base(inner.Message, inner) { }
    }

    private static bool IsDiskFull(Exception e) =>
        e is IOException io && ((io.HResult & 0xFFFF) is 112 /* ERROR_DISK_FULL */ or 39 /* ERROR_HANDLE_DISK_FULL */);

    /// <summary>
    /// Processes the files one after another. Each output is written to a temporary name and renamed when complete.
    /// A failing file is reported and the run continues; a full disk or cancellation stops it.
    /// </summary>
    public static SeriesReport Run(SeriesJob job, IReadOnlyList<string> runnable, IProgress<SeriesProgress>? progress,
        CancellationToken token, Func<IFrameSource, string, IFrameSink>? sinkFactory = null)
    {
        sinkFactory ??= ImageFiles.CreateSink;
        var completed = new List<string>();
        var failed = new List<(string File, string Reason)>();
        var skipped = new List<string>();

        try
        {
            Directory.CreateDirectory(job.OutputFolder);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            foreach (var f in runnable) failed.Add((f, "The output folder cannot be created: " + e.Message));
            return new SeriesReport(completed, failed, skipped, false, false);
        }

        bool cancelled = false, diskFull = false;
        for (int i = 0; i < runnable.Count; i++)
        {
            string file = runnable[i];
            if (token.IsCancellationRequested) { cancelled = true; }
            if (cancelled || diskFull) { skipped.Add(file); continue; }

            string? tmp = null;
            try
            {
                if (ProcessFile(job, file, i, runnable.Count, progress, token, sinkFactory, out tmp, out var sidecarError))
                {
                    completed.Add(file);
                    if (sidecarError != null) failed.Add((file, "sidecar: " + sidecarError));
                }
                else
                {
                    cancelled = true;
                    skipped.Add(file);
                }
            }
            catch (DiskFullException e)
            {
                failed.Add((file, "The disk is full: " + e.Message));
                diskFull = true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or UnsupportedImageException
                                          or InvalidOperationException or ArgumentException or NotSupportedException)
            {
                failed.Add((file, e.Message));
            }
            finally
            {
                if (tmp != null) TryDelete(tmp);
            }
        }

        return new SeriesReport(completed, failed, skipped, cancelled, diskFull);
    }

    /// <returns>False when cancelled. Throws on failure; the caller deletes the temporary file.</returns>
    private static bool ProcessFile(SeriesJob job, string file, int fileIndex, int fileCount,
        IProgress<SeriesProgress>? progress, CancellationToken token,
        Func<IFrameSource, string, IFrameSink> sinkFactory, out string? tmp, out string? sidecarError)
    {
        tmp = null;
        sidecarError = null;

        using var source = ImageFiles.Open(file, job.SwapBytes);
        var info = source.Info;
        if (info.BitDepth != job.Set.BitDepth)
            throw new InvalidOperationException(
                $"The file has {info.BitDepth} bit but the parameter set is for {job.Set.BitDepth} bit.");

        var corrector = new FrameCorrector(job.Pattern, job.Set.BuildConfirmedTables(info.WhiteLevel));
        var rng = new Random(unchecked(job.Seed + fileIndex));

        string final = OutputNaming.Build(file, job.OutputFolder, job.Prefix, job.Suffix);
        string ext = Path.GetExtension(final);
        tmp = Path.Combine(Path.GetDirectoryName(final)!, Path.GetFileNameWithoutExtension(final) + ".tmp" + ext);

        int pixels = info.Width * info.Height;
        var src = new ushort[pixels];
        var dst = new ushort[pixels];

        try
        {
            using (var sink = sinkFactory(source, tmp))
            {
                for (int f = 0; f < info.FrameCount; f++)
                {
                    if (token.IsCancellationRequested) return false;
                    source.ReadFrame(f, src);
                    corrector.Apply(src, dst, info.Width, info.Height, rng);
                    sink.WriteFrame(dst);
                    progress?.Report(new SeriesProgress(fileIndex, fileCount, f + 1, info.FrameCount));
                }
                if (token.IsCancellationRequested) return false;
                sink.Complete();
            }
            File.Move(tmp, final);
        }
        catch (Exception e) when (IsDiskFull(e))
        {
            throw new DiskFullException(e);
        }
        tmp = null; // now under its final name

        try
        {
            string sidecar = Path.ChangeExtension(file, ".txt");
            if (!string.Equals(sidecar, file, StringComparison.OrdinalIgnoreCase) && File.Exists(sidecar))
                File.Copy(sidecar, Path.ChangeExtension(final, ".txt"), false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            sidecarError = e.Message;
        }
        return true;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
