using System.Numerics;

namespace AntiBayer.Core.Imaging;

public static class WhiteLevel
{
    /// <summary>
    /// Effective maximum value of the data. bitDepth is the container depth (8 or 16),
    /// headerDepth the depth the file header claims.
    /// </summary>
    public static ushort Detect(ushort[] firstFrame, int bitDepth, int headerDepth)
    {
        if (bitDepth <= 8) return 255;

        int or = 0, max = 0;
        foreach (ushort v in firstFrame) { or |= v; if (v > max) max = v; }
        if (or == 0) return ushort.MaxValue;

        int zeroLowBits = BitOperations.TrailingZeroCount(or);
        if (zeroLowBits > 0) return (ushort)(65536 - (1 << zeroLowBits));

        if (headerDepth is > 0 and < 16 && max < (1 << headerDepth))
            return (ushort)((1 << headerDepth) - 1);

        return ushort.MaxValue;
    }
}
