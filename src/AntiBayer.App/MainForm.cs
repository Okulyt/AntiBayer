using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using BayerGeometry = AntiBayer.Core.Bayer;

namespace AntiBayer;

public partial class MainForm : Form
{
    private const int PreviewZoom = 8;
    private const string AppTitle = "AntiBayer";

    private sealed record Row(BayerFilter Filter, NumericUpDown OriginalValue, NumericUpDown NewValue, Label Name,
        Button Mark, Button SetPoint, Color FieldHighlight, Color NameHighlight);

    private sealed record ListEntry(BayerFilter Filter, CurvePoint Point, bool IsPreview);

    private readonly Row[] rows;

    private IFrameSource? source;
    private ParameterSet? set;
    private int frameIndex;
    private ushort[] rawFrame = Array.Empty<ushort>();
    private int frameMax;

    private (int X, int Y) previewCenter;
    private PixelRect? previewRect;
    private Bitmap? overviewBitmap;
    private Bitmap? previewBitmap;

    private bool suppressEvents;
    private string? statusMessage;
    private string? curveHint;

    public MainForm()
    {
        InitializeComponent();
        rows = new[]
        {
            new Row(BayerFilter.Red, textBoxOriginalValueRed, textBoxNewValueRed, labelRed,
                buttonMarkR, buttonSetStrongPointRed, Color.Red, Color.Red),
            new Row(BayerFilter.Green1, textBoxOriginalValueGreen1, textBoxNewValueGreen1, labelGreen1,
                buttonMarkG1, buttonSetStrongPointGreen1, Color.LightGreen, Color.Green),
            new Row(BayerFilter.Green2, textBoxOriginalValueGreen2, textBoxNewValueGreen2, labelGreen2,
                buttonMarkG2, buttonSetStrongPointGreen2, Color.LightGreen, Color.Green),
            new Row(BayerFilter.Blue, textBoxOriginalValueBlue, textBoxNewValueBlue, labelBlue,
                buttonMarkB, buttonSetStrongPointBlue, Color.LightBlue, Color.Blue),
        };
        PlaceHistograms();
    }

    [MemberNotNullWhen(true, nameof(source), nameof(set))]
    private bool Loaded => source != null && set != null;

    private void WithoutEvents(Action action)
    {
        bool before = suppressEvents;
        suppressEvents = true;
        try { action(); }
        finally { suppressEvents = before; }
    }

    // ---- Loading ----

    private void buttonLoadImage_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            Title = "Open image file...",
            Filter = "Images (*.ser;*.tif;*.tiff;*.png)|*.ser;*.tif;*.tiff;*.png|All files (*.*)|*.*",
            ValidateNames = true,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) LoadFile(dialog.FileName);
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files) return;
        string path = files[0];
        // Dialogs must not block the drag source, so the load runs after the drop has returned.
        BeginInvoke(() => LoadFile(path));
    }

    /// <summary>The one way a file gets loaded. On failure the previous state stays untouched.</summary>
    private void LoadFile(string path)
    {
        IFrameSource opened;
        ushort[] frame;
        try
        {
            (opened, frame) = OpenAtFrame(path, swapBytes: false, 0);
        }
        catch (Exception e) when (IsLoadFailure(e))
        {
            ReportLoadFailure(path, e.Message);
            return;
        }

        var info = opened.Info;
        var pattern = info.SuggestedPattern ?? set?.Pattern ?? SelectedPattern();
        var curves = CurvesForFile(info, pattern, out string? curvesNote);
        if (curves == null)
        {
            opened.Dispose();
            return;
        }

        source?.Dispose();
        source = opened;
        set = curves;
        rawFrame = frame;
        frameIndex = 0;
        previewRect = null;
        SetPreviewBitmap(null);

        WithoutEvents(() =>
        {
            SelectPatternRadio(pattern);
            foreach (var row in rows)
            {
                row.OriginalValue.Maximum = info.WhiteLevel;
                row.NewValue.Maximum = info.WhiteLevel;
                row.OriginalValue.Value = 0;
                row.NewValue.Value = 0;
            }
            ConfigureFrameControls(info);
            checkBoxSwapBytes.Checked = false;
            checkBoxSwapBytes.Visible = info.Format == ImageFormat.Ser && info.BitDepth == 16;
        });

        Text = AppTitle + " - " + Path.GetFileName(info.Path);
        buttonConvertFolder.Enabled = true;
        ClearHighlights();
        ApplyRowStates();
        statusMessage = curvesNote ?? "Loaded " + Path.GetFileName(info.Path);
        ShowFrameData();
    }

    private static bool IsLoadFailure(Exception e) =>
        e is UnsupportedImageException or IOException or UnauthorizedAccessException;

    private static (IFrameSource Source, ushort[] Frame) OpenAtFrame(string path, bool swapBytes, int index)
    {
        var opened = ImageFiles.Open(path, swapBytes);
        try
        {
            var frame = new ushort[opened.Info.Width * opened.Info.Height];
            opened.ReadFrame(index, frame);
            return (opened, frame);
        }
        catch
        {
            opened.Dispose();
            throw;
        }
    }

    private void ReportLoadFailure(string path, string reason)
    {
        SetStatus("Could not load " + Path.GetFileName(path) + ": " + reason);
        MessageBox.Show(this, reason, "Could not load " + Path.GetFileName(path), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>The parameter set for a newly opened file; null when the user cancels the load.</summary>
    private ParameterSet? CurvesForFile(ImageInfo info, BayerPattern pattern, out string? note)
    {
        note = null;
        var fresh = new ParameterSet(pattern, info.BitDepth) { Capture = info.Capture };
        if (set == null || !set.HasConfirmedPoints) return fresh;

        var compatibility = set.CompatibleWith(info.BitDepth, pattern);
        if (compatibility is SetCompatibility.BitDepthMismatch or SetCompatibility.CurveCountMismatch)
        {
            string reason = compatibility == SetCompatibility.BitDepthMismatch
                ? $"their bit depth ({set.BitDepth}) differs from this file ({info.BitDepth})"
                : $"their number of curves does not fit pattern {pattern}";
            note = $"The curves cannot be used, {reason}. A new empty set was created.";
            MessageBox.Show(this, note, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return fresh;
        }

        switch (MessageBox.Show(this, "Keep curves?", AppTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
        {
            case DialogResult.Cancel:
                return null;
            case DialogResult.No:
                return fresh;
        }
        set.ClearPreview();
        set.ChangePattern(pattern);
        note = "Curves kept for " + Path.GetFileName(info.Path);
        return set;
    }

    private void ConfigureFrameControls(ImageInfo info)
    {
        bool several = info.FrameCount > 1;
        trackBarFrame.Value = trackBarFrame.Minimum;
        trackBarFrame.Maximum = Math.Max(info.FrameCount, trackBarFrame.Minimum);
        trackBarFrame.LargeChange = Math.Max(1, info.FrameCount / 20);
        numericUpDownFrame.Value = numericUpDownFrame.Minimum;
        numericUpDownFrame.Maximum = Math.Max(info.FrameCount, 1);
        labelFrame.Visible = several;
        trackBarFrame.Visible = several;
        numericUpDownFrame.Visible = several;
    }

    // ---- Pattern ----

    private BayerPattern SelectedPattern()
    {
        if (radioButtonBayerRGGB.Checked) return BayerPattern.RGGB;
        if (radioButtonBayerGRBG.Checked) return BayerPattern.GRBG;
        if (radioButtonBayerGBRG.Checked) return BayerPattern.GBRG;
        if (radioButtonBayerBGGR.Checked) return BayerPattern.BGGR;
        return BayerPattern.None;
    }

    private void SelectPatternRadio(BayerPattern pattern)
    {
        var radio = pattern switch
        {
            BayerPattern.RGGB => radioButtonBayerRGGB,
            BayerPattern.GRBG => radioButtonBayerGRBG,
            BayerPattern.GBRG => radioButtonBayerGBRG,
            BayerPattern.BGGR => radioButtonBayerBGGR,
            _ => radioButtonbayerNone,
        };
        radio.Checked = true;
    }

    private void RadioButtonBayer_CheckedChanged(object? sender, EventArgs e)
    {
        if (suppressEvents || sender is not RadioButton { Checked: true } || !Loaded) return;
        var wanted = SelectedPattern();
        if (wanted == set.Pattern) return;

        bool hadPoints = set.HasConfirmedPoints;
        bool crossesNone = (wanted == BayerPattern.None) != (set.Pattern == BayerPattern.None);
        if (crossesNone && hadPoints)
        {
            var answer = MessageBox.Show(this, "Changing to or from None resets the curves. Continue?", AppTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                WithoutEvents(() => SelectPatternRadio(set.Pattern));
                return;
            }
        }

        set.ChangePattern(wanted);
        ClearHighlights();
        ApplyRowStates();
        if (crossesNone && hadPoints) statusMessage = "Pattern is now " + wanted + ", the curves were reset.";
        else if (hadPoints) statusMessage = "Pattern is now " + wanted + ", the curves stay with their filter position.";
        else statusMessage = "Pattern is now " + wanted + ".";
        RefreshCurvesAndPreview();
    }

    private void ApplyRowStates()
    {
        bool none = set?.Pattern == BayerPattern.None;
        foreach (var row in rows)
        {
            bool active = !none || row.Filter == BayerFilter.Red;
            row.OriginalValue.Enabled = active;
            row.NewValue.Enabled = active;
            row.Mark.Enabled = active;
            row.SetPoint.Enabled = active;
        }
        labelRed.Text = none ? "All" : "Red";
        buttonMeasureCurves.Enabled = Loaded && !none;
    }

    private IEnumerable<Row> ActiveRows() => rows.Where(r => r.OriginalValue.Enabled);

    private BayerFilter FilterOf(Row row) =>
        set?.Pattern == BayerPattern.None ? BayerFilter.All : row.Filter;

    private Row RowOf(BayerFilter filter) =>
        filter == BayerFilter.All ? rows[0] : rows.First(r => r.Filter == filter);

    private void ClearHighlights()
    {
        foreach (var row in rows)
        {
            row.Name.ForeColor = SystemColors.ControlText;
            row.OriginalValue.BackColor = SystemColors.Window;
        }
    }

    // ---- Frame and byte order ----

    private void trackBarFrame_ValueChanged(object? sender, EventArgs e)
    {
        if (!suppressEvents) ShowFrame(trackBarFrame.Value - 1);
    }

    private void numericUpDownFrame_ValueChanged(object? sender, EventArgs e)
    {
        if (!suppressEvents) ShowFrame((int)numericUpDownFrame.Value - 1);
    }

    private void ShowFrame(int index)
    {
        if (!Loaded) return;
        try
        {
            source.ReadFrame(index, rawFrame);
            frameIndex = index;
            statusMessage = $"Frame {index + 1} of {source.Info.FrameCount}";
        }
        catch (IOException e)
        {
            statusMessage = $"Could not read frame {index + 1}: {e.Message}";
        }
        WithoutEvents(() =>
        {
            trackBarFrame.Value = frameIndex + 1;
            numericUpDownFrame.Value = frameIndex + 1;
        });
        ShowFrameData();
    }

    private void checkBoxSwapBytes_CheckedChanged(object? sender, EventArgs e)
    {
        if (suppressEvents || !Loaded) return;
        try
        {
            var (opened, frame) = OpenAtFrame(source.Info.Path, checkBoxSwapBytes.Checked, frameIndex);
            source.Dispose();
            source = opened;
            rawFrame = frame;
        }
        catch (Exception ex) when (IsLoadFailure(ex))
        {
            WithoutEvents(() => checkBoxSwapBytes.Checked = !checkBoxSwapBytes.Checked);
            SetStatus("Could not swap bytes: " + ex.Message);
            return;
        }

        ushort white = source.Info.WhiteLevel;
        WithoutEvents(() =>
        {
            foreach (var row in rows)
            {
                row.OriginalValue.Maximum = white;
                row.NewValue.Maximum = white;
            }
        });
        statusMessage = checkBoxSwapBytes.Checked ? "Bytes swapped for display and curves." : "Byte order as stored.";
        ShowFrameData();
    }

    // ---- Display ----

    /// <summary>Overview and preview after the raw frame changed.</summary>
    private void ShowFrameData()
    {
        if (!Loaded) return;
        frameMax = rawFrame.Max();
        var bitmap = FrameBitmap.ToBitmap(DisplayMapping.To8Bit(rawFrame), source.Info.Width, source.Info.Height);
        var old = overviewBitmap;
        overviewBitmap = bitmap;
        _pbWorkingImage.Image = bitmap;
        old?.Dispose();
        RefreshCurvesAndPreview();
    }

    /// <summary>Point list, curve hint, corrected preview, variances: everything that depends on the curves.</summary>
    private void RefreshCurvesAndPreview()
    {
        if (!Loaded) return;
        var tables = set.BuildPreviewTables(source.Info.WhiteLevel);
        var bent = tables.Where(t => !t.Value.IsMonotonic).Select(t => t.Key.ToString()).ToList();
        curveHint = bent.Count > 0 ? "Non-monotonic curve: " + string.Join(", ", bent) : null;
        FillPointList();

        IReadOnlyDictionary<BayerFilter, FilterVariance>? variances = null;
        if (previewRect is PixelRect r)
        {
            var crop = RawCrop(r);
            var corrected = new ushort[crop.Length];
            new FrameCorrector(set.Pattern, tables).Apply(crop, corrected, r.Width, r.Height, null);
            SetPreviewBitmap(FrameBitmap.ToBitmap(DisplayMapping.To8Bit(corrected, frameMax), r.Width, r.Height));
            variances = Variances.Compute(corrected, r.Width, r.Height, set.Pattern);
        }
        textBoxImageInfo.Text = BuildImageInfo(variances);
        RefreshStatus();
    }

    private ushort[] RawCrop(PixelRect r)
    {
        var crop = new ushort[r.Width * r.Height];
        int imageWidth = source!.Info.Width;
        for (int y = 0; y < r.Height; y++)
            Array.Copy(rawFrame, (r.Y + y) * imageWidth + r.X, crop, y * r.Width, r.Width);
        return crop;
    }

    /// <summary>The picture box gets the new bitmap before the old one is disposed.</summary>
    private void SetPreviewBitmap(Bitmap? bitmap)
    {
        var old = previewBitmap;
        previewBitmap = bitmap;
        _pbPreview.Image = bitmap;
        old?.Dispose();
    }

    private PixelRect ComputePreviewRect()
    {
        var info = source!.Info;
        return PreviewGeometry.Compute(previewCenter.X, previewCenter.Y,
            Math.Max(1, _pbPreview.Width / PreviewZoom), Math.Max(1, _pbPreview.Height / PreviewZoom),
            info.Width, info.Height);
    }

    private void _pbWorkingImage_MouseClick(object? sender, MouseEventArgs e)
    {
        if (!Loaded) return;
        var info = source.Info;
        previewCenter = (
            Math.Clamp((int)((long)e.X * info.Width / _pbWorkingImage.Width), 0, info.Width - 1),
            Math.Clamp((int)((long)e.Y * info.Height / _pbWorkingImage.Height), 0, info.Height - 1));
        var r = ComputePreviewRect();
        previewRect = r;
        set.ClearPreview();
        statusMessage = $"Preview ({r.X},{r.Y})-({r.X + r.Width},{r.Y + r.Height}) of {info.Width}x{info.Height}";
        RefreshCurvesAndPreview();
    }

    private void _pbPreview_MouseClick(object? sender, MouseEventArgs e)
    {
        if (!Loaded || previewRect is not PixelRect r) return;
        var hit = PreviewGeometry.ZoomedClickToPixel(e.X, e.Y, _pbPreview.Width, _pbPreview.Height, r.Width, r.Height);
        if (hit == null) return;

        var info = source.Info;
        int x = r.X + hit.Value.X, y = r.Y + hit.Value.Y;
        var means = BayerGeometry.NeighbourhoodMean(rawFrame, info.Width, info.Height, set.Pattern, x, y);
        WithoutEvents(() =>
        {
            foreach (var row in ActiveRows())
                if (means.TryGetValue(FilterOf(row), out double mean))
                {
                    SetField(row.OriginalValue, mean);
                    // Start from what the curve already does here, so the first change does not jump.
                    SetField(row.NewValue, set.ConfirmedCurveValue(FilterOf(row), (ushort)row.OriginalValue.Value, info.WhiteLevel));
                }
        });

        ClearHighlights();
        var clicked = RowOf(BayerGeometry.FilterAt(set.Pattern, x, y));
        clicked.Name.ForeColor = clicked.NameHighlight;
        clicked.OriginalValue.BackColor = clicked.FieldHighlight;

        set.ClearPreview();
        statusMessage = $"Pixel ({x},{y}) = {rawFrame[y * info.Width + x]}";
        RefreshCurvesAndPreview();
    }

    private static void SetField(NumericUpDown field, double value) =>
        field.Value = Math.Clamp((decimal)Math.Round(value), field.Minimum, field.Maximum);

    private void MarkButton_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!Loaded || previewRect is not PixelRect r) return;
        byte[] display = DisplayMapping.To8Bit(RawCrop(r), frameMax);
        var row = rows.FirstOrDefault(c => c.Mark == sender);
        SetPreviewBitmap(row == null
            ? FrameBitmap.ToBitmap(display, r.Width, r.Height)
            : FrameBitmap.ToMarkedBitmap(display, r.Width, r.Height, set.Pattern, FilterOf(row)));
    }

    private void MarkButton_MouseUp(object? sender, MouseEventArgs e) => RefreshCurvesAndPreview();

    // ---- Curve points ----

    private void HistogramValueChanged(object? sender, EventArgs e)
    {
        if (suppressEvents || !Loaded) return;
        var row = rows.FirstOrDefault(r => r.OriginalValue == sender || r.NewValue == sender);
        if (row == null) return;
        set.SetPreview(FilterOf(row), new CurvePoint((ushort)row.OriginalValue.Value, (ushort)row.NewValue.Value));
        RefreshCurvesAndPreview();
    }

    private void buttonSetStrongPoint_Click(object? sender, EventArgs e)
    {
        if (!Loaded) return;
        var chosen = sender == buttonSetStrongPointRGGB
            ? ActiveRows().ToList()
            : rows.Where(r => r.SetPoint == sender).ToList();
        var message = new StringBuilder();
        foreach (var row in chosen)
        {
            var point = new CurvePoint((ushort)row.OriginalValue.Value, (ushort)row.NewValue.Value);
            set.SetConfirmed(FilterOf(row), point);
            message.Append(message.Length == 0 ? "Set " : ", ").Append($"{FilterOf(row)} {point.Original}->{point.New}");
        }
        if (chosen.Any(r => (ushort)r.OriginalValue.Value == source.Info.WhiteLevel))
            message.Append(". The white level itself always stays fixed.");
        set.ClearPreview();
        statusMessage = message.ToString();
        RefreshCurvesAndPreview();
    }

    private void buttonDeleteStrongPoints_Click(object? sender, EventArgs e)
    {
        if (!Loaded) return;
        if (listViewStrongPointList.CheckedItems.Count == 0)
        {
            SetStatus("Tick the points to delete in the list first.");
            return;
        }
        foreach (ListViewItem item in listViewStrongPointList.CheckedItems)
            if (item.Tag is ListEntry { IsPreview: false } entry)
                set.RemoveConfirmed(entry.Filter, entry.Point.Original);
        set.ClearPreview();
        statusMessage = "Points deleted.";
        SyncNewFieldsToCurve();
        RefreshCurvesAndPreview();
    }

    private void buttonMeasureCurves_Click(object? sender, EventArgs e)
    {
        if (!Loaded || set.Pattern == BayerPattern.None) return;
        if (IsColorFilter(source.Info.Capture.Filter) && MessageBox.Show(this,
                "The filter is RGB, so this may be a color capture. Measuring would treat real color differences " +
                "between the channels as Bayer pattern and remove them. Measure anyway?",
                AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        if (set.HasConfirmedPoints && MessageBox.Show(this, "Replace the existing curve points?",
                AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        MeasuredCurves measured;
        var oldCursor = Cursor;
        Cursor = Cursors.WaitCursor;
        try
        {
            measured = CurveMeasurement.Measure(source, set.Pattern);
        }
        catch (IOException ex)
        {
            statusMessage = "Could not measure the curves: " + ex.Message;
            RefreshStatus();
            return;
        }
        finally
        {
            Cursor = oldCursor;
        }

        if (measured.Points.Values.All(l => l.Count == 0))
        {
            statusMessage = "No curve points could be measured (too few usable pixels). The curves are unchanged.";
            RefreshStatus();
            return;
        }

        set.ReplaceConfirmed(measured.Points);
        var counts = rows.Select(FilterOf).Distinct()
            .Select(f => $"{f} {(measured.Points.TryGetValue(f, out var l) ? l.Count : 0)}");
        statusMessage = $"Measured from {measured.FramesUsed} frames: {string.Join(", ", counts)} points.";
        if (measured.FramesUsed < 10) statusMessage += " Few frames make the measurement less certain.";
        ClearHighlights();
        SyncNewFieldsToCurve();
        RefreshCurvesAndPreview();
    }

    /// <summary>Sets every active New field to what the confirmed curve makes of its Original field.</summary>
    private void SyncNewFieldsToCurve()
    {
        if (!Loaded) return;
        WithoutEvents(() =>
        {
            foreach (var row in ActiveRows())
                if (row.OriginalValue.Value > 0)
                    SetField(row.NewValue, set.ConfirmedCurveValue(FilterOf(row), (ushort)row.OriginalValue.Value, source.Info.WhiteLevel));
        });
    }

    private void FillPointList()
    {
        listViewStrongPointList.BeginUpdate();
        listViewStrongPointList.Items.Clear();
        int number = 0;
        foreach (var filter in ActiveRows().Select(FilterOf))
        {
            var entries = set!.Confirmed(filter).Select(p => new ListEntry(filter, p, false)).ToList();
            if (set.PreviewPoint(filter) is CurvePoint preview) entries.Add(new ListEntry(filter, preview, true));
            foreach (var entry in entries)
            {
                number++;
                var item = new ListViewItem(number.ToString(CultureInfo.CurrentCulture)) { Tag = entry };
                item.SubItems.AddRange(new[]
                {
                    entry.Filter.ToString(),
                    entry.Point.Original.ToString(CultureInfo.CurrentCulture),
                    entry.Point.New.ToString(CultureInfo.CurrentCulture),
                    entry.IsPreview ? "●" : "",
                });
                listViewStrongPointList.Items.Add(item);
            }
        }
        listViewStrongPointList.EndUpdate();
    }

    /// <summary>Shows the selected point in its row and what the confirmed curves do at that value in the others.</summary>
    private void listViewStrongPointList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!Loaded || listViewStrongPointList.SelectedItems.Count == 0) return;
        if (listViewStrongPointList.SelectedItems[0].Tag is not ListEntry entry) return;

        var tables = set.BuildConfirmedTables(source.Info.WhiteLevel);
        ushort original = entry.Point.Original;
        WithoutEvents(() =>
        {
            foreach (var row in ActiveRows())
            {
                var filter = FilterOf(row);
                SetField(row.OriginalValue, original);
                SetField(row.NewValue, filter == entry.Filter ? entry.Point.New : tables[filter].Map(original));
            }
        });
    }

    // ---- Texts ----

    private void SetStatus(string message)
    {
        statusMessage = message;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var parts = new List<string?> { statusMessage, source?.Info.Warning, curveHint };
        if (IsColorFilter(source?.Info.Capture.Filter))
            parts.Add("Filter=RGB: this may be a color capture, channel differences can be real object color.");
        textBoxStatus.Text = string.Join(" | ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }

    private static bool IsColorFilter(string? filter) =>
        string.Equals(filter?.Trim(), "RGB", StringComparison.OrdinalIgnoreCase);

    private string BuildImageInfo(IReadOnlyDictionary<BayerFilter, FilterVariance>? variances)
    {
        var text = new StringBuilder();
        if (variances != null)
        {
            bool none = set!.Pattern == BayerPattern.None;
            var columns = (none
                ? new[] { BayerFilter.All }
                : new[] { BayerFilter.Red, BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue })
                .Select(f => variances[f]).ToList();
            text.AppendLine(none ? "Variances (All):" : "Variances (R|G1|G2|B):");
            AppendVariance(text, "H", columns, v => v.Horizontal);
            AppendVariance(text, "V", columns, v => v.Vertical);
            AppendVariance(text, "D1", columns, v => v.Diagonal1);
            AppendVariance(text, "D2", columns, v => v.Diagonal2);
            AppendVariance(text, "C", columns, v => v.Cross);
            AppendVariance(text, "X", columns, v => v.X);
            AppendVariance(text, "O", columns, v => v.Circle);
        }

        var info = source!.Info;
        var c = info.Capture;
        text.AppendLine($"{Path.GetFileName(info.Path)}: {info.Width}x{info.Height}, {info.BitDepth} bit, white level {info.WhiteLevel}, {info.FrameCount} frame(s)");
        text.AppendLine("Filter: " + (c.Filter ?? "unknown") + (IsColorFilter(c.Filter) ? " (may be a color capture)" : ""));
        AppendCapture(text, "Camera", c.Camera);
        AppendCapture(text, "Exposure", c.ExposureMs is double ms ? Number(ms) + " ms" : null);
        AppendCapture(text, "Gain", Number(c.Gain));
        if (c.WRed != null || c.WGreen != null || c.WBlue != null)
            AppendCapture(text, "Color gains (R/G/B)", $"{Number(c.WRed)} / {Number(c.WGreen)} / {Number(c.WBlue)}");
        AppendCapture(text, "Temperature", c.TemperatureC is double t ? Number(t) + " °C" : null);
        AppendCapture(text, "Note", c.Note);
        return text.ToString();
    }

    private static void AppendVariance(StringBuilder text, string name, List<FilterVariance> columns, Func<FilterVariance, double> value) =>
        text.AppendLine(name + ": " + string.Join("   |   ", columns.Select(v => Number(value(v)))));

    private static void AppendCapture(StringBuilder text, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value)) text.AppendLine(name + ": " + value);
    }

    private static string? Number(double? value) => value?.ToString("0.##", CultureInfo.CurrentCulture);

    // ---- Window ----

    private void PlaceHistograms()
    {
        var hTop = _pbWorkingImage.Bottom + 5;
        var hBottom = textBoxStatus.Top - 5;
        var hLeft = _pbPreview.Right + 5;
        var hRight = textBoxStatus.Right;
        var hHeight = (hBottom - hTop - 15) / 4;
        var hWidth = hRight - hLeft;

        if (checkBoxOnlyRGBHistogram.Checked)
        {
            _pbHistoRGB.Location = new Point(hLeft, hTop);
            _pbHistoRGB.Size = new Size(hWidth, hBottom - hTop - 5);
            _pbHistoB.Location = _pbHistoG.Location = _pbHistoR.Location = new Point(0, 0);
            _pbHistoB.Size = _pbHistoG.Size = _pbHistoR.Size = new Size(0, 0);
            return;
        }

        _pbHistoRGB.Location = new Point(hLeft, hTop);
        _pbHistoRGB.Size = new Size(hWidth, hHeight);
        _pbHistoR.Location = new Point(hLeft, hTop + hHeight + 5);
        _pbHistoR.Size = new Size(hWidth, hHeight);
        _pbHistoG.Location = new Point(hLeft, hTop + hHeight * 2 + 10);
        _pbHistoG.Size = new Size(hWidth, hHeight);
        _pbHistoB.Location = new Point(hLeft, hTop + hHeight * 3 + 15);
        _pbHistoB.Size = new Size(hWidth, hHeight);
    }

    private void Form1_Resize(object? sender, EventArgs e)
    {
        PlaceHistograms();
        Invalidate();
        if (!Loaded || previewRect == null || WindowState == FormWindowState.Minimized) return;
        var r = ComputePreviewRect();
        if (r == previewRect) return;
        previewRect = r;
        RefreshCurvesAndPreview();
    }

    private void checkBoxOnlyRGBHistogram_CheckedChanged(object? sender, EventArgs e)
    {
        PlaceHistograms();
        Invalidate();
    }

    private void buttonConvertFolder_Click(object? sender, EventArgs e)
    {
        if (!Loaded) return;
        using var dialog = new SeriesForm(source.Info.Path, set, textBoxFilenamePrefix.Text,
            textBoxFilenameSuffix.Text, checkBoxSwapBytes.Checked);
        dialog.ShowDialog(this);
    }

    private void buttonExit_Click(object? sender, EventArgs e) => Close();

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        source?.Dispose();
        source = null;
        _pbWorkingImage.Image = null;
        overviewBitmap?.Dispose();
        overviewBitmap = null;
        SetPreviewBitmap(null);
        base.OnFormClosed(e);
    }
}
