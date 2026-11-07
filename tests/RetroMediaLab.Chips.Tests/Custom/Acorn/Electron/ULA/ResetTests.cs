using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class ResetTests
{
    private sealed class Pins : IElectronTapePins
    {
        private int _clock;
        public bool InputPresent => true;
        public bool InputLevel => (_clock / 416 & 1) != 0;
        public bool OutputLevel { get; private set; }
        public void SetOutputLevel(bool value) => OutputLevel = value;
        public void Tick(bool motor) => _clock++;
    }

    private static (ElectronUla Ula, ElectronVideo Video, Pins Pins) Create(int mode)
    {
        var pins = new Pins();
        var video = new ElectronVideo(address => (byte)(address ^ address >> 8));
        var ula = new ElectronUla(video, pins);
        ula.Read(0);
        ula.Write(0, 0x7c);
        ula.Write(2, 0xa0);
        ula.Write(3, 0x26);
        for (var register = 8; register < 16; register++) ula.Write(register, (byte)(register * 13));
        ula.Write(5, 12);
        ula.Write(5, 5);
        ula.Write(6, 37);
        ula.Write(7, (byte)(0xe0 | mode << 1));
        ula.Write(4, 0xa5);
        ula.Nmi = true;
        ula.Nmi = false;
        for (var clock = 0; clock < 4321; clock++) ula.Tick();
        return (ula, video, pins);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void BreakPreservesRegistersAndContinuousVideoSoundAndSerialPhases(int mode)
    {
        var actual = Create(mode);
        var reference = Create(mode);
        actual.Ula.Reset(false);
        for (var clock = 0; clock < 80000; clock++)
        {
            Assert.Equal(reference.Ula.Read(0), actual.Ula.Read(0));
            Assert.Equal(reference.Ula.Irq, actual.Ula.Irq);
            Assert.Equal(reference.Ula.RomPage, actual.Ula.RomPage);
            Assert.Equal(reference.Ula.Motor, actual.Ula.Motor);
            Assert.Equal(reference.Ula.CapsLock, actual.Ula.CapsLock);
            Assert.Equal(reference.Ula.CpuRamPriority, actual.Ula.CpuRamPriority);
            Assert.Equal(reference.Ula.SoundSample, actual.Ula.SoundSample);
            Assert.Equal(reference.Pins.OutputLevel, actual.Pins.OutputLevel);
            Assert.Equal(reference.Video.VideoAddress, actual.Video.VideoAddress);
            Assert.Equal(reference.Video.PixelColor, actual.Video.PixelColor);
            actual.Ula.Tick();
            reference.Ula.Tick();
        }
        Assert.Equal(reference.Ula.Read(4), actual.Ula.Read(4));
        Assert.Equal(reference.Video.Frame, actual.Video.Frame);
        Assert.Equal(reference.Video.CompletedFrame, actual.Video.CompletedFrame);
    }

    [Fact]
    public void BreakNeitherSetsNorConsumesThePowerOnFlag()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new Pins());
        ula.Reset(false);
        Assert.Equal(0x82, ula.Read(0));
        ula.Reset(false);
        Assert.Equal(0x80, ula.Read(0));
    }

    [Fact]
    public void PowerOnStillInitializesUlaRegistersAndClearsPendingTransmission()
    {
        var (ula, _, pins) = Create(2);
        ula.Reset(true);
        Assert.Equal(0, ula.RomPage);
        Assert.False(ula.Motor);
        Assert.False(ula.CapsLock);
        Assert.False(ula.Irq);
        Assert.False(ula.CpuRamPriority);
        Assert.Equal(0x82, ula.Read(0));
        for (var clock = 0; clock < 20000; clock++)
        {
            ula.Tick();
            Assert.False(pins.OutputLevel);
            Assert.Equal(0, ula.SoundSample);
        }
        Assert.Equal(0, ula.Read(0) & 0x30);
        Assert.Equal(0, ula.Read(4));
    }
}
