using System;
using System.IO;
using System.Text;

namespace AntiBayer.Tests;

internal static class TestData
{
    /// <summary>
    /// Writes a synthetic SER file. pixel(frame, x, y) gives the value; depth &lt;= 8 writes one byte,
    /// otherwise two bytes little endian. The trailer holds 8 distinct bytes per frame.
    /// </summary>
    public static void WriteSer(string path, int w, int h, int depth, int colorId, int frames,
        Func<int, int, int, ushort> pixel, bool trailer = true, int littleEndianFlag = 0)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);
        bw.Write(Encoding.ASCII.GetBytes("LUCAM-RECORDER"));
        bw.Write(0);
        bw.Write(colorId);
        bw.Write(littleEndianFlag);
        bw.Write(w);
        bw.Write(h);
        bw.Write(depth);
        bw.Write(frames);
        WriteText(bw, "offset=0");
        WriteText(bw, "Camera");
        WriteText(bw, "Scope");
        bw.Write(123456789L);
        bw.Write(987654321L);

        for (int f = 0; f < frames; f++)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ushort v = pixel(f, x, y);
                    if (depth <= 8) bw.Write((byte)v);
                    else { bw.Write((byte)(v & 0xFF)); bw.Write((byte)(v >> 8)); }
                }

        if (trailer)
            for (int f = 0; f < frames; f++) bw.Write(0x0100000000000000L * (f + 1) + 0x1122334455L + f);
    }

    private static void WriteText(BinaryWriter bw, string s)
    {
        var b = new byte[40];
        Encoding.ASCII.GetBytes(s).CopyTo(b, 0);
        bw.Write(b);
    }

    public static string TempDir()
    {
        string d = Path.Combine(Path.GetTempPath(), "AntiBayerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
