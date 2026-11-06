namespace AntiBayer.Core.Imaging;

public enum ImageFormat { Ser, Tiff, Png, Fits }

public sealed record ImageInfo(
    string Path, ImageFormat Format, int Width, int Height, int BitDepth, int FrameCount,
    ushort WhiteLevel, BayerPattern? SuggestedPattern, CaptureInfo Capture, string? Warning);
