using System;
using System.Globalization;
using System.IO;
using System.Text;
using AntiBayer.Core;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class CaptureInfoReaderTests
{
    private const string Sidecar =
        "------\r\nFireCapture v2.7\r\nCamera=QHY5III462C\r\nFilter=IR740\r\nShutter=12.45ms\r\nGain=95 (13%)\r\n" +
        "WBlue=128\r\nWGreen=64\r\nWRed=64\r\n---------- Timestamps at start of exposure ----------\r\n" +
        "Frame 1:\t170225 192458.947\r\nGain=7\r\nFrame 2:\t170225 192458.960\r\n";

    [Fact] public void Sidecar_liefert_alle_Werte()
    {
        var c = CaptureInfoReader.ParseSidecar(Sidecar);
        Assert.Equal("QHY5III462C", c.Camera);
        Assert.Equal("IR740", c.Filter);
        Assert.Equal(12.45, c.ExposureMs);
        Assert.Equal(95, c.Gain);
        Assert.Equal(64, c.WRed);
        Assert.Equal(64, c.WGreen);
        Assert.Equal(128, c.WBlue);
        Assert.Null(c.TemperatureC);
    }

    [Fact] public void Sidecar_liest_nicht_in_die_Zeitstempel()
    {
        var c = CaptureInfoReader.ParseSidecar(Sidecar);
        Assert.Equal(95, c.Gain); // "Gain=7" after the timestamps header is ignored
        var d = CaptureInfoReader.ParseSidecar("Camera=A\r\n---------- Timestamps at start of exposure ----------\r\nFilter=X\r\n");
        Assert.Null(d.Filter);
    }

    [Fact] public void Sidecar_mit_Temperatur()
    {
        var c = CaptureInfoReader.ParseSidecar("Camera=A\r\nSensor Temperature=-5.5 C\r\nTEMP2=ignored\r\n");
        Assert.Equal(-5.5, c.TemperatureC);
    }

    [Fact] public void SER_Felder_FireCapture()
    {
        var c = CaptureInfoReader.ParseSerFields("offset=0", "QHY=QHY5III462C", "fps=80.00gain=95exp=12.45");
        Assert.Equal("QHY5III462C", c.Camera);
        Assert.Equal(95, c.Gain);
        Assert.Equal(12.45, c.ExposureMs);
    }

    [Fact] public void SER_Felder_TIS_Beispiel()
    {
        var c = CaptureInfoReader.ParseSerFields("", "TIS=DMx 21AU04.AS", "fps=6.10gain=912exp=83.60");
        Assert.Equal("DMx 21AU04.AS", c.Camera);
        Assert.Equal(912, c.Gain);
        Assert.Equal(83.60, c.ExposureMs);
    }

    [Fact] public void Fremde_SER_Felder_bleiben_leer()
    {
        var c = CaptureInfoReader.ParseSerFields("Max", "ZWO ASI462MC", "C11");
        Assert.Equal("ZWO ASI462MC", c.Camera);
        Assert.Null(c.Gain);
        Assert.Null(c.ExposureMs);
        Assert.Null(c.Filter);
    }

    [Fact] public void Dezimalkomma_und_Punkt_invariant()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal(12.45, CaptureInfoReader.ParseSidecar("Shutter=12.45ms").ExposureMs);
            Assert.Equal(83.60, CaptureInfoReader.ParseSerFields("", "", "fps=6.10gain=912exp=83.60").ExposureMs);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    private static string Temp()
    {
        string d = Path.Combine(Path.GetTempPath(), "ab-ci-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    private static SerHeader Header(string obs, string ins, string tel) =>
        new() { Observer = obs, Instrument = ins, Telescope = tel };

    [Fact] public void Sidecar_hat_Vorrang_vor_SER_Feldern_und_fehlende_werden_ergaenzt()
    {
        string d = Temp();
        try
        {
            string ser = Path.Combine(d, "a.ser");
            File.WriteAllText(Path.Combine(d, "a.txt"), "Camera=SideCam\r\nGain=10\r\n", Encoding.Latin1);
            var c = CaptureInfoReader.ForFile(ser, Header("", "QHY=SerCam", "fps=1gain=99exp=5.5"));
            Assert.Equal("SideCam", c.Camera);
            Assert.Equal(10, c.Gain);
            Assert.Equal(5.5, c.ExposureMs); // missing in sidecar, filled from SER
        }
        finally { Directory.Delete(d, true); }
    }

    [Fact] public void Anders_benannte_txt_ist_kein_Sidecar_Rueckfall_auf_SER()
    {
        string d = Temp();
        try
        {
            string ser = Path.Combine(d, "2024-09-14-0844_9-U-CaK-Sun.ser");
            File.WriteAllText(Path.Combine(d, "2024-09-14-0844_9-U-CaK-Sun_info.txt"), "Camera=Other\r\nGain=1\r\n");
            var c = CaptureInfoReader.ForFile(ser, Header("", "QHY=QHY5III462C", "fps=6.10gain=912exp=83.60"));
            Assert.Equal("QHY5III462C", c.Camera);
            Assert.Equal(912, c.Gain);
        }
        finally { Directory.Delete(d, true); }
    }

    [Fact] public void Latin1_Sidecar_und_kein_Header()
    {
        string d = Temp();
        try
        {
            File.WriteAllText(Path.Combine(d, "b.txt"), "Camera=Cam\u00B0\r\nFilter=Gr\u00FCn\r\n", Encoding.Latin1);
            var c = CaptureInfoReader.ForFile(Path.Combine(d, "b.tif"), null);
            Assert.Equal("Cam\u00B0", c.Camera);
            Assert.Equal("Gr\u00FCn", c.Filter);
            Assert.Equal(new CaptureInfo(), CaptureInfoReader.ForFile(Path.Combine(d, "none.tif"), null));
        }
        finally { Directory.Delete(d, true); }
    }
}
