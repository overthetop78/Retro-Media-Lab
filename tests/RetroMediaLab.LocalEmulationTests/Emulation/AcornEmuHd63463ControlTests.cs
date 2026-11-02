using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Services;
using System.Buffers.Binary;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuHd63463Tests
{
    [Theory]
    [InlineData(1000000, 5, 32, 23, 38)]
    [InlineData(2000000, 10, 63, 45, 75)]
    [InlineData(8000000, 40, 250, 180, 300)]
    public void ControlCommandsUseControllerClocksRatherThanDiskSpeed(
        int clock, int recall, int specify, int outputs, int abort)
    {
        using var disk = new Disk();
        var controller = new Hd63463(_ => disk, clock);
        controller.Advance((int)(((long)clock * 150 + 7999999) / 8000000));
        controller.Configure(0, 0, 0);
        byte[] parameters = [0x1f, 0x80, 0, 6, 16, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0];
        for (var offset = 0; offset < parameters.Length; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16BigEndian(parameters.AsSpan(offset)));
        controller.WriteCommand(0xe8);
        Assert.True(controller.Busy);
        controller.Advance(specify - 1);
        Assert.True(controller.Busy);
        controller.WriteCommand(0x28); // Ignored while Specify is executing.
        controller.Advance(1);
        Assert.False(controller.Busy);
        Assert.False(controller.Irq);
        Assert.Equal(0x40, controller.Status);

        controller.WriteCommand(8);
        controller.Advance(recall - 1);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0, controller.Status);
        controller.WriteParameter(0x0100);
        controller.WriteCommand(0x28);
        Assert.Equal(new byte[] { 0, 0, 1, 0, 0xe0, 0 }, Results(controller, 6));

        controller.WriteCommand(0xf0);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        controller.Advance(outputs - 1);
        Assert.True(controller.Active(0));
        controller.Advance(1);
        Assert.False(controller.Active(0));
        Assert.True(controller.Busy);
        controller.Advance(abort - outputs - 1);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.False(controller.Busy);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(4, controller.ReadParameter());
        Assert.True(controller.Irq);
    }

    [Fact]
    public void AbortStopsParallelSeekAndPreservesSpecifiedDriveGeometry()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, mode: 0x81);
        controller.Configure(0, 50, 100);
        Issue(controller, 0xc0, [1, 0, 0, 2]);
        controller.Advance(10000);
        Assert.False(controller.Busy);
        Assert.True(controller.Active(0));
        controller.WriteCommand(0xf0);
        controller.Advance(38);
        Assert.False(controller.Active(0));
        Issue(controller, 0x10, []);
        controller.Advance(100000);
        Assert.True(controller.Busy); // Canceled seek cannot raise a late POL completion.
        controller.WriteCommand(0xf0);
        controller.Advance(38);
        controller.Configure(0, 0, 0);
        Issue(controller, 0x40, Transfer(1, 2, 0, 1, 1));
        Assert.Equal(disk.Block(17), Read(controller));
        Assert.False(disk.HasChanges);
    }

    [Fact]
    public void AbortBeforeSpecifyCompletesDoesNotInstallItsPendingParameters()
    {
        using var disk = new Disk();
        var controller = new Hd63463(_ => disk, 1000000);
        controller.Advance(19);
        byte[] parameters = [0x1f, 0x80, 0, 6, 16, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0];
        for (var offset = 0; offset < parameters.Length; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16BigEndian(parameters.AsSpan(offset)));
        controller.WriteCommand(0xe8);
        controller.Advance(31);
        controller.WriteCommand(0xf0);
        controller.Advance(38);
        Issue(controller, 0x28, [1, 0]);
        Assert.Equal(0x10, controller.ReadParameter());
    }

    [Theory]
    [InlineData(1000000, 19)]
    [InlineData(2000000, 38)]
    [InlineData(8000000, 150)]
    public void ResetHoldsBusyForInitializationAndIgnoresCommands(int clock, int ticks)
    {
        var controller = new Hd63463(_ => null, clock);
        Assert.Equal(0x80, controller.Status);
        Assert.False(controller.Irq);
        Assert.False(controller.Drq);
        controller.Advance(ticks - 1);
        Assert.True(controller.Busy);
        controller.WriteCommand(8);
        controller.WriteCommand(0xf0);
        controller.WriteCommand(0xe0);
        controller.WriteParameter(0xdead);
        controller.Advance(1);
        Assert.Equal(0, controller.Status);
        Assert.False(controller.Irq);
        Assert.True(controller.DriveOutputsEnabled);
        Assert.Equal(0, controller.ReadParameter());
        controller.WriteCommand(0xe0);
        controller.Advance((int)(((long)clock * 60 + 7999999) / 8000000));
        Assert.False(controller.DriveOutputsEnabled);
    }

    [Fact]
    public void ResetDuringDmaCancelsTheWriteAndRequiresANewSpecify()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk);
        Issue(controller, 0x87, Transfer(1, 0, 0, 0, 1));
        controller.Advance(10000);
        Assert.True(controller.Drq);
        controller.WriteDma(0x1234);
        controller.Reset();
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        Assert.False(controller.Irq);
        controller.WriteDma(0xffff, done: true);
        controller.Advance(100000);
        Assert.False(controller.Busy);
        Assert.False(controller.Active(0));
        Assert.False(disk.HasChanges);
        Assert.False(disk.Disposed);
        Issue(controller, 0x28, [1, 0]);
        Assert.Equal(0x10, controller.ReadParameter());
    }

    [Fact]
    public void ResetCancelsAParallelSeekWithoutALateInterrupt()
    {
        using var disk = new Disk();
        var controller = Controller(_ => disk, mode: 0x81);
        Issue(controller, 0xc0, [1, 0, 0, 2]);
        controller.Advance(100);
        Assert.True(controller.Active(0));
        controller.Reset();
        controller.Advance(100000);
        Assert.Equal(0, controller.Status);
        Assert.False(controller.Irq);
        Assert.False(controller.Active(0));
    }
}
