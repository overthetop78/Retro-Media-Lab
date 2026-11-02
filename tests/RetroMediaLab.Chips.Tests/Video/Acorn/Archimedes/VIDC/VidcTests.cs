using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;
using System.Buffers.Binary;
using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;
using static RetroMediaLab.Chips.Tests.Shared.ArchimedesTestCircuits;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.Archimedes.VIDC;
public sealed class VidcTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VidcSerializesLowBitsFirstInEveryDepth(int depth)
    {
        var memc = CreateMemory();
        var vidc = CreateVideo(memc, depth);
        var bits = 1 << depth;
        uint pattern = 0;
        for (var pixel = 0; pixel < 32 / bits; pixel += 2)
            pattern |= 1u << (pixel * bits);
        for (uint address = 0; address < 1024; address += 4)
            memc.WritePhysicalWord(address, pattern);
        vidc.Write(0x0400000f);
        RunVideo(vidc, memc, 2);
        var start = 4 + new[]
        {
            19,
            11,
            7,
            5
        }[depth] - 1;
        Assert.Equal(48, vidc.Width);
        Assert.Equal(2, vidc.Height);
        Assert.Equal(depth == 3 ? 0x770000 : 0xff0000, Pixel(vidc, start, 0));
        Assert.Equal(0, Pixel(vidc, start + 1, 0));
    }

    [Fact]
    public void VidcEightBitPaletteUsesDirectHighRgbBits()
    {
        var memc = CreateMemory();
        var vidc = CreateVideo(memc, 3);
        for (uint address = 0; address < 256; address += 4)
            memc.WritePhysicalWord(address, 0xf0f0f0f0);
        vidc.Write(0x00000000);
        RunVideo(vidc, memc, 2);
        Assert.Equal(0x88cc88, Pixel(vidc, 8, 0));
    }

    [Fact]
    public void VidcCursorTransparencyPreservesUnderlyingVideoAndDmaIsLinear()
    {
        var memc = CreateMemory();
        var vidc = CreateVideo(memc, 3);
        Dma(memc, 3, 512);
        memc.WritePhysicalWord(512, 0xe1);
        vidc.Write(0x440000f0);
        vidc.Write(0x48000f00);
        vidc.Write(0x4c000fff);
        vidc.Write(0x98000000 | (3u << 13));
        Timing(vidc, VidcRegister.VerticalCursorStart, 0);
        Timing(vidc, VidcRegister.VerticalCursorEnd, 2);
        RunVideo(vidc, memc, 2);
        Assert.Equal(0x00ff00, Pixel(vidc, 8, 0));
        Assert.Equal(0, Pixel(vidc, 9, 0));
        Assert.Equal(0x0000ff, Pixel(vidc, 10, 0));
        Assert.Equal(0xffffff, Pixel(vidc, 11, 0));
    }

    [Fact]
    public void VidcInterlaceCountsTwoFieldsWithTheProgrammedHalfLine()
    {
        var memc = CreateMemory();
        var vidc = CreateVideo(memc, 3);
        vidc.Write(0xe000004f);
        Timing(vidc, VidcRegister.HorizontalInterlace, 16);
        Assert.Equal(576, vidc.FrameMasterClocks);
        Assert.Equal(24000000d / 576, vidc.FramesPerSecond!.Value, 8);
        for (var clock = 0; clock < 575; clock++)
            ClockVideo(vidc, memc);
        Assert.Equal(0, vidc.Frame);
        ClockVideo(vidc, memc);
        Assert.Equal(1, vidc.Frame);
    }

    [Theory]
    [InlineData(0, 768)]
    [InlineData(1, 512)]
    [InlineData(2, 384)]
    [InlineData(3, 256)]
    public void VidcReportsItsProgrammedCadenceAndClearsItAtReset(int pixelClock, int clocks)
    {
        var vidc = new ArchimedesVidc();
        Assert.Null(vidc.FrameMasterClocks);
        Assert.Null(vidc.FramesPerSecond);
        Timing(vidc, VidcRegister.HorizontalCycle, 31);
        Assert.Null(vidc.FramesPerSecond);
        Timing(vidc, VidcRegister.VerticalCycle, 3);
        vidc.Write(0xe000000cu | (uint)pixelClock);
        Assert.Equal(clocks, vidc.FrameMasterClocks);
        Assert.Equal(24000000d / clocks, vidc.FramesPerSecond!.Value, 8);
        vidc.Reset();
        Assert.Null(vidc.FrameMasterClocks);
        Assert.Null(vidc.FramesPerSecond);
    }

    [Fact]
    public void VidcTimingWriteChangesTheCurrentFieldWithoutRestartingTheBeam()
    {
        var memory = CreateMemory();
        var video = CreateVideo(memory, 3);
        for (var clock = 0; clock < 10; clock++)
            ClockVideo(video, memory);
        Assert.Equal(10, video.HorizontalPixel);
        Timing(video, VidcRegister.HorizontalCycle, 15);
        Assert.Equal(10, video.HorizontalPixel);
        for (var clock = 0; clock < 117; clock++)
            ClockVideo(video, memory);
        Assert.Equal(0, video.Frame);
        ClockVideo(video, memory);
        Assert.Equal(1, video.Frame);
        video.Reset();
        Assert.Equal(1, video.BitsPerPixel);
        Assert.Equal(0u, video.Control);
        Assert.Equal(0, video.HorizontalPixel);
    }

    [Fact]
    public void VidcDepthWriteDuringDisplayImmediatelyChangesPixelDecodingAndDisplayOffset()
    {
        var memory = CreateMemory();
        var video = CreateVideo(memory, 0);
        for (uint address = 0; address < 1024; address += 4)
            memory.WritePhysicalWord(address, 0x01010101);
        video.Write(0x0400000f);
        RunVideo(video, memory, 1);
        for (var clock = 0; clock < 76; clock++)
            ClockVideo(video, memory);
        Assert.Equal(1, video.Raster);
        Assert.Equal(12, video.HorizontalPixel);
        video.Write(0xe000000f);
        Assert.Equal(8, video.BitsPerPixel);
        for (var clock = 76; clock < 256; clock++)
            ClockVideo(video, memory);
        Assert.Equal(0, Pixel(video, 10, 0));
        Assert.Equal(0x770000, Pixel(video, 11, 0));
        Assert.Equal(0x770000, Pixel(video, 22, 0));
    }

    [Fact]
    public void VidcSoundConvertsSignedLogarithmsAndIntegratesStereoMultiplexing()
    {
        var vidc = new ArchimedesVidc();
        for (uint register = 0x60; register <= 0x7c; register += 4)
            vidc.Write((register << 24) | 1);
        vidc.Write(0xc0000001);
        vidc.SupplySoundBurst(new uint[] { 0x21202120, 0, 0, 0 });
        vidc.AdvanceMasterClocks(72);
        vidc.TakeAudioSample();
        vidc.AdvanceMasterClocks(72);
        Assert.Equal(((short)96, (short)0), vidc.TakeAudioSample());
        vidc.AdvanceMasterClocks(72);
        Assert.Equal(((short)-96, (short)0), vidc.TakeAudioSample());
    }
}
