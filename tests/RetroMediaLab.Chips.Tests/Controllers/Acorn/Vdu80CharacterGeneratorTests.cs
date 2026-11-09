using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class Vdu80CharacterGeneratorTests
{
    [Fact]
    public void StableDotPeriodsMatchExplicitEdgesAcrossChangingInputsAndAllLocalDeadlines()
    {
        var image = Enumerable.Range(0, 2048).Select(i => (byte)(i * 73 + 11)).ToArray();
        var grouped = new Vdu80CharacterGenerator(image);
        var unit = new Vdu80CharacterGenerator(image);
        var groupEvents = new List<(long, bool?)>();
        var unitEvents = new List<(long, bool?)>();
        grouped.VideoChanged = level => groupEvents.Add((grouped.TimeNanoseconds, level));
        unit.VideoChanged = level => unitEvents.Add((unit.TimeNanoseconds, level));
        long groupedPeriods = 0;
        for (var window = 0; window < 512; window++)
        {
            foreach (var video in new[] { grouped, unit })
            {
                video.SetCharacterData((byte)window);
                video.SetRasterAddress(window >> 4);
                video.SetDisplayEnabled(window % 7 != 0);
                video.SetCursor(window % 31 == 2);
                video.SetInverted((window & 128) != 0);
                video.SetCharacterClock((window & 1) == 0);
            }
            var remaining = 3;
            while (remaining > 0)
            {
                var sample = grouped.VideoWhite;
                var accepted = grouped.AdvanceStableDotClocks(remaining);
                if (accepted == 0)
                {
                    Assert.Equal(unit.SetDotClock(true), grouped.SetDotClock(true));
                    unit.SetDotClock(false); grouped.SetDotClock(false);
                    remaining--;
                }
                else
                {
                    groupedPeriods += accepted;
                    for (var clock = 0; clock < accepted; clock++)
                    {
                        Assert.Equal(sample, unit.SetDotClock(true));
                        unit.SetDotClock(false);
                    }
                    remaining -= accepted;
                }
                Assert.Equal(unit.TimeNanoseconds, grouped.TimeNanoseconds);
                Assert.Equal(unit.DotClockCount, grouped.DotClockCount);
                Assert.Equal(unit.CharacterClockCount, grouped.CharacterClockCount);
                Assert.Equal(unit.VideoWhite, grouped.VideoWhite);
                Assert.Equal(unitEvents, groupEvents);
            }
        }
        Assert.True(groupedPeriods > 0); // The test must exercise the fast path.
    }

    [Fact]
    public void StableDotPeriodsRetainInitialUnknownAndRejectHighClockOrEventBoundaries()
    {
        var video = new Vdu80CharacterGenerator(new byte[2048]);
        Assert.Null(video.VideoWhite);
        Assert.Equal(0, video.AdvanceStableDotClocks(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => video.AdvanceStableDotClocks(-1));
        video.SetCharacterClock(true); // NL expires after 20 ns, before one dot period.
        Assert.Equal(0, video.AdvanceStableDotClocks(100));
        Assert.Equal(0, video.TimeNanoseconds);
        video.SetDotClock(true);
        var time = video.TimeNanoseconds;
        Assert.Equal(0, video.AdvanceStableDotClocks(100));
        Assert.Equal(time, video.TimeNanoseconds);
    }

    [Fact]
    public void CancelledDisplayTransitionDoesNotBecomeASyntheticPulse()
    {
        var video = new Vdu80CharacterGenerator(Enumerable.Repeat((byte)0xf8, 2048).ToArray());
        var source = new SystemClockGenerator();
        var samples = Connect(source, video);
        video.SetDisplayEnabled(true);
        source.AdvanceClocks(8);
        video.SetDisplayEnabled(false);
        source.AdvanceClocks(40);
        Assert.All(samples.Where(x => x.Clock >= 7), x => Assert.False(x.White));
    }

    [Fact]
    public void InversionActsOnEveryGlyphDotAndBothInversionsCancel()
    {
        var image = new byte[2048];
        image[16] = 0xa8;
        var video = new Vdu80CharacterGenerator(image);
        var source = new SystemClockGenerator();
        var samples = Connect(source, video);
        video.SetDisplayEnabled(true);
        video.SetCharacterData(1);
        video.SetCursor(true);
        source.AdvanceClocks(30);
        Assert.Equal(new bool?[] { true, false, true, false, true, false },
            samples.Where(x => x.Clock is >= 19 and <= 29).Select(x => x.White));
        video.SetInverted(true);
        source.AdvanceClocks(12);
        Assert.Equal(new bool?[] { false, true, false, true, false, true },
            samples.Where(x => x.Clock is >= 31 and <= 41).Select(x => x.White));
    }

    [Fact]
    public void LoadKeepsPreviousRomWordAndShiftsSpaceThenFiveGlyphDots()
    {
        var image = new byte[2048];
        image[16] = 0xa8;
        image[32] = 0x50;
        var video = new Vdu80CharacterGenerator(image);
        var source = new SystemClockGenerator();
        var samples = Connect(source, video);
        video.SetDisplayEnabled(true);
        video.SetCharacterData(1);
        source.AdvanceClocks(6); // First character captured.
        video.SetCharacterData(2); // RAM changes do not overwrite the latch.
        source.AdvanceClocks(24);
        Assert.Equal(new bool?[] { false, true, false, true, false, true },
            samples.Where(x => x.Clock is >= 19 and <= 29).Select(x => x.White));
        source.AdvanceClocks(12);
        Assert.Equal(new bool?[] { false, false, true, false, true, false },
            samples.Where(x => x.Clock is >= 31 and <= 41).Select(x => x.White));
    }

    [Fact]
    public void RasterAddressReachesRomWithoutRecapturingCharacter()
    {
        var image = new byte[2048];
        image[16] = 0xf8;
        image[17] = 0x08;
        var video = new Vdu80CharacterGenerator(image);
        var source = new SystemClockGenerator();
        var samples = Connect(source, video);
        video.SetDisplayEnabled(true);
        video.SetCharacterData(1);
        source.AdvanceClocks(18);
        video.SetRasterAddress(17); // Only RA0..3 are wired.
        source.AdvanceClocks(24);
        Assert.Equal(new bool?[] { false, false, false, false, false, true },
            samples.Where(x => x.Clock is >= 31 and <= 41).Select(x => x.White));
    }

    [Fact]
    public void DisplayEnableIsDelayedOneCharacterBeforeLatchCapture()
    {
        var image = Enumerable.Repeat((byte)0xf8, 2048).ToArray();
        var video = new Vdu80CharacterGenerator(image);
        var source = new SystemClockGenerator();
        var samples = Connect(source, video);
        video.SetDisplayEnabled(true);
        source.AdvanceClocks(30);
        Assert.All(samples.Where(x => x.Clock is >= 7 and <= 17), x => Assert.False(x.White));
        Assert.Contains(samples.Where(x => x.Clock is >= 19 and <= 29), x => x.White == true);
        video.SetDisplayEnabled(false);
        source.AdvanceClocks(24);
        Assert.All(samples.Where(x => x.Clock is >= 43 and <= 53), x => Assert.False(x.White));
    }

    [Fact]
    public void CursorAndInversionXorThePatternInsteadOfFillingIt()
    {
        var video = new Vdu80CharacterGenerator(new byte[2048]);
        var changes = new List<bool?>();
        video.VideoChanged = changes.Add;
        var source = new SystemClockGenerator();
        Connect(source, video);
        source.AdvanceClocks(12); // DE is known low after first latch.
        Assert.False(video.VideoWhite);
        changes.Clear();
        video.SetCursor(true);
        Assert.False(video.VideoWhite);
        source.AdvanceClocks(4); // 666 ns: before the calculated RC crossing at 667 ns.
        Assert.False(video.VideoWhite);
        source.AdvanceClocks(1);
        Assert.True(video.VideoWhite);
        video.SetInverted(true);
        Assert.False(video.VideoWhite);
        video.SetInverted(true);
        Assert.Equal(new bool?[] { true, false }, changes);
    }

    [Fact]
    public void RepeatedLevelsDoNotCreateClocksOrTimeAndFreeClocksStayExact()
    {
        var video = new Vdu80CharacterGenerator(new byte[2048]);
        Assert.Null(video.VideoWhite); // No reset/power-on pattern invented.
        var source = new SystemClockGenerator();
        Connect(source, video);
        source.AdvanceClocks(240);
        Assert.Equal(10_000, video.TimeNanoseconds);
        Assert.Equal(20, video.CharacterClockCount);
        Assert.Equal(120, video.DotClockCount);
        video.SetCharacterClock(source.Clock2MHz);
        video.SetDotClock(source.Clock12MHz);
        Assert.Equal(10_000, video.TimeNanoseconds);
        Assert.Equal(20, video.CharacterClockCount);
        Assert.Equal(120, video.DotClockCount);
    }

    private static List<(long Clock, bool? White)> Connect(SystemClockGenerator source,
        Vdu80CharacterGenerator video)
    {
        var samples = new List<(long, bool?)>();
        source.Clock12MHzChanged = high =>
        {
            var sample = video.SetDotClock(high);
            if (high) samples.Add((source.InputClockCount, sample));
        };
        source.Clock2MHzChanged = video.SetCharacterClock;
        return samples;
    }
}
