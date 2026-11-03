using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class SequenceFailureTests
{
    public static IEnumerable<object[]> Modes =>
        from mfm in new[] { false, true }
        from model in new[] { Wd1770Model.Wd1770, Wd1770Model.Wd1772 }
        select new object[] { mfm, model };

    [Theory]
    [MemberData(nameof(Modes))]
    public void TypeOneVerificationWaitsSixIndexesAndRejectsAMatchingBadCrc(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model);
        rig.Command(0x1c);
        rig.Advance(240_000);
        for (var index = 0; index < 5; index++) rig.Index();
        Assert.True(rig.Chip.Active);
        rig.Index();
        Assert.False(rig.Chip.Active);
        Assert.Equal(16, rig.Chip.Read(0) & 0x18);
        rig.Command(0x1c);
        rig.Advance(240_000);
        rig.FeedId(badCrc: true);
        Assert.False(rig.Chip.Active);
        Assert.Equal(0x18, rig.Chip.Read(0) & 0x18);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void EverySizeCodeUsesTheTwoRecordedLowBits(bool mfm, Wd1770Model model)
    {
        for (byte size = 0; size < 8; size++)
        {
            var rig = new PinFixture(mfm, model);
            var payload = Enumerable.Range(0, 128 << (size & 3)).Select(index => (byte)index).ToArray();
            rig.Command(0x88);
            rig.FeedId(size: size);
            rig.FeedField(0xfb, payload);
            rig.Advance(1024);
            Assert.False(rig.Chip.Active);
            Assert.Equal(0, rig.Chip.Read(0) & 0x7c);
            Assert.Equal(payload, rig.Read);
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void BadDataCrcReturnsTheDataAndCrcError(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model);
        rig.Command(0x88);
        rig.FeedId();
        rig.FeedField(0xfb, new byte[128], badCrc: true);
        rig.Advance(1024);
        Assert.False(rig.Chip.Active);
        Assert.Equal(128, rig.Read.Count);
        Assert.Equal(8, rig.Chip.Read(0) & 0x7c);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void BadMatchingIdDoesNotDeliverPayloadAndTheNextGoodIdClearsTheCrcError(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model);
        rig.Command(0x88);
        rig.FeedId(badCrc: true);
        rig.FeedField(0xfb, Enumerable.Repeat((byte)0x11, 128).ToArray());
        Assert.Empty(rig.Read);
        rig.FeedId();
        rig.FeedField(0xfb, Enumerable.Repeat((byte)0x22, 128).ToArray());
        rig.Advance(1024);
        Assert.Equal(Enumerable.Repeat((byte)0x22, 128), rig.Read);
        Assert.Equal(0, rig.Chip.Read(0) & 0x7c);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void MultipleReadIncrementsTheSectorRegisterAndStillUsesTheRecordedIds(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model);
        rig.Command(0x98, sector: 7);
        rig.FeedId(number: 7);
        rig.FeedField(0xfb, Enumerable.Repeat((byte)7, 128).ToArray());
        Assert.Equal(8, rig.Chip.Read(2));
        rig.FeedId(number: 2);
        rig.FeedField(0xfb, new byte[128]);
        Assert.Equal(128, rig.Read.Count);
        rig.FeedId(number: 8);
        rig.FeedField(0xf8, Enumerable.Repeat((byte)8, 128).ToArray());
        Assert.Equal(9, rig.Chip.Read(2));
        Assert.Equal(Enumerable.Repeat((byte)7, 128).Concat(Enumerable.Repeat((byte)8, 128)), rig.Read);
        rig.Command(0xd0);
        Assert.False(rig.Chip.Active);
        Assert.Equal(0x20, rig.Chip.Read(0) & 0x20);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void MissedReadDrqKeepsTheLastByteAndSetsLostData(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model) { ServiceReads = false };
        rig.Command(0x88);
        rig.FeedId();
        rig.FeedField(0xfb, Enumerable.Range(0, 128).Select(index => (byte)index).ToArray());
        rig.Advance(1024);
        Assert.False(rig.Chip.Active);
        Assert.Equal(127, rig.Chip.Read(3));
        Assert.Equal(4, rig.Chip.Read(0) & 0x7c);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void MissingFirstWriteByteTerminatesWithoutOpeningTheGate(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model) { ServiceWrites = false };
        rig.Command(0xa8);
        rig.FeedId();
        rig.Advance(20_000);
        Assert.False(rig.Chip.Active);
        Assert.False(rig.Chip.WriteGate);
        Assert.Empty(rig.Written);
        Assert.Equal(4, rig.Chip.Read(0) & 0x7c);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void LaterMissingWriteBytesBecomeZerosAndHaveARealCrc(bool mfm, Wd1770Model model)
    {
        var rig = new PinFixture(mfm, model);
        rig.Write.Enqueue(0x77);
        rig.Command(0xa8);
        rig.FeedId();
        rig.Advance(100_000);
        Assert.False(rig.Chip.Active);
        Assert.Equal(4, rig.Chip.Read(0) & 0x7c);
        var offset = rig.Written.FindIndex(item => item.Kind == Wd1770ReadWordKind.AddressMark && item.Data == 0xfb);
        Assert.True(offset >= 0);
        Assert.Equal(new byte[] { 0x77 }.Concat(new byte[127]), rig.Written.Skip(offset + 1).Take(128).Select(item => item.Data));
        ushort crc = mfm ? (ushort)0xcdb4 : (ushort)0xffff;
        foreach (var item in rig.Written.Skip(offset).Take(131)) crc = Wd1770Crc.Add(crc, item.Data);
        Assert.Equal(0, crc);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void WriteProtectionRefusesBothWriteCommandsWithoutDrqOrGate(bool mfm, Wd1770Model model)
    {
        foreach (var command in new byte[] { 0xa8, 0xf8 })
        {
            var rig = new PinFixture(mfm, model);
            rig.Chip.SetWriteProtected(true);
            rig.Command(command);
            Assert.False(rig.Chip.Active);
            Assert.False(rig.Chip.Drq);
            Assert.False(rig.Chip.WriteGate);
            Assert.Equal(64, rig.Chip.Read(0) & 0x7c);
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void ResetAndForceInterruptStopTheActualWriteGateAndFuturePulses(bool mfm, Wd1770Model model)
    {
        foreach (var reset in new[] { false, true })
        {
            var rig = new PinFixture(mfm, model);
            for (var index = 0; index < 256; index++) rig.Write.Enqueue(0x55);
            rig.Command(0xf8);
            rig.Advance(2048);
            rig.Index();
            rig.Advance(3072);
            Assert.True(rig.Chip.WriteGate);
            if (reset) rig.Chip.SetReset(true); else rig.Command(0xd8);
            Assert.False(rig.Chip.WriteGate);
            Assert.False(rig.Chip.Active);
            Assert.False(rig.Chip.WriteData);
            Assert.False(rig.Chip.Drq);
            var rising = 0;
            rig.Chip.WriteDataChanged += (_, high) => { if (high) rising++; };
            rig.Advance(8192);
            Assert.Equal(0, rising);
            Assert.Equal(!reset, rig.Chip.Irq);
        }
    }
}
