using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachEccRereadGetsItsOwnIdBudgetOnAFullTrack(bool extended)
    {
        var target = CreateManualTarget();
        SendCommand(target, extended ? Long(0x28, address: 3, count: 1) : Short(8, address: 3, count: 1));
        var payload = Enumerable.Range(0, 256).Select(index => (byte)index).ToArray();
        var damaged = PhysicalData(payload);
        damaged[5] ^= 0x80;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            for (uint sector = 0; sector < 33; sector++)
                if (sector != 3) FeedTranslationId(target, sector);
            Assert.True(target.ReadGate);
            PhysicalPair(target, PhysicalId(3), damaged);
        }
        Assert.Equal(payload, DrainRead(target, out var status));
        Assert.Equal(0, status);
        Assert.Equal(6, target.ReadBlocks(0));
    }

    [Fact]
    public void SearchDataCanRereadAfterItsLastAllowedIdHadABadDataCheck()
    {
        var target = CreateManualTarget();
        SendCommand(target, SearchCdb());
        foreach (var value in SearchArgument(256, 1, 0x55)) { WaitRequest(target); Transfer(target, value); }
        for (var tick = 0; tick < 8 && !target.ReadGate; tick++) Tick(target);
        for (var id = 0; id < 131; id++) FeedTranslationId(target, 1);
        var payload = Enumerable.Repeat((byte)0x55, 256).ToArray();
        var bad = PhysicalData(payload);
        bad[5] ^= 0x80;
        PhysicalPair(target, PhysicalId(0), bad);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            for (uint id = 1; id < 33; id++) FeedTranslationId(target, id);
            Assert.True(target.ReadGate);
            PhysicalPair(target, PhysicalId(0), attempt == 0 ? bad : PhysicalData(payload));
        }
        Assert.Equal(4, ReceiveResult(target).Status);
        Assert.Equal(3, target.ReadBlocks(0));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void LastMatchingIdAllowsItsDataButCannotRestartAnExhaustedSearch(bool verify, bool dataPresent)
    {
        var target = CreateManualTarget();
        SendCommand(target, verify ? Long(0x2f, address: 1, count: 1) : Short(8, address: 1, count: 1));
        for (var index = 0; index < 131; index++) FeedTranslationId(target, 2);
        FeedTranslationId(target, 1);
        Assert.True(target.ReadGate);
        Assert.False(target.Bus.Request);
        var payload = Enumerable.Repeat((byte)0x6c, 256).ToArray();
        if (dataPresent) PhysicalField(target, PhysicalData(payload));
        else FeedTranslationId(target, 1);
        var result = ReceiveResult(target);
        Assert.Equal(dataPresent ? 0 : 2, result.Status);
        Assert.Equal(dataPresent && !verify ? payload : [], result.Data);
        if (!dataPresent)
        {
            var sense = Run(target, Short(3, count: 4)).Data;
            Assert.Equal(0x14, sense[0] & 0x7f);
            Assert.Equal(1, sense[3]);
        }
    }

    [Theory]
    [InlineData(Acb4000CommandCode.Read, false, 0x14)]
    [InlineData(Acb4000CommandCode.Read, true, 0x10)]
    [InlineData(Acb4000CommandCode.Verify, false, 0x14)]
    [InlineData(Acb4000CommandCode.Verify, true, 0x10)]
    [InlineData(Acb4000CommandCode.TranslateOrExpandedWriteBuffer, false, 0x14)]
    [InlineData(Acb4000CommandCode.TranslateOrExpandedWriteBuffer, true, 0x10)]
    public void PhysicalSearchReturnsSenseWhenItsIdBudgetIsExhausted(Acb4000CommandCode command, bool corrupt, byte expectedSense)
    {
        var target = CreateManualTarget();
        var cdb = command == Acb4000CommandCode.Verify ? Long((byte)command, address: 1, count: 1)
            : Short((byte)command, address: 1, count: command == Acb4000CommandCode.TranslateOrExpandedWriteBuffer ? 0 : 1);
        SendCommand(target, cdb);
        target.SetIndex(false);
        target.SetIndex(true);
        for (var index = 0; index < 131; index++) FeedTranslationId(target, 2, corrupt: corrupt);
        Assert.False(target.Bus.Request);
        FeedTranslationId(target, 2, corrupt: corrupt);
        var result = ReceiveResult(target);
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        var sense = Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data;
        Assert.Equal(expectedSense, (byte)(sense[0] & 0x7f));
        Assert.Equal(1, sense[3]);
    }

    [Fact]
    public void WriteCannotModifyTheTrackWhenTheTargetIdWasNeverFound()
    {
        var disk = new Disk();
        var target = CreateManualTarget(disk);
        SendCommand(target, Short((byte)Acb4000CommandCode.Write, count: 1));
        for (var index = 0; index < 256; index++)
        {
            WaitRequest(target);
            Transfer(target, 0xe5);
        }
        for (var tick = 0; tick < 8 && !target.ReadGate; tick++) Tick(target);
        Assert.True(target.ReadGate);
        for (var index = 0; index < 132; index++) FeedTranslationId(target, 2);
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.False(target.WriteGate);
        Assert.Empty(disk.TrackWrites);
        Assert.Equal(0, disk.Writes);
    }

    [Fact]
    public void AValidTargetAtTheLastAttemptCanStillCompleteTranslation()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: 1));
        target.SetIndex(false);
        target.SetIndex(true);
        for (var index = 0; index < 131; index++) FeedTranslationId(target, 2);
        FeedTranslationId(target, 1);
        Assert.Equal(0, ReceiveResult(target).Status);
    }
}
