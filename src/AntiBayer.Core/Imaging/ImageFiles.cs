using System;
using System.IO;

namespace AntiBayer.Core.Imaging;

public static class ImageFiles
{
    private static readonly string[] Extensions = { ".ser", ".tif", ".tiff", ".png" };

    public static bool IsSupportedExtension(string path) =>
        Array.IndexOf(Extensions, Path.GetExtension(path).ToLowerInvariant()) >= 0;

    public static IFrameSource Open(string path, bool swapBytes = false)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".ser": return new SerSource(path, swapBytes);
            case ".tif":
            case ".tiff":
            case ".png": return new WicSource(path);
            default: throw new UnsupportedImageException("This file type is not supported. Use .ser, .tif, .tiff or .png.");
        }
    }

    public static IFrameSink CreateSink(IFrameSource source, string targetPath) => source switch
    {
        SerSource ser => new SerSink(ser, targetPath),
        WicSource wic => new WicSink(wic, targetPath),
        _ => throw new UnsupportedImageException("This source type cannot be saved."),
    };
}
