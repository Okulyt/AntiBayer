using System;
using System.IO;

namespace AntiBayer.Core.Imaging;

public sealed class SerSink : IFrameSink
{
    private readonly SerSource _source;
    private readonly FileStream _out;
    private readonly byte[] _buffer;
    private readonly int _pixels;
    private int _written;

    public SerSink(SerSource source, string targetPath)
    {
        _source = source;
        if (source.IsTruncated)
            throw new UnsupportedImageException("An incomplete SER file can be viewed but not saved.");
        if (string.Equals(Path.GetFullPath(targetPath), Path.GetFullPath(source.Info.Path), StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedImageException("The output must not overwrite the source file.");

        _pixels = source.Header.Width * source.Header.Height;
        _buffer = new byte[source.Header.FrameBytes];
        _out = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
        _out.Write(source.RawHeader, 0, SerHeader.Size);
    }

    public void WriteFrame(ReadOnlySpan<ushort> frame)
    {
        if (frame.Length < _pixels) throw new ArgumentException("Frame is too small.", nameof(frame));
        if (_written >= _source.Info.FrameCount) throw new InvalidOperationException("Too many frames written.");

        if (_source.Header.BytesPerPixel == 1)
        {
            for (int i = 0; i < _pixels; i++) _buffer[i] = (byte)frame[i];
        }
        else if (_source.SwapBytes)
        {
            for (int i = 0; i < _pixels; i++) { _buffer[2 * i] = (byte)(frame[i] >> 8); _buffer[2 * i + 1] = (byte)frame[i]; }
        }
        else
        {
            for (int i = 0; i < _pixels; i++) { _buffer[2 * i] = (byte)frame[i]; _buffer[2 * i + 1] = (byte)(frame[i] >> 8); }
        }
        _out.Write(_buffer, 0, _buffer.Length);
        _written++;
    }

    public void Complete()
    {
        if (_written != _source.Info.FrameCount)
            throw new InvalidOperationException($"Expected {_source.Info.FrameCount} frames but {_written} were written.");
        _source.CopyTrailerTo(_out);
        _out.Flush();
    }

    public void Dispose() => _out.Dispose();
}
