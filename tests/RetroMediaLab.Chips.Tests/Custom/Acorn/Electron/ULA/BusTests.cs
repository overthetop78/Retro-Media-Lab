using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class BusTests
{
    private sealed class Pins : IElectronCassettePins
    {
        public bool InputPresent => false;
        public bool InputLevel => true;
        public int Ticks { get; private set; }
        public void SetOutputLevel(bool value) { }
        public void Tick(bool motor) => Ticks++;
    }

    private static ElectronUla Create(out ElectronVideo video, out Pins cassette)
    {
        video = new(_ => 0);
        cassette = new();
        return new(video, cassette);
    }

    private static int Access(ElectronUla ula, ushort address)
    {
        var clocks = 1;
        ula.BeginCpuCycle(address, false, 0);
        ula.Tick();
        while (ula.CpuWaiting && clocks < 256)
        {
            ula.Tick();
            clocks++;
        }
        Assert.False(ula.CpuWaiting);
        ula.EndCpuCycle();
        return clocks;
    }

    [Theory]
    [InlineData(0x0000)]
    [InlineData(0x7fff)]
    [InlineData(0xfc00)]
    [InlineData(0xfdff)]
    public void SlowDomainsAlwaysTakeTwoOrThreeClocksAndStayInPhase(int address)
    {
        var ula = Create(out _, out _);
        ula.Write(7, 0x20);
        Assert.Equal(2, Access(ula, (ushort)address));
        Assert.Equal(2, Access(ula, (ushort)address));
        Assert.Equal(1, Access(ula, 0xc000));
        Assert.Equal(3, Access(ula, (ushort)address));
        Assert.Equal(2, Access(ula, (ushort)address));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void KeyboardPagesUseSlowDomainAndBasicPagesUseFastDomain(int page)
    {
        var ula = Create(out _, out _);
        ula.Write(5, (byte)page);
        Assert.Equal(2, Access(ula, 0x8000));
        Assert.Equal(2, Access(ula, 0xbfff));
        ula.Write(5, 10);
        Assert.Equal(1, Access(ula, 0x8000));
        Assert.Equal(1, Access(ula, 0xbfff));
    }

    [Theory]
    [InlineData(0, 82)]
    [InlineData(1, 82)]
    [InlineData(2, 82)]
    [InlineData(3, 82)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 2)]
    public void ActiveRamContentionMatchesModeAndAdvancesEveryOtherCircuit(int mode, int expected)
    {
        var ula = Create(out var video, out var cassette);
        ula.Write(7, (byte)(mode << 3));
        Assert.Equal(expected, Access(ula, 0x4000));
        Assert.Equal(expected, video.Clock);
        Assert.Equal(expected, cassette.Ticks);
    }

    [Fact]
    public void RomAndExternalIoDoNotWaitForActiveVideo()
    {
        var ula = Create(out var video, out _);
        Assert.True(video.DisplayFetching);
        Assert.Equal(1, Access(ula, 0xc000));
        Assert.Equal(1, Access(ula, 0xfbff));
        Assert.Equal(2, Access(ula, 0xfc00));
        Assert.Equal(2, Access(ula, 0xfd00));
        Assert.True(video.DisplayFetching);
    }

    [Fact]
    public void SpacedTextRastersReleaseRamBeforeTheNextVisibleRow()
    {
        var ula = Create(out var video, out _);
        ula.Write(7, 0x18);
        for (var tick = 0; tick < 8 * 128; tick++) ula.Tick();
        Assert.Equal(8, video.Line);
        Assert.False(video.DisplayFetching);
        Assert.Equal(2, Access(ula, 0x0000));
    }

    [Fact]
    public void NmiAssertionReleasesThePendingRamAccessButNotItsOneMhzAlignment()
    {
        var ula = Create(out var video, out _);
        ula.BeginCpuCycle(0x0200, false, 0);
        for (var i = 0; i < 8; i++) ula.Tick();
        Assert.True(ula.CpuWaiting);
        ula.Nmi = true;
        Assert.True(ula.Nmi);
        Assert.True(ula.CpuRamPriority);
        Assert.True(ula.CpuWaiting);
        ula.Tick();
        ula.Tick();
        Assert.False(ula.CpuWaiting);
        ula.EndCpuCycle();
        Assert.True(video.DisplayFetching);
        ula.Nmi = false;
        Assert.False(ula.Nmi);
        Assert.True(ula.CpuRamPriority);
        Assert.Equal(2, Access(ula, 0x0200));
        ula.Write(5, 0x80);
        Assert.False(ula.CpuRamPriority);
        Assert.Equal(70, Access(ula, 0x0200));
    }

    [Fact]
    public void PriorityClearDoesNotChangeNmiPinRomLatchOrMaskedInterrupts()
    {
        var ula = Create(out _, out _);
        ula.Read(0);
        ula.Nmi = true;
        ula.Nmi = false;
        ula.Write(5, 10);
        ula.Write(0, 0x7c);
        Assert.False(ula.Irq);
        Assert.Equal(0x80, ula.Read(0));
        ula.Write(5, 0x80);
        Assert.Equal(10, ula.RomPage);
        Assert.False(ula.CpuRamPriority);
        Assert.False(ula.Nmi);
        Assert.False(ula.Irq);
    }

    [Fact]
    public void AssertedNmiKeepsPriorityUntilReleasedAndAcknowledged()
    {
        var ula = Create(out _, out _);
        ula.Nmi = true;
        ula.Write(5, 0x80);
        Assert.True(ula.CpuRamPriority);
        Assert.True(ula.Nmi);
        ula.Nmi = false;
        Assert.True(ula.CpuRamPriority);
        ula.Write(5, 0x80);
        Assert.False(ula.CpuRamPriority);
        ula.Nmi = true;
        Assert.True(ula.CpuRamPriority);
    }
}
