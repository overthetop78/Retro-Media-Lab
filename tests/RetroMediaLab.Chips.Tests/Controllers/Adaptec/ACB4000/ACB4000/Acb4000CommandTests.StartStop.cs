using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void StopUsesTheVolatileParkingTableAndStartRestoresThePhysicalTable()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var original = Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Data;
        Assert.True(Acb4000DriveParameters.TryDecode(original, Acb4000DriveParameters.Default, out var physical));
        var selected = physical with { LandingZone = 5 };
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSelect, count: 22), EncodeParameters(selected)).Status);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.StartStop)).Status);
        Assert.Equal(6, disk.MechanicalCylinder);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.StartStop, count: 1)).Status);
        Assert.Equal(original, Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Data);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 6)]
    [InlineData(127, 128)]
    [InlineData(128, 0)]
    [InlineData(133, -5)]
    [InlineData(255, -127)]
    public void StopParksUsingPhysicalParametersWithoutWritingData(byte landing, int expectedCylinder)
    {
        var disk = new Disk { LandingZone = landing, AllowNegativeCylinder = true };
        var before = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.StartStop)).Status);
        Assert.Equal(expectedCylinder, disk.MechanicalCylinder);
        Assert.True(disk.MechanicallySettled);
        Assert.Equal(before, disk.Bytes);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0, disk.ParameterWrites);
    }

    [Fact]
    public void StartRecalibratesAndReacquiresThePhysicalParametersAfterParking()
    {
        var disk = new Disk { LandingZone = 5 };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.StartStop)).Status);
        Assert.Equal(6, disk.MechanicalCylinder);
        var acquisitions = disk.ParameterAcquisitions;
        var steps = disk.MechanicalSteps;
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.StartStop, count: 1)).Status);
        Assert.Equal(0, disk.MechanicalCylinder);
        Assert.True(disk.MechanicallySettled);
        Assert.True(disk.MechanicalSteps > steps);
        Assert.Equal(acquisitions + 1, disk.ParameterAcquisitions);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.Read, count: 1)).Status);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(128, 0)]
    [InlineData(0, 1)]
    public void StartStopRejectsReservedFieldsBeforeAnyMechanicalOperation(int flags, int address)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(2, Run(target, Short((byte)Acb4000CommandCode.StartStop, address: address, count: flags)).Status);
        Assert.Equal(0x24, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
        Assert.Equal(0, disk.MechanicalSteps);
        Assert.Equal(0, disk.ParameterAcquisitions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StopAbortsWhenTheDriveDisappearsOrIsReplaced(bool replace)
    {
        var first = new Disk { LandingZone = 127 };
        var active = first;
        var target = CreateTarget(_ => active, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Status);
        SendCommand(target, Short((byte)Acb4000CommandCode.StartStop));
        for (var tick = 0; tick < 100; tick++) Tick(target);
        if (replace) active = new Disk();
        else first.Ready = false;
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(4, Run(target, Short((byte)Acb4000CommandCode.RequestSense, count: 4)).Data[0]);
        Assert.Equal(0, first.Writes);
        Assert.Equal(0, active.Writes);
    }

    [Fact]
    public void ResetStopsParkingWithoutInventingAHeadMovement()
    {
        var disk = new Disk { LandingZone = 127 };
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ModeSense, count: 22)).Status);
        SendCommand(target, Short((byte)Acb4000CommandCode.StartStop));
        for (var tick = 0; tick < 100; tick++) Tick(target);
        target.Reset();
        var cylinder = disk.MechanicalCylinder;
        for (var tick = 0; tick < 500; tick++) Tick(target);
        Assert.Equal(cylinder, disk.MechanicalCylinder);
        Assert.False(target.Bus.Busy);
        Assert.Equal(0, disk.Writes);
    }
}
