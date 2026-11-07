using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed partial class ReadChannelTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StableReadLevelsRetainEverySampleAndDecodedWord(bool mfm, bool readTrack)
    {
        var unit = new Wd1770ReadChannel { DoubleDensity = mfm };
        var grouped = new Wd1770ReadChannel { DoubleDensity = mfm };
        unit.Reset(readTrack); grouped.Reset(readTrack);
        var unitClock = 0;
        var groupedClock = 0;
        var unitWords = new List<(int Clock, byte Data, Wd1770ReadWordKind Kind, bool Valid)>();
        var groupedWords = new List<(int Clock, byte Data, Wd1770ReadWordKind Kind, bool Valid)>();
        unit.ByteReceived += (data, kind, valid) => unitWords.Add((unitClock, data, kind, valid));
        grouped.ByteReceived += (data, kind, valid) => groupedWords.Add((groupedClock, data, kind, valid));
        void Level(bool high, int clocks)
        {
            for (var clock = 0; clock < clocks; clock++) { unitClock++; unit.Tick(high); }
            var remaining = clocks;
            while (remaining > 0)
            {
                var quiet = Math.Min(remaining, grouped.ClocksUntilWork(high) - 1);
                if (quiet > 0) { grouped.AdvanceQuiet(high, quiet); groupedClock += quiet; remaining -= quiet; }
                else { groupedClock++; grouped.Tick(high); remaining--; }
            }
            Assert.Equal(unit.CaptureState(), grouped.CaptureState());
        }
        Level(false, 103);
        var words = Prefix(mfm);
        var previous = true;
        for (var data = 0; data < 256; data++) words.Add(Encode((byte)data, mfm, ref previous));
        var cell = mfm ? 16 : 32;
        var width = mfm ? 2 : 4;
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--)
            {
                if ((word >> bit & 1) != 0) { Level(true, width); Level(false, cell - width); }
                else Level(false, cell);
            }
        Level(false, 101);
        Assert.NotEmpty(unitWords);
        Assert.Equal(unitWords, groupedWords);
    }
}
