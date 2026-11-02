using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;
using System.Buffers.Binary;
using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;
using static RetroMediaLab.Chips.Tests.Shared.ArchimedesTestCircuits;

namespace RetroMediaLab.Chips.Tests.Memory.Acorn.Archimedes.MEMC;
public sealed class MemcTests
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
        var memc = CreateMemory();
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
        var memc = CreateMemory();
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
        var memc = CreateMemory(8 << 20);
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
        var memc = CreateMemory(1 << 20);
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
        var memc = CreateMemory();
        Control(memc, 3, 0x400);
        Dma(memc, 0, 32);
        Dma(memc, 1, 16);
        Dma(memc, 2, 32);
        Dma(memc, 3, 128);
        for (uint address = 0; address < 160; address += 4)
            memc.WritePhysicalWord(address, address);
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
        var memc = CreateMemory();
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
}
