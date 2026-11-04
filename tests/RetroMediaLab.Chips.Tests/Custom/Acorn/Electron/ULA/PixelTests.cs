using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class PixelTests
{
    // Each row gives the logical pens wired to one physical pair of palette registers.
    private static readonly int[][] Pens = [[0, 2, 8, 10], [4, 6, 12, 14], [5, 7, 13, 15], [1, 3, 9, 11]];

    private static int Color(int rgb) => ((rgb & 1) != 0 ? 0xff : 0)
        | ((rgb & 2) != 0 ? 0xff00 : 0) | ((rgb & 4) != 0 ? 0xff0000 : 0);

    private static int PaletteColor(int pen) => (pen & 7) ^ ((pen >> 3) * 7);

    private static void SetPalette(ElectronVideo video)
    {
        byte[] registers = Enumerable.Repeat((byte)0xff, 8).ToArray();
        for (var pair = 0; pair < 4; pair++)
        for (var slot = 0; slot < 4; slot++)
        {
            var color = PaletteColor(Pens[pair][slot]);
            var even = pair * 2;
            if ((color & 1) != 0) registers[even + 1] &= (byte)~(1 << slot);
            if ((color & 2) != 0)
            {
                var register = slot < 2 ? even + 1 : even;
                var bit = slot < 2 ? slot + 4 : slot;
                registers[register] &= (byte)~(1 << bit);
            }
            if ((color & 4) != 0) registers[even] &= (byte)~(1 << (slot + 4));
        }
        for (var register = 0; register < 8; register++) video.WritePalette(register, registers[register]);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 2, 1)]
    [InlineData(2, 4, 1)]
    [InlineData(3, 1, 1)]
    [InlineData(4, 1, 2)]
    [InlineData(5, 2, 2)]
    [InlineData(6, 1, 2)]
    [InlineData(7, 1, 2)]
    public void EveryDataByteUsesItsBitPlanesAndPixelRepetitions(int mode, int bits, int repeat)
    {
        byte data = 0;
        var video = new ElectronVideo(_ => data);
        video.SetMode((byte)(mode << 3));
        SetPalette(video);
        var bytePoints = 8 * repeat;
        for (var value = 0; value < 256; value++)
        {
            while (!video.DisplayFetching || video.Dot + bytePoints * 2 > 640) video.TickPixel();
            data = (byte)value;
            // The next cell is fetched through two CAS strobes while this one is displayed.
            for (var point = 0; point < bytePoints; point++) video.TickPixel();
            for (var point = 0; point < bytePoints; point++)
            {
                var index = point / (bits * repeat);
                var pen = ((value >> (7 - index)) & 1) * 8;
                if (bits >= 2) pen |= ((value >> (bits == 2 ? 3 - index : 5 - index)) & 1) * (bits == 2 ? 2 : 4);
                if (bits == 4) pen |= ((value >> (3 - index)) & 1) * 2 | ((value >> (1 - index)) & 1);
                video.TickPixel();
                Assert.Equal(Color(PaletteColor(pen)), video.PixelColor);
            }
        }
    }

    [Fact]
    public void EveryPaletteWireControlsOnlyItsDocumentedPenAndComponent()
    {
        byte data = 0;
        var video = new ElectronVideo(_ => data);
        for (var register = 0; register < 8; register++)
        for (var bit = 0; bit < 8; bit++)
        {
            video.Reset();
            video.SetMode(2 << 3);
            for (var r = 0; r < 8; r++) video.WritePalette(r, 0xff);
            video.WritePalette(register, (byte)(0xff ^ (1 << bit)));
            for (var pen = 0; pen < 16; pen++)
            {
                data = (byte)(((pen & 8) << 4) | ((pen & 4) << 3) | ((pen & 2) << 2) | ((pen & 1) << 1));
                var expected = 0;
                var pair = register / 2;
                for (var slot = 0; slot < 4; slot++)
                {
                    if (Pens[pair][slot] != pen) continue;
                    if ((register & 1) != 0 && bit == slot) expected |= 1;
                    if ((register & 1) == 0 && bit == slot + 4) expected |= 4;
                    if (slot < 2 && (register & 1) != 0 && bit == slot + 4) expected |= 2;
                    if (slot >= 2 && (register & 1) == 0 && bit == slot) expected |= 2;
                }
                for (var point = 0; point < 8; point++) video.TickPixel();
                video.TickPixel();
                Assert.Equal(Color(expected), video.PixelColor);
                for (var point = 1; point < 8; point++) video.TickPixel();
            }
        }
    }

    [Fact]
    public void PaletteAndModeChangesUseLatchedByteAtNextPoint()
    {
        var reads = 0;
        var video = new ElectronVideo(_ => { reads++; return 0x80; });
        video.SetMode(4 << 3);
        SetPalette(video);
        for (var point = 0; point < 16; point++) video.TickPixel();
        video.TickPixel();
        Assert.Equal(Color(7), video.PixelColor);
        video.WritePalette(0, 0xff);
        video.WritePalette(1, 0xff);
        Assert.Equal(Color(7), video.PixelColor);
        video.TickPixel();
        Assert.Equal(0, video.PixelColor);
        Assert.True(reads >= 2);
        video.SetMode(0);
        video.TickPixel();
        Assert.Equal(0, video.PixelColor);
        Assert.True(reads >= 2);
        Assert.Equal(0x80, video.VideoData);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(7, true)]
    public void GroupedCpuPeriodsAndIndividualPointsProduceIdenticalField(int mode, bool unaligned)
    {
        var points = new ElectronVideo(address => (byte)(address ^ (address >> 8)));
        var groups = new ElectronVideo(address => (byte)(address ^ (address >> 8)));
        points.SetMode((byte)(mode << 3));
        groups.SetMode((byte)(mode << 3));
        SetPalette(points);
        SetPalette(groups);
        // One unaligned point also exercises the grouped fallback through sync and field end.
        if (unaligned)
        {
            points.TickPixel();
            groups.TickPixel();
        }
        for (var cycle = 0; cycle < 39_936; cycle++)
        {
            var field = false;
            var rtc = false;
            var end = false;
            for (var dot = 0; dot < 8; dot++)
            {
                field |= points.TickPixel();
                rtc |= points.ClockInterruptEdge;
                end |= points.DisplayEndInterruptEdge;
            }
            Assert.Equal(field, groups.Tick());
            Assert.Equal(rtc, groups.ClockInterruptEdge);
            Assert.Equal(end, groups.DisplayEndInterruptEdge);
            Assert.Equal(points.VideoAddress, groups.VideoAddress);
            Assert.Equal(points.VideoData, groups.VideoData);
            Assert.Equal(points.PixelColor, groups.PixelColor);
        }
        Assert.Equal(points.CompletedFrame, groups.CompletedFrame);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BlankingColorChangesAfterTheBoundaryRamPhase(bool aligned)
    {
        var video = new ElectronVideo(_ => 0xff);
        try
        {
            for (var register = 0; register < 8; register++) video.WritePalette(register, 0);
            while (video.Dot < 640) video.Tick();
            Assert.Equal(0xffffff, video.PixelColor);
            List<(int Point, int Color)> observed = [];
            video.CpuClockChanged = (_, point) => observed.Add((point, video.PixelColor));
            if (aligned) video.Tick();
            else for (var point = 0; point < 8; point++) video.TickPixel();
            Assert.Equal(new[] { (640, 0xffffff), (644, 0) }, observed);
            Assert.Equal(0, video.PixelColor);
        }
        finally { video.CpuClockChanged = null; video.Reset(); }
    }

}
