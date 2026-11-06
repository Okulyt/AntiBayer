using System;

namespace AntiBayer.Core.Imaging;

public interface IFrameSink : IDisposable
{
    void WriteFrame(ReadOnlySpan<ushort> frame);

    /// <summary>Finishes the output (for SER: writes the original timestamp trailer).</summary>
    void Complete();
}
