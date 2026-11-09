using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class Vdu80CharacterGeneratorTests
{
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
