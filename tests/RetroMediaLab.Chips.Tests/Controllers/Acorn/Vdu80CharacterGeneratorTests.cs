using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class Vdu80CharacterGeneratorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SettledCharactersPreserveLoadPulseShiftsAndTheNextOrdinaryCharacter(bool phase)
    {
        foreach (var length in new[] { 1, 3, 17, 101 })
        {
            var actual = new Vdu80CharacterGenerator(new byte[2048]); var reference = new Vdu80CharacterGenerator(new byte[2048]);
            foreach (var video in new[] { actual, reference })
            {
                video.SetInverted((length & 2) != 0);
                video.SetDisplayEnabled(true); video.SetDotClock(phase);
                for (var character = 0; character < 8; character++) Character(video);
            }
            var events = new List<bool?>(); actual.VideoChanged = events.Add;
            Assert.True(actual.TryAdvanceSettledCharacterPeriods(length, out var white));
            Assert.Equal(reference.VideoWhite, white);
            for (var character = 0; character < length; character++) Character(reference);
            Assert.Empty(events);
            Compare();
            actual.SetCharacterData(1); reference.SetCharacterData(1);
            Assert.False(actual.TryAdvanceSettledCharacterPeriods(1, out _));
            Character(actual); Character(reference); Compare();
            actual.SetCursor(true); reference.SetCursor(true);
            Assert.False(actual.TryAdvanceSettledCharacterPeriods(1, out _));
            Character(actual); Character(reference); Compare();
            actual.SetDisplayEnabled(false); reference.SetDisplayEnabled(false);
            Assert.False(actual.TryAdvanceSettledCharacterPeriods(1, out _));
            Character(actual); Character(reference); Compare();
            actual.SetCharacterClock(true); reference.SetCharacterClock(true);
            Assert.False(actual.TryAdvanceSettledCharacterPeriods(1, out _));
            actual.SetCharacterClock(false); reference.SetCharacterClock(false);
            Assert.False(actual.TryAdvanceSettledCharacterPeriods(1, out _));
            Character(actual); Character(reference); Compare();
            void Compare()
            {
                Assert.Equal(reference.TimeNanoseconds, actual.TimeNanoseconds);
                Assert.Equal(reference.DotClockCount, actual.DotClockCount);
                Assert.Equal(reference.CharacterClockCount, actual.CharacterClockCount);
                Assert.Equal(reference.VideoWhite, actual.VideoWhite);
                Assert.Equal(Serializer(reference).ShiftClockCount, Serializer(actual).ShiftClockCount);
                Assert.Equal(Serializer(reference).GatedClockHigh, Serializer(actual).GatedClockHigh);
            }
        }
        void Character(Vdu80CharacterGenerator video)
        {
            for (var i = 0; i < 3; i++) { video.SetDotClock(!phase); video.SetDotClock(phase); }
            video.SetCharacterClock(true);
            for (var i = 0; i < 3; i++) { video.SetDotClock(!phase); video.SetDotClock(phase); }
            video.SetCharacterClock(false);
        }
        static RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services.Sn74Ls165 Serializer(Vdu80CharacterGenerator video)
            => (RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services.Sn74Ls165)typeof(Vdu80CharacterGenerator)
                .GetField("_serializer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(video)!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SettledZeroPeriodsHandleLoadInternallyButStopBeforeRomAndCursor(bool cursor)
    {
        var image = new byte[2048];
        image[16] = 0xa8;
        var grouped = new Vdu80CharacterGenerator(image);
        var unit = new Vdu80CharacterGenerator(image);
        var groupEvents = new List<(long, bool?)>();
        var unitEvents = new List<(long, bool?)>();
        var groupQualification = new List<(long, bool)>();
        var unitQualification = new List<(long, bool)>();
        grouped.VideoChanged = level => groupEvents.Add((grouped.TimeNanoseconds, level));
        unit.VideoChanged = level => unitEvents.Add((unit.TimeNanoseconds, level));
        grouped.StableClockCandidateChanged = level => groupQualification.Add((grouped.TimeNanoseconds, level));
        unit.StableClockCandidateChanged = level => unitQualification.Add((unit.TimeNanoseconds, level));
        for (var window = 0; window < 24; window++)
        {
            foreach (var video in new[] { unit, grouped })
            {
                video.SetDisplayEnabled(true);
                video.SetCharacterClock((window & 1) == 0);
                for (var clock = 0; clock < 3; clock++)
                {
                    video.SetDotClock(true); video.SetDotClock(false);
                }
            }
        }
        Assert.True(grouped.StableClockCandidate);
        foreach (var video in new[] { unit, grouped })
        {
            video.SetCharacterData(1);
            video.SetCharacterClock(true); // NL+20 ns; changed ROM word at +450 ns.
            if (cursor) video.SetCursor(true); // RC crossing before the ROM event.
        }
        var accepted = grouped.AdvanceStableDotClocks(3);
        Assert.Equal(cursor ? 2 : 3, accepted);
        AdvanceUnit(accepted);
        AssertEquivalent();
        if (!cursor)
        {
            grouped.SetCharacterClock(false); unit.SetCharacterClock(false);
            accepted = grouped.AdvanceStableDotClocks(3);
            Assert.Equal(2, accepted); // Complete periods strictly before +450 ns.
            AdvanceUnit(accepted);
            AssertEquivalent();
        }
        var time = grouped.TimeNanoseconds;
        Assert.Equal(0, grouped.AdvanceStableDotClocks(1));
        Assert.Equal(time, grouped.TimeNanoseconds);
        // The real boundary still goes through explicit edges and notifications.
        grouped.SetDotClock(true); grouped.SetDotClock(false);
        AdvanceUnit(1);
        AssertEquivalent();

        void AdvanceUnit(int count)
        {
            for (var clock = 0; clock < count; clock++)
            {
                unit.SetDotClock(true); unit.SetDotClock(false);
            }
        }
        void AssertEquivalent()
        {
            Assert.Equal(unit.TimeNanoseconds, grouped.TimeNanoseconds);
            Assert.Equal(unit.DotClockCount, grouped.DotClockCount);
            Assert.Equal(unit.CharacterClockCount, grouped.CharacterClockCount);
            Assert.Equal(unit.VideoWhite, grouped.VideoWhite);
            Assert.Equal(unit.StableClockCandidate, grouped.StableClockCandidate);
            Assert.Equal(unitEvents, groupEvents);
            Assert.Equal(unitQualification, groupQualification);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StableDotPeriodsMatchExplicitEdgesAcrossChangingInputsAndAllLocalDeadlines(bool startHigh)
    {
        var image = Enumerable.Range(0, 2048).Select(i => (byte)(i * 73 + 11)).ToArray();
        var grouped = new Vdu80CharacterGenerator(image);
        var unit = new Vdu80CharacterGenerator(image);
        var groupEvents = new List<(long, bool?)>();
        var unitEvents = new List<(long, bool?)>();
        grouped.VideoChanged = level => groupEvents.Add((grouped.TimeNanoseconds, level));
        unit.VideoChanged = level => unitEvents.Add((unit.TimeNanoseconds, level));
        grouped.SetDotClock(startHigh); unit.SetDotClock(startHigh);
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
                var accepted = startHigh ? grouped.AdvanceStableDotClockPeriods(remaining)
                    : grouped.AdvanceStableDotClocks(remaining);
                if (accepted == 0)
                {
                    Assert.Equal(unit.SetDotClock(!startHigh), grouped.SetDotClock(!startHigh));
                    Assert.Equal(unit.SetDotClock(startHigh), grouped.SetDotClock(startHigh));
                    remaining--;
                }
                else
                {
                    groupedPeriods += accepted;
                    for (var clock = 0; clock < accepted; clock++)
                    {
                        Assert.Equal(sample, unit.SetDotClock(!startHigh));
                        Assert.Equal(sample, unit.SetDotClock(startHigh));
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
