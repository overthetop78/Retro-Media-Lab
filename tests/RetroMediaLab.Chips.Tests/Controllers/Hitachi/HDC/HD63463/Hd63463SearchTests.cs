using RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;
using System.Buffers.Binary;
namespace RetroMediaLab.Chips.Tests.Controllers.Hitachi.HDC.HD63463;

public sealed partial class Hd63463Tests
{
    [Theory]
    [InlineData(1000000, 10000)]
    [InlineData(2000000, 20000)]
    [InlineData(8000000, 80000)]
    public void MissingIdUsesTheControllerClockAndDocumentedTimeoutBounds(int clock, int quantum)
    {
        using var disk = new Disk();
        var controller = SearchController(disk, clock, 4);
        Issue(controller, 0x40, Transfer(1, 1, 0, 0, 1)); // Head remains on physical cylinder zero.
        var setup = ClockTicks(clock, 150) + ClockTicks(clock, 250) + ClockTicks(clock, 40);
        controller.Advance(4 * quantum - setup);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        Assert.False(controller.Irq);
        controller.Advance(quantum);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(new byte[] { 0, 0x58, 1, 0, 0, 1, 0, 0, 0, 1 }, Results(controller, 10));
        Assert.Equal(0, controller.ReadBlocks(0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(800)]
    public void TimeoutDoesNotScaleWithTheDiskSpeed(int speed)
    {
        using var disk = new Disk();
        var controller = SearchController(disk, 1000000, 1);
        controller.Configure(0, 0, speed);
        Issue(controller, 0x40, Transfer(1, 0, 0, 4, 1));
        controller.Advance(20000 - 56 - 1);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x58, controller.ReadParameter());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UnmatchedLogicalIdsWaitWithoutSelectingAnotherPhysicalTrack(int mismatch)
    {
        using var disk = new Disk();
        var controller = SearchController(disk, 1000000, 1);
        var parameters = Transfer(1, mismatch == 1 ? 1 : mismatch == 3 ? 65535 : 0,
            mismatch == 0 ? 1 : 0, mismatch == 2 ? 4 : 0, 1);
        parameters[1] = 0;
        Issue(controller, 0x40, parameters);
        controller.Advance(1000);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        WaitIdle(controller);
        Assert.Equal(0x58, controller.ReadParameter());
        Assert.Equal(0, controller.ReadBlocks(0));
        Assert.Equal(0, controller.SeekCount(0));
    }

    [Fact]
    public void AValidIdThatPassesAfterTheDeadlineStillTimesOut()
    {
        using var disk = new Disk();
        var controller = new Hd63463(_ => disk, 8000000);
        controller.Configure(0, 0, 100, rotationsPerMinute: 3000);
        controller.Advance(150);
        SpecifySearchController(controller, 8000000, 1);
        RawRotationCommand(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        // Its AM has already passed. The next ID ends at 160500 cycles,
        // beyond the TO=1 upper bound of 160000 cycles from RESET.
        controller.Advance(160000 - 440 - 1);
        Assert.True(controller.Busy);
        controller.Advance(1);
        Assert.Equal(0x58, controller.ReadParameter());
        Assert.False(controller.Drq);
        Assert.Equal(0, controller.ReadBlocks(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AbortOrResetCancelsThePendingTimeoutWithoutALateInterrupt(bool reset)
    {
        using var disk = new Disk();
        var controller = SearchController(disk, 1000000, 1);
        Issue(controller, 0x40, Transfer(1, 1, 0, 0, 1));
        controller.Advance(1000);
        Assert.True(controller.Busy);
        if (reset) { controller.Reset(); controller.Advance(19); }
        else { controller.WriteCommand(0xf0); controller.Advance(38); Assert.Equal(4, controller.ReadParameter()); }
        controller.WriteCommand(8);
        controller.Advance(5);
        controller.Advance(100000);
        Assert.Equal(0, controller.Status);
        Assert.False(controller.Irq);
        Assert.False(controller.Drq);
        Assert.False(disk.HasChanges);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void SpecifyRejectsZeroToEvenWhenTheCylinderHighBitsAreNonzero(byte cylinderHigh)
    {
        using var disk = new Disk();
        var controller = SearchController(disk, 1000000, 1);
        Issue(controller, 0xe8, [0x1f, 0x80, 0, 6, cylinderHigh, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0]);
        Assert.Equal(0x0c, controller.ReadParameter());
    }

    private static Hd63463 SearchController(Disk disk, int clock, byte timeout)
    {
        var controller = new Hd63463(_ => disk, clock);
        controller.Configure(0, 0, 100);
        controller.Advance(ClockTicks(clock, 150));
        SpecifySearchController(controller, clock, timeout);
        return controller;
    }

    private static void SpecifySearchController(Hd63463 controller, int clock, byte timeout)
    {
        byte[] specification = [0x1f, 0x82, 0, 6, (byte)(timeout << 2), 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0];
        for (var offset = 0; offset < specification.Length; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16BigEndian(specification.AsSpan(offset)));
        controller.WriteCommand(0xe8);
        controller.Advance(ClockTicks(clock, 250));
    }

    private static int ClockTicks(int clock, int cycles) => (int)(((long)clock * cycles + 7999999) / 8000000);
}
