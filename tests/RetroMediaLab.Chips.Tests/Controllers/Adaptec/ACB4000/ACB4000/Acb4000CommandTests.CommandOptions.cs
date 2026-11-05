using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(Acb4000CommandCode.ExpandedCheckTrack)]
    [InlineData(Acb4000CommandCode.ExpandedInitialize)]
    [InlineData(Acb4000CommandCode.ExpandedReadEccBurst)]
    [InlineData(Acb4000CommandCode.ExpandedReadBuffer)]
    [InlineData(Acb4000CommandCode.ExpandedRamDiagnostic)]
    [InlineData(Acb4000CommandCode.ExpandedDriveDiagnostic)]
    [InlineData(Acb4000CommandCode.ExpandedControllerDiagnostic)]
    public void OptionalCommandsRequireThePhysicalJumper(Acb4000CommandCode command)
    {
        var target = CreateTarget(_ => null, 2_000_000);
        Assert.Equal(2, Run(target, Short((byte)command)).Status);
        Assert.Equal(0x20, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    [Theory]
    [InlineData(Acb4000CommandCode.ExpandedCheckTrack)]
    [InlineData(Acb4000CommandCode.ExpandedInitialize)]
    [InlineData(Acb4000CommandCode.ExpandedRamDiagnostic)]
    [InlineData(Acb4000CommandCode.ExpandedDriveDiagnostic)]
    [InlineData(Acb4000CommandCode.ExpandedControllerDiagnostic)]
    public void AppendixBNoOpCommandsReturnGoodWithoutAccessingAMedium(Acb4000CommandCode command)
    {
        var driveLookups = 0;
        var target = CreateTarget(_ => { driveLookups++; return null; }, 2_000_000);
        target.SetExpandedCommandSet(true);
        var result = Run(target, Short((byte)command),
            command == Acb4000CommandCode.ExpandedInitialize ? new byte[8] : null);
        Assert.Equal(0, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(0, driveLookups);
    }

    [Theory]
    [InlineData(256, 0)]
    [InlineData(512, 1)]
    [InlineData(1024, 0)]
    public void ExpandedBuffersTransferOneSelectedBlockAndPreserveTheRestOfTheCommonBuffer(ushort size, int unit)
    {
        var disk = new Disk { IsReadOnly = true };
        var target = CreateTarget(_ => disk, 2_000_000);
        var original = Enumerable.Range(0, 1024).Select(index => (byte)(index * 13)).ToArray();
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.WriteBuffer), original).Status);
        var selected = Acb4000DriveParameters.Default with { BlockSize = size };
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSelect, unit, count: 22), EncodeParameters(selected)).Status);
        target.SetExpandedCommandSet(true);
        var next = Enumerable.Range(0, size).Select(index => (byte)(index * 7 + 1)).ToArray();
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer, unit), next).Status);
        var read = Run(target, Short((byte)Acb4000CommandCode.ExpandedReadBuffer, unit));
        Assert.Equal(0, read.Status);
        Assert.Equal(next, read.Data);
        // MODE SELECT itself used the beginning of the common buffer. The overwritten block is now next.
        next.CopyTo(original, 0);
        Assert.Equal(original, Run(target, Short((byte)Acb4000CommandCode.ReadBuffer, unit)).Data);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Fact]
    public void InitializeConsumesItsParameterBlockWithoutOverridingModeSelect()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Status);
        var selected = Acb4000DriveParameters.Default with { BlockSize = 512, LandingZone = 5 };
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSelect, count: 22), EncodeParameters(selected)).Status);
        target.SetExpandedCommandSet(true);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ExpandedInitialize),
            Enumerable.Repeat((byte)0xff, 8).ToArray()).Status);
        Assert.Equal(EncodeParameters(selected), Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Data);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.Equal(0, disk.Writes);
    }

    [Fact]
    public void TheJumperCanBeInsertedAndRemovedWithoutResetAndSurvivesReset()
    {
        var target = CreateTarget(_ => null, 2_000_000);
        target.SetExpandedCommandSet(true);
        Assert.Equal(new byte[] { 8 }, Run(target, Short((byte)Acb4000CommandCode.ExpandedReadEccBurst)).Data);
        target.Reset();
        Assert.True(target.ExpandedCommandsEnabled);
        Assert.Equal(new byte[] { 8 }, Run(target, Short((byte)Acb4000CommandCode.ExpandedReadEccBurst)).Data);
        target.SetExpandedCommandSet(false);
        Assert.Equal(2, Run(target, Short((byte)Acb4000CommandCode.ExpandedReadEccBurst)).Status);
    }

    [Theory]
    [InlineData(0xfc, true, 0)]
    [InlineData(0x80, false, 2)]
    [InlineData(1, true, 2)]
    [InlineData(2, true, 2)]
    public void ReservedControlBitsDependOnTheOptionButLinkedCommandsRemainRejected(byte control, bool expanded, byte status)
    {
        var target = CreateTarget(_ => null, 2_000_000);
        target.SetExpandedCommandSet(expanded);
        var cdb = Short((byte)Acb4000CommandCode.WriteBuffer);
        cdb[^1] = control;
        Assert.Equal(status, Run(target, cdb, status == 0 ? new byte[1024] : null).Status);
    }

    [Theory]
    [InlineData(0xfc, true)]
    [InlineData(0x80, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public void FormatDecoderUsesTheSameControlBitPolicy(byte control, bool accepted)
    {
        byte[] cdb = [4, 0, 0, 0, 2, control];
        Assert.False(Acb4000FormatRequest.TryDecode(cdb, Acb4000DriveParameters.Default, out _));
        Assert.Equal(accepted, Acb4000FormatRequest.TryDecode(cdb, Acb4000DriveParameters.Default, out _, expandedCommandSet: true));
    }

    [Fact]
    public void RemovingTheJumperRestoresTranslateInsteadOfLeavingAWriteBufferAlias()
    {
        var disk = new Disk();
        var target = CreateManualTarget(disk);
        target.SetExpandedCommandSet(true);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer), new byte[256]).Status);
        target.SetExpandedCommandSet(false);
        SendCommand(target, Short((byte)Acb4000CommandCode.TranslateOrExpandedWriteBuffer));
        target.SetIndex(false);
        target.SetIndex(true);
        FeedTranslationId(target, 0);
        Assert.Equal(8, ReceiveResult(target).Data.Length);
        Assert.Equal(0, disk.Writes);
    }
}
