using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed partial class TapeTests
{
    private static void BitRange(ElectronUla receiver, Pins pins, bool mark, int first, int last)
    {
        var halfPeriod = mark ? BitClocks / 4 : BitClocks / 2;
        for (var clock = first; clock <= last; clock++)
        {
            pins.InputLevel = ((clock / halfPeriod) & 1) != 0;
            receiver.Tick();
        }
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x80)]
    [InlineData(0xa5)]
    [InlineData(0xff)]
    public void EighthDataBitSetsReceiveFullAtItsMidpointRatherThanItsEnd(int value)
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 7; bit++) Bit(receiver, pins, (value & (1 << bit)) != 0);
        var mark = (value & 0x80) != 0;
        BitRange(receiver, pins, mark, 1, BitClocks / 2 - 1);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        BitRange(receiver, pins, mark, BitClocks / 2, BitClocks / 2);
        Assert.NotEqual(0, receiver.Read(0) & 0x10);
        Assert.Equal(value, receiver.Read(4));
    }

    [Theory]
    [InlineData(0x00, 0xff)]
    [InlineData(0xff, 0x00)]
    [InlineData(0xa5, 0x3c)]
    public void HalfStopRearmsReceptionWithoutShiftingTheStopIntoCd(int first, int second)
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        foreach (var value in new[] { first, second })
        {
            Bit(receiver, pins, false);
            for (var bit = 0; bit < 8; bit++) Bit(receiver, pins, (value & (1 << bit)) != 0);
            Assert.NotEqual(0, receiver.Read(0) & 0x10);
            Assert.Equal(value, receiver.Read(4));
            BitRange(receiver, pins, true, 1, BitClocks / 2);
            Assert.Equal(value, receiver.Read(4));
        }
    }

    [Fact]
    public void NextStartShiftsIntoCdAndRemovesAnUnreadReceiveFull()
    {
        var pins = new Pins { InputPresent = true };
        var receiver = Create(pins);
        receiver.Write(7, 0);
        for (var bit = 0; bit < 12; bit++) Bit(receiver, pins, true);
        Bit(receiver, pins, false);
        for (var bit = 0; bit < 8; bit++) Bit(receiver, pins, true);
        Assert.NotEqual(0, receiver.Read(0) & 0x10);
        Bit(receiver, pins, true);
        BitRange(receiver, pins, false, 1, BitClocks / 2 - 1);
        Assert.NotEqual(0, receiver.Read(0) & 0x10);
        BitRange(receiver, pins, false, BitClocks / 2, BitClocks / 2);
        Assert.Equal(0, receiver.Read(0) & 0x10);
        Assert.Equal(0x7f, receiver.Read(4));
    }
}
