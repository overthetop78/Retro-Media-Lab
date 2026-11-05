using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ReinitializeCalibratesAndRestoresPhysicalParametersOnTheSelectedUnit(int unit)
    {
        var disk = new Disk();
        var target = CreateTarget(index => index == unit ? disk : null, 2_000_000);
        var original = Run(target, Short((byte)Acb4000CommandCode.ModeSense, unit, count: 22)).Data;
        var selected = Acb4000DriveParameters.Default with { LandingZone = 5, StepRate = 2 };
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSelect, unit, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.Seek, unit, address: 66)).Status);
        for (var tick = 0; tick < 10_000; tick++) Tick(target);
        var acquisitions = disk.ParameterAcquisitions;
        Assert.Equal(1, disk.MechanicalCylinder);
        var result = Run(target, Short((byte)Acb4000CommandCode.SendDiagnostic, unit, count: 4), [0x60, 0x30, 0xff, 0xff]);
        Assert.Equal(0, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(0, disk.MechanicalCylinder);
        Assert.Equal(acquisitions + 1, disk.ParameterAcquisitions);
        Assert.Equal(original, Run(target, Short((byte)Acb4000CommandCode.ModeSense, unit, count: 22)).Data);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.Equal(0, disk.Writes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x31)]
    [InlineData(0xff)]
    public void ReinitializeRequiresTheQualifierObservedInTheFirmware(byte qualifier)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short((byte)Acb4000CommandCode.SendDiagnostic, count: 4), [0x60, qualifier, 0, 0]).Status);
        Assert.Equal(0x24, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
        Assert.Equal(0, disk.MechanicalSteps);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void ReinitializeMapsPhysicalFailuresToItsOwnSenseCode(bool ready, bool stuckZero, bool writeFault)
    {
        var disk = new Disk { Ready = ready, StuckTrackZero = stuckZero };
        var target = CreateTarget(_ => disk, 2_000_000);
        target.SetWriteFault(writeFault);
        Assert.Equal(2, Run(target, Short((byte)Acb4000CommandCode.SendDiagnostic, count: 4), [0x60, 0x30, 0, 0]).Status);
        Assert.Equal(0x1d, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
        Assert.Equal(0, disk.Writes);
    }

    [Fact]
    public void ReinitializeWithoutAFormatDoesNotInventAParameterTable()
    {
        var disk = new Disk { Formatted = false };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short((byte)Acb4000CommandCode.SendDiagnostic, count: 4), [0x60, 0x30, 0, 0]).Status);
        Assert.Equal(0x1d, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Fact]
    public void ReplacementDuringReinitializeCannotCompleteUsingTheNewDrive()
    {
        var first = new Disk();
        var active = first;
        var target = CreateTarget(_ => active, 2_000_000);
        StartReinitialize(target);
        for (var tick = 0; tick < 100; tick++) Tick(target);
        active = new Disk();
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(0x1d, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
        Assert.Equal(0, active.ParameterAcquisitions);
    }

    [Fact]
    public void ResetAbandonsReinitializationWithoutReturningTheHeadToZero()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        StartReinitialize(target);
        for (var tick = 0; tick < 100; tick++) Tick(target);
        target.Reset();
        var cylinder = disk.MechanicalCylinder;
        for (var tick = 0; tick < 500; tick++) Tick(target);
        Assert.Equal(cylinder, disk.MechanicalCylinder);
        Assert.False(target.Bus.Busy);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
    }

    private static void StartReinitialize(Acb4000 target)
    {
        SendCommand(target, Short((byte)Acb4000CommandCode.SendDiagnostic, count: 4));
        foreach (var value in new byte[] { 0x60, 0x30, 0, 0 })
        {
            WaitRequest(target);
            Transfer(target, value);
        }
    }
}
