using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RamBackgroundTests
{
    private sealed class Pins : IElectronRamPins
    {
        private bool _ras = true;
        private bool _cas = true;
        internal readonly List<byte> Rows = [];
        internal readonly List<byte> Columns = [];
        internal int RowReleases;
        public byte Data => 0x0a;

        public void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
        {
            Assert.True(writeEnableHigh);
            if (_ras && !rowStrobeHigh) Rows.Add(address);
            if (!_ras && rowStrobeHigh) RowReleases++;
            if (_cas && !columnStrobeHigh)
            {
                Assert.False(rowStrobeHigh);
                Columns.Add(address);
            }
            _ras = rowStrobeHigh;
            _cas = columnStrobeHigh;
        }

        internal void Clear()
        {
            Rows.Clear();
            Columns.Clear();
            RowReleases = 0;
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0)]
    [InlineData(3, 8)]
    [InlineData(6, 8)]
    [InlineData(0, 260)]
    [InlineData(4, 260)]
    public void EverySlotKeepsItsTwoCasStrobesOnActiveHiddenAndVerticalBlankLines(int mode, int line)
    {
        var pins = new Pins();
        var video = new ElectronVideo(pins);
        video.SetMode((byte)(mode << 3));
        for (var cycle = 0; cycle < line * 128; cycle++) video.Tick();
        pins.Clear();
        var frame = video.Frame;
        for (var cycle = 0; cycle < 128; cycle++) video.Tick();
        Assert.Equal(128, pins.Rows.Count);
        Assert.Equal(128, pins.RowReleases);
        Assert.Equal(256, pins.Columns.Count);
        for (var slot = 0; slot < 128; slot++)
        {
            Assert.Equal(0, pins.Columns[slot * 2] & 128);
            Assert.Equal(pins.Columns[slot * 2] | 128, pins.Columns[slot * 2 + 1]);
        }
        Assert.Equal(frame, video.Frame);
        Assert.Equal(line + 1, video.Line);
        Assert.Equal(0, video.Dot);
        if (line == 260) Assert.False(video.DisplayFetching);
    }

    [Fact]
    public void EveryModeTransitionAtEveryPointPreservesTheAddressOfTheTransactionAlreadyStarted()
    {
        for (var oldMode = 0; oldMode < 8; oldMode++)
        for (var newMode = 0; newMode < 8; newMode++)
        for (var phase = 0; phase < 8; phase++)
        {
            var pins = new Pins();
            var video = new ElectronVideo(pins);
            video.SetMode((byte)(oldMode << 3));
            // Start the transaction before changing mode, including the phase-zero case.
            video.TickPixel();
            for (var point = 1; point <= phase; point++) video.TickPixel();
            video.SetMode((byte)(newMode << 3));
            while (video.Dot < 8) video.TickPixel();
            Assert.Equal(1, pins.RowReleases);
            Assert.Equal(new byte[] { 24, 152 }, pins.Columns);
            Assert.Equal(oldMode < 4 ? 0x01 : 0x00, Assert.Single(pins.Rows));
            for (var point = 0; point < 8; point++) video.TickPixel();
            Assert.Equal(2, pins.Rows.Count);
            Assert.Equal(2, pins.RowReleases);
            Assert.Equal(4, pins.Columns.Count);
        }
    }
}
