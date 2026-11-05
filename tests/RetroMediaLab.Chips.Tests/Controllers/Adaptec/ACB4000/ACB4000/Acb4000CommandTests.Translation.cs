using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(256, false, 0)]
    [InlineData(512, false, 3)]
    [InlineData(1024, false, 5)]
    [InlineData(256, true, 4)]
    [InlineData(512, true, 2)]
    [InlineData(1024, true, 1)]
    public void TranslateUsesPhysicalOrdinalAndLayoutWithoutRequestingData(ushort size, bool sequential, int ordinal)
    {
        var disk = new Disk(size) { SequentialParameterIds = sequential };
        var target = CreateManualTarget(disk);
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: 1));
        Assert.True(target.ReadGate);
        // A matching ID before INDEX must not produce a guessed position.
        FeedTranslationId(target, 1);
        Assert.False(target.Bus.Request);
        target.SetIndex(false);
        target.SetIndex(true);
        for (var index = 0; index < ordinal; index++) FeedTranslationId(target, 1, defective: true);
        FeedTranslationId(target, 1);
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(8, result.Data.Length);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32BigEndian(result.Data));
        Assert.Equal((uint)(150 + ordinal * (size + (sequential ? 64 : 54))),
            BinaryPrimitives.ReadUInt32BigEndian(result.Data.AsSpan(4)));
        Assert.Equal(0, disk.Reads);
        Assert.Equal(0, disk.Writes);
    }

    [Fact]
    public void TranslateReportsTheSelectedCylinderAndHeadAfterPhysicalPositioning()
    {
        var disk = new Disk();
        var target = CreateManualTarget(disk);
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: 99));
        for (var tick = 0; tick < 20_000 && !target.ReadGate; tick++) Tick(target);
        Assert.True(target.ReadGate);
        Assert.Equal(1, disk.MechanicalCylinder);
        Assert.Equal(1, target.SelectedHead);
        target.SetIndex(false);
        target.SetIndex(true);
        FeedTranslationId(target, 99);
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(0x101u, BinaryPrimitives.ReadUInt32BigEndian(result.Data));
        Assert.Equal(150u, BinaryPrimitives.ReadUInt32BigEndian(result.Data.AsSpan(4)));
        Assert.Equal(0, disk.Reads);
    }

    [Fact]
    public void NewIndexRestartsTheOrdinalAndCorruptIdsCannotSupplyAnAddress()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: 1));
        target.SetIndex(false);
        target.SetIndex(true);
        FeedTranslationId(target, 1, corrupt: true);
        FeedTranslationId(target, 10);
        Assert.False(target.Bus.Request);
        target.SetIndex(false);
        target.SetIndex(true);
        FeedTranslationId(target, 1);
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(150u, BinaryPrimitives.ReadUInt32BigEndian(result.Data.AsSpan(4)));
    }

    [Theory]
    [InlineData(1, 0, 0x24)]
    [InlineData(0, 132, 0x21)]
    public void TranslateRejectsInvalidCountOrAddress(int count, int address, byte sense)
    {
        var target = CreateManualTarget();
        var result = Run(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: address, count: count));
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(sense, (byte)(Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0] & 0x7f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TranslateSamplesTheCurrentIndexLevelAtEntry(bool releasedBeforeEntry)
    {
        var target = CreateManualTarget();
        Assert.Equal(0, Run(target, Short(0x1a, count: 22)).Status);
        target.SetIndex(true);
        if (releasedBeforeEntry) target.SetIndex(false);
        SendCommand(target, Short(0x0f, address: 1));
        for (var tick = 0; tick < 8 && !target.ReadGate; tick++) Tick(target);
        Assert.True(target.ReadGate);
        FeedTranslationId(target, 1);
        if (releasedBeforeEntry)
        {
            Assert.False(target.Bus.Request);
            target.SetIndex(true);
            FeedTranslationId(target, 1);
        }
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(150u, BinaryPrimitives.ReadUInt32BigEndian(result.Data.AsSpan(4)));
    }

    [Fact]
    public void HeldIndexDoesNotRestartThePhysicalOrdinalOnEachSample()
    {
        var target = CreateManualTarget();
        Assert.Equal(0, Run(target, Short(0x1a, count: 22)).Status);
        target.SetIndex(true);
        SendCommand(target, Short(0x0f, address: 1));
        for (var tick = 0; tick < 8 && !target.ReadGate; tick++) Tick(target);
        FeedTranslationId(target, 2);
        target.SetIndex(true);
        target.SetIndex(true);
        FeedTranslationId(target, 1);
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        Assert.Equal(460u, BinaryPrimitives.ReadUInt32BigEndian(result.Data.AsSpan(4)));
    }

    [Fact]
    public void ResetAbandonsTranslationWithoutPublishingAPartialDescriptor()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: 1));
        target.SetIndex(false);
        target.SetIndex(true);
        target.Reset();
        target.SetIndex(true);
        FeedTranslationId(target, 1);
        Assert.False(target.Bus.Busy);
        Assert.False(target.Bus.Request);
    }

    [Fact]
    public void ReplacementDuringTranslationReturnsNotReady()
    {
        var active = new Disk();
        var target = CreateTarget(_ => active, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Status);
        Clocks.GetValue(target, _ => throw new InvalidOperationException()).ManualCells = true;
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, address: 1));
        active = new Disk();
        Tick(target);
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(4, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    private static void FeedTranslationId(Acb4000 target, uint address, bool defective = false, bool corrupt = false)
    {
        Span<byte> field = stackalloc byte[10];
        Acb4000FieldCodec.WriteId(field, address, 0, defective);
        if (corrupt) field[^1] ^= 1;
        PhysicalField(target, field);
    }
}
