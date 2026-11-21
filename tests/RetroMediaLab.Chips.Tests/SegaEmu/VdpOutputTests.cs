using System.Buffers.Binary;
using System.Reflection;
using RetroMediaLab.Chips.Video.Sega.VDP;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class VdpOutputTests
{
    [Theory]
    [InlineData(256, 0, 0, false, 0xff204060u)]
    [InlineData(320, 8, 0, true, 0xff204060u)]
    [InlineData(256, 8, 128, false, 0xff507090u)]
    [InlineData(320, 0, 128, true, 0xff507090u)]
    [InlineData(256, 0, 255, true, 0xff7f9fbfu)]
    [InlineData(320, 8, 255, false, 0xff7f9fbfu)]
    public void OutputPreservesColorsLcdBordersAndInterlacedRows(int width, int border, int lcd, bool interlace, uint expected)
    {
        const int pitch = 4096;
        byte[] framebuffer = new byte[pitch * 32];
        for (int i = 0; i < framebuffer.Length; i += 4)
            BinaryPrimitives.WriteUInt32LittleEndian(framebuffer.AsSpan(i), 0xff80a0c0);
        var vdp = new Vdp(Vdp.SYSTEM_MD, null!, new() { lcd = lcd, render = interlace ? 1 : 0 }, framebuffer, 1024, 32, pitch)
        {
            ViewportWidth = width, ViewportX = border, ViewportY = 2,
            lines_per_frame = 262, interlaced = interlace ? (byte)1 : (byte)0, odd_frame = 1
        };
        var palette = (uint[])typeof(Vdp).GetField("pixel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vdp)!;
        palette[64] = 0xff204060;
        int outputRow = interlace ? 11 : 5, calls = 0;
        vdp.LineRendered = line =>
        {
            Assert.Equal(3, line);
            Assert.Equal(expected, BinaryPrimitives.ReadUInt32LittleEndian(framebuffer.AsSpan(outputRow * pitch)));
            calls++;
        };
        try
        {
            vdp.BlankLine(3, -border, width + 2 * border);
            Assert.Equal(1, calls);
            for (int x = 0; x < width + 2 * border; x++)
                Assert.Equal(expected, BinaryPrimitives.ReadUInt32LittleEndian(framebuffer.AsSpan(outputRow * pitch + x * 4)));
            Assert.Equal(0xff80a0c0u, BinaryPrimitives.ReadUInt32LittleEndian(framebuffer.AsSpan(outputRow * pitch - 4)));
            Assert.Equal(0xff80a0c0u, BinaryPrimitives.ReadUInt32LittleEndian(framebuffer.AsSpan(outputRow * pitch + (width + 2 * border) * 4)));
        }
        finally { vdp.LineRendered = null; }
    }

    [Theory, InlineData(8), InlineData(15), InlineData(16), InlineData(32)]
    public void NotificationFollowsTheConvertedPixelsInEveryDepth(int depth)
    {
        byte[] framebuffer = new byte[4096 * 4];
        var vdp = new Vdp(Vdp.SYSTEM_MD, null!, new() { PixelDepth = depth }, framebuffer, 1024, 4, 4096)
        { lines_per_frame = 262 };
        ((uint[])typeof(Vdp).GetField("pixel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vdp)!)[64] = 0xff204060;
        int calls = 0;
        vdp.LineRendered = line =>
        {
            uint actual = depth == 8 ? framebuffer[0] : depth == 32
                ? BinaryPrimitives.ReadUInt32LittleEndian(framebuffer)
                : BinaryPrimitives.ReadUInt16LittleEndian(framebuffer);
            Assert.Equal(depth == 8 ? 0x60u : depth == 32 ? 0xff204060u : 0x4060u, actual);
            calls++;
        };
        try { vdp.BlankLine(0, 0, 256); Assert.Equal(1, calls); }
        finally { vdp.LineRendered = null; }
    }
}
