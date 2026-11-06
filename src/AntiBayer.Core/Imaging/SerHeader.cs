using System;
using System.Buffers.Binary;
using System.Text;

namespace AntiBayer.Core.Imaging;

public sealed record SerHeader
{
    public const int Size = 178;
    private const string FileId = "LUCAM-RECORDER";

    public int ColorId { get; init; }
    public int LittleEndianFlag { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int PixelDepth { get; init; }
    public int FrameCount { get; init; }
    public string Observer { get; init; } = "";
    public string Instrument { get; init; } = "";
    public string Telescope { get; init; } = "";

    public int BytesPerPixel => PixelDepth <= 8 ? 1 : 2;
    public long FrameBytes => (long)Width * Height * BytesPerPixel;
    public long DataBytes => FrameBytes * FrameCount;

    public long FrameOffset(int index) => Size + index * FrameBytes;

    public static SerHeader Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Size)
            throw new UnsupportedImageException("The file is too short to be a SER file.");
        if (Encoding.ASCII.GetString(bytes[..14]) != FileId)
            throw new UnsupportedImageException("The file is not a SER file (missing LUCAM-RECORDER signature).");

        static int Int(ReadOnlySpan<byte> b, int off) => BinaryPrimitives.ReadInt32LittleEndian(b[off..]);
        static string Text(ReadOnlySpan<byte> b, int off) => Encoding.ASCII.GetString(b.Slice(off, 40)).TrimEnd('\0', ' ');

        var h = new SerHeader
        {
            ColorId = Int(bytes, 18),
            LittleEndianFlag = Int(bytes, 22),
            Width = Int(bytes, 26),
            Height = Int(bytes, 30),
            PixelDepth = Int(bytes, 34),
            FrameCount = Int(bytes, 38),
            Observer = Text(bytes, 42),
            Instrument = Text(bytes, 82),
            Telescope = Text(bytes, 122),
        };
        if (h.Width <= 0 || h.Height <= 0 || h.FrameCount <= 0 || h.PixelDepth <= 0 || h.PixelDepth > 16)
            throw new UnsupportedImageException("The SER header contains invalid dimensions, depth or frame count.");
        return h;
    }
}
