using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;
using System.Buffers.Binary;
using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;

namespace RetroMediaLab.Chips.Tests.Shared;
internal static class ArchimedesTestCircuits
{
    internal static ArchimedesMemc CreateMemory(int bytes = 4 << 20)
    {
        var memory = new ArchimedesMemc(bytes, new byte[256]);
        memory.ObserveCpuAddress(0);
        memory.ObserveCpuAddress(0x03800000);
        return memory;
    }

    internal static void Control(ArchimedesMemc memory, int size, uint flags = 0) => memory.WriteRegister(0x036e0000 | (uint)(size << 2) | flags);
    internal static void Dma(ArchimedesMemc memory, int register, uint pointer) => memory.WriteRegister(0x03600000 | (uint)(register << 17) | ((pointer >> 4) << 2));
    internal static uint Cam(int size, int logical, int physical, int protection = 0, int controller = 0)
    {
        var encoded = size switch
        {
            0 => physical,
            1 => ((physical & 63) << 1) | (physical >> 6),
            2 => ((physical & 31) << 2) | (physical >> 5),
            _ => ((physical & 15) << 3) | ((physical >> 4) & 1) | (((physical >> 6) & 1) << 1) | (((physical >> 5) & 1) << 2)};
        return 0x03800000 | (uint)(encoded | (protection << 8) | ((controller & 1) << 7) | ((controller >> 1) << 12) | ((logical & ((1 << (11 - size)) - 1)) << (12 + size)) | (((logical >> (11 - size)) & 3) << 10));
    }

    internal static ArchimedesVidc CreateVideo(ArchimedesMemc memory, int depth)
    {
        Control(memory, 3, 0x400);
        Dma(memory, 0, 0);
        Dma(memory, 1, 0);
        Dma(memory, 2, 1008);
        var video = new ArchimedesVidc();
        video.Write(0xe0000003 | (uint)(depth << 2));
        Timing(video, VidcRegister.HorizontalCycle, 31);
        Timing(video, VidcRegister.HorizontalSync, 0);
        Timing(video, VidcRegister.HorizontalBorderStart, 0);
        Timing(video, VidcRegister.HorizontalBorderEnd, 24);
        Timing(video, VidcRegister.HorizontalDisplayStart, 2);
        Timing(video, VidcRegister.HorizontalDisplayEnd, 10);
        Timing(video, VidcRegister.VerticalCycle, 3);
        Timing(video, VidcRegister.VerticalBorderStart, 0);
        Timing(video, VidcRegister.VerticalBorderEnd, 2);
        Timing(video, VidcRegister.VerticalDisplayStart, 0);
        Timing(video, VidcRegister.VerticalDisplayEnd, 2);
        return video;
    }

    internal static void Timing(ArchimedesVidc video, VidcRegister register, int timing) => video.Write(((uint)register << 24) | ((uint)timing << 14));
    internal static void ClockVideo(ArchimedesVidc video, ArchimedesMemc memory)
    {
        Span<uint> words = stackalloc uint[4];
        if (video.VideoDmaRequest && memory.VideoDmaEnabled)
        {
            memory.ReadVideoBurst(words);
            video.SupplyVideoBurst(words);
        }

        if (video.CursorDmaRequest && memory.CursorEnabled)
        {
            memory.ReadCursorBurst(words);
            video.SupplyCursorBurst(words);
        }

        video.AdvanceMasterClocks(1);
    }

    internal static void RunVideo(ArchimedesVidc video, ArchimedesMemc memory, int frames)
    {
        video.VideoFrameStarted = memory.BeginVideoFrame;
        video.CursorFrameStarted = memory.BeginCursorFrame;
        try
        {
            for (var clock = 0; clock < frames * 256; clock++)
                ClockVideo(video, memory);
        }
        finally
        {
            video.VideoFrameStarted = null;
            video.CursorFrameStarted = null;
        }
    }

    internal static int Pixel(ArchimedesVidc video, int x, int y)
    {
        var index = (y * video.Width + x) * 4;
        return video.CompletedFrame[index] | (video.CompletedFrame[index + 1] << 8) | (video.CompletedFrame[index + 2] << 16);
    }

    internal static ArchimedesKeyboard CreateKeyboard()
    {
        var keyboard = new ArchimedesKeyboard();
        Assert.Equal(0xff, KeyboardRead(keyboard));
        keyboard.Write(0xff);
        Assert.Equal(0xff, KeyboardRead(keyboard));
        keyboard.Write(0xfe);
        Assert.Equal(0xfe, KeyboardRead(keyboard));
        keyboard.Write(0xfd);
        Assert.Equal(0xfd, KeyboardRead(keyboard));
        keyboard.Write(0x33);
        return keyboard;
    }

    internal static byte KeyboardRead(ArchimedesKeyboard keyboard)
    {
        keyboard.Advance(2000);
        Assert.True(keyboard.TryTakeTransmit(out var value));
        return value;
    }
}
