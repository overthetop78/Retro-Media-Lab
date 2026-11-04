using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RamAddressTests
{
    // Independent wiring lists in physical pin order, transcribed from sheet 8.
    private static readonly int[] RowBits = [3, 4, 5, 6, 7, 2, 1, 8];
    private static readonly int[] ColumnBits = [9, 10, 11, 12, 13, 14, 0];

    private static byte Pins(int address, int[] bits)
    {
        var pins = 0;
        for (var pin = 0; pin < bits.Length; pin++)
            if ((address & (1 << bits[pin])) != 0) pins |= 1 << pin;
        return (byte)pins;
    }

    [Fact]
    public void EveryAddressWireReachesItsDocumentedRowOrColumnPin()
    {
        for (var bit = 0; bit < 16; bit++)
        {
            var address = (ushort)(1 << bit);
            Assert.Equal(Pins(address, RowBits), ElectronRamAddress.Row(address));
            Assert.Equal(Pins(address, ColumnBits), ElectronRamAddress.Column(address, false));
            Assert.Equal(Pins(address, ColumnBits) | 0x80, ElectronRamAddress.Column(address, true));
        }
    }

    [Fact]
    public void AllLogicalBytesAndBothDataGroupsOccupyDistinctPhysicalAddresses()
    {
        var visited = new bool[65536];
        for (var address = 0; address < 32768; address++)
        foreach (var odd in new[] { false, true })
        {
            var row = ElectronRamAddress.Row((ushort)address);
            var column = ElectronRamAddress.Column((ushort)address, odd);
            Assert.Equal(Pins(address, RowBits), row);
            Assert.Equal(Pins(address, ColumnBits) | (odd ? 0x80 : 0), column);
            var physical = (row << 8) | column;
            Assert.Equal(address, ElectronRamAddress.Logical(row, column));
            Assert.False(visited[physical]);
            visited[physical] = true;
        }
        Assert.All(visited, Assert.True);
    }

    [Fact]
    public void A15DoesNotChangeMultiplexedPinsAndCannotReplaceParity()
    {
        for (var address = 0; address < 32768; address++)
        {
            var low = (ushort)address;
            var high = (ushort)(address | 0x8000);
            Assert.Equal(ElectronRamAddress.Row(low), ElectronRamAddress.Row(high));
            foreach (var odd in new[] { false, true })
                Assert.Equal(ElectronRamAddress.Column(low, odd), ElectronRamAddress.Column(high, odd));
            Assert.Equal(0x80, ElectronRamAddress.Column(low, false) ^ ElectronRamAddress.Column(low, true));
        }
    }
}
