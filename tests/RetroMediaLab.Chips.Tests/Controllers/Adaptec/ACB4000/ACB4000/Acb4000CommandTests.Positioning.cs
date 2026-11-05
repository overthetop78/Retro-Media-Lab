using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void FormatDoesNotOpenTheWriteGateOnTheNextCylinderBeforeSeekComplete()
    {
        var disk = new Disk { SeekCompletionDelayTicks = 10_000 };
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(4));
        for (var tick = 0; disk.MechanicalCylinder == 0 && tick < 1_000_000; tick++) Tick(target);
        Assert.Equal(1, disk.MechanicalCylinder);
        Assert.False(target.WriteGate);
        Assert.NotEmpty(disk.TrackWrites);
        Assert.All(disk.TrackWrites, track => Assert.Equal(0, track.Cylinder));
        Assert.Equal(0, ReceiveResult(target).Status);
        Assert.Contains(disk.TrackWrites, track => track.Cylinder == 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImpliedReadAndVerifyDoNotReadBeforeTheDriveCompletesItsSeek(bool verify)
    {
        var disk = new Disk { SeekCompletionDelayTicks = 10_000 };
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, verify ? Long(0x2f, address: 66, count: 1) : Short(8, address: 66, count: 1));
        for (var tick = 0; tick < 100; tick++) Tick(target);
        Assert.Equal(1, disk.MechanicalCylinder);
        Assert.False(target.ReadGate);
        Assert.Equal(0, disk.Reads);
        var result = ReceiveResult(target);
        Assert.Equal(0, result.Status);
        if (verify) Assert.Empty(result.Data);
        else Assert.Equal(disk.Bytes.AsSpan(66 * 256, 256).ToArray(), result.Data);
    }

    [Fact]
    public void ImpliedWriteRetainsItsReceivedBlockUntilSeekComplete()
    {
        var disk = new Disk { SeekCompletionDelayTicks = 10_000 };
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(0x0a, address: 66, count: 1));
        var data = Enumerable.Repeat((byte)0xe5, 256).ToArray();
        foreach (var value in data)
        {
            WaitRequest(target);
            Assert.Equal(Acb4000BusPhase.DataOut, target.Bus.Phase);
            Transfer(target, value);
        }
        for (var tick = 0; tick < 100; tick++) Tick(target);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(1, disk.MechanicalCylinder);
        Assert.Equal(0, ReceiveResult(target).Status);
        Assert.Equal(data, disk.Bytes.AsSpan(66 * 256, 256).ToArray());
    }

    [Fact]
    public void TwoSectorsOnOneCylinderDoNotIssueUnneededStepEdges()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short(0x1a, count: 22)).Status);
        var calibrationSteps = disk.MechanicalSteps;
        Assert.Equal(0, Run(target, Short(8, address: 66, count: 2)).Status);
        Assert.Equal(calibrationSteps + 1, disk.MechanicalSteps);
    }

    [Fact]
    public void ReplacingTheMediumDuringAnImpliedSeekDoesNotReadFromTheReplacement()
    {
        var first = new Disk { SeekCompletionDelayTicks = 10_000 };
        var replacement = new Disk();
        var active = first;
        var target = CreateTarget(_ => active, 2_000_000);
        SendCommand(target, Short(8, address: 66, count: 1));
        for (var tick = 0; tick < 100; tick++) Tick(target);
        active = replacement;
        var result = ReceiveResult(target);
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(4, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Equal(0, first.Reads);
        Assert.Equal(0, replacement.Reads);
    }

    [Fact]
    public void ResetDuringAnImpliedWriteDoesNotPublishTheBufferedData()
    {
        var disk = new Disk { SeekCompletionDelayTicks = 10_000 };
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(0x0a, address: 66, count: 1));
        for (var index = 0; index < 256; index++)
        {
            WaitRequest(target);
            Transfer(target, 0xe5);
        }
        for (var tick = 0; tick < 100; tick++) Tick(target);
        target.Reset();
        for (var tick = 0; tick < 10_010; tick++) Tick(target);
        Assert.Equal(before, disk.Bytes);
        Assert.Equal(0, disk.Writes);
    }

    [Fact]
    public void SeekFreesTheBusBeforeMechanicalCompletionAndOnlyItsUnitRemainsBusy()
    {
        var first = new Disk { SeekCompletionDelayTicks = 10_000 };
        var second = new Disk();
        var target = CreateTarget(unit => unit == 0 ? first : second, 2_000_000);
        Assert.Equal(0, Run(target, Short(0x0b, address: 66)).Status);
        Assert.False(target.Bus.Busy);
        Assert.Equal(1, first.MechanicalCylinder);
        Assert.Equal(8, Run(target, Short(0, unit: 0)).Status);
        Assert.Equal(0, Run(target, Short(0, unit: 1)).Status);
        for (var tick = 0; tick < 10_010; tick++) Tick(target);
        Assert.Equal(0, Run(target, Short(0, unit: 0)).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RezeroActuallyMovesTheHeadAndChecksTrackZero(int initialCylinder)
    {
        var disk = new Disk { MechanicalCylinder = initialCylinder };
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Short(1));
        Assert.Equal(0, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(0, disk.MechanicalCylinder);
        Assert.False(target.Bus.Busy);
    }

    [Fact]
    public void RezeroRejectsATrackZeroSignalThatRemainsActiveOffTrack()
    {
        var disk = new Disk { StuckTrackZero = true };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short(1)).Status);
        Assert.Equal(6, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Equal(16, disk.MechanicalCylinder);
    }

    [Fact]
    public void ReadyIsADriveSignalRatherThanTheExistenceOfAnImage()
    {
        var disk = new Disk { Ready = false };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short(0)).Status);
        Assert.Equal(4, Run(target, Short(3, count: 4)).Data[0]);
        disk.Ready = true;
        Assert.Equal(0, Run(target, Short(0)).Status);
    }

    [Fact]
    public void ResetCancelsRemainingStepsWithoutReturningThePhysicalHeadToZero()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(1));
        for (var tick = 0; tick < 100; tick++) Tick(target);
        Assert.True(disk.MechanicalCylinder > 0);
        var stoppedPosition = disk.MechanicalCylinder;
        target.Reset();
        for (var tick = 0; tick < 1000; tick++) Tick(target);
        Assert.Equal(stoppedPosition, disk.MechanicalCylinder);
        Assert.False(target.DriveStepIssued(0));
        Assert.False(target.Bus.Busy);
    }

    [Fact]
    public void ExternalSeekCompleteExpiryTerminatesTheWaitingCommandWithSense02()
    {
        var disk = new Disk { SeekCompletionDelayTicks = int.MaxValue };
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(1));
        for (var tick = 0; tick < 100_000; tick++) Tick(target);
        Assert.False(target.Bus.Request);
        target.ExpireDriveSeek(0);
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(2, Run(target, Short(3, count: 4)).Data[0]);
    }
}
