using AntiBayer.Core.Imaging;
using Xunit;

namespace AntiBayer.Tests;

public class WhiteLevelTests
{
    private static ushort[] Frame(System.Func<int, ushort> f)
    {
        var a = new ushort[64];
        for (int i = 0; i < a.Length; i++) a[i] = f(i);
        return a;
    }

    [Fact] public void Weisswert_linksbuendig_12_Bit()
        => Assert.Equal(65520, WhiteLevel.Detect(Frame(i => (ushort)((i * 61 % 4096) << 4)), 16, 16));

    [Fact] public void Weisswert_rechtsbuendig_12_Bit()
        => Assert.Equal(4095, WhiteLevel.Detect(Frame(i => (ushort)(i * 63 + 1)), 16, 12));

    [Fact] public void Weisswert_volle_16_Bit()
        => Assert.Equal(65535, WhiteLevel.Detect(Frame(i => (ushort)(i * 1000 + 1)), 16, 16));

    [Fact] public void Weisswert_8_Bit()
        => Assert.Equal(255, WhiteLevel.Detect(Frame(i => (ushort)(i * 4)), 8, 8));

    [Fact] public void Weisswert_leeres_Schwarzbild()
        => Assert.Equal(65535, WhiteLevel.Detect(Frame(_ => 0), 16, 16));
}
