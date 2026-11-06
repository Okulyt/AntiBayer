using AntiBayer.Core;
using Xunit;

namespace AntiBayer.Tests;

public class DisplayMappingTests
{
    [Fact]
    public void Anzeige_normiert_auf_Maximum()
    {
        var frame = new ushort[] { 0, 2047, 4095 };
        var o = DisplayMapping.To8Bit(frame);
        Assert.Equal(0, o[0]);
        Assert.Equal(255, o[2]);
        Assert.Equal(127, o[1]); // 2047*255/4095 = 127.46
        Assert.All(DisplayMapping.To8Bit(new ushort[] { 0, 0, 0 }), b => Assert.Equal(0, b));
        Assert.Equal(new byte[] { 0, 17, 255 }, DisplayMapping.To8Bit(new ushort[] { 0, 17, 255 }));
    }

    [Fact]
    public void Anzeige_aendert_Quelldaten_nicht()
    {
        var frame = new ushort[] { 5, 4000, 1234 };
        var copy = (ushort[])frame.Clone();
        DisplayMapping.To8Bit(frame);
        Assert.Equal(copy, frame);
    }
}
