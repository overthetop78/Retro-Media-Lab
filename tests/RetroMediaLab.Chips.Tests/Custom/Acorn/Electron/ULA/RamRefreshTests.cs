using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RamRefreshTests(ITestOutputHelper output)
{
    private sealed class Pins : IElectronRamPins
    {
        private bool _ras = true;
        private readonly int?[] _last = new int?[128];
        internal readonly HashSet<int> Rows = [];
        internal int Point;
        internal int LongestGap;
        public byte Data => 0;

        public void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
        {
            Assert.True(writeEnableHigh);
            if (_ras && !rowStrobeHigh)
            {
                // 4164 128-cycle refresh uses RA0–RA6; RA7 is not a refresh address bit.
                var row = address & 127;
                Rows.Add(row);
                if (_last[row] is int previous) LongestGap = Math.Max(LongestGap, Point - previous);
                else LongestGap = Math.Max(LongestGap, Point);
                _last[row] = Point;
            }
            _ras = rowStrobeHigh;
        }

        internal void FinishWindow(int end)
        {
            foreach (var last in _last)
                if (last is int point) LongestGap = Math.Max(LongestGap, end - point);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void VideoAloneRefreshesAll128RowsWithinTwoMilliseconds(int mode)
    {
        var pins = new Pins();
        var video = new ElectronVideo(pins);
        video.SetMode((byte)(mode << 3));
        // Two unequal fields: enough to observe a gap across their boundary, not only one line.
        for (var cycle = 0; cycle < 80000; cycle++)
        {
            pins.Point = cycle * 8;
            video.Tick();
        }
        pins.FinishWindow(640000);
        output.WriteLine($"mode={mode}; rows={pins.Rows.Count}/128; longest observed gap={pins.LongestGap} master periods; missing={string.Join(',', Enumerable.Range(0, 128).Except(pins.Rows))}");
        Assert.Equal(128, pins.Rows.Count);
        Assert.InRange(pins.LongestGap, 0, 32000);
    }
}
