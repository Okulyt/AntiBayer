using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using AntiBayer.Core.Series;

namespace AntiBayer;

/// <summary>Runs the confirmed curves over a series of files in the background.</summary>
public partial class SeriesForm : Form
{
    private readonly ParameterSet set;
    private readonly bool swapBytes;
    private readonly List<string> files = new();

    private PreflightResult? lastCheck;
    private CancellationTokenSource? cts;
    private bool running;
    private bool closeAfterRun;

    public SeriesForm(string loadedFile, ParameterSet set, string prefix, string suffix, bool swapBytes)
    {
        InitializeComponent();
        this.set = set;
        this.swapBytes = swapBytes;
        files.Add(loadedFile);
        textBoxOutputFolder.Text = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(loadedFile)) ?? "", "AntiBayer");
        textBoxPrefix.Text = prefix;
        textBoxSuffix.Text = suffix;
        labelNote.Text = set.HasConfirmedPoints
            ? ""
            : "No curve points confirmed, output equals input.";
        RefreshFileList();
        RunCheck();
    }

    // ---- Inputs ----

    private void RefreshFileList()
    {
        listBoxFiles.BeginUpdate();
        listBoxFiles.Items.Clear();
        foreach (var f in files) listBoxFiles.Items.Add(f);
        listBoxFiles.EndUpdate();
    }

    private void buttonAddFiles_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = true,
            Title = "Add image files...",
            Filter = "Images (*.ser;*.tif;*.tiff;*.png;*.fit;*.fits;*.fts)|*.ser;*.tif;*.tiff;*.png;*.fit;*.fits;*.fts",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var f in dialog.FileNames)
            if (!files.Contains(f, StringComparer.OrdinalIgnoreCase)) files.Add(f);
        RefreshFileList();
        RunCheck();
    }

    private void buttonRemoveFiles_Click(object? sender, EventArgs e)
    {
        var selected = listBoxFiles.SelectedItems.Cast<string>().ToList();
        files.RemoveAll(selected.Contains);
        RefreshFileList();
        RunCheck();
    }

    private void buttonBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Output folder", UseDescriptionForTitle = true };
        if (Directory.Exists(textBoxOutputFolder.Text)) dialog.InitialDirectory = textBoxOutputFolder.Text;
        if (dialog.ShowDialog(this) == DialogResult.OK) textBoxOutputFolder.Text = dialog.SelectedPath;
    }

    /// <summary>Typing in the folder box re-checks after a short pause, not on every key.</summary>
    private void SettingChanged(object? sender, EventArgs e)
    {
        checkTimer.Stop();
        checkTimer.Start();
    }

    private void checkTimer_Tick(object? sender, EventArgs e)
    {
        checkTimer.Stop();
        RunCheck();
    }

    // ---- Preflight ----

    private SeriesJob MakeJob(IReadOnlyList<string> jobFiles, int seed) =>
        new(jobFiles, textBoxOutputFolder.Text.Trim(), textBoxPrefix.Text, textBoxSuffix.Text,
            set, set.Pattern, seed, swapBytes);

    private void RunCheck()
    {
        if (running) return;
        PreflightResult result;
        try
        {
            result = Preflight.Check(MakeJob(files.ToList(), 0));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or IOException)
        {
            // An empty or invalid output folder path: shown as an issue, Start stays disabled.
            result = new PreflightResult(Array.Empty<string>(),
                new[] { new PreflightIssue(null, "The output folder is not valid: " + e.Message, true) });
        }
        lastCheck = result;

        listBoxIssues.BeginUpdate();
        listBoxIssues.Items.Clear();
        foreach (var issue in result.Issues)
        {
            string name = issue.File == null ? "" : Path.GetFileName(issue.File) + ": ";
            listBoxIssues.Items.Add((issue.Excludes ? "Excluded - " : "Note - ") + name + issue.Message);
        }
        if (result.Issues.Count == 0) listBoxIssues.Items.Add("No problems found.");
        listBoxIssues.EndUpdate();
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        buttonStart.Enabled = !running && lastCheck is { Runnable.Count: > 0 };
        buttonAddFiles.Enabled = !running;
        buttonRemoveFiles.Enabled = !running;
        buttonBrowse.Enabled = !running;
        textBoxOutputFolder.Enabled = !running;
        textBoxPrefix.Enabled = !running;
        textBoxSuffix.Enabled = !running;
        buttonCancel.Text = running ? "Cancel" : "Close";
    }

    // ---- Run ----

    private async void buttonStart_Click(object? sender, EventArgs e)
    {
        if (running || lastCheck is not { Runnable.Count: > 0 } check) return;

        var runnable = check.Runnable.ToList();
        var job = MakeJob(runnable, Environment.TickCount);
        cts = new CancellationTokenSource();
        var token = cts.Token;
        running = true;
        UpdateButtons();
        textBoxReport.Clear();
        progressBar.Value = 0;
        labelProgress.Text = "Starting...";

        var progress = new Progress<SeriesProgress>(ShowProgress); // created on the UI thread
        SeriesReport? report = null;
        string? crash = null;
        try
        {
            report = await Task.Run(() => SeriesRunner.Run(job, runnable, progress, token));
        }
        catch (Exception ex)
        {
            crash = ex.Message;
        }

        running = false;
        cts.Dispose();
        cts = null;
        if (IsDisposed) return;

        if (report != null)
        {
            textBoxReport.Text = BuildReport(report, check);
            labelProgress.Text = report.Cancelled ? "Cancelled." : report.StoppedDiskFull ? "Stopped, disk full." : "Finished.";
            if (!report.Cancelled && !report.StoppedDiskFull) progressBar.Value = progressBar.Maximum;
        }
        else
        {
            textBoxReport.Text = "The run failed unexpectedly: " + crash;
            labelProgress.Text = "Failed.";
        }

        if (closeAfterRun) { Close(); return; }
        RunCheck(); // the output folder now holds files; refresh the check
    }

    private void ShowProgress(SeriesProgress p)
    {
        if (!running) return;
        double part = p.FrameCount > 0 ? (double)p.Frame / p.FrameCount : 1;
        double overall = (p.FileIndex + part) / Math.Max(1, p.FileCount);
        progressBar.Value = Math.Clamp((int)(overall * progressBar.Maximum), 0, progressBar.Maximum);
        labelProgress.Text = $"File {p.FileIndex + 1} of {p.FileCount}, frame {p.Frame} of {p.FrameCount}";
    }

    private static string BuildReport(SeriesReport r, PreflightResult check)
    {
        var text = new StringBuilder();
        text.AppendLine(r.Cancelled ? "Cancelled." : r.StoppedDiskFull ? "Stopped: the disk is full." : "Finished.");
        text.AppendLine($"Completed: {r.Completed.Count}, failed: {r.Failed.Count}, skipped: {r.Skipped.Count}");
        if (r.Failed.Count > 0)
        {
            text.AppendLine().AppendLine("Failed:");
            foreach (var (file, reason) in r.Failed) text.AppendLine($"  {file}: {reason}");
        }
        if (r.Skipped.Count > 0)
        {
            text.AppendLine().AppendLine("Skipped (not processed):");
            foreach (var file in r.Skipped) text.AppendLine("  " + file);
        }
        var excluded = check.Issues.Where(i => i.Excludes && i.File != null).ToList();
        if (excluded.Count > 0)
        {
            text.AppendLine().AppendLine("Excluded before the run:");
            foreach (var i in excluded) text.AppendLine($"  {i.File}: {i.Message}");
        }
        return text.ToString();
    }

    private void buttonCancel_Click(object? sender, EventArgs e)
    {
        if (running) cts?.Cancel();
        else Close();
    }

    private void SeriesForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!running) return;
        e.Cancel = true; // wait until the run has ended, then close
        if (closeAfterRun) return;
        var answer = MessageBox.Show(this, "A run is in progress. Cancel it and close?", "AntiBayer",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        closeAfterRun = true;
        cts?.Cancel();
        labelProgress.Text = "Cancelling...";
    }
}
