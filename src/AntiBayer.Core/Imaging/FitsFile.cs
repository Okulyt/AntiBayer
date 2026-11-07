using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AntiBayer.Core.Imaging;

internal static class FitsLayout
{
    public const int Block = 2880;
    public const int CardLength = 80;
}

/// <summary>
/// Simple FITS: a 2-D primary image with BITPIX 8 or 16 (BZERO 0 or 32768, BSCALE 1). Pixels are
/// big endian and kept in file row order. Header, padding and any HDUs after the data are preserved by <see cref="FitsSink"/>.
/// </summary>
public sealed class FitsSource : IFrameSource
{
    private readonly ushort[] _pixels;

    public ImageInfo Info { get; }

    /// <summary>Header blocks of the primary HDU, verbatim.</summary>
    internal byte[] HeaderBytes { get; }
    /// <summary>File offset of the first byte after the padded primary data (start of further HDUs).</summary>
    internal long TailOffset { get; }
    internal int Bitpix { get; }
    internal bool HasBzero { get; }

    public FitsSource(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var header = ReadHeader(fs, out var cards);
            HeaderBytes = header;

            int bitpix = Int(cards, "BITPIX") ?? throw Bad();
            int naxis = Int(cards, "NAXIS") ?? throw Bad();
            if (naxis != 2)
                throw new UnsupportedImageException("Only two-dimensional FITS images are supported (NAXIS must be 2).");
            if (bitpix != 8 && bitpix != 16)
                throw new UnsupportedImageException("Only 8 and 16 bit integer FITS images are supported.");
            int w = Int(cards, "NAXIS1") ?? throw Bad();
            int h = Int(cards, "NAXIS2") ?? throw Bad();
            if (w <= 0 || h <= 0) throw Bad();

            double bscale = Dbl(cards, "BSCALE") ?? 1.0;
            double bzero = Dbl(cards, "BZERO") ?? 0.0;
            bool hasBzero = false;
            if (bscale != 1.0) throw new UnsupportedImageException("FITS images with BSCALE other than 1 are not supported.");
            if (bitpix == 8 && bzero != 0.0) throw new UnsupportedImageException("FITS images with BZERO other than 0 are not supported.");
            if (bitpix == 16)
            {
                if (bzero == 32768.0) hasBzero = true;
                else if (bzero != 0.0) throw new UnsupportedImageException("FITS images with this BZERO value are not supported.");
            }
            Bitpix = bitpix;
            HasBzero = hasBzero;

            int bytesPer = bitpix / 8;
            long dataBytes = (long)w * h * bytesPer;
            long padded = (dataBytes + FitsLayout.Block - 1) / FitsLayout.Block * FitsLayout.Block;
            long dataStart = header.Length;
            if (fs.Length < dataStart + dataBytes)
                throw new UnsupportedImageException("The FITS file is truncated.");
            TailOffset = Math.Min(dataStart + padded, fs.Length);

            fs.Position = dataStart;
            var raw = new byte[dataBytes];
            fs.ReadExactly(raw);
            _pixels = new ushort[(long)w * h];
            if (bitpix == 8)
                for (int i = 0; i < _pixels.Length; i++) _pixels[i] = raw[i];
            else
                for (int i = 0; i < _pixels.Length; i++)
                {
                    int s = (short)((raw[2 * i] << 8) | raw[2 * i + 1]);
                    if (hasBzero) _pixels[i] = (ushort)(s + 32768);
                    else if (s < 0) throw new UnsupportedImageException("This FITS image contains negative values and no BZERO; it is not supported.");
                    else _pixels[i] = (ushort)s;
                }

            ushort white;
            if (bitpix == 8) white = 255;
            else if (hasBzero) white = WhiteLevel.Detect(_pixels, 16, 16);
            else white = 32767;
            Info = new ImageInfo(path, ImageFormat.Fits, w, h, bitpix, 1, white, null, CaptureInfoReader.ForFile(path, null), null);
        }
        catch (Exception e) when (e is IOException or OverflowException or OutOfMemoryException)
        {
            throw new UnsupportedImageException("The file could not be read as a FITS image.");
        }
    }

    private static UnsupportedImageException Bad() => new("The FITS header is damaged or incomplete.");

    private static byte[] ReadHeader(Stream fs, out Dictionary<string, string> cards)
    {
        cards = new Dictionary<string, string>(StringComparer.Ordinal);
        var all = new List<byte>();
        var block = new byte[FitsLayout.Block];
        bool first = true;
        while (true)
        {
            int n = fs.Read(block, 0, block.Length);
            if (n != block.Length) throw new UnsupportedImageException("The file is not a valid FITS file.");
            all.AddRange(block);
            for (int c = 0; c < FitsLayout.Block; c += FitsLayout.CardLength)
            {
                string card = Encoding.ASCII.GetString(block, c, FitsLayout.CardLength);
                if (first)
                {
                    if (!card.StartsWith("SIMPLE  =", StringComparison.Ordinal))
                        throw new UnsupportedImageException("The file is not a valid FITS file.");
                    first = false;
                }
                string key = card.Substring(0, 8).TrimEnd();
                if (key == "END") return all.ToArray();
                if (card[8] == '=' && !cards.ContainsKey(key))
                {
                    string v = card.Substring(10);
                    int slash = v.IndexOf('/');
                    if (slash >= 0 && !v.TrimStart().StartsWith('\'')) v = v.Substring(0, slash);
                    cards[key] = v.Trim();
                }
            }
        }
    }

    private static int? Int(Dictionary<string, string> c, string key) =>
        c.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;

    private static double? Dbl(Dictionary<string, string> c, string key) =>
        c.TryGetValue(key, out var v) && double.TryParse(v.Replace('D', 'E').Replace('d', 'e'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    public void ReadFrame(int index, ushort[] target)
    {
        if (target.Length < _pixels.Length) throw new ArgumentException("Target is too small for a frame.", nameof(target));
        if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
        Array.Copy(_pixels, target, _pixels.Length);
    }

    public void Dispose() { }
}

/// <summary>Writes one frame in the source's FITS layout: same header, same encoding, same trailing HDUs.</summary>
public sealed class FitsSink : IFrameSink
{
    private readonly FitsSource _source;
    private readonly FileStream _out;
    private bool _written, _completed;

    public FitsSink(FitsSource source, string targetPath)
    {
        _source = source;
        if (string.Equals(Path.GetFullPath(targetPath), Path.GetFullPath(source.Info.Path), StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedImageException("The output must not overwrite the source file.");
        _out = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void WriteFrame(ReadOnlySpan<ushort> frame)
    {
        int n = _source.Info.Width * _source.Info.Height;
        if (frame.Length < n) throw new ArgumentException("Frame is too small.", nameof(frame));
        if (_written) throw new InvalidOperationException("Too many frames written.");

        int per = _source.Bitpix / 8;
        long dataBytes = (long)n * per;
        long padded = (dataBytes + FitsLayout.Block - 1) / FitsLayout.Block * FitsLayout.Block;
        var raw = new byte[padded];
        if (per == 1)
            for (int i = 0; i < n; i++) raw[i] = (byte)Math.Min(frame[i], (ushort)255);
        else
            for (int i = 0; i < n; i++)
            {
                int s = _source.HasBzero ? frame[i] - 32768 : Math.Min((int)frame[i], 32767);
                raw[2 * i] = (byte)(s >> 8);
                raw[2 * i + 1] = (byte)s;
            }
        _out.Write(_source.HeaderBytes);
        _out.Write(raw);
        _written = true;
    }

    public void Complete()
    {
        if (!_written) throw new InvalidOperationException("Expected 1 frame but 0 were written.");
        if (_completed) return;
        using (var src = new FileStream(_source.Info.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            src.Position = _source.TailOffset;
            src.CopyTo(_out);
        }
        _out.Flush();
        _completed = true;
    }

    public void Dispose() => _out.Dispose();
}
