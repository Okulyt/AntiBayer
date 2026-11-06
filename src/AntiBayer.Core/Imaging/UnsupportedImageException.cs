using System;

namespace AntiBayer.Core.Imaging;

/// <summary>Thrown when a file cannot be processed (colour data, damaged header, truncated output source).</summary>
public sealed class UnsupportedImageException : Exception
{
    public UnsupportedImageException(string message) : base(message) { }
}
