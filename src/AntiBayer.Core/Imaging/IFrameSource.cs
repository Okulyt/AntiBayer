using System;

namespace AntiBayer.Core.Imaging;

public interface IFrameSource : IDisposable
{
    ImageInfo Info { get; }

    /// <summary>Reads one frame into target (length at least Width*Height), row-major.</summary>
    void ReadFrame(int index, ushort[] target);
}
