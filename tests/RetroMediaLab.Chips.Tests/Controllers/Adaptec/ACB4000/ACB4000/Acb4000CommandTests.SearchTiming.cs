using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using T = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchTimingConstants;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadAndVerifyWithoutFieldsExhaustTheirFiniteSearch(bool verify)
    {
        var target = CreateManualTarget();
        SendCommand(target, verify ? Long(0x2f, address: 1, count: 1) : Short(8, address: 1, count: 1));
        Assert.True(target.ReadGate);
        var limit = 132L * T.FollowingFieldWaitTStates * 2_000_000 / T.RevisionBSequencerClock + 16;
        for (long tick = 0; tick < limit && !target.Bus.Request; tick++) target.Tick();
        Assert.True(target.Bus.Request);
        Assert.False(target.ReadGate);
        Assert.Equal(2, ReceiveResult(target).Status);
        var sense = Run(target, Short(3, count: 4)).Data;
        Assert.Equal(0x14, sense[0] & 0x7f);
        Assert.Equal(1, sense[3]);
    }

    [Fact]
    public void TranslationWithoutIndexFinishesInsteadOfWaitingForever()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(0x0f, address: 1));
        for (var tick = 0; tick < 8 && !target.ReadGate; tick++) target.Tick();
        Assert.True(target.ReadGate);
        var limit = (T.IndexWaitTStates + T.IndexSetupTStates) * 2L + 16;
        for (long tick = 0; tick < limit && !target.Bus.Request; tick++) target.Tick();
        Assert.True(target.Bus.Request);
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(T.NoIndex, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Fact]
    public void DataAfterAFieldTimeoutCannotUseAnOldMatchingId()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, address: 1, count: 1));
        FeedTranslationId(target, 1);
        for (var tick = 0; tick <= T.FollowingFieldWaitTStates; tick++) target.Tick();
        var payload = Enumerable.Repeat((byte)0x6c, 256).ToArray();
        PhysicalField(target, PhysicalData(payload));
        Assert.False(target.Bus.Request);
        FeedTranslationId(target, 1);
        PhysicalField(target, PhysicalData(payload));
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(payload, result.Data);
    }

    [Fact]
    public void AReceivedFieldRearmsTheWindowAndHostAcknowledgementHasNoSearchDeadline()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, address: 1, count: 1));
        var window = (int)((long)T.FollowingFieldWaitTStates * 2_000_000 / T.RevisionBSequencerClock);
        // The field serializer now advances the controller clock as physical cells arrive.
        const int fieldArrivalTicks = 64;
        for (var tick = 0; tick < window - fieldArrivalTicks; tick++) target.Tick();
        FeedTranslationId(target, 2);
        for (var tick = 0; tick < window - fieldArrivalTicks; tick++) target.Tick();
        Assert.False(target.Bus.Request);
        var payload = Enumerable.Repeat((byte)0x72, 256).ToArray();
        PhysicalPair(target, PhysicalId(1), PhysicalData(payload));
        WaitRequest(target);
        Assert.True(target.Bus.Request);
        Assert.Equal(Acb4000BusPhase.DataIn, target.Bus.Phase);
        for (var tick = 0; tick < 400_000; tick++) target.Tick();
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(payload, result.Data);
    }

    [Fact]
    public void ResetDiscardsAnArmedSearchDeadline()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, address: 1, count: 1));
        target.Reset();
        for (var tick = 0; tick < 400_000; tick++) target.Tick();
        Assert.False(target.Bus.Busy);
        Assert.False(target.Bus.Request);
        Assert.Equal(0, Run(target, Short(0)).Status);
    }

    [Fact]
    public void ParameterAcquisitionWithoutIndexTerminatesWithInvalidFormat()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(8, count: 1), waitForParameters: false);
        for (var tick = 0; tick < 1_000_000 && !target.ReadingParameterFields && !target.Bus.Request; tick++) Tick(target);
        Assert.True(target.ReadingParameterFields);
        target.SetIndex(false);
        for (var tick = 0; tick < 1_000_000 && !target.Bus.Request; tick++) target.Tick();
        Assert.True(target.Bus.Request);
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(0x1c, Run(target, Short(3, count: 4)).Data[0]);
    }
}
