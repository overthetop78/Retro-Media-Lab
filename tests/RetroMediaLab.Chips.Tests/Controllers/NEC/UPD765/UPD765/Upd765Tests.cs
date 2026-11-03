using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class Upd765Tests
{
    private static byte[] Payload(int length = 128) => Enumerable.Range(0, length).Select(value => (byte)value).ToArray();

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ReadUsesExternalFmOrMfmAndDmaOrPio(bool mfm, bool pio)
    {
        var rig = new PinFixture(mfm, pio) { TerminalAfter = 128 };
        var data = Payload();
        rig.Sector(1, data);
        rig.Command(6);
        rig.FeedTrack();
        Assert.Equal(data, rig.Received);
        Assert.Equal(new byte[] { 0, 0, 0, 1, 0, 1, 0 }, rig.Result());
        Assert.Equal(128, rig.Chip.ReadBytes(0));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EotWithoutTcReportsEndAndNextCylinder(bool mfm)
    {
        var rig = new PinFixture(mfm);
        rig.Sector(1, Payload()); rig.Command(6); rig.FeedTrack();
        Assert.Equal(0x80, rig.Result()[1]);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReadIdReportsTheRecordedHeaderWithoutHostData(bool badCrc)
    {
        var rig = new PinFixture();
        rig.Sector(7, Payload(), cylinder: 9, head: 1, size: 3, badId: badCrc);
        Send(rig.Chip, 0x4a, 0); rig.FeedTrack();
        Assert.Equal(new byte[] { badCrc ? (byte)0x40 : (byte)0, badCrc ? (byte)0x20 : (byte)0, 0, 9, 1, 7, 3 }, rig.Result());
        Assert.Empty(rig.Received);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CrcErrorsComeFromTheActualIdOrDataBits(bool id)
    {
        var rig = new PinFixture { TerminalAfter = 128 };
        rig.Sector(1, Payload(), badId: id, badData: !id); rig.Command(6); rig.FeedTrack();
        var result = rig.Result();
        Assert.Equal(0x20, result[1]); Assert.Equal(id ? 0 : 0x20, result[2]);
        Assert.Equal(id ? 0 : 128, rig.Received.Count);
    }

    [Theory]
    [InlineData(6, false, 0)] [InlineData(6, true, 0x40)]
    [InlineData(12, true, 0)] [InlineData(12, false, 0x40)]
    public void DeletedMarksAreComparedAfterDeliveringTheData(byte command, bool deleted, byte detail)
    {
        var rig = new PinFixture { TerminalAfter = 128 };
        rig.Sector(1, Payload(), deleted: deleted); rig.Command(command); rig.FeedTrack();
        Assert.Equal(Payload(), rig.Received); Assert.Equal(detail, rig.Result()[2]);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SkipConsumesTheWrongMarkWithoutDmaAndFindsTheNextSector(bool mfm)
    {
        var rig = new PinFixture(mfm) { TerminalAfter = 128 };
        rig.Sector(1, Enumerable.Repeat((byte)0xee, 128).ToArray(), deleted: true, badData: true);
        rig.Sector(2, Payload()); rig.Command(0x26, end: 2); rig.FeedTrack();
        Assert.Equal(Payload(), rig.Received); Assert.Equal(0, rig.Result()[1]);
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(255, 0x12)] [InlineData(2, 0x10)]
    public void MissingRequestedIdUsesTwoRealIndexesAndRemembersWrongCylinder(byte cylinder, byte detail)
    {
        var rig = new PinFixture();
        rig.Sector(cylinder == 0 ? (byte)2 : (byte)1, Payload(), cylinder: cylinder);
        rig.Command(6); rig.FeedTrack();
        Assert.True(rig.Chip.Active); rig.Index(); Assert.True(rig.Chip.Active); rig.Index();
        var result = rig.Result(); Assert.Equal(4, result[1]); Assert.Equal(detail, result[2]);
    }

    [Fact]
    public void NoIndexCannotInventARotationalTimeout()
    {
        var rig = new PinFixture(); rig.Command(6); rig.ReadyForTrack();
        rig.Chip.Advance(8_000_000); Assert.True(rig.Chip.Active);
        rig.Index(); rig.Index(); Assert.Equal(5, rig.Result()[1]);
    }

    [Fact]
    public void MissingDataMarkIsDifferentFromMissingId()
    {
        var rig = new PinFixture(); rig.Sector(1, [], includeData: false);
        rig.Command(6); rig.FeedTrack(); rig.Index(); rig.Index();
        var result = rig.Result(); Assert.Equal(1, result[1]); Assert.Equal(1, result[2]);
    }

    [Fact]
    public void DtlLimitsTheHostButTheFullFieldCrcStillRuns()
    {
        var rig = new PinFixture { TerminalAfter = 11 };
        rig.Sector(1, Payload(), badData: true); rig.Command(6, dtl: 11); rig.FeedTrack();
        Assert.Equal(Payload().Take(11), rig.Received); Assert.Equal(0x20, rig.Result()[1]);
    }

    [Theory]
    [InlineData(Upd765Model.Upd765A)] [InlineData(Upd765Model.Upd765B)] [InlineData(Upd765Model.Upd72065B)]
    public void MissingHostAcknowledgmentReportsOverrun(Upd765Model model)
    {
        var rig = new PinFixture(model: model) { HostEnabled = false };
        rig.Sector(1, Payload()); rig.Command(6); rig.FeedTrack();
        Assert.Equal(0x10, rig.Result()[1]);
        Assert.Equal(model == Upd765Model.Upd765A, rig.Chip.Drq);
        rig.Chip.ReadDma(); Assert.False(rig.Chip.Drq);
    }

    [Theory]
    [InlineData(0x11, 3, 3, 8)] [InlineData(0x11, 255, 3, 8)]
    [InlineData(0x11, 3, 255, 8)] [InlineData(0x19, 2, 3, 0)]
    [InlineData(0x1d, 4, 3, 0)] [InlineData(0x11, 2, 3, 4)]
    public void ScanComparesEveryByteAndMasksEitherFf(byte command, byte disk, byte host, byte detail)
    {
        var rig = new PinFixture { TerminalAfter = 128 };
        foreach (var value in Enumerable.Repeat(host, 128)) rig.HostInput.Enqueue(value);
        rig.Sector(1, Enumerable.Repeat(disk, 128).ToArray()); rig.Command(command, dtl: 1); rig.FeedTrack();
        Assert.Equal(detail, rig.Result()[2]); Assert.Equal(128, rig.Chip.ReadBytes(0));
    }

    [Fact]
    public void ScanStepTwoSkipsTheInterveningIdAndStartsANewComparison()
    {
        var rig = new PinFixture();
        foreach (var value in Enumerable.Repeat((byte)3, 256)) rig.HostInput.Enqueue(value);
        rig.Sector(1, Enumerable.Repeat((byte)2, 128).ToArray());
        rig.Sector(2, Enumerable.Repeat((byte)9, 128).ToArray());
        rig.Sector(3, Enumerable.Repeat((byte)3, 128).ToArray());
        rig.Command(0x11, end: 3, dtl: 2); rig.FeedTrack();
        Assert.Equal(8, rig.Result()[2]); Assert.Equal(256, rig.HostTransfers);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void WriteSerializesTheHostPayloadAndRealCrc(bool mfm, bool pio)
    {
        var rig = new PinFixture(mfm, pio) { TerminalAfter = 128 };
        foreach (var value in Payload()) rig.HostInput.Enqueue(value);
        rig.Sector(1, new byte[128]); rig.Command(5); rig.FeedTrack(); rig.UntilResult();
        Assert.Equal(0, rig.Result()[1]);
        var bytes = rig.Output.Select(PinFixture.Decode).ToArray();
        var mark = Array.IndexOf(bytes, (byte)0xfb);
        Assert.True(mark >= 0); Assert.Equal(Payload(), bytes.Skip(mark + 1).Take(128));
        var crc = rig.CrcPrefix(0xfb);
        foreach (var value in bytes.Skip(mark + 1).Take(130)) crc = PinFixture.Crc(crc, value);
        Assert.Equal(0, crc); Assert.Equal(128, rig.Chip.WrittenBytes(0));
    }

    [Fact]
    public void WriteProtectAndReadyLossAbortFromPhysicalPins()
    {
        var rig = new PinFixture(); rig.Status[0] |= 0x40; rig.Command(5);
        Assert.Equal(2, rig.Result()[1]); Assert.Empty(rig.Output);
        rig.Status[0] = 0x38; rig.Command(6); rig.ReadyForTrack(); rig.Status[0] = 0x10;
        rig.Chip.Advance(1); Assert.Equal(0x48, rig.Result()[0]);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReadTrackWaitsForIndexAndCountsFieldsInRecordedOrder(bool pio)
    {
        var rig = new PinFixture(pio: pio);
        rig.Sector(4, Payload()); rig.Sector(2, Payload());
        rig.Command(2, record: 4, end: 2); rig.FeedTrack(); Assert.Empty(rig.Received);
        rig.Index(); rig.FeedTrack();
        Assert.Equal(Payload().Concat(Payload()), rig.Received);
        Assert.Equal(0x84, rig.Result()[1]); Assert.Equal(0, rig.Chip.Head);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FormatWritesCpuIdsAndParameterSizedFillUntilTheSecondIndex(bool mfm)
    {
        var rig = new PinFixture(mfm);
        foreach (var value in new byte[] { 5, 1, 7, 3 }) rig.HostInput.Enqueue(value);
        Send(rig.Chip, mfm ? (byte)0x4d : (byte)0x0d, 0, 0, 1, 0x1b, 0xe5);
        rig.ReadyForTrack(); rig.Clocks(100); Assert.Empty(rig.Output);
        rig.Index(); rig.Clocks(7000); Assert.True(rig.Chip.Active); rig.Index();
        var result = rig.Result(); Assert.Equal(new byte[] { 0, 0, 0, 5, 1, 8, 3 }, result);
        var bytes = rig.Output.Select(PinFixture.Decode).ToArray();
        var id = Array.IndexOf(bytes, (byte)0xfe); var data = Array.IndexOf(bytes, (byte)0xfb, id + 7);
        Assert.Equal(new byte[] { 5, 1, 7, 3 }, bytes.Skip(id + 1).Take(4));
        Assert.Equal(Enumerable.Repeat((byte)0xe5, 128), bytes.Skip(data + 1).Take(128));
        Assert.Equal(128, rig.Chip.WrittenBytes(0));
    }

    [Theory]
    [InlineData(Upd765Model.Upd765A, 0x80)]
    [InlineData(Upd765Model.Upd765B, 0x10)]
    [InlineData(Upd765Model.Upd72065B, 0x10)]
    public void LastByteOverrunDistinguishesAFromB(Upd765Model model, byte error)
    {
        var rig = new PinFixture(model: model) { HostLimit = 127 };
        rig.Sector(1, Payload()); rig.Command(6); rig.FeedTrack();
        Assert.Equal(error, rig.Result()[1]); Assert.Equal(127, rig.Received.Count);
        Assert.Equal(model == Upd765Model.Upd765A, rig.Chip.Drq);
        rig.Chip.ReadDma(); Assert.False(rig.Chip.Drq);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WriteDeletedAndEarlyTcEmitTheAcceptedBytesThenZeroFill(bool mfm)
    {
        var rig = new PinFixture(mfm) { TerminalAfter = 11 };
        foreach (var value in Payload().Take(11)) rig.HostInput.Enqueue(value);
        rig.Sector(1, new byte[128]); rig.Command(9); rig.FeedTrack(); rig.UntilResult();
        Assert.Equal(0, rig.Result()[1]);
        var bytes = rig.Output.Select(PinFixture.Decode).ToArray();
        var mark = Array.IndexOf(bytes, (byte)0xf8); Assert.True(mark >= 0);
        Assert.Equal(Payload().Take(11).Concat(new byte[117]), bytes.Skip(mark + 1).Take(128));
        var crc = rig.CrcPrefix(0xf8);
        foreach (var value in bytes.Skip(mark + 1).Take(130)) crc = PinFixture.Crc(crc, value);
        Assert.Equal(0, crc);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BadMatchingHeaderPreventsReadAndWriteData(bool writing)
    {
        var rig = new PinFixture(); rig.Sector(1, Payload(), badId: true);
        rig.Command(writing ? (byte)5 : (byte)6); rig.FeedTrack();
        var result = rig.Result(); Assert.Equal(0x20, result[1]); Assert.Equal(0, result[2]);
        Assert.Empty(rig.Output); Assert.Empty(rig.Received);
    }

    [Fact]
    public void MultiTrackSelectsTheOtherSideAtEotAndStopsAtItsEot()
    {
        var rig = new PinFixture();
        rig.Sector(1, Payload()); rig.Command(0x86); rig.FeedTrack();
        Assert.True(rig.Chip.Active); Assert.Equal(1, rig.Chip.Head);
        rig.Sector(1, Payload(), head: 1); rig.FeedTrack();
        Assert.Equal(Payload().Concat(Payload()), rig.Received);
        Assert.Equal(new byte[] { 0x44, 0x80, 0, 1, 0, 1, 0 }, rig.Result());
    }

    [Fact]
    public void ScanTcStillChecksTheRemainderOfTheFieldCrc()
    {
        var rig = new PinFixture { TerminalAfter = 1 };
        rig.HostInput.Enqueue(0); rig.Sector(1, Payload(), badData: true);
        rig.Command(0x11, dtl: 1); rig.FeedTrack();
        var result = rig.Result(); Assert.Equal(0x20, result[1]); Assert.Equal(0x20, result[2]);
        Assert.Equal(1, rig.HostTransfers);
    }

    [Fact]
    public void ReadTrackContinuesAfterBothCrcErrors()
    {
        var rig = new PinFixture(); rig.Sector(1, Payload(), badId: true, badData: true);
        rig.Sector(2, Payload()); rig.Command(2, end: 2); rig.ReadyForTrack(); rig.Index(); rig.FeedTrack();
        var result = rig.Result(); Assert.Equal(0xa0, result[1]); Assert.Equal(0x20, result[2]);
        Assert.Equal(Payload().Concat(Payload()), rig.Received);
    }

    [Fact]
    public void ResetAtWriteEnableRetainsTheResetStateAndCancelsPendingPulse()
    {
        var rig = new PinFixture(); rig.HostInput.Enqueue(0x55);
        rig.Chip.WriteEnableChanged = (_, active) => { if (active) rig.Chip.SetReset(true); };
        rig.Sector(1, Payload()); rig.Command(5); rig.FeedTrack();
        Assert.Equal(0, rig.Chip.ReadMainStatus()); Assert.False(rig.Chip.WriteEnable);
        Assert.False(rig.Chip.WriteData); Assert.False(rig.Chip.Irq); Assert.False(rig.Chip.Drq);
        Assert.Empty(rig.Output);
    }

    [Fact]
    public void ResetDuringWritingPreservesTheAlreadyEmittedPartialField()
    {
        var rig = new PinFixture(); foreach (var value in Payload()) rig.HostInput.Enqueue(value);
        rig.Sector(1, Payload(), includeData: false); rig.Command(5); rig.FeedTrack();
        rig.Clocks(1000); Assert.True(rig.Chip.WriteEnable); Assert.NotEmpty(rig.Output);
        var previous = rig.Output.ToArray(); rig.Chip.Reset(); rig.Clocks(1000);
        Assert.Equal(previous, rig.Output); Assert.False(rig.Chip.WriteEnable); Assert.False(rig.Chip.Active);
    }

    [Fact]
    public void FormatOverrunClosesTheGateWithoutUndoingTheWrittenLead()
    {
        var rig = new PinFixture { HostEnabled = false };
        Send(rig.Chip, 0x4d, 0, 0, 1, 0x1b, 0xe5);
        rig.ReadyForTrack(); rig.Index(); rig.UntilResult();
        Assert.Equal(0x10, rig.Result()[1]); Assert.NotEmpty(rig.Output); Assert.False(rig.Chip.WriteEnable);
    }

    [Fact]
    public void DackAssertionAloneLowersDrqWithoutConsumingData()
    {
        var rig = new PinFixture { HostEnabled = false };
        rig.Chip.DataRequestChanged = active =>
        {
            if (!active) return;
            rig.Chip.SetBus(false, false, false, true, dmaAcknowledge: true);
            Assert.False(rig.Chip.Drq);
        };
        rig.Sector(1, Payload()); rig.Command(6); rig.FeedTrack();
        Assert.Equal(0x10, rig.Result()[1]); Assert.Equal(0, rig.Chip.ReadBytes(0));
    }

    [Theory]
    [InlineData(Upd765Model.Upd765A, 77, 0x70)]
    [InlineData(Upd765Model.Upd765B, 77, 0x70)]
    [InlineData(Upd765Model.Upd72065B, 100, 0x20)]
    public void CmosRecalibrationCanReachTrackZeroBeyondSeventySevenSteps(Upd765Model model, int expectedSteps, byte status)
    {
        var position = 100;
        var steps = 0;
        var chip = new Upd765(unit => unit == 0 ? (byte)(0x20 | (position == 0 ? 0x10 : 0)) : (byte)0,
            (_, direction) => { position += direction; steps++; }, model: model);
        Specify(chip, false); Send(chip, 7, 0); Wait(chip, () => chip.Irq);
        Assert.Equal(expectedSteps, steps); Assert.Equal(100 - expectedSteps, position);
        Send(chip, 8); Assert.Equal(new byte[] { status, 0 }, Result(chip, 2));
    }

    [Theory]
    [InlineData(Upd765Model.Upd765A, 77)]
    [InlineData(Upd765Model.Upd765B, 77)]
    [InlineData(Upd765Model.Upd72065B, 255)]
    public void RecalibrationStopsAtTheVariantsPulseLimitWhenTrackZeroNeverAppears(Upd765Model model, int limit)
    {
        var steps = 0;
        var chip = new Upd765(_ => 0x20, (_, _) => steps++, model: model);
        Specify(chip, false); Send(chip, 7, 0); Wait(chip, () => chip.Irq);
        Assert.Equal(limit, steps);
        Send(chip, 8); Assert.Equal(new byte[] { 0x70, 0 }, Result(chip, 2));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FormatKeepsMultipleDuplicateIdsAndUsesTheCommandFillLength(bool mfm)
    {
        var rig = new PinFixture(mfm);
        byte[][] ids = [[9, 1, 5, 3], [0, 0, 1, 2], [0, 0, 5, 2]];
        foreach (var value in ids.SelectMany(id => id)) rig.HostInput.Enqueue(value);
        Send(rig.Chip, mfm ? (byte)0x4d : (byte)0x0d, 0, 2, 3, 0x54, 0xe5);
        rig.ReadyForTrack(); rig.Index(); rig.Clocks(40000); rig.Index();
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 6, 2 }, rig.Result());
        Assert.Equal(1536, rig.Chip.WrittenBytes(0));
        var bytes = rig.Output.Select(PinFixture.Decode).ToArray();
        var offset = 0;
        foreach (var id in ids)
        {
            var header = Array.IndexOf(bytes, (byte)0xfe, offset);
            Assert.True(header >= 0); Assert.Equal(id, bytes.Skip(header + 1).Take(4));
            var data = Array.IndexOf(bytes, (byte)0xfb, header + 7);
            Assert.True(data >= 0); Assert.All(bytes.Skip(data + 1).Take(512), value => Assert.Equal(0xe5, value));
            offset = data + 515;
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WriteProtectionDuringFormattingStopsFuturePulsesWithoutUndoingTheWrittenLead(bool mfm)
    {
        var rig = new PinFixture(mfm);
        foreach (var value in new byte[] { 0, 0, 1, 0 }) rig.HostInput.Enqueue(value);
        Send(rig.Chip, mfm ? (byte)0x4d : (byte)0x0d, 0, 0, 1, 0x1b, 0xe5);
        rig.ReadyForTrack(); rig.Index(); rig.Clocks(2000);
        var written = rig.Output.ToArray(); Assert.NotEmpty(written);
        rig.Status[0] |= 0x40; rig.Chip.Advance(1);
        Assert.False(rig.Chip.WriteEnable); Assert.Equal(2, rig.Result()[1]);
        rig.Clocks(3000); Assert.Equal(written, rig.Output);
    }

}
