using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class PrecompensationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AllBytesAndCrossBytePatternsPreserveDataCrcAndWordRequestBoundaries(bool mfm)
    {
        foreach (byte following in new byte[] { 0, 1, 0x55, 0x80, 0xff })
        for (var value = 0; value < 256; value++)
            CheckStream([(0x55, Upd765WriteWordKind.Data), ((byte)value, Upd765WriteWordKind.Data),
                (following, Upd765WriteWordKind.Data), (0xaa, Upd765WriteWordKind.Data)], mfm);
    }

    [Fact]
    public void MissingClockMarksParticipateInTheContinuousPhysicalPulseHistory()
    {
        CheckStream([(0, Upd765WriteWordKind.Data), (0xa1, Upd765WriteWordKind.SyncA1),
            (0xa1, Upd765WriteWordKind.SyncA1), (0xa1, Upd765WriteWordKind.SyncA1),
            (0xfb, Upd765WriteWordKind.Data), (0x11, Upd765WriteWordKind.Data),
            (0xc2, Upd765WriteWordKind.SyncC2), (0xfc, Upd765WriteWordKind.Data)], true);
    }

    [Fact]
    public void CrcPredictionNeverChangesTheLiveRegisterOrAdvancesAWord()
    {
        var pins = new Upd765WriteChannel { DoubleDensity = true };
        pins.ResetCrc(0x1234); pins.TryQueue(0x97); pins.SetGate(true);
        var expected = PinFixture.Crc(0x1234, 0x97);
        for (var cell = 0; cell < 16; cell++)
        {
            Clock(pins);
            var current = pins.Crc;
            Assert.Equal(expected, pins.CrcAtWordEnd);
            Assert.Equal(expected, pins.CrcAtWordEnd);
            Assert.Equal(current, pins.Crc);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ClosingOrResettingTheGateRestoresNormalAndCancelsFutureChanges(bool reset)
    {
        var pins = new Upd765WriteChannel { DoubleDensity = true };
        pins.WordRequested += () => pins.TryQueue(0x11);
        pins.PreviewNextWord = () => (0x11, Upd765WriteWordKind.Data);
        pins.SetGate(true);
        for (var cell = 0; pins.Precompensation == Upd765Precompensation.Normal && cell < 32; cell++) Clock(pins);
        Assert.NotEqual(Upd765Precompensation.Normal, pins.Precompensation);
        if (reset) pins.Reset(); else pins.SetGate(false);
        Assert.Equal(Upd765Precompensation.Normal, pins.Precompensation);
        var changes = 0;
        pins.PrecompensationChanged += (_, _) => changes++;
        pins.Advance(1000);
        Assert.Equal(0, changes); Assert.False(pins.WriteData); Assert.False(pins.WriteEnable);
    }

    [Fact]
    public void ResetFromPreshiftNotificationCannotPublishTheCancelledPulse()
    {
        var pins = new Upd765WriteChannel { DoubleDensity = true };
        pins.WordRequested += () => pins.TryQueue(0x11);
        pins.PreviewNextWord = () => (0x11, Upd765WriteWordKind.Data);
        var reset = false;
        pins.PrecompensationChanged += (_, value) =>
        {
            if (value == Upd765Precompensation.Normal) return;
            reset = true; pins.Reset();
        };
        pins.SetGate(true);
        for (var cell = 0; !reset && cell < 32; cell++) Clock(pins);
        Assert.True(reset); Assert.False(pins.Gate);
        Assert.False(pins.WriteData); Assert.False(pins.WriteEnable);
        Assert.Equal(Upd765Precompensation.Normal, pins.Precompensation);
    }

    private static void CheckStream((byte Value, Upd765WriteWordKind Kind)[] source, bool mfm)
    {
        var bits = Encode(source, mfm);
        var pins = new Upd765WriteChannel { DoubleDensity = mfm };
        var next = 0; var cell = 0; var completed = 0;
        pins.WordRequested += () =>
        {
            Assert.Equal(next * 16, cell);
            if (next < source.Length) { var word = source[next++]; Assert.True(pins.TryQueue(word.Value, word.Kind)); }
        };
        pins.PreviewNextWord = () => next < source.Length ? source[next] : null;
        pins.WordCompleted += () => completed++;
        pins.SetGate(true);
        var previousPulse = -1;
        ushort crc = 0xffff;
        for (; cell < bits.Length; cell++)
        {
            pins.SetClock(true);
            var expected = Upd765Precompensation.Normal;
            if (mfm && bits[cell] && previousPulse >= 0)
            {
                var following = 0;
                for (var distance = 1; distance <= 4 && cell + distance < bits.Length; distance++)
                    if (bits[cell + distance]) { following = distance; break; }
                if (following != 0)
                {
                    if (cell - previousPulse == 2 && following > 2) expected = Upd765Precompensation.Early;
                    if (cell - previousPulse > 2 && following == 2) expected = Upd765Precompensation.Late;
                }
            }
            Assert.Equal(expected, pins.Precompensation);
            if (bits[cell]) previousPulse = cell;
            pins.AdvanceHalfClocks(1); Assert.Equal(bits[cell], pins.WriteData);
            pins.AdvanceHalfClocks(3); pins.SetClock(false);
            pins.AdvanceHalfClocks(1); Assert.False(pins.WriteData);
            pins.AdvanceHalfClocks(11);
            if ((cell & 15) == 15)
            {
                crc = PinFixture.Crc(crc, source[cell / 16].Value);
                Assert.Equal(crc, pins.Crc);
            }
        }
        Assert.Equal(source.Length, completed);
    }

    private static bool[] Encode((byte Value, Upd765WriteWordKind Kind)[] source, bool mfm)
    {
        var bits = new List<bool>(); var previous = false;
        foreach (var (value, kind) in source)
        {
            if (mfm && kind is Upd765WriteWordKind.SyncA1 or Upd765WriteWordKind.SyncC2)
            {
                var word = kind == Upd765WriteWordKind.SyncA1 ? 0x4489 : 0x5224;
                for (var bit = 15; bit >= 0; bit--) bits.Add((word & (1 << bit)) != 0);
                previous = (word & 1) != 0;
                continue;
            }
            for (var bit = 7; bit >= 0; bit--)
            {
                var data = (value & (1 << bit)) != 0;
                bits.Add(!mfm || !previous && !data); bits.Add(data); previous = data;
            }
        }
        return bits.ToArray();
    }

    private static void Clock(Upd765WriteChannel pins)
    {
        pins.SetClock(true); pins.AdvanceHalfClocks(4);
        pins.SetClock(false); pins.AdvanceHalfClocks(12);
    }
}
