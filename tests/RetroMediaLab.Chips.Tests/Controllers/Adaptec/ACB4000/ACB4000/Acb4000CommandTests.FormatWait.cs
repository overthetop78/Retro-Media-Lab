using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using T = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchTimingConstants;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormatWaitsBeyondTheSearchTimeoutAndCanResumeOrReset(bool reset)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var selected = Acb4000DriveParameters.Default with { Cylinders = 1, Heads = 1 };
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        disk.SuppressIndex = true;
        SendCommand(target, [4, 0, 0, 0, 2, 0]);
        var ticks = (T.IndexWaitTStates + T.IndexSetupTStates) * 2L * 2_000_000 / T.RevisionBSequencerClock;
        for (long tick = 0; tick < ticks; tick++) Tick(target);
        Assert.True(target.Bus.Busy);
        Assert.False(target.Bus.Request);
        Assert.False(target.WriteGate);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0, disk.ParameterWrites);
        if (reset)
        {
            target.Reset();
            Assert.False(target.Bus.Busy);
            Assert.Equal(0, disk.ParameterWrites);
        }
        else
        {
            disk.SuppressIndex = false;
            Assert.Equal(0, ReceiveResult(target).Status);
            Assert.Equal(33, disk.Writes);
            Assert.Equal(1, disk.ParameterWrites);
        }
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 3)]
    public void FormatWaitingForIndexStillRespondsToDriveFailure(bool writeFault, byte sense)
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        var selected = Acb4000DriveParameters.Default with { Cylinders = 1, Heads = 1 };
        Assert.Equal(0, Run(target, Short(0x15, count: 22), EncodeParameters(selected)).Status);
        disk.SuppressIndex = true;
        SendCommand(target, [4, 0, 0, 0, 2, 0]);
        for (var tick = 0; tick < 10_000; tick++) Tick(target);
        if (writeFault) target.SetWriteFault(true);
        else disk.Ready = false;
        Assert.Equal(2, ReceiveResult(target).Status);
        Assert.Equal(sense, Run(target, Short(3, count: 4)).Data[0] & 0x7f);
        Assert.Equal(0, disk.Writes);
        Assert.Equal(0, disk.ParameterWrites);
        Assert.False(target.WriteGate);
    }
}
