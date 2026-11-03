using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class SequenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadSectorUsesRecordedIdMarkDataAndCrc(bool mfm)
    {
        var rig = new PinFixture(mfm);
        var data = Enumerable.Range(0, 128).Select(value => (byte)value).ToArray();
        rig.Command(0x88);
        rig.FeedId(number: 2);
        rig.FeedField(0xfb, new byte[128]);
        Assert.Empty(rig.Read);
        rig.FeedId();
        rig.FeedBytes(Enumerable.Repeat((byte)(mfm ? 0x4e : 0xff), mfm ? 22 : 11));
        rig.FeedField(0xfb, data);
        rig.Advance(1024);
        Assert.False(rig.Chip.Active);
        Assert.True(rig.Chip.Irq);
        Assert.Equal(data, rig.Read);
        Assert.Equal(0, rig.Chip.Read(0) & 0x7c);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadAddressReturnsTheRecordedSixBytesIncludingBadCrc(bool mfm)
    {
        var rig = new PinFixture(mfm);
        rig.Command(0xc8);
        rig.FeedId(7, 19, 3, badCrc: true, head: 4);
        rig.Advance(1024);
        Assert.Equal(6, rig.Read.Count);
        Assert.Equal(new byte[] { 7, 4, 19, 3 }, rig.Read.Take(4));
        Assert.Equal(7, rig.Chip.Read(2));
        Assert.Equal(8, rig.Chip.Read(0) & 8);
        Assert.False(rig.Chip.Active);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingFluxAndIndexCannotInventAReadySignalOrATimeout(bool mfm)
    {
        var rig = new PinFixture(mfm);
        rig.Command(0x88);
        rig.Advance(80_000);
        Assert.True(rig.Chip.Active);
        Assert.False(rig.Chip.Irq);
        for (var index = 0; index < 4; index++) rig.Index();
        Assert.True(rig.Chip.Active);
        rig.Index();
        Assert.False(rig.Chip.Active);
        Assert.Equal(16, rig.Chip.Read(0) & 16);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void SpinUpUsesSixExternalIndexesAndMotorStopsAfterNineIdleIndexes(Wd1770Model model)
    {
        var rig = new PinFixture(true, model);
        rig.Command(0x80);
        for (var index = 0; index < 5; index++) rig.Index();
        rig.FeedId();
        Assert.Empty(rig.Read);
        rig.Index();
        rig.FeedId();
        rig.FeedBytes(Enumerable.Repeat((byte)0x4e, 22));
        rig.FeedField(0xfb, new byte[128]);
        rig.Advance(1024);
        Assert.False(rig.Chip.Active);
        for (var index = 0; index < 8; index++) rig.Index();
        Assert.True(rig.Chip.Motor);
        rig.Index();
        Assert.False(rig.Chip.Motor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeekAndRestoreUseStepDirectionAndTrackZeroPins(bool mfm)
    {
        var rig = new PinFixture(mfm);
        rig.Chip.Write(3, 3);
        rig.Command(0x18);
        rig.Advance(160_000);
        Assert.False(rig.Chip.Active);
        Assert.Equal(3, rig.Track);
        Assert.Equal(3, rig.Chip.Read(1));
        Assert.Equal(3, rig.Chip.StepPulses);
        rig.Command(0x08);
        rig.Advance(160_000);
        Assert.False(rig.Chip.Active);
        Assert.Equal(0, rig.Track);
        Assert.Equal(0, rig.Chip.Read(1));
        Assert.Equal(6, rig.Chip.StepPulses);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriteSectorProducesTheActualDataMarkPayloadAndCrc(bool mfm)
    {
        var rig = new PinFixture(mfm);
        var data = Enumerable.Range(0, 128).Select(value => (byte)(255 - value)).ToArray();
        foreach (var value in data) rig.Write.Enqueue(value);
        rig.Command(0xa9);
        rig.FeedId();
        rig.Advance(100_000);
        Assert.False(rig.Chip.Active);
        Assert.False(rig.Chip.WriteGate);
        Assert.Equal(0, rig.Chip.Read(0) & 0x5c);
        var mark = rig.Written.FindIndex(item => item.Kind == Wd1770ReadWordKind.AddressMark && item.Data == 0xf8);
        Assert.True(mark >= 0);
        Assert.Equal(data, rig.Written.Skip(mark + 1).Take(128).Select(item => item.Data));
        ushort crc = mfm ? (ushort)0xcdb4 : (ushort)0xffff;
        foreach (var item in rig.Written.Skip(mark).Take(131)) crc = Wd1770Crc.Add(crc, item.Data);
        Assert.Equal(0, crc);
        Assert.Empty(rig.Write);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadTrackTransfersGapsFromIndexAndStopsAtTheNextIndex(bool mfm)
    {
        var rig = new PinFixture(mfm);
        rig.Command(0xe8);
        rig.FeedBytes(Enumerable.Repeat((byte)0x4e, 8));
        Assert.Empty(rig.Read);
        rig.Index();
        rig.FeedBytes(Enumerable.Repeat((byte)0x4e, 32));
        Assert.Equal(Enumerable.Repeat((byte)0x4e, 32), rig.Read);
        rig.Index();
        Assert.False(rig.Chip.Active);
        Assert.True(rig.Chip.Irq);
        Assert.Equal(0, rig.Chip.Read(0) & 0x7c);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriteTrackWaitsForIndexAndClosesTheGateOnTheNextIndex(bool mfm)
    {
        var rig = new PinFixture(mfm);
        for (var index = 0; index < 128; index++) rig.Write.Enqueue(0x4e);
        rig.Command(0xf8);
        rig.Advance(2048);
        Assert.False(rig.Chip.WriteGate);
        Assert.True(rig.Chip.Active);
        rig.Index();
        Assert.True(rig.Chip.WriteGate);
        rig.Advance(8192);
        rig.Index();
        Assert.False(rig.Chip.WriteGate);
        Assert.False(rig.Chip.Active);
        Assert.Equal(0, rig.Chip.Read(0) & 0x7c);
    }
}
