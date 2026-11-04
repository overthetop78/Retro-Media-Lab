namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed partial class CassetteTests
{
    [Fact]
    public void SoundCounterCompletesTheLoadedTransmitFrameAndRaisesItsMaskedInterrupt()
    {
        var ula = Create(new Pins());
        ula.Write(0, 0x20);
        ula.Write(6, 0);
        ula.Write(7, 2);
        ula.Write(4, 0xa5);
        // PD=0 gives a boundary every 32 reference clocks, starting at the first clock.
        // The first boundary synchronizes start; the ninth following boundary enters stop.
        for (var clock = 1; clock < 289; clock++)
        {
            ula.Tick();
            Assert.False(ula.Irq);
            Assert.Equal(0, ula.Read(0) & 0x20);
        }
        ula.Tick();
        Assert.True(ula.Irq);
        Assert.Equal(0x21, ula.Read(0) & 0x21);
        ula.Write(4, 0x55);
        Assert.False(ula.Irq);
        Assert.Equal(0, ula.Read(0) & 0x20);
    }

    [Theory]
    [InlineData(0x01, true)]
    [InlineData(0x02, false)]
    [InlineData(0xa5, true)]
    public void SoundAdvancesFrameStateWithoutShiftingTheDataRegister(int value, bool lowDataBit)
    {
        var pins = new Pins();
        var ula = Create(pins);
        ula.Write(6, 31);
        ula.Write(7, 2);
        ula.Write(4, (byte)value);
        // Three boundaries: start is synchronized and left, but DREG has not shifted.
        for (var clock = 0; clock < 2100; clock++) ula.Tick();
        Assert.Equal(0, ula.Read(0) & 0x20);
        ula.Write(7, 4);
        ula.Tick();
        // At this retained phase bit 12 is high, bit 13 low: CAS OUT reveals DREG bit zero.
        Assert.Equal(lowDataBit, pins.OutputLevel);
    }

    [Fact]
    public void UnusedModeFreezesTheCommsClockAndTransmitFrameRatherThanConsumingWallTime()
    {
        var actualPins = new Pins();
        var witnessPins = new Pins();
        var actual = Create(actualPins);
        var witness = Create(witnessPins);
        foreach (var ula in new[] { actual, witness })
        {
            ula.Write(6, 31);
            ula.Write(7, 2);
            ula.Write(4, 0xa5);
            for (var clock = 0; clock < 2100; clock++) ula.Tick();
        }
        actual.Write(7, 6);
        for (var clock = 0; clock < 10000; clock++) actual.Tick();
        Assert.Equal(0, actual.Read(0) & 0x20);
        actual.Write(7, 4);
        witness.Write(7, 4);
        for (var clock = 0; clock < 8 * BitClocks; clock++)
        {
            actual.Tick();
            witness.Tick();
            Assert.Equal(witnessPins.OutputLevel, actualPins.OutputLevel);
            Assert.Equal(witness.Read(0) & 0x20, actual.Read(0) & 0x20);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(416)]
    [InlineData(16000)]
    [InlineData(20384)]
    [InlineData(20800)]
    [InlineData(21216)]
    [InlineData(26208)]
    [InlineData(33700)]
    public void UnusedModePreservesReceiveCarrierStartAndDataPhase(int pauseAt)
    {
        const int value = 0xa5;
        const int carrierBits = 12;
        const int frameBits = 10;
        const int pausedClocks = 1000;
        const int cassetteInterrupts = 0x70;
        var actualPins = new Pins { InputPresent = true };
        var witnessPins = new Pins { InputPresent = true };
        var actual = Create(actualPins);
        var witness = Create(witnessPins);
        actual.Write(7, 0);
        witness.Write(7, 0);
        for (var clock = 1; clock <= (carrierBits + frameBits) * BitClocks; clock++)
        {
            if (clock == pauseAt)
            {
                var status = actual.Read(0);
                actual.Write(7, 6);
                for (var idle = 0; idle < pausedClocks; idle++) actual.Tick();
                // Video keeps running during the pause; compare cassette causes only.
                Assert.Equal(status & cassetteInterrupts, actual.Read(0) & cassetteInterrupts);
                actual.Write(7, 0);
            }
            var bit = (clock - 1) / BitClocks;
            var frameBit = bit - carrierBits;
            var mark = bit < carrierBits || frameBit == 9
                || frameBit > 0 && (value & (1 << (frameBit - 1))) != 0;
            var halfPeriod = mark ? BitClocks / 4 : BitClocks / 2;
            var withinBit = (clock - 1) % BitClocks + 1;
            actualPins.InputLevel = witnessPins.InputLevel = ((withinBit / halfPeriod) & 1) != 0;
            actual.Tick();
            witness.Tick();
            Assert.Equal(witness.Read(0) & cassetteInterrupts, actual.Read(0) & cassetteInterrupts);
        }
        Assert.Equal(value, witness.Read(4));
        Assert.Equal(value, actual.Read(4));
    }

    [Theory]
    [InlineData(2, 0, 1057)]
    [InlineData(2, 31, 16897)]
    [InlineData(4, 0, 13729)]
    public void SharedPhaseClockRaisesReceiveFullAfterTheEighthDataSample(int mode, int divider, int readyAt)
    {
        var ula = Create(new Pins());
        ula.Read(0);
        ula.Write(0, 0x10);
        ula.Write(6, (byte)divider);
        ula.Write(7, (byte)mode);
        Assert.False(ula.Irq);
        for (var clock = 1; clock < readyAt; clock++)
        {
            ula.Tick();
            Assert.False(ula.Irq);
            Assert.Equal(0, ula.Read(0) & 0x10);
        }
        ula.Tick();
        Assert.True(ula.Irq);
        Assert.Equal(0x11, ula.Read(0) & 0x11);
        Assert.Equal(0, ula.Read(4));
        Assert.False(ula.Irq);
        ula.Write(7, (byte)mode);
        Assert.False(ula.Irq);
    }
}
