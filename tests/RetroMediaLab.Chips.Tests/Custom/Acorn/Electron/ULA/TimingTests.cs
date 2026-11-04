using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class TimingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FieldsAndInterruptsMatchQuantizedHardwareAcquisitions(bool spaced)
    {
        var timing = new ElectronVideoTiming();
        List<int> fields = [];
        List<int> rtc = [];
        List<int> display = [];
        for (var clock = 1; clock <= 1_920_000; clock++)
        {
            timing.Tick(spaced);
            if (timing.FieldEnded) fields.Add(clock);
            if (timing.RtcEdge) rtc.Add(clock);
            if (timing.DisplayEndEdge) display.Add(clock);
        }

        Assert.Equal([319_488, 640_000, 959_488, 1_280_000, 1_599_488, 1_920_000], fields);
        Assert.Equal([101_360, 421_360, 741_360, 1_061_360, 1_381_360, 1_701_360], rtc);
        int[] expectedDisplay = spaced
            ? [255_744, 575_232, 895_744, 1_215_232, 1_535_744, 1_855_232]
            : [261_888, 581_376, 901_888, 1_221_376, 1_541_888, 1_861_376];
        Assert.Equal(expectedDisplay, display);
        Assert.Equal(6, timing.Fields);
        Assert.Equal(0, timing.PictureClock);
    }

    [Fact]
    public void SyncBoundariesMatchMeasuredWidthsAndHalfLineFieldPhase()
    {
        var timing = new ElectronVideoTiming();
        for (var clock = 0; clock < 640_000; clock++)
        {
            var dot = clock % 1_024;
            var expectedHorizontal = dot >= 766 && dot < 830;
            var expectedVertical = (clock >= 287_728 && clock < 290_288)
                || (clock >= 607_728 && clock < 610_288);
            Assert.Equal(expectedHorizontal, timing.HorizontalSync);
            Assert.Equal(expectedHorizontal || expectedVertical, timing.CompositeSync);
            Assert.Equal(clock >= 319_488, timing.LongField);
            var fieldClock = clock >= 319_488 ? clock - 319_488 : clock;
            Assert.Equal(fieldClock / 1_024, timing.Line);
            Assert.Equal(dot, timing.Dot);
            timing.Tick(false);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CpuPeriodBatchPreservesAllCounterAndIrqEdges(bool spaced)
    {
        var dots = new ElectronVideoTiming();
        var cpu = new ElectronVideoTiming();
        for (var cycle = 0; cycle < 160_000; cycle++)
        {
            var rtc = false;
            var display = false;
            var field = false;
            for (var dot = 0; dot < 8; dot++)
            {
                dots.Tick(spaced);
                rtc |= dots.RtcEdge;
                display |= dots.DisplayEndEdge;
                field |= dots.FieldEnded;
            }
            cpu.TickCpuClock(spaced);
            Assert.Equal(dots.PictureClock, cpu.PictureClock);
            Assert.Equal(dots.Fields, cpu.Fields);
            Assert.Equal(dots.HorizontalSync, cpu.HorizontalSync);
            Assert.Equal(dots.CompositeSync, cpu.CompositeSync);
            Assert.Equal(rtc, cpu.RtcEdge);
            Assert.Equal(display, cpu.DisplayEndEdge);
            Assert.Equal(field, cpu.FieldEnded);
        }
    }

    [Fact]
    public void ModeSelectionAffectsDisplayEndWithoutRestartingFieldOrRtc()
    {
        var timing = new ElectronVideoTiming();
        for (var clock = 0; clock < 255_743; clock++) timing.Tick(false);
        timing.Tick(true);
        Assert.True(timing.DisplayEndEdge);
        timing.Tick(true);
        Assert.False(timing.DisplayEndEdge);
        for (var clock = timing.PictureClock; clock < 261_888; clock++) timing.Tick(true);
        Assert.False(timing.DisplayEndEdge);
        for (var clock = timing.PictureClock; clock < 421_360; clock++) timing.Tick(true);
        Assert.True(timing.RtcEdge);
        Assert.Equal(1, timing.Fields);
        timing.Tick(false);
        Assert.False(timing.RtcEdge);
    }

    [Fact]
    public void ResetRestoresCounterOriginAndClearsEventPulses()
    {
        var timing = new ElectronVideoTiming();
        for (var clock = 0; clock < 959_488; clock++) timing.Tick(false);
        Assert.True(timing.FieldEnded);
        timing.Reset();
        Assert.Equal(0, timing.PictureClock);
        Assert.Equal(0, timing.Fields);
        Assert.False(timing.LongField);
        Assert.False(timing.RtcEdge);
        Assert.False(timing.DisplayEndEdge);
        Assert.False(timing.FieldEnded);
        Assert.False(timing.CompositeSync);
    }
    [Fact]
    public void CachedLineAndDotMatchArithmeticAcrossUnalignedSingleAndEightClockSteps()
    {
        var timing = new ElectronVideoTiming();
        var elapsed = 0;
        var step = 0;
        while (elapsed < 1280000)
        {
            var clocks = step++ % 7 < 3 ? 8 : 1;
            if (clocks == 8) timing.TickCpuClock(false);
            else timing.Tick(false);
            elapsed += clocks;
            var picture = elapsed % 640000;
            var field = picture < 319488 ? picture : picture - 319488;
            Assert.Equal(picture, timing.PictureClock);
            Assert.Equal(field, timing.FieldClock);
            Assert.Equal(field / 1024, timing.Line);
            Assert.Equal(field % 1024, timing.Dot);
            Assert.Equal((long)(elapsed / 640000 * 2 + (picture >= 319488 ? 1 : 0)), timing.Fields);
        }
    }

}
