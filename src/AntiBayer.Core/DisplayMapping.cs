using System;

namespace AntiBayer.Core;

public static class DisplayMapping
{
    /// <summary>
    /// Scales a frame to 8 bit, normalised to the frame maximum (rounded). An all-zero frame
    /// gives zeros; 8-bit data with maximum 255 is returned unchanged. The input is not modified.
    /// </summary>
    public static byte[] To8Bit(ReadOnlySpan<ushort> frame)
    {
        var result = new byte[frame.Length];
        int max = 0;
        foreach (var v in frame) if (v > max) max = v;
        if (max == 0) return result;
        long half = max / 2;
        for (int i = 0; i < frame.Length; i++)
            result[i] = (byte)((frame[i] * 255L + half) / max);
        return result;
    }

    /// <summary>
    /// Scales with a given maximum and limits to 255, so that several crops (corrected or not) stay comparable.
    /// A maximum of 0 or below gives zeros. The input is not modified.
    /// </summary>
    public static byte[] To8Bit(ReadOnlySpan<ushort> frame, int scaleMax)
    {
        var result = new byte[frame.Length];
        if (scaleMax <= 0) return result;
        long half = scaleMax / 2;
        for (int i = 0; i < frame.Length; i++)
            result[i] = (byte)Math.Min((frame[i] * 255L + half) / scaleMax, 255L);
        return result;
    }
}
