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
    private (int X, int Y)? selectedPixel;
    private string? pixelInfo;
    private string? statusMessage;
    private string? curveHint;

    private readonly string configPath = AppConfig.DefaultPath;
    private readonly AppConfig config;
    private Histogram? rawHistogram;
    private BayerPattern? rawHistogramPattern;
    private Histogram? correctedHistogram;

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
        config = AppConfig.Load(configPath);
        SelectPatternRadio(config.LastPattern);
        WireUp();
        PlaceHistograms();
    }

    private void WireUp()
    {
        buttonSave.Click += buttonSave_Click;
        buttonSaveValues.Click += buttonSaveValues_Click;
        buttonLoadValues.Click += buttonLoadValues_Click;
        _pbHistoRGB.Paint += (_, e) => PaintHistogram(_pbHistoRGB, e.Graphics, RgbChannels());
        _pbHistoR.Paint += (_, e) => PaintHistogram(_pbHistoR, e.Graphics, SingleChannel(BayerFilter.Red, Color.Red));
        _pbHistoG.Paint += (_, e) => PaintHistogram(_pbHistoG, e.Graphics, GreenChannels());
        _pbHistoB.Paint += (_, e) => PaintHistogram(_pbHistoB, e.Graphics, SingleChannel(BayerFilter.Blue, Color.Blue));
        // The list and the histograms share the right-hand column; PlaceHistograms sets their bounds.
        foreach (Control c in new Control[] { listViewStrongPointList, _pbHistoRGB, _pbHistoR, _pbHistoG, _pbHistoB })
            c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
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
            Filter = "Images (*.ser;*.tif;*.tiff;*.png;*.fit;*.fits;*.fts)|*.ser;*.tif;*.tiff;*.png;*.fit;*.fits;*.fts|All files (*.*)|*.*",
            ValidateNames = true,
            InitialDirectory = StartFolder(),
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
        selectedPixel = null;
        pixelInfo = null;
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
        buttonSave.Enabled = true;
        buttonSaveValues.Enabled = true;
        buttonLoadValues.Enabled = true;
        Remember(path);
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
            // The current curves are discarded on OK; Cancel keeps the open file and its curves.
            var answer = MessageBox.Show(this,
                $"The curves cannot be used for this file: {reason}.\n\n"
                + "OK opens the file with a new empty set and discards the current curves "
                + "(you can save them first with \"save values\" and cancel now).\n"
                + "Cancel keeps the current file and curves.",
                AppTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (answer != DialogResult.OK)
            {
                note = null;
                return null;
            }
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
        set.PatternKnown = true; // the pattern now comes from this file, not from a legacy placeholder
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
        else if (hadPoints) statusMessage = "Pattern is now " + wanted
            + ". Curves stay with their colour name; if they were measured under the old pattern, measure again.";
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
        buttonMeasureHere.Enabled = Loaded && !none && previewRect != null;
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
        if (set.HasConfirmedPoints)
            statusMessage += " The existing points belong to the previous reading; measure them again.";
        ShowFrameData();
    }

    // ---- Display ----

    /// <summary>Overview and preview after the raw frame changed.</summary>
    private void ShowFrameData()
    {
        if (!Loaded) return;
        frameMax = rawFrame.Max();
        rawHistogramPattern = null; // the raw frame changed: the raw histogram is recomputed once
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
        UpdateHistograms(tables);
        textBoxImageInfo.Text = BuildImageInfo(variances);
        pixelInfo = BuildPixelInfo(tables);
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
        selectedPixel = null;
        pixelInfo = null;
        _pbPreview.Invalidate();
        ApplyRowStates();
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
        selectedPixel = (x, y);
        statusMessage = null; // the pixel part of the status takes over
        RefreshCurvesAndPreview();
        _pbPreview.Invalidate();
    }

    private void _pbPreview_Paint(object? sender, PaintEventArgs e)
    {
        if (!Loaded || previewRect is not PixelRect r || selectedPixel is not (int x, int y)) return;
        if (PreviewGeometry.PixelToZoomedRect(x - r.X, y - r.Y, _pbPreview.Width, _pbPreview.Height, r.Width, r.Height)
            is not PixelRect box) return;
        using var pen = new Pen(Color.Yellow, 1);
        e.Graphics.DrawRectangle(pen, box.X, box.Y, Math.Max(0, box.Width - 1), Math.Max(0, box.Height - 1));
    }

    /// <summary>Raw value of the clicked pixel and what the preview curve makes of it, plus how many pixels of that filter change.</summary>
    private string? BuildPixelInfo(IReadOnlyDictionary<BayerFilter, CurveTable> previewTables)
    {
        if (!Loaded || selectedPixel is not (int x, int y)) return null;
        var info = source.Info;
        var filter = BayerGeometry.FilterAt(set.Pattern, x, y);
        ushort raw = rawFrame[y * info.Width + x];
        string name = filter switch
        {
            BayerFilter.Red => "R",
            BayerFilter.Green1 => "G1",
            BayerFilter.Green2 => "G2",
            BayerFilter.Blue => "B",
            _ => "All",
        };
        var text = $"Pixel ({x},{y}) {name}: {raw} -> {previewTables[filter].Map(raw)}";
        if (set.PreviewPoint(filter) != null)
        {
            var confirmed = set.BuildConfirmedTables(info.WhiteLevel)[filter];
            int changed = FrameCorrector.CountChanged(rawFrame, info.Width, info.Height, set.Pattern, filter,
                confirmed, previewTables[filter]);
            text += $" | {changed} pixels change";
        }
        return text;
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

    private void buttonMeasureHere_Click(object? sender, EventArgs e)
    {
        if (!Loaded || set.Pattern == BayerPattern.None || previewRect is not PixelRect r) return;
        bool hadPoints = set.HasConfirmedPoints;

        MeasuredCurves measured;
        var oldCursor = Cursor;
        Cursor = Cursors.WaitCursor;
        try
        {
            measured = CurveMeasurement.MeasureRegion(source, set.Pattern, r);
        }
        catch (IOException ex)
        {
            statusMessage = "Could not measure the crop: " + ex.Message;
            RefreshStatus();
            return;
        }
        finally
        {
            Cursor = oldCursor;
        }

        if (measured.Points.Values.All(l => l.Count == 0))
        {
            statusMessage = "No curve points could be measured in the crop (too few usable pixels). The curves are unchanged.";
            RefreshStatus();
            return;
        }

        int replaced = set.MergeMeasured(measured.Points);
        var counts = rows.Select(FilterOf).Distinct()
            .Select(f => $"{f} {(measured.Points.TryGetValue(f, out var l) ? l.Count : 0)}");
        statusMessage = $"Measured in the crop from {measured.FramesUsed} frames: {string.Join(", ", counts)} points, {replaced} existing points replaced.";
        if (!hadPoints) statusMessage += " Measuring the whole series with \"Measure curves\" first usually makes sense.";
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
        var parts = new List<string?> { statusMessage, pixelInfo, source?.Info.Warning, curveHint };
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

    private const int ColumnTop = 347;
    private const int ColumnLeft = 566;

    /// <summary>Point list (40 %) and histograms (60 %) share the right-hand column below the button rows.</summary>
    private void PlaceHistograms()
    {
        int bottom = textBoxStatus.Top - 5;
        int width = Math.Max(100, textBoxStatus.Right - ColumnLeft - 6);
        int total = Math.Max(0, bottom - ColumnTop);
        int listHeight = total * 40 / 100;
        listViewStrongPointList.SetBounds(ColumnLeft, ColumnTop, width, listHeight);

        int hTop = ColumnTop + listHeight + 5;
        int hHeight4 = Math.Max(0, (bottom - hTop - 15) / 4);

        if (checkBoxOnlyRGBHistogram.Checked)
        {
            _pbHistoRGB.SetBounds(ColumnLeft, hTop, width, Math.Max(0, bottom - hTop));
            foreach (var box in new[] { _pbHistoR, _pbHistoG, _pbHistoB }) box.SetBounds(0, 0, 0, 0);
            return;
        }

        _pbHistoRGB.SetBounds(ColumnLeft, hTop, width, hHeight4);
        _pbHistoR.SetBounds(ColumnLeft, hTop + hHeight4 + 5, width, hHeight4);
        _pbHistoG.SetBounds(ColumnLeft, hTop + hHeight4 * 2 + 10, width, hHeight4);
        _pbHistoB.SetBounds(ColumnLeft, hTop + hHeight4 * 3 + 15, width, hHeight4);
    }

    // ---- Histograms ----

    /// <summary>Raw histogram only when the frame or the pattern changed; the corrected half on every value change.</summary>
    private void UpdateHistograms(IReadOnlyDictionary<BayerFilter, CurveTable> previewTables)
    {
        if (!Loaded) return;
        var info = source.Info;
        if (rawHistogram == null || rawHistogramPattern != set.Pattern)
        {
            rawHistogram = Histogram.Compute(rawFrame, info.Width, info.Height, set.Pattern, info.BitDepth);
            rawHistogramPattern = set.Pattern;
        }
        correctedHistogram = rawHistogram.Transform(previewTables);
        foreach (var box in new[] { _pbHistoRGB, _pbHistoR, _pbHistoG, _pbHistoB }) box.Invalidate();
    }

    private (BayerFilter Filter, Color Color)[] RgbChannels() => set?.Pattern == BayerPattern.None
        ? new[] { (BayerFilter.All, Color.Gray) }
        : new[]
        {
            (BayerFilter.Red, Color.Red), (BayerFilter.Green1, Color.LimeGreen),
            (BayerFilter.Green2, Color.DarkGreen), (BayerFilter.Blue, Color.Blue),
        };

    private (BayerFilter Filter, Color Color)[] GreenChannels() => set?.Pattern == BayerPattern.None
        ? Array.Empty<(BayerFilter, Color)>()
        : new[] { (BayerFilter.Green1, Color.LimeGreen), (BayerFilter.Green2, Color.DarkGreen) };

    private (BayerFilter Filter, Color Color)[] SingleChannel(BayerFilter filter, Color color) =>
        set?.Pattern == BayerPattern.None ? Array.Empty<(BayerFilter, Color)>() : new[] { (filter, color) };

    /// <summary>Left half: raw classes, right half: corrected classes. All boxes share one scale, G2 included.</summary>
    private void PaintHistogram(PictureBox box, Graphics g, (BayerFilter Filter, Color Color)[] channels)
    {
        g.Clear(Color.White);
        if (rawHistogram == null || correctedHistogram == null || channels.Length == 0) return;

        int height = box.Height - 1;
        double scaleX = (box.Width - 15 - channels.Length) / (2.0 * Histogram.Classes);
        if (scaleX <= 0 || height <= 0) return;
        long max = Math.Max(rawHistogram.MaxAll, correctedHistogram.MaxAll);

        for (int j = 0; j < channels.Length; j++)
        {
            using var brush = new SolidBrush(channels[j].Color);
            var raw = rawHistogram.Of(channels[j].Filter);
            var corrected = correctedHistogram.Of(channels[j].Filter);
            for (int i = 0; i < Histogram.Classes; i++)
            {
                DrawBar(g, brush, (float)(i * scaleX + j), Histogram.BarHeight(raw[i], max, height), height);
                DrawBar(g, brush, (float)((Histogram.Classes + i) * scaleX + j + 15), Histogram.BarHeight(corrected[i], max, height), height);
            }
        }
        float divider = (float)(Histogram.Classes * scaleX + channels.Length + 7);
        g.DrawLine(Pens.LightGray, divider, 0, divider, height);
    }

    private static void DrawBar(Graphics g, Brush brush, float x, int barHeight, int height)
    {
        if (barHeight > 0) g.FillRectangle(brush, x, height - barHeight, 1, barHeight);
    }

    // ---- Settings and dialog folders ----

    private string? StartFolder() =>
        config.LastFolder != null && Directory.Exists(config.LastFolder) ? config.LastFolder : null;

    private void Remember(string path)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) config.LastFolder = folder;
    }

    private void SaveConfig()
    {
        config.LastPattern = set?.Pattern ?? SelectedPattern();
        try
        {
            config.Save(configPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine("Could not save the settings: " + e.Message);
        }
    }

    // ---- Save image ----

    private void buttonSave_Click(object? sender, EventArgs e)
    {
        if (!Loaded) return;
        var info = source.Info;
        bool ser = info.Format == ImageFormat.Ser;
        string stem = textBoxFilenamePrefix.Text + Path.GetFileNameWithoutExtension(info.Path) + textBoxFilenameSuffix.Text
            + (ser ? "_frame" + (frameIndex + 1) : "");
        string ext = ser ? ".tif" : Path.GetExtension(info.Path).ToLowerInvariant();
        bool fits = info.Format == ImageFormat.Fits;
        string filter = fits ? "FITS image (*.fit;*.fits;*.fts)|*.fit;*.fits;*.fts"
            : ext == ".png" ? "PNG image (*.png)|*.png" : "TIFF image (*.tif;*.tiff)|*.tif;*.tiff";

        using var dialog = new SaveFileDialog
        {
            Title = ser ? "Save the current frame as TIFF..." : "Save the corrected image...",
            Filter = filter,
            DefaultExt = ext.TrimStart('.'),
            AddExtension = true,
            OverwritePrompt = true,
            CheckPathExists = true,
            FileName = stem + ext,
            InitialDirectory = StartFolder() ?? Path.GetDirectoryName(info.Path),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string target = dialog.FileName;
        string targetExt = Path.GetExtension(target).ToLowerInvariant();
        bool extOk = fits ? targetExt is ".fit" or ".fits" or ".fts"
            : ext == ".png" ? targetExt == ".png" : targetExt is ".tif" or ".tiff";
        if (!extOk)
        {
            MessageBox.Show(this, "The file name must end in " + (fits ? ".fit, .fits or .fts" : ext == ".png" ? ".png" : ".tif or .tiff") + ".",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.Equals(Path.GetFullPath(target), Path.GetFullPath(info.Path), StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "The target must not be the source file.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Confirmed points only: no preview point, no dither.
        var corrected = new ushort[rawFrame.Length];
        new FrameCorrector(set.Pattern, set.BuildConfirmedTables(info.WhiteLevel))
            .Apply(rawFrame, corrected, info.Width, info.Height, null);

        string tmp = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(target))!, "." + Guid.NewGuid().ToString("N") + targetExt);
        try
        {
            if (ser)
            {
                TiffFrameWriter.Write(tmp, corrected, info.Width, info.Height, info.BitDepth);
            }
            else
            {
                using var sink = ImageFiles.CreateSink(source, tmp);
                sink.WriteFrame(corrected);
                sink.Complete();
            }
            File.Move(tmp, target, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or UnsupportedImageException
                                       or NotSupportedException or System.Runtime.InteropServices.COMException)
        {
            SetStatus("Could not save " + Path.GetFileName(target) + ": " + ex.Message);
            MessageBox.Show(this, ex.Message, "Could not save " + Path.GetFileName(target), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        finally
        {
            try
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("Could not remove the temporary file: " + cleanup.Message);
            }
        }
        Remember(target);
        SetStatus("Saved " + Path.GetFileName(target)
            + (set.HasPreviewPoints ? ". The value not yet set is not part of the saved image." : ""));
    }

    // ---- Parameter sets (.cps) ----

    private void buttonSaveValues_Click(object? sender, EventArgs e)
    {
        if (!Loaded) return;
        var info = source.Info;
        // The capture data comes from the current file; the note of an earlier set is kept as a starting point.
        var initial = info.Capture with { Note = info.Capture.Note ?? set.Capture.Note };
        using var form = new CaptureInfoForm(initial);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        using var dialog = new SaveFileDialog
        {
            Title = "Save curve values...",
            Filter = "Parameter sets (*.cps)|*.cps",
            DefaultExt = "cps",
            AddExtension = true,
            OverwritePrompt = true,
            CheckPathExists = true,
            FileName = Path.GetFileNameWithoutExtension(info.Path) + ".cps",
            InitialDirectory = StartFolder() ?? Path.GetDirectoryName(info.Path),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        // Never write a parameter set over another kind of file (an image or a recording).
        if (!string.Equals(Path.GetExtension(dialog.FileName), ".cps", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("Not saved: the file name must end in .cps.");
            return;
        }

        set.Capture = form.Result;
        try
        {
            CpsFile.Save(dialog.FileName, set);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            SetStatus("Could not save the values: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Could not save " + Path.GetFileName(dialog.FileName), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Remember(dialog.FileName);
        SetStatus("Saved the values to " + Path.GetFileName(dialog.FileName));
    }

    private void buttonLoadValues_Click(object? sender, EventArgs e)
    {
        if (!Loaded) return;
        using var dialog = new OpenFileDialog
        {
            Title = "Load curve values...",
            Filter = "Parameter sets (*.cps)|*.cps|All files (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            InitialDirectory = StartFolder(),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        ParameterSet loaded;
        try
        {
            loaded = CpsFile.Load(dialog.FileName);
        }
        catch (Exception ex) when (ex is CpsFormatException or IOException or UnauthorizedAccessException)
        {
            ReportLoadFailure(dialog.FileName, ex.Message);
            return;
        }
        Remember(dialog.FileName);

        var info = source.Info;
        var current = set.Pattern;
        var compatibility = loaded.CompatibleWith(info.BitDepth, current);
        if (compatibility is SetCompatibility.BitDepthMismatch or SetCompatibility.CurveCountMismatch)
        {
            string reason = compatibility == SetCompatibility.BitDepthMismatch
                ? $"their bit depth ({loaded.BitDepth}) differs from this file ({info.BitDepth})"
                : $"their number of curves does not fit pattern {current}";
            SetStatus("The values were not used, " + reason + ".");
            MessageBox.Show(this, "The values cannot be used, " + reason + ".", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (compatibility == SetCompatibility.PatternDiffers && MessageBox.Show(this,
                $"The values were made for pattern {loaded.Pattern}, this file uses {current}. " +
                "The curves would stay with their filter position. Use them anyway?",
                AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var differences = loaded.Capture.DifferencesTo(info.Capture);
        if (differences.Count > 0 && MessageBox.Show(this,
                "The values were measured under different capture settings:\n\n  " + string.Join("\n  ", differences) +
                "\n\nUse them anyway?", AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        if (set.HasConfirmedPoints && MessageBox.Show(this, "Replace the existing curve points?",
                AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // A legacy file carries only a placeholder pattern; from here on the pattern of the current file counts.
        loaded.ChangePattern(current);
        loaded.PatternKnown = true;
        set = loaded;
        ClearHighlights();
        statusMessage = "Loaded the values from " + Path.GetFileName(dialog.FileName);
        SyncNewFieldsToCurve();
        RefreshCurvesAndPreview();
    }

    private void Form1_Resize(object? sender, EventArgs e)
    {
        PlaceHistograms();
        Invalidate();
        if (!Loaded || previewRect == null || WindowState == FormWindowState.Minimized) return;
        var r = ComputePreviewRect();
        if (r == previewRect) return;
        previewRect = r;
        if (selectedPixel is (int sx, int sy) && (sx < r.X || sy < r.Y || sx >= r.X + r.Width || sy >= r.Y + r.Height))
            selectedPixel = null;
        ApplyRowStates();
        RefreshCurvesAndPreview();
        _pbPreview.Invalidate();
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
        SaveConfig();
        source?.Dispose();
        source = null;
        _pbWorkingImage.Image = null;
        overviewBitmap?.Dispose();
        overviewBitmap = null;
        SetPreviewBitmap(null);
        base.OnFormClosed(e);
    }
}
