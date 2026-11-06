using System.IO;
using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class ImageFilesTests
{
    [Theory]
    [InlineData("a.ser", true)]
    [InlineData("a.TIF", true)]
    [InlineData("a.tiff", true)]
    [InlineData("a.PNG", true)]
    [InlineData("a.jpg", false)]
    [InlineData("a", false)]
    public void Endungen(string name, bool expected) =>
        Assert.Equal(expected, ImageFiles.IsSupportedExtension(name));

    [Fact]
    public void Ser_wird_ueber_ImageFiles_geoeffnet_und_geschrieben()
    {
        string dir = TestData.TempDir();
        string src = Path.Combine(dir, "a.ser"), dst = Path.Combine(dir, "b.ser");
        TestData.WriteSer(src, 4, 4, 8, 0, 2, (f, x, y) => (ushort)(x + y + f));
        var fr = new ushort[16];
        using (var s = ImageFiles.Open(src))
        {
            Assert.Equal(ImageFormat.Ser, s.Info.Format);
            using var sink = ImageFiles.CreateSink(s, dst);
            for (int i = 0; i < 2; i++) { s.ReadFrame(i, fr); sink.WriteFrame(fr); }
            sink.Complete();
        }
        Assert.Equal(File.ReadAllBytes(src), File.ReadAllBytes(dst));
    }

    [Fact]
    public void Unbekannte_Endung_liefert_UnsupportedImageException() =>
        Assert.Throws<UnsupportedImageException>(() => ImageFiles.Open("x.jpg"));
}
