using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RamDataTests
{
    private static int Spread(int pins, int lane)
    {
        var result = 0;
        for (var line = 0; line < 4; line++)
            if ((pins & (1 << line)) != 0) result |= 1 << (line * 2 + lane);
        return result;
    }

    [Fact]
    public void EachStrobeReplacesOnlyItsOwnFourBitsForAllPinValues()
    {
        var data = new ElectronRamData();
        for (var old = 0; old < 256; old++)
        for (var pins = 0; pins < 256; pins++)
        {
            // Seed each latch independently; the high nibble is not wired.
            data.LoadEven(ElectronRamDataPins.Select((byte)old, false));
            data.LoadOdd(ElectronRamDataPins.Select((byte)old, true));
            data.LoadEven((byte)pins);
            Assert.Equal((old & 0xaa) | Spread(pins, 0), data.Data);
            data.LoadEven(ElectronRamDataPins.Select((byte)old, false));
            data.LoadOdd((byte)pins);
            Assert.Equal((old & 0x55) | Spread(pins, 1), data.Data);
        }
    }

    [Fact]
    public void WriteSelectorPresentsTheFourPhysicalDataLinesForEveryByte()
    {
        for (var value = 0; value < 256; value++)
        foreach (var odd in new[] { false, true })
        {
            var expected = 0;
            for (var line = 0; line < 4; line++)
                if ((value & (1 << (line * 2 + (odd ? 1 : 0)))) != 0) expected |= 1 << line;
            Assert.Equal(expected, ElectronRamDataPins.Select((byte)value, odd));
        }
    }

    [Fact]
    public void InputChangesBetweenStrobesLeaveTheFirstGroupLatched()
    {
        var data = new ElectronRamData();
        for (var even = 0; even < 16; even++)
        for (var odd = 0; odd < 16; odd++)
        {
            data.LoadOdd(0);
            data.LoadEven((byte)even);
            Assert.Equal(Spread(even, 0), data.Data);
            data.LoadOdd((byte)odd);
            var word = Spread(even, 0) | Spread(odd, 1);
            Assert.Equal(word, data.Data);
            data.LoadEven((byte)(even ^ 15));
            Assert.Equal(word ^ 0x55, data.Data);
        }
    }
    [Fact]
    public void ParallelPinMergeMatchesEachIndividualWireAndPreservesTheOtherGroup()
    {
        for (var previous = 0; previous < 256; previous++)
        for (var pins = 0; pins < 256; pins++)
        foreach (var odd in new[] { false, true })
        {
            var untouched = previous & (odd ? 0x55 : 0xaa);
            var expected = untouched | Spread(pins, odd ? 1 : 0);
            var actual = ElectronRamDataPins.Merge((byte)previous, (byte)pins, odd);
            Assert.Equal(expected, actual);
            Assert.Equal(pins & 15, ElectronRamDataPins.Select(actual, odd));
        }
    }

}
