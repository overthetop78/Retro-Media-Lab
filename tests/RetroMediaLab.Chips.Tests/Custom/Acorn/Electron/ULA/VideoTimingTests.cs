using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class VideoTimingTests
{
    private sealed class Pins : IElectronTapePins
    {
        public bool InputPresent => false;
        public bool InputLevel => true;
        public void SetOutputLevel(bool value) { }
        public void Tick(bool motor) { }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void VideoAndUlaUseMeasuredFieldAndIrqBoundaries(int mode)
    {
        var video = new ElectronVideo(_ => 0);
        var ula = new ElectronUla(video, new Pins());
        ula.Write(7, (byte)(mode << 3));
        ula.Write(0, 0x0c);
        ula.Read(0);
        List<int> rtc = [];
        List<int> end = [];
        List<int> fields = [];
        for (var cycle = 1; cycle <= 80_000; cycle++)
        {
            var previous = video.Frame;
            ula.Tick();
            if (previous != video.Frame) fields.Add(cycle);
            if (video.ClockInterruptEdge) rtc.Add(cycle);
            if (video.DisplayEndInterruptEdge) end.Add(cycle);
            Assert.Equal(video.ClockInterruptEdge || video.DisplayEndInterruptEdge, ula.Irq);
            var expected = (video.ClockInterruptEdge ? 8 : 0) | (video.DisplayEndInterruptEdge ? 4 : 0);
            Assert.Equal(expected, ula.Read(0) & 0x0c);
            ula.Write(5, 0x30);
            Assert.False(ula.Irq);
        }
        Assert.Equal([39_936, 80_000], fields);
        Assert.Equal([12_670, 52_670], rtc);
        int[] expectedEnd = mode is 3 or 6 ? [31_968, 71_904] : [32_736, 72_672];
        Assert.Equal(expectedEnd, end);
        Assert.Equal(0, video.Line);
        Assert.Equal(0, video.Clock);
    }

    [Fact]
    public void VideoExposesNominalSyncLevelsAndResetRestoresFirstField()
    {
        var video = new ElectronVideo(_ => 0);
        for (var cycle = 0; cycle < 80_000; cycle++)
        {
            var point = cycle * 8;
            var dot = point % 1_024;
            var horizontal = dot >= 766 && dot < 830;
            var vertical = (point >= 287_728 && point < 290_288)
                || (point >= 607_728 && point < 610_288);
            Assert.Equal(horizontal, video.HorizontalSyncActive);
            Assert.Equal(horizontal || vertical, video.CompositeSyncActive);
            video.Tick();
        }
        video.Reset();
        Assert.Equal(0, video.Frame);
        Assert.False(video.ClockInterruptEdge);
        Assert.False(video.DisplayEndInterruptEdge);
        for (var cycle = 0; cycle < 39_936; cycle++) video.Tick();
        Assert.Equal(1, video.Frame);
    }
}
