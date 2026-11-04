using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RegisterTests
{
    private sealed class CassettePins : IElectronCassettePins
    {
        public bool InputPresent { get; set; }
        public bool InputLevel { get; set; } = true;
        public bool OutputLevel { get; private set; }
        public bool Motor { get; private set; }
        public void SetOutputLevel(bool value) => OutputLevel = value;
        public int HalfPeriod { get; set; } = 416;
        private int _phase;
        public void Tick(bool motor)
        {
            Motor = motor;
            if (InputPresent && ++_phase == HalfPeriod)
            {
                _phase = 0;
                InputLevel = !InputLevel;
            }
        }
    }

    private static ElectronUla Create(out CassettePins cassette)
    {
        cassette = new();
        return new(new ElectronVideo(_ => 0), cassette);
    }

    private static void Advance(ElectronUla ula, int clocks)
    {
        for (var clock = 0; clock < clocks; clock++) ula.Tick();
    }

    private static void RaiseAllCauses(ElectronUla ula, CassettePins cassette)
    {
        ula.Read(0);
        ula.Write(7, 4);
        ula.Tick();
        cassette.InputPresent = true;
        ula.Write(7, 0x40);
        Advance(ula, 33000);
        // Follow the carrier with space data; obtain RX Full from actual receive clocks.
        cassette.HalfPeriod = 832;
        var remaining = 20000;
        while ((ula.Read(0) & 0x10) == 0 && --remaining > 0) ula.Tick();
        Assert.True(remaining > 0);
        Assert.Equal(0xfc, ula.Read(0));
    }

    [Fact]
    public void PowerOnIsClearedOnlyByReadingAnInterruptStatusMirror()
    {
        var ula = Create(out _);
        for (var address = 1; address < 256; address++)
        {
            var register = address & 15;
            if (register is 0 or 4) continue;
            Assert.Equal(0xfe, ula.Read(address));
        }
        Assert.Equal(0, ula.Read(0xfe94));
        Assert.Equal(0x82, ula.Read(0xfe70));
        for (var mirror = 0; mirror < 16; mirror++)
            Assert.Equal(0x80, ula.Read(0xfe00 + mirror * 16));
    }

    [Fact]
    public void AllInterruptMasksGateSummaryWithoutDestroyingAnyCause()
    {
        var ula = Create(out var cassette);
        RaiseAllCauses(ula, cassette);
        for (var mask = 0; mask < 256; mask++)
        {
            ula.Write(0xfe80, (byte)mask);
            var expected = (mask & 0x7c) != 0;
            Assert.Equal(expected, ula.Irq);
            Assert.Equal(expected ? 0xfd : 0xfc, ula.Read(0));
        }
    }

    [Theory]
    [InlineData(0x10, 0x78)]
    [InlineData(0x20, 0x74)]
    [InlineData(0x40, 0x3c)]
    [InlineData(0x70, 0x30)]
    [InlineData(0x80, 0x7c)]
    public void PageRegisterClearsOnlyItsThreeSpecifiedCauses(int clear, int remaining)
    {
        var ula = Create(out var cassette);
        RaiseAllCauses(ula, cassette);
        ula.Write(0, 0x7c);
        ula.Write(5, 10);
        ula.Write(0xfef5, (byte)clear);
        Assert.Equal(10, ula.RomPage);
        Assert.Equal(0x80 | remaining | 1, ula.Read(0));
        Assert.True(ula.Irq);
        ula.Read(0xfe94);
        Assert.Equal(0, ula.Read(0) & 0x10);
        Assert.Equal(remaining & ~0x10, ula.Read(0) & 0x7c);
    }

    [Fact]
    public void EveryPageTransitionHonoursTheInternalRomLatch()
    {
        var ula = Create(out _);
        for (var from = 0; from < 16; from++)
        for (var value = 0; value < 256; value++)
        {
            ula.Write(5, 12);
            ula.Write(5, (byte)from);
            ula.Write(5, (byte)value);
            var requested = value & 15;
            var expected = from is >= 8 and <= 11 && requested < 8 ? from : requested;
            Assert.Equal(expected, ula.RomPage);
        }
    }

    [Fact]
    public void EveryControlValueUpdatesIndependentMotorCapsAndDisplayFields()
    {
        var cassette = new CassettePins();
        var video = new ElectronVideo(_ => 0);
        var ula = new ElectronUla(video, cassette);
        for (var value = 0; value < 256; value++)
        {
            ula.Write(0xfe77, (byte)value);
            Assert.Equal((value & 64) != 0, ula.Motor);
            Assert.Equal((value & 128) != 0, ula.CapsLock);
            Assert.Equal((value >> 3) & 7, video.Mode);
            ula.Tick();
            Assert.Equal(ula.Motor, cassette.Motor);
            if ((value & 6) != 2) Assert.Equal(0, ula.SoundSample);
        }
    }

    [Fact]
    public void EnteringOutputDoesNotInventAReceiveFullBeforeTheCounterClocks()
    {
        var ula = Create(out _);
        ula.Read(0);
        ula.Write(7, 4);
        Assert.Equal(0x80, ula.Read(0));
        ula.Tick();
        Assert.Equal(0xa0, ula.Read(0));
        ula.Write(4, 0x55);
        Assert.Equal(0x80, ula.Read(0));
        Advance(ula, 14000);
        Assert.NotEqual(0, ula.Read(0) & 0x10);
        ula.Read(4);
        Assert.Equal(0, ula.Read(0) & 0x10);
        ula.Write(7, 4);
        Assert.Equal(0, ula.Read(0) & 0x10);
    }
}
