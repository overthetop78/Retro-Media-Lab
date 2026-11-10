using System.Reflection;
using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS374.Services;
using RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class Vdu80CharacterPeriodTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WholeCharactersPreserveSamplesPackagesAndOrdinaryResumption(bool phase)
    {
        var rom = Enumerable.Range(0, 2048).Select(x => (byte)(x * 73 ^ (x >> 3))).ToArray();
        var actual = new Vdu80CharacterGenerator(rom); var reference = new Vdu80CharacterGenerator(rom);
        if (phase) { actual.SetDotClock(true); reference.SetDotClock(true); }
        var accepted = 0;
        for (var character = 0; character < 400; character++)
        {
            foreach (var video in new[] { actual, reference })
            {
                video.SetCharacterData((byte)(character % 128));
                video.SetRasterAddress(character % 16);
                video.SetDisplayEnabled((character & 7) != 0);
                video.SetInverted((character & 16) != 0);
                if ((character & 31) == 0) video.SetCursor((character & 32) != 0);
            }
            var samples = Character(reference);
            if (actual.TryAdvanceCharacterPeriod(out var values, out var known))
            {
                accepted++;
                for (var n = 0; n < 6; n++)
                {
                    Assert.Equal(samples[n].HasValue, (known & (128 >> n)) != 0);
                    if (samples[n].HasValue) Assert.Equal(samples[n]!.Value, (values & (128 >> n)) != 0);
                }
            }
            else Assert.Equal(samples, Character(actual));
            Compare();
        }
        Assert.True(accepted > 300);
        var eventsA = new List<(long, bool?)>(); var eventsB = new List<(long, bool?)>();
        actual.VideoChanged = value => eventsA.Add((actual.TimeNanoseconds, value));
        reference.VideoChanged = value => eventsB.Add((reference.TimeNanoseconds, value));
        Assert.False(actual.TryAdvanceCharacterPeriod(out _, out _));
        Assert.Equal(Character(reference), Character(actual));
        Compare(); Assert.Equal(eventsB, eventsA);

        bool?[] Character(Vdu80CharacterGenerator video)
        {
            var result = new bool?[6];
            for (var n = 0; n < 6; n++)
            {
                if (n == 3) video.SetCharacterClock(true);
                if (phase) { video.SetDotClock(false); result[n] = video.SetDotClock(true); }
                else { result[n] = video.SetDotClock(true); video.SetDotClock(false); }
            }
            video.SetCharacterClock(false); return result;
        }
        void Compare()
        {
            Assert.Equal(reference.TimeNanoseconds, actual.TimeNanoseconds);
            Assert.Equal(reference.VideoWhite, actual.VideoWhite);
            Assert.Equal(reference.StableClockCandidate, actual.StableClockCandidate);
            Assert.Equal(reference.CharacterClockCount, actual.CharacterClockCount);
            Assert.Equal(reference.DotClockCount, actual.DotClockCount);
            Assert.Equal(Field<long>(reference, "_fraction"), Field<long>(actual, "_fraction"));
            var x = Field<Sn74Ls165>(actual, "_serializer"); var y = Field<Sn74Ls165>(reference, "_serializer");
            Assert.Equal(y.StoredData, x.StoredData); Assert.Equal(y.KnownBits, x.KnownBits);
            Assert.Equal(y.ShiftClockCount, x.ShiftClockCount); Assert.Equal(y.ClockHigh, x.ClockHigh);
            Assert.Equal(y.GatedClockHigh, x.GatedClockHigh); Assert.Equal(y.ParallelData, x.ParallelData);
            var a = Field<Sn74Ls374>(actual, "_latch"); var b = Field<Sn74Ls374>(reference, "_latch");
            Assert.Equal(b.StoredData, a.StoredData); Assert.Equal(b.DataInput, a.DataInput);
            var c = Field<Mm52116>(actual, "_rom"); var d = Field<Mm52116>(reference, "_rom");
            Assert.Equal(d.Address, c.Address); Assert.Equal(d.OutputData, c.OutputData);
            Assert.Equal(d.NextEventNanoseconds, c.NextEventNanoseconds);
            Assert.Equal(d.TimeNanoseconds, c.TimeNanoseconds);
        }
    }
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
