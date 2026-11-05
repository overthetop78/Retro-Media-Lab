namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void FirstCommandAcquiresPhysicalParametersAndResumesWithTheirActualBlockSize(ushort size)
    {
        var disk = new Disk(size);
        var target = CreateTarget(_ => disk, 2_000_000);
        var result = Run(target, Short(8, count: 1));
        Assert.Equal(0, result.Status);
        Assert.Equal(disk.Bytes[..size], result.Data);
        Assert.Equal(1, disk.ParameterAcquisitions);
        Assert.Equal(32, disk.MechanicalSteps);
        Assert.False(target.LoadingParameters);
        Assert.Equal(0, target.SelectedHead);
        Assert.Equal(0, Run(target, Short(8, count: 1)).Status);
        Assert.Equal(1, disk.ParameterAcquisitions);
    }

    [Fact]
    public void ResetAbandonsThePendingCommandBeforeAnyParameterDataIsAccepted()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(8, count: 1), waitForParameters: false);
        Assert.True(target.LoadingParameters);
        target.Reset();
        for (var tick = 0; tick < 100; tick++) Tick(target);
        Assert.False(target.LoadingParameters);
        Assert.False(target.ReadGate);
        Assert.False(target.Bus.Busy);
        Assert.Equal(0, disk.Reads);
        Assert.Equal(0, disk.ParameterAcquisitions);
    }

    [Fact]
    public void ReplacingTheSupportDuringCalibrationCannotResumeTheOldCommandOnTheNewSupport()
    {
        var first = new Disk();
        var second = new Disk(512);
        var attached = first;
        var target = CreateTarget(_ => attached, 2_000_000);
        SendCommand(target, Short(8, count: 1), waitForParameters: false);
        attached = second;
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(4, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Equal(0, first.Reads);
        Assert.Equal(0, second.Reads);
        var read = Run(target, Short(8, count: 1));
        Assert.Equal(0, read.Status);
        Assert.Equal(512, read.Data.Length);
    }
}
