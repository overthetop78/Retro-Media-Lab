namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed partial class CassetteTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StableInputCanClockRxWithoutInventingCarrier(bool present, bool level)
    {
        var pins = new Pins { InputPresent = present, InputLevel = level };
        var ula = Create(pins);
        ula.Read(0);
        ula.Write(0, 0x10);
        ula.Write(7, 0);
        var causes = 0;
        for (var clock = 0; clock < 40000; clock++)
        {
            ula.Tick();
            var status = ula.Read(0);
            Assert.Equal(0, status & 0x40);
            if ((status & 0x10) == 0) continue;
            Assert.True(ula.Irq);
            Assert.Equal(0, ula.Read(4));
            Assert.False(ula.Irq);
            causes++;
        }
        Assert.True(causes >= 2);
    }

    [Fact]
    public void MissingMediaDoesNotDiscardAnAlreadyPartialFrame()
    {
        var pins = new Pins { InputPresent = true };
        var ula = Create(pins);
        ula.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(ula, pins, true);
        Bit(ula, pins, false);
        for (var bit = 0; bit < 4; bit++) Bit(ula, pins, false);
        Assert.Equal(0, ula.Read(0) & 0x10);
        pins.InputPresent = false;
        var remaining = 10000;
        while ((ula.Read(0) & 0x10) == 0 && --remaining > 0) ula.Tick();
        Assert.True(remaining > 0);
        Assert.Equal(0, ula.Read(4));
    }

    [Fact]
    public void QuietAfterHighToneRetainsTheHighDataLatchWithoutSynthesizingAnotherStart()
    {
        var pins = new Pins { InputPresent = true };
        var ula = Create(pins);
        ula.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(ula, pins, true);
        ula.Write(5, 0x40);
        pins.InputPresent = false;
        for (var clock = 0; clock < 40000; clock++)
        {
            ula.Tick();
            Assert.Equal(0, ula.Read(0) & 0x50);
        }
    }
}
