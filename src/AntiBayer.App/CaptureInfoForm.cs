using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AntiBayer.Core;

namespace AntiBayer;

/// <summary>Lets the user check and edit the capture data that is stored with a parameter set.</summary>
public sealed class CaptureInfoForm : Form
{
    private readonly TextBox camera = new(), filter = new(), exposure = new(), gain = new(),
        wRed = new(), wGreen = new(), wBlue = new(), temperature = new(), note = new();

    public CaptureInfo Result { get; private set; }

    public CaptureInfoForm(CaptureInfo initial)
    {
        Result = initial;
        Text = "Capture data for the parameter set";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 330);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 270, ColumnCount = 2, RowCount = 9, Padding = new Padding(10, 10, 10, 0),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(table, "Camera", camera, initial.Camera);
        AddRow(table, "Filter", filter, initial.Filter);
        AddRow(table, "Exposure (ms)", exposure, Format(initial.ExposureMs));
        AddRow(table, "Gain", gain, Format(initial.Gain));
        AddRow(table, "Color gain R", wRed, Format(initial.WRed));
        AddRow(table, "Color gain G", wGreen, Format(initial.WGreen));
        AddRow(table, "Color gain B", wBlue, Format(initial.WBlue));
        AddRow(table, "Temperature (°C)", temperature, Format(initial.TemperatureC));
        AddRow(table, "Note", note, initial.Note);
        Controls.Add(table);

        var ok = new Button { Text = "OK", DialogResult = DialogResult.None, Location = new Point(240, 290), Size = new Size(80, 28) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(330, 290), Size = new Size(80, 28) };
        ok.Click += (_, _) => Accept();
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel table, string label, TextBox box, string? value)
    {
        box.Text = value ?? "";
        box.Dock = DockStyle.Fill;
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) });
        table.Controls.Add(box);
    }

    private static string? Format(double? v) => v?.ToString("R", CultureInfo.CurrentCulture);

    private void Accept()
    {
        if (!TryNumber(exposure, "Exposure", out var exp) || !TryNumber(gain, "Gain", out var g)
            || !TryNumber(wRed, "Color gain R", out var r) || !TryNumber(wGreen, "Color gain G", out var gr)
            || !TryNumber(wBlue, "Color gain B", out var b) || !TryNumber(temperature, "Temperature", out var t))
            return;
        Result = new CaptureInfo(Trimmed(camera), Trimmed(filter), exp, g, r, gr, b, t, Trimmed(note));
        DialogResult = DialogResult.OK;
    }

    private static string? Trimmed(TextBox box) => string.IsNullOrWhiteSpace(box.Text) ? null : box.Text.Trim();

    private bool TryNumber(TextBox box, string name, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(box.Text)) return true;
        if (double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out var v) && double.IsFinite(v))
        {
            value = v;
            return true;
        }
        MessageBox.Show(this, name + " must be a number or empty.", "Capture data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        box.Focus();
        return false;
    }
}
