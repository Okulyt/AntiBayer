using System;
using System.IO;

namespace AntiBayer.Core.Imaging;

public sealed class SerSource : IFrameSource
{
    private readonly FileStream _stream;
    private readonly byte[] _rawHeader = new byte[SerHeader.Size];
    private readonly byte[] _buffer;
    private readonly bool _swapBytes;
    private readonly long _trailerOffset;
    private int _readableFrames = 1;

    public SerHeader Header { get; }
    public ImageInfo Info { get; }
    public bool HasTimestampTrailer { get; }

    /// <summary>True when the file holds fewer complete frames than the header announces.</summary>
    public bool IsTruncated { get; }

    public SerSource(string path, bool swapBytes = false)
    {
        _swapBytes = swapBytes;
        _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, FileOptions.SequentialScan);
        try
        {
            if (_stream.Length < SerHeader.Size)
                throw new UnsupportedImageException("The file is too short to be a SER file.");
            _stream.ReadExactly(_rawHeader);
            Header = SerHeader.Parse(_rawHeader);

            if (Header.ColorId is 100 or 101)
                throw new UnsupportedImageException(
                    "Colour SER files (RGB/BGR) are not supported. AntiBayer needs single-channel raw data.");

            long frameBytes = Header.FrameBytes;
            long complete = (_stream.Length - SerHeader.Size) / frameBytes;
            int frames = (int)Math.Min(complete, Header.FrameCount);
            if (frames < 1)
                throw new UnsupportedImageException("The SER file contains no complete frame.");

            IsTruncated = frames < Header.FrameCount;
            _trailerOffset = SerHeader.Size + Header.DataBytes;
            HasTimestampTrailer = !IsTruncated && _stream.Length >= _trailerOffset + 8L * Header.FrameCount;

            string? warning = IsTruncated
                ? $"The file is incomplete: {frames} of {Header.FrameCount} frames could be read. Saving is not possible."
                : null;

            _buffer = new byte[frameBytes];
            int bitDepth = Header.BytesPerPixel == 1 ? 8 : 16;
            var first = new ushort[Header.Width * Header.Height];
            ReadFrame(0, first);
            _readableFrames = frames;

            Info = new ImageInfo(path, ImageFormat.Ser, Header.Width, Header.Height, bitDepth, frames,
                WhiteLevel.Detect(first, bitDepth, Header.PixelDepth),
                Bayer.FromSerColorId(Header.ColorId), new CaptureInfo(), warning);
        }
        catch
        {
            _stream.Dispose();
            throw;
        }
    }

    public void ReadFrame(int index, ushort[] target)
    {
        int count = Header.Width * Header.Height;
        if (target.Length < count) throw new ArgumentException("Target is too small for a frame.", nameof(target));
        if ((uint)index >= (uint)_readableFrames) throw new ArgumentOutOfRangeException(nameof(index));

        _stream.Position = Header.FrameOffset(index);
        _stream.ReadExactly(_buffer);

        if (Header.BytesPerPixel == 1)
        {
            for (int i = 0; i < count; i++) target[i] = _buffer[i];
        }
        else if (_swapBytes)
        {
            for (int i = 0; i < count; i++) target[i] = (ushort)((_buffer[2 * i] << 8) | _buffer[2 * i + 1]);
        }
        else
        {
            for (int i = 0; i < count; i++) target[i] = (ushort)(_buffer[2 * i] | (_buffer[2 * i + 1] << 8));
        }
    }

    internal bool SwapBytes => _swapBytes;
    internal byte[] RawHeader => _rawHeader;

    /// <summary>Copies the original timestamp trailer (if any) to the output stream.</summary>
    internal void CopyTrailerTo(Stream output)
    {
        if (!HasTimestampTrailer) return;
        long remaining = 8L * Header.FrameCount;
        _stream.Position = _trailerOffset;
        var chunk = new byte[64 * 1024];
        while (remaining > 0)
        {
            int n = (int)Math.Min(chunk.Length, remaining);
            _stream.ReadExactly(chunk, 0, n);
            output.Write(chunk, 0, n);
            remaining -= n;
        }
    }

    public void Dispose() => _stream.Dispose();
}
