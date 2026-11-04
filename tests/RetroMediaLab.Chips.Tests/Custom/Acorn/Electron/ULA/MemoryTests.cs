using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class MemoryTests
{
    private static List<int> VisibleAddresses(ElectronVideo video, int points)
    {
        List<int> addresses = [];
        for (var point = 0; point < points; point++)
        {
            var width = video.LowBandwidth ? 16 : 8;
            var fetch = video.DisplayFetching && (video.Dot % width) == 0;
            video.TickPixel();
            if (fetch) addresses.Add(video.VideoAddress);
        }
        return addresses;
    }

    [Theory]
    [InlineData(0, 80, 8)]
    [InlineData(1, 80, 8)]
    [InlineData(2, 80, 8)]
    [InlineData(3, 80, 10)]
    [InlineData(4, 40, 8)]
    [InlineData(5, 40, 8)]
    [InlineData(6, 40, 10)]
    [InlineData(7, 40, 8)]
    public void AddressCounterInterleavesRastersAndAdvancesPastHiddenTextLines(int mode, int columns, int rowLines)
    {
        var video = new ElectronVideo(_ => 0);
        video.SetMode((byte)(mode << 3));
        for (var line = 0; line < rowLines + 1; line++)
        {
            var addresses = VisibleAddresses(video, 1024);
            if (line is 8 or 9 && rowLines == 10)
            {
                Assert.Empty(addresses);
                continue;
            }
            var start = 0x3000 + (line == rowLines ? columns * 8 : line);
            Assert.Equal(Enumerable.Range(0, columns).Select(column => start + column * 8), addresses);
        }
    }

    [Theory]
    [InlineData(0, 0x3000)]
    [InlineData(3, 0x4000)]
    [InlineData(4, 0x5800)]
    [InlineData(6, 0x6000)]
    [InlineData(7, 0x3000)]
    public void StartRegisterIsFieldLatchedAndCounterWrapUsesModeSize(int mode, int wrap)
    {
        var video = new ElectronVideo(_ => 0);
        video.SetMode((byte)(mode << 3));
        video.WriteStart(true, 0xff);
        video.WriteStart(false, 0xff);
        Assert.Equal(0x3000, Assert.Single(VisibleAddresses(video, 8)));
        while (video.Frame == 0) video.Tick();
        var addresses = VisibleAddresses(video, 18 * 8);
        Assert.Equal(Enumerable.Range(0, 8).Select(column => 0x7fc0 + column * 8), addresses.Take(8));
        Assert.Equal(wrap, addresses[8]);
    }

    [Theory]
    [InlineData(8, 0x3000)]
    [InlineData(9, 0x3001)]
    public void GraphicsModeClearsVa3ImmediatelyAndPreservesLowerRasterBits(int line, int expected)
    {
        var video = new ElectronVideo(_ => 0);
        video.SetMode(3 << 3);
        for (var clock = 0; clock < line * 128; clock++) video.Tick();
        Assert.False(video.DisplayFetching);
        video.SetMode(0);
        Assert.True(video.DisplayFetching);
        Assert.Equal(expected, Assert.Single(VisibleAddresses(video, 1)));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void LowBandwidthPrefetchUsesTheSecondHalfOfTheCpuPeriod(int mode)
    {
        ElectronVideo? video = null;
        List<(int Address, int Point)> samples = [];
        video = new ElectronVideo(address =>
        {
            samples.Add((address, video!.Dot));
            return (byte)address;
        });
        video.SetMode((byte)(mode << 3));
        for (var point = 0; point < 16; point++) video.TickPixel();
        Assert.Equal(new[] { (0x3000, 1), (0x3000, 4), (0x3008, 9), (0x3008, 12) }, samples);
        video.TickPixel();
        Assert.Equal(8, video.VideoData);
        Assert.Equal(0x3008, video.VideoAddress);
    }

}
