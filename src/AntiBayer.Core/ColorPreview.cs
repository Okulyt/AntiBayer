using System;

namespace AntiBayer.Core;

/// <summary>Colour view of a raw crop, to see colour fringes of the optics. Display only, never part of the output.</summary>
public static class ColorPreview
{
    /// <summary>
    /// RGB bytes per pixel, row by row. Each channel is the mean of the pixels of that colour in the 3x3 surroundings.
    /// Every filter position is first balanced to the mean of the crop, so a plain area looks grey and only
    /// differences between places remain as colour. scaleMax is the raw value that becomes 255.
    /// </summary>
    public static byte[] ToRgb(ReadOnlySpan<ushort> frame, int imageWidth, int imageHeight, BayerPattern pattern, PixelRect region, int scaleMax)
    {
        if (pattern == BayerPattern.None)
            throw new ArgumentException("A colour view needs a Bayer pattern.", nameof(pattern));

        var sums = new double[4];
        var counts = new int[4];
        for (int y = region.Y; y < region.Y + region.Height; y++)
            for (int x = region.X; x < region.X + region.Width; x++)
            {
                int f = (int)Bayer.FilterAt(pattern, x, y);
                sums[f] += frame[y * imageWidth + x];
                counts[f]++;
            }
        double total = 0;
        int present = 0;
        var means = new double[4];
        for (int f = 0; f < 4; f++)
        {
            if (counts[f] == 0) continue;
            means[f] = sums[f] / counts[f];
            total += means[f];
            present++;
        }
        double level = present > 0 ? total / present : 0;
        var gains = new double[4];
        for (int f = 0; f < 4; f++) gains[f] = means[f] > 0 ? level / means[f] : 1;

        var rgb = new byte[3 * region.Width * region.Height];
        if (scaleMax <= 0) return rgb;
        double toByte = 255.0 / scaleMax;
        var channel = new double[3];
        var number = new int[3];
        int o = 0;
        for (int y = region.Y; y < region.Y + region.Height; y++)
            for (int x = region.X; x < region.X + region.Width; x++)
            {
                Array.Clear(channel);
                Array.Clear(number);
                for (int ny = Math.Max(0, y - 1); ny <= Math.Min(imageHeight - 1, y + 1); ny++)
                    for (int nx = Math.Max(0, x - 1); nx <= Math.Min(imageWidth - 1, x + 1); nx++)
                    {
                        var filter = Bayer.FilterAt(pattern, nx, ny);
                        int c = filter == BayerFilter.Red ? 0 : filter == BayerFilter.Blue ? 2 : 1;
                        channel[c] += frame[ny * imageWidth + nx] * gains[(int)filter];
                        number[c]++;
                    }
                for (int c = 0; c < 3; c++)
                    rgb[o++] = number[c] == 0 ? (byte)0 : (byte)Math.Min(255.0, Math.Round(channel[c] / number[c] * toByte));
            }
        return rgb;
    }
}
