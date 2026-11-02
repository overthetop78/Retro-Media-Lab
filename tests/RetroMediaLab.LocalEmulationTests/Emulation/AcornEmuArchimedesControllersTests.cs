using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ioc.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Memc.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Services;
using System.Buffers.Binary;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuArchimedesControllersTests
{
    [Fact]
    public void ResetRomStaysMappedUntilLowThenHighAddressSequence()
    {
        var rom = new byte[256];
        BinaryPrimitives.WriteUInt32LittleEndian(rom, 0xea000000);
        var memc = new ArchimedesMemc(4 << 20, rom);
        memc.ObserveCpuAddress(0x03800000);
        Assert.True(memc.BootRomMapped);
        Assert.True(memc.TryReadLogicalWord(0, true, out var instruction));
        Assert.Equal(0xea000000u, instruction);
        memc.ObserveCpuAddress(0x03800000);
        Assert.False(memc.BootRomMapped);
        Assert.False(memc.TryReadLogicalWord(0, false, out _));
        Assert.Equal(0xea000000u, memc.ReadRomWord(0x03800100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CamEncodingMapsAllPhysicalPageBitsAndRemappingRemovesOldLogicalPage(int size)
    {
        var memc = Memory();
        Control(memc, size);
        var pageShift = 12 + size;
        for (var page = 0; page < 128; page++)
        {
            var logical = page + 160;
            memc.WriteCam(Cam(size, logical, page));
            memc.WritePhysicalWord((uint)(page << pageShift), (uint)(page + 1000));
            Assert.True(memc.TryReadLogicalWord((uint)(logical << pageShift), false, out var value));
            Assert.Equal((uint)(page + 1000), value);
        }
        memc.WriteCam(Cam(size, 400, 127));
        Assert.False(memc.TryReadLogicalWord((uint)(287 << pageShift), false, out _));
        Assert.True(memc.TryReadLogicalWord((uint)(400 << pageShift), false, out var moved));
        Assert.Equal(1127u, moved);
    }

    [Theory]
    [InlineData(0, false, true, true)]
    [InlineData(1, false, true, false)]
    [InlineData(2, false, false, false)]
    [InlineData(3, false, false, false)]
    [InlineData(0, true, true, true)]
    [InlineData(1, true, true, true)]
    [InlineData(2, true, true, false)]
    [InlineData(3, true, true, false)]
    public void CamProtectionDistinguishesUserOsAndSupervisor(int protection, bool os, bool read, bool write)
    {
        var memc = Memory();
        Control(memc, 3, os ? 0x1000u : 0);
        memc.WriteCam(Cam(3, 0, 2, protection));
        Assert.True(memc.TryWriteLogicalWord(0, 123, false));
        Assert.Equal(read, memc.TryReadLogicalWord(0, true, out _));
        Assert.Equal(write, memc.TryWriteLogicalWord(0, 456, true));
        Assert.True(memc.TryReadLogicalWord(0, false, out var stored));
        Assert.Equal(write ? 456u : 123u, stored);
    }

    [Fact]
    public void MultipleMemcsUseIndependentPhysicalBanks()
    {
        var memc = Memory(8 << 20);
        Control(memc, 3);
        memc.WriteCam(Cam(3, 2, 7, controller: 0));
        memc.WriteCam(Cam(3, 3, 7, controller: 1));
        Assert.True(memc.TryWriteLogicalWord(2 << 15, 12, false));
        Assert.True(memc.TryWriteLogicalWord(3 << 15, 34, false));
        Assert.Equal(12u, memc.ReadPhysicalWord(7 << 15));
        Assert.Equal(34u, memc.ReadPhysicalWord((4 << 20) + (7 << 15)));
        Assert.Equal(uint.MaxValue, memc.ReadPhysicalWord(8 << 20));
    }

    [Fact]
    public void DramRowAndColumnWiringPreservesDataAcrossPageSizeChanges()
    {
        var memc = Memory(1 << 20);
        Control(memc, 1);
        memc.WritePhysicalWord(0x10, 0x11223344);
        memc.WritePhysicalWord(0x80000, 0xaabbccdd);
        Control(memc, 3);
        Assert.Equal(0x11223344u, memc.ReadPhysicalWord(0x10));
        Assert.Equal(0xaabbccddu, memc.ReadPhysicalWord(0x80000));
        // Une colonne non câblée sur la RAM 1 Mio crée un alias, pas de mémoire supplémentaire.
        Assert.Equal(0x11223344u, memc.ReadPhysicalWord(0x100010));
    }

    [Fact]
    public void VideoDmaWrapsAtInclusiveEndBurstAndCursorStaysLinear()
    {
        var memc = Memory();
        Control(memc, 3, 0x400);
        Dma(memc, 0, 32);
        Dma(memc, 1, 16);
        Dma(memc, 2, 32);
        Dma(memc, 3, 128);
        for (uint address = 0; address < 160; address += 4) memc.WritePhysicalWord(address, address);
        memc.BeginVideoFrame();
        memc.BeginCursorFrame();
        Span<uint> burst = stackalloc uint[4];
        memc.ReadVideoBurst(burst);
        Assert.Equal(new uint[] { 32, 36, 40, 44 }, burst.ToArray());
        Assert.Equal(16u, memc.VideoPointer);
        memc.ReadVideoBurst(burst);
        Assert.Equal(new uint[] { 16, 20, 24, 28 }, burst.ToArray());
        memc.ReadCursorBurst(burst);
        Assert.Equal(new uint[] { 128, 132, 136, 140 }, burst.ToArray());
        memc.ReadCursorBurst(burst);
        Assert.Equal(144u, burst[0]);
    }

    [Fact]
    public void SoundDmaSwapsPreparedBuffersAndRepeatsCurrentBufferWhenNotRefilled()
    {
        var memc = Memory();
        Control(memc, 3, 0x800);
        Dma(memc, 4, 0);
        Dma(memc, 5, 0);
        Dma(memc, 6, 0);
        Assert.True(memc.SoundInterrupt);
        Dma(memc, 4, 64);
        Assert.False(memc.SoundInterrupt);
        Dma(memc, 5, 80);
        Span<uint> words = stackalloc uint[4];
        memc.ReadSoundBurst(words);
        Assert.Equal(64u, memc.SoundPointer);
        Assert.True(memc.SoundInterrupt);
        memc.ReadSoundBurst(words);
        memc.ReadSoundBurst(words);
        Assert.Equal(64u, memc.SoundPointer);
        memc.Reset(cold: false);
        Assert.False(memc.SoundDmaEnabled);
        Assert.True(memc.SoundInterrupt);
    }

    [Fact]
    public void IocInterruptMasksAndClearRespectLatchedAndLevelInputs()
    {
        var ioc = new ArchimedesIoc();
        ioc.Reset(cold: true);
        Assert.Equal(0x90, ioc.Read(0x10));
        Assert.False(ioc.Irq);
        ioc.Write(0x18, 0x10);
        Assert.True(ioc.Irq);
        ioc.Write(0x14, 0x10);
        Assert.False(ioc.Irq);
        ioc.SetIrqA(1, true);
        ioc.Write(0x14, 1);
        Assert.Equal(0x81, ioc.Read(0x10));
        ioc.Write(0x38, 0x80);
        Assert.True(ioc.Fiq);
        ioc.Write(0x38, 0);
        ioc.SetFlyback(true);
        ioc.Write(0x14, 8);
        ioc.SetFlyback(true);
        Assert.Equal(0, ioc.Read(0x10) & 8);
        ioc.SetFlyback(false);
        ioc.SetFlyback(true);
        Assert.Equal(8, ioc.Read(0x10) & 8);
    }

    [Fact]
    public void IocTimerOutputIsLatchedAndPeriodsDoNotDependOnCpuSpeed()
    {
        var ioc = new ArchimedesIoc();
        ioc.Reset(cold: false);
        ioc.Write(0x40, 3);
        ioc.Write(0x44, 0);
        ioc.Write(0x48, 0);
        ioc.Advance(2);
        ioc.Write(0x4c, 0);
        Assert.Equal(1, ioc.Read(0x40));
        ioc.Advance(2);
        Assert.Equal(1, ioc.Read(0x40));
        Assert.Equal(0x20, ioc.Read(0x10) & 0x20);
        ioc.Write(0x14, 0x20);
        ioc.Advance(3);
        Assert.Equal(0, ioc.Read(0x10) & 0x20);
        ioc.Advance(1);
        Assert.Equal(0x20, ioc.Read(0x10) & 0x20);
    }

    [Fact]
    public void IocKartUsesEightDataAndTwoStopBitsAndAcknowledgesReceiveByRead()
    {
        var ioc = new ArchimedesIoc();
        ioc.Reset(cold: false);
        ioc.Write(0x70, 1);
        ioc.Write(0x78, 0);
        byte? transmitted = null;
        ioc.KeyboardTransmitted = value => transmitted = value;
        try
        {
            ioc.Write(4, 0x55);
            Assert.True(ioc.ReceiveKeyboardByte(0xa5));
            ioc.Advance(543);
            Assert.Equal(0, ioc.Read(0x20) & 0xc0);
            ioc.Advance(1);
            Assert.Equal(0xa5, ioc.Read(4));
            Assert.Equal(0, ioc.Read(0x20) & 0x80);
            Assert.True(ioc.KeyboardReceiveBusy);
            ioc.Advance(160);
            Assert.False(ioc.KeyboardReceiveBusy);
            Assert.Equal((byte)0x55, transmitted);
            Assert.Equal(0x40, ioc.Read(0x20) & 0x40);
        }
        finally { ioc.KeyboardTransmitted = null; }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VidcSerializesLowBitsFirstInEveryDepth(int depth)
    {
        var memc = Memory();
        var vidc = Video(memc, depth);
        var bits = 1 << depth;
        uint pattern = 0;
        for (var pixel = 0; pixel < 32 / bits; pixel += 2) pattern |= 1u << (pixel * bits);
        for (uint address = 0; address < 1024; address += 4) memc.WritePhysicalWord(address, pattern);
        vidc.Write(0x0400000f);
        RunVideo(vidc, memc, 2);
        var start = 4 + new[] { 19, 11, 7, 5 }[depth] - 1;
        Assert.Equal(48, vidc.Width);
        Assert.Equal(2, vidc.Height);
        Assert.Equal(depth == 3 ? 0x770000 : 0xff0000, Pixel(vidc, start, 0));
        Assert.Equal(0, Pixel(vidc, start + 1, 0));
    }

    [Fact]
    public void VidcEightBitPaletteUsesDirectHighRgbBits()
    {
        var memc = Memory();
        var vidc = Video(memc, 3);
        for (uint address = 0; address < 256; address += 4) memc.WritePhysicalWord(address, 0xf0f0f0f0);
        vidc.Write(0x00000000);
        RunVideo(vidc, memc, 2);
        Assert.Equal(0x88cc88, Pixel(vidc, 8, 0));
    }

    [Fact]
    public void VidcCursorTransparencyPreservesUnderlyingVideoAndDmaIsLinear()
    {
        var memc = Memory();
        var vidc = Video(memc, 3);
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
        var memc = Memory();
        var vidc = Video(memc, 3);
        vidc.Write(0xe000004f);
        Timing(vidc, VidcRegister.HorizontalInterlace, 16);
        Assert.Equal(576, vidc.FrameMasterClocks);
        Assert.Equal(24000000d / 576, vidc.FramesPerSecond!.Value, 8);
        for (var clock = 0; clock < 575; clock++) ClockVideo(vidc, memc);
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
        var memory = Memory();
        var video = Video(memory, 3);
        for (var clock = 0; clock < 10; clock++) ClockVideo(video, memory);
        Assert.Equal(10, video.HorizontalPixel);
        Timing(video, VidcRegister.HorizontalCycle, 15);
        Assert.Equal(10, video.HorizontalPixel);
        for (var clock = 0; clock < 117; clock++) ClockVideo(video, memory);
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
        var memory = Memory();
        var video = Video(memory, 0);
        for (uint address = 0; address < 1024; address += 4) memory.WritePhysicalWord(address, 0x01010101);
        video.Write(0x0400000f);
        RunVideo(video, memory, 1);
        for (var clock = 0; clock < 76; clock++) ClockVideo(video, memory);
        Assert.Equal(1, video.Raster);
        Assert.Equal(12, video.HorizontalPixel);
        video.Write(0xe000000f);
        Assert.Equal(8, video.BitsPerPixel);
        for (var clock = 76; clock < 256; clock++) ClockVideo(video, memory);
        Assert.Equal(0, Pixel(video, 10, 0));
        Assert.Equal(0x770000, Pixel(video, 11, 0));
        Assert.Equal(0x770000, Pixel(video, 22, 0));
    }

    [Fact]
    public void VidcSoundConvertsSignedLogarithmsAndIntegratesStereoMultiplexing()
    {
        var vidc = new ArchimedesVidc();
        for (uint register = 0x60; register <= 0x7c; register += 4) vidc.Write((register << 24) | 1);
        vidc.Write(0xc0000001);
        vidc.SupplySoundBurst(new uint[] { 0x21202120, 0, 0, 0 });
        vidc.AdvanceMasterClocks(72);
        vidc.TakeAudioSample();
        vidc.AdvanceMasterClocks(72);
        Assert.Equal(((short)96, (short)0), vidc.TakeAudioSample());
        vidc.AdvanceMasterClocks(72);
        Assert.Equal(((short)-96, (short)0), vidc.TakeAudioSample());
    }

    [Fact]
    public void KeyboardRequiresBothByteAcknowledgementsAndCommandsCanBeInterleaved()
    {
        var keyboard = Keyboard();
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.A } });
        keyboard.Advance(20000);
        Assert.Equal(0xc3, KeyboardRead(keyboard));
        keyboard.Write(7);
        Assert.Equal(7, keyboard.Leds);
        keyboard.Write(0x3f);
        Assert.Equal(0xcc, KeyboardRead(keyboard));
        keyboard.Write(0x30);
        Assert.False(keyboard.ScanEnabled);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        keyboard.Advance(20000);
        Assert.False(keyboard.TryTakeTransmit(out _));
        keyboard.Write(0x20);
        Assert.Equal(0x81, KeyboardRead(keyboard));
        keyboard.Write(0x31);
        keyboard.Advance(20000);
        Assert.Equal(0xd3, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(0xdc, KeyboardRead(keyboard));
    }

    [Fact]
    public void KeyboardMouseCountersLockTogetherOnOverflowUntilPacketIsSent()
    {
        var keyboard = Keyboard();
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Pointer = EmulationInputSnapshot.Empty.Pointer with { DeltaX = 100, DeltaY = -100 } });
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Pointer = EmulationInputSnapshot.Empty.Pointer with { DeltaX = -5, DeltaY = 5 } });
        keyboard.Advance(20000);
        Assert.Equal(63, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(63, KeyboardRead(keyboard));
        keyboard.Write(0x33);
        keyboard.Advance(20000);
        Assert.False(keyboard.TryTakeTransmit(out _));
        keyboard.Write(0x22);
        keyboard.Advance(20000);
        Assert.Equal(0, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(0, KeyboardRead(keyboard));
    }

    [Fact]
    public void KeyboardInvalidAckRestartsProtocolRatherThanInjectingGuestMemory()
    {
        var keyboard = Keyboard();
        keyboard.Write(0x40 | 5);
        Assert.Equal(0xe5, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(0xff, KeyboardRead(keyboard));
        Assert.False(keyboard.ScanEnabled);
    }

    private static ArchimedesMemc Memory(int bytes = 4 << 20)
    {
        var memory = new ArchimedesMemc(bytes, new byte[256]);
        memory.ObserveCpuAddress(0);
        memory.ObserveCpuAddress(0x03800000);
        return memory;
    }

    private static void Control(ArchimedesMemc memory, int size, uint flags = 0) => memory.WriteRegister(0x036e0000 | (uint)(size << 2) | flags);
    private static void Dma(ArchimedesMemc memory, int register, uint pointer) => memory.WriteRegister(0x03600000 | (uint)(register << 17) | ((pointer >> 4) << 2));

    private static uint Cam(int size, int logical, int physical, int protection = 0, int controller = 0)
    {
        var encoded = size switch
        {
            0 => physical,
            1 => ((physical & 63) << 1) | (physical >> 6),
            2 => ((physical & 31) << 2) | (physical >> 5),
            _ => ((physical & 15) << 3) | ((physical >> 4) & 1) | (((physical >> 6) & 1) << 1) | (((physical >> 5) & 1) << 2)
        };
        return 0x03800000 | (uint)(encoded | (protection << 8) | ((controller & 1) << 7) |
            ((controller >> 1) << 12) | ((logical & ((1 << (11 - size)) - 1)) << (12 + size)) |
            (((logical >> (11 - size)) & 3) << 10));
    }

    private static ArchimedesVidc Video(ArchimedesMemc memory, int depth)
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

    private static void Timing(ArchimedesVidc video, VidcRegister register, int timing) => video.Write(((uint)register << 24) | ((uint)timing << 14));

    private static void ClockVideo(ArchimedesVidc video, ArchimedesMemc memory)
    {
        Span<uint> words = stackalloc uint[4];
        if (video.VideoDmaRequest && memory.VideoDmaEnabled) { memory.ReadVideoBurst(words); video.SupplyVideoBurst(words); }
        if (video.CursorDmaRequest && memory.CursorEnabled) { memory.ReadCursorBurst(words); video.SupplyCursorBurst(words); }
        video.AdvanceMasterClocks(1);
    }

    private static void RunVideo(ArchimedesVidc video, ArchimedesMemc memory, int frames)
    {
        video.VideoFrameStarted = memory.BeginVideoFrame;
        video.CursorFrameStarted = memory.BeginCursorFrame;
        try
        {
            for (var clock = 0; clock < frames * 256; clock++) ClockVideo(video, memory);
        }
        finally
        {
            video.VideoFrameStarted = null;
            video.CursorFrameStarted = null;
        }
    }

    private static int Pixel(ArchimedesVidc video, int x, int y)
    {
        var index = (y * video.Width + x) * 4;
        return video.CompletedFrame[index] | (video.CompletedFrame[index + 1] << 8) | (video.CompletedFrame[index + 2] << 16);
    }

    private static ArchimedesKeyboard Keyboard()
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

    private static byte KeyboardRead(ArchimedesKeyboard keyboard)
    {
        keyboard.Advance(2000);
        Assert.True(keyboard.TryTakeTransmit(out var value));
        return value;
    }
}
