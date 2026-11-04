using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class LowStartTests
{
    private static void Start(ElectronVideo video, int address)
    {
        video.WriteStart(false, (byte)(address >> 1));
        video.WriteStart(true, (byte)(address >> 9));
    }

    private static int[] Addresses(ElectronVideo video, int columns)
    {
        var result = new int[columns];
        for (var column = 0; column < columns; column++)
        {
            video.TickPixel();
            result[column] = video.VideoAddress;
            for (var point = 1; point < 8; point++) video.TickPixel();
        }
        return result;
    }

    [Theory]
    [InlineData(0x0000, 0x3000)]
    [InlineData(0x0040, 0x3040)]
    [InlineData(0x07c0, 0x37c0)]
    [InlineData(0x0800, 0x0800)]
    [InlineData(0x0fc0, 0x0fc0)]
    [InlineData(0x1000, 0x3000)]
    [InlineData(0x1040, 0x3040)]
    [InlineData(0x17c0, 0x37c0)]
    [InlineData(0x1800, 0x1800)]
    public void ModeZeroLoadsTheObservedInitialAliasOnlyAtTheFieldBoundary(int start, int expected)
    {
        var video = new ElectronVideo(_ => 0);
        Start(video, start);
        Assert.Equal(0x3000, Assert.Single(Addresses(video, 1)));
        while (video.Frame == 0) video.Tick();
        Assert.Equal(expected, Assert.Single(Addresses(video, 1)));
    }

    [Theory]
    [InlineData(0x0800, 256)]
    [InlineData(0x0fc0, 8)]
    public void LinearLowStartCarriesFrom1000Into3000AndKeepsTheRaster(int start, int distance)
    {
        var video = new ElectronVideo(_ => 0);
        Start(video, start);
        while (video.Frame == 0) video.Tick();
        var consumed = 0;
        // Each row of 80 bytes is repeated on eight rasters; test the row where carry occurs.
        var rows = (distance - 1) / 80;
        for (var row = 0; row < rows; row++)
        for (var point = 0; point < 8 * 1024; point++) video.TickPixel();
        consumed = rows * 80;
        for (var raster = 0; raster < 8; raster++)
        {
            var addresses = Addresses(video, 80);
            for (var column = 0; column < 80; column++)
            {
                var offset = consumed + column;
                var expected = offset < distance ? start + offset * 8 : 0x3000 + (offset - distance) * 8;
                Assert.Equal(expected + raster, addresses[column]);
            }
            for (var point = 640; point < 1024; point++) video.TickPixel();
        }
    }

    [Theory]
    [InlineData(0x0000, 0x3000)]
    [InlineData(0x07c0, 0x37c0)]
    [InlineData(0x0800, 0x0800)]
    [InlineData(0x0fc0, 0x0fc0)]
    [InlineData(0x1000, 0x3000)]
    [InlineData(0x17c0, 0x37c0)]
    public void LastLinePrefetchUsesTheSameInitialMappingAsTheNextField(int start, int expected)
    {
        ElectronVideo? video = null;
        List<int> fetched = [];
        video = new ElectronVideo(address =>
        {
            if (video!.Line == 311 && video.Dot >= 1016) fetched.Add(address);
            return 0;
        });
        Start(video, start);
        while (video.Frame == 0) video.TickPixel();
        Assert.Equal(new[] { expected, expected }, fetched);
        Assert.Equal(expected, Assert.Single(Addresses(video, 1)));
    }

    [Fact]
    public void RawStartSurvivesAChangeToAModeWithoutThisObservedAlias()
    {
        var video = new ElectronVideo(_ => 0);
        Start(video, 0x1040);
        while (video.Frame == 0) video.Tick();
        Assert.Equal(0x3040, Assert.Single(Addresses(video, 1)));
        video.SetMode(1 << 3);
        while (video.Frame == 1) video.Tick();
        Assert.Equal(0x1040, Assert.Single(Addresses(video, 1)));
    }
}
