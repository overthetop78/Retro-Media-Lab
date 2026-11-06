using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class ClockTests
{
    [Fact]
    public void IdleClockAdvancesWithoutAnInternalDeadline()
    {
        var chip = new Intel8271();
        var notifications = new List<long?>();
        chip.NextClockEventChanged += notifications.Add;
        chip.AdvanceTo(1_000_000);
        Assert.Equal(1_000_000L, chip.InputClockCount);
        Assert.Null(chip.NextClockEvent);
        Assert.Empty(notifications);
        Assert.Equal(0, chip.Read(0));
        chip.WriteBus(true, false, 0, 0x2c);
        Assert.Equal(1_000_001L, chip.NextClockEvent);
        Assert.Equal(new long?[] { 1_000_001 }, notifications);
        chip.AdvanceTo(1_000_001);
        Assert.Equal(0x10, chip.Read(0));
        Assert.Null(chip.NextClockEvent);
    }

    [Theory]
    [InlineData(4_000_000, 2_000_000)]
    [InlineData(2_000_000, 2_000_000)]
    [InlineData(2_000_000, 4_000_000)]
    public void BufferedRegisterEventsMatchUnitClocking(int inputClock, int controllerClock)
    {
        var grouped = new Intel8271(inputClock, controllerClock);
        var unit = new Intel8271(inputClock, controllerClock);
        foreach (var chip in new[] { grouped, unit })
        {
            chip.WriteBus(true, false, 0, 0x3d);
            chip.WriteBus(true, false, 1, 0x17);
        }
        for (var time = 1; time <= 8; ++time)
        {
            grouped.AdvanceTo(time);
            unit.Tick();
            Assert.Equal(unit.Read(0), grouped.Read(0));
            Assert.Equal(unit.Nmi, grouped.Nmi);
        }
        Assert.Equal(0xc0, grouped.Read(1));
        Assert.Equal(0xc0, unit.Read(1));
    }

    [Fact]
    public void GroupedPositioningPreservesOutputTransitionsAndTheirTimes()
    {
        var grouped = new Intel8271();
        var unit = new Intel8271();
        var groupedEdges = new List<(long Time, byte Outputs)>();
        var unitEdges = new List<(long Time, byte Outputs)>();
        grouped.DriveSignalsChanged += () => groupedEdges.Add((grouped.InputClockCount, grouped.DriveControl));
        unit.DriveSignalsChanged += () => unitEdges.Add((unit.InputClockCount, unit.DriveControl));
        foreach (var chip in new[] { grouped, unit })
        {
            chip.SetReady(0, true);
            chip.SetTrackZero(0, false);
            chip.Write(0, 0x69);
            chip.Write(1, 1);
        }
        grouped.AdvanceTo(100_000);
        for (var cycle = 0; cycle < 100_000; ++cycle) unit.Tick();
        Assert.Equal(unitEdges, groupedEdges);
        Assert.Equal(unit.Read(0), grouped.Read(0));
        Assert.Equal(unit.SeekSteps(0), grouped.SeekSteps(0));
        Assert.Equal(unit.Nmi, grouped.Nmi);
        Assert.Equal(unit.NextClockEvent, grouped.NextClockEvent);
    }
}
