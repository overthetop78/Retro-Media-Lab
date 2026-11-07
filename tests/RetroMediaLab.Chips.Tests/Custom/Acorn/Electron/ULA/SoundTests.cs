using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class SoundTests
{
    private sealed class Pins : IElectronTapePins
    {
        public bool InputPresent => false;
        public bool InputLevel => false;
        public void SetOutputLevel(bool value) { }
        public void Tick(bool motor) { }
    }

    [Fact]
    public void EveryDividerProducesTheNominalSquareWavePeriod()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new Pins());
        for (var divider = 0; divider < 256; divider++)
        {
            ula.Reset(true);
            ula.Write(6, (byte)divider);
            ula.Write(7, 2);
            // Cold CNT is zero: the first master clock loads PD; its initial phase is conventional.
            for (var clock = 0; clock < 1; clock++) ula.Tick();
            Assert.Equal(8192, ula.SoundSample);
            var halfPeriod = 32 * (divider + 1);
            for (var clock = 1; clock <= halfPeriod * 2; clock++)
            {
                ula.Tick();
                var high = clock < halfPeriod || clock == halfPeriod * 2;
                Assert.Equal(high ? (short)8192 : (short)-8192, ula.SoundSample);
            }
        }
    }

    [Fact]
    public void VideoMotorAndCapsWritesDoNotRestartTheSoundCounter()
    {
        var video = new ElectronVideo(_ => 0);
        var ula = new ElectronUla(video, new Pins());
        ula.Reset(true);
        ula.Write(6, 1);
        ula.Write(7, 2);
        for (var clock = 0; clock < 1; clock++) ula.Tick();
        for (var clock = 0; clock < 31; clock++) ula.Tick();
        ula.Write(7, 0xfa);
        Assert.True(ula.Motor);
        Assert.True(ula.CapsLock);
        Assert.Equal(7, video.Mode);
        for (var clock = 31; clock < 63; clock++) ula.Tick();
        Assert.Equal(8192, ula.SoundSample);
        ula.Tick();
        Assert.Equal(-8192, ula.SoundSample);
    }

    [Fact]
    public void OnlySoundModeDrivesAudioAndDividerWritesPreserveTheCurrentPeriod()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new Pins());
        foreach (var control in new byte[] { 0, 4, 6 })
        {
            ula.Write(7, control);
            for (var clock = 0; clock < 100; clock++) ula.Tick();
            Assert.Equal(0, ula.SoundSample);
        }
        ula.Reset(true);
        ula.Write(6, 1);
        ula.Write(7, 2);
        for (var clock = 0; clock < 1; clock++) ula.Tick();
        Assert.Equal(8192, ula.SoundSample);
        for (var clock = 0; clock < 10; clock++) ula.Tick();
        ula.Write(6, 2);
        Assert.Equal(8192, ula.SoundSample);
        for (var clock = 10; clock < 63; clock++) ula.Tick();
        Assert.Equal(8192, ula.SoundSample);
        ula.Tick();
        Assert.Equal(-8192, ula.SoundSample);
        for (var clock = 0; clock < 95; clock++) ula.Tick();
        Assert.Equal(-8192, ula.SoundSample);
        ula.Tick();
        Assert.Equal(8192, ula.SoundSample);
        ula.Reset(true);
        Assert.Equal(0, ula.SoundSample);
    }

    [Fact]
    public void ModeRoundTripWithoutAClockPreservesTheSoundLatchAndDividerPhase()
    {
        var actual = new ElectronUla(new ElectronVideo(_ => 0), new Pins());
        var expected = new ElectronUla(new ElectronVideo(_ => 0), new Pins());
        foreach (var ula in new[] { actual, expected })
        {
            ula.Write(6, 3);
            ula.Write(7, 2);
            for (var clock = 0; clock < 42; clock++) ula.Tick();
        }
        Assert.Equal(8192, actual.SoundSample);
        foreach (var control in new byte[] { 0, 4, 6 })
        {
            actual.Write(7, control);
            Assert.Equal(0, actual.SoundSample);
            actual.Write(7, 2);
            Assert.Equal(expected.SoundSample, actual.SoundSample);
        }
        for (var clock = 0; clock < 300; clock++)
        {
            actual.Tick();
            expected.Tick();
            Assert.Equal(expected.SoundSample, actual.SoundSample);
        }
    }

}
