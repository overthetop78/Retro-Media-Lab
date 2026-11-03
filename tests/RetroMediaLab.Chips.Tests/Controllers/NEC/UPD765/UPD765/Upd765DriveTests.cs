using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class Upd765DriveTests
{
    [Theory]
    [InlineData(8000000, 0xb3, 0x0a, 5, 48, 10)]
    [InlineData(4000000, 0xb3, 0x0a, 10, 96, 20)]
    [InlineData(8000000, 0xf0, 0x00, 1, 256, 256)]
    [InlineData(4000000, 0x00, 0x00, 32, 512, 512)]
    public void ResetPreservesAllThreeSpecifyTimingParameters(int clock, byte rate, byte load,
        int stepTicks, int unloadTicks, int loadTicks)
    {
        var pins = new Pins();
        var control = pins.Control(clock);
        control.Specify(rate, load);
        control.Reset();
        Assert.Equal(unloadTicks, control.HeadUnloadTicks);
        Assert.Equal(loadTicks, control.HeadLoadTicks);
        control.Seek(0, 1);
        control.Advance(stepTicks - 1);
        Assert.Empty(pins.Steps);
        control.Advance(1);
        Assert.Single(pins.Steps);
        Assert.Equal(1, pins.Position[0]);
    }

    [Theory]
    [InlineData(8000000, 1)]
    [InlineData(4000000, 2)]
    public void SpecifyStepRateUsesTheControllerClockAndBoardClock(int controllerClock, int stepTicks)
    {
        var drive = new Pins();
        var control = drive.Control(controllerClock);
        control.Specify(0xf1, 3);
        Assert.True(control.NonDma);
        Assert.Equal(2 * stepTicks, control.HeadLoadTicks);
        Assert.Equal(16 * stepTicks, control.HeadUnloadTicks);
        Assert.True(control.Seek(0, 3));
        control.Advance(stepTicks - 1);
        Assert.Equal(0, drive.Position[0]);
        control.Advance(1);
        Assert.Equal(1, drive.Position[0]);
        control.Advance(2 * stepTicks);
        Assert.Equal(3, drive.Position[0]);
        Assert.Equal(1, control.BusyMask);
        Assert.True(control.TrySenseInterrupt(out var status, out var cylinder));
        Assert.Equal(0x20, status);
        Assert.Equal(3, cylinder);
        Assert.Equal(0, control.BusyMask);
    }

    [Fact]
    public void CoarseAndSingleClockAdvancesProduceTheSamePhysicalSteps()
    {
        var first = new Pins();
        var second = new Pins();
        var batched = first.Control();
        var stepped = second.Control();
        batched.Specify(0xd1, 2);
        stepped.Specify(0xd1, 2);
        batched.Seek(0, 11);
        stepped.Seek(0, 11);
        batched.Seek(1, 4);
        stepped.Seek(1, 4);
        batched.Advance(33);
        for (var tick = 0; tick < 33; tick++) stepped.Advance(1);
        Assert.Equal(first.Steps, second.Steps);
        Assert.Equal(first.Position, second.Position);
        Assert.Equal(11, batched.PresentCylinder(0));
        Assert.Equal(11, stepped.PresentCylinder(0));
    }

    [Fact]
    public void ParallelSeeksCompleteIndependentlyAndRequireTheirOwnAcknowledgment()
    {
        var pins = new Pins();
        var control = pins.Control();
        control.Specify(0xf1, 2);
        control.Seek(4, 5); // Head 1, unit 0.
        control.Seek(1, 2);
        control.Advance(3);
        Assert.Equal(3, control.BusyMask);
        Assert.True(control.Irq);
        Assert.True(control.TrySenseInterrupt(out var first, out var firstCylinder));
        Assert.Equal(0x21, first);
        Assert.Equal(2, firstCylinder);
        Assert.Equal(1, control.BusyMask);
        Assert.False(control.Irq);
        Assert.False(control.Seek(0, 8));
        control.Advance(3);
        Assert.True(control.TrySenseInterrupt(out var second, out var secondCylinder));
        Assert.Equal(0x24, second);
        Assert.Equal(5, secondCylinder);
        Assert.Equal(0, control.BusyMask);
        Assert.False(control.TrySenseInterrupt(out var invalid, out _));
        Assert.Equal(0x80, invalid);
    }

    [Fact]
    public void RecalibrateUsesThePhysicalSensorEvenWhenTheRememberedCylinderIsZero()
    {
        var pins = new Pins();
        pins.Position[0] = 9;
        var control = pins.Control();
        control.Specify(0xf1, 2);
        Assert.Equal(0, control.PresentCylinder(0));
        control.Recalibrate(0);
        control.Advance(8);
        Assert.False(control.Irq);
        Assert.Equal(1, pins.Position[0]);
        control.Advance(1);
        Assert.Equal(0, pins.Position[0]);
        Assert.Equal(9, pins.Steps.Count);
        Assert.All(pins.Steps, step => Assert.Equal(-1, step.Direction));
        Assert.True(control.TrySenseInterrupt(out var status, out var cylinder));
        Assert.Equal(0x20, status);
        Assert.Equal(0, cylinder);
    }

    [Fact]
    public void StuckTrackZeroSensorTerminatesAfter77StepsWithEquipmentCheck()
    {
        var pins = new Pins { TrackZeroBroken = true };
        var control = pins.Control();
        control.Specify(0xf1, 2);
        control.Seek(0, 5);
        control.Advance(5);
        control.TrySenseInterrupt(out _, out _);
        pins.Steps.Clear();
        control.Recalibrate(0);
        Assert.Equal(0, control.PresentCylinder(0));
        control.Advance(76);
        Assert.False(control.Irq);
        control.Advance(1);
        Assert.Equal(77, pins.Steps.Count);
        Assert.True(control.TrySenseInterrupt(out var status, out var cylinder));
        Assert.Equal(0x70, status);
        Assert.Equal(0, cylinder);
    }

    [Fact]
    public void ReadyLossDuringSeekStopsPulsesWithoutDuplicatingTheCauseAsAPollEvent()
    {
        var pins = new Pins();
        var control = pins.Control();
        control.Specify(0xf1, 2);
        control.Seek(2, 6);
        control.Advance(2);
        pins.Ready[2] = false;
        control.Advance(1);
        Assert.Equal(2, pins.Position[2]);
        Assert.True(control.TrySenseInterrupt(out var status, out var cylinder));
        Assert.Equal(0x6a, status);
        Assert.Equal(2, cylinder);
        Assert.False(control.TrySenseInterrupt(out _, out _));
        control.Advance(100);
        Assert.Equal(2, pins.Position[2]);
    }

    [Fact]
    public void ReadyPollingReportsBothEdgesAndSenseDriveStatusKeepsTheSelectedHeadAndUnit()
    {
        var pins = new Pins();
        var control = pins.Control();
        control.Specify(0xf1, 2);
        byte status = 0;
        control.DriveStatusCompleted = value => status = value;
        control.SenseDriveStatus(5); control.Advance(1);
        Assert.Equal(0x3d, status);
        pins.Ready[1] = false;
        control.Advance(2); // At 1 kHz, two owner ticks cover the complete 1.024 ms scan period.
        Assert.True(control.TrySenseInterrupt(out var absent, out _));
        Assert.Equal(0xc9, absent);
        pins.Ready[1] = true;
        control.Advance(2); // At 1 kHz, two owner ticks cover the complete 1.024 ms scan period.
        Assert.True(control.TrySenseInterrupt(out var present, out _));
        Assert.Equal(0xc1, present);
        Assert.Equal(0, control.BusyMask);
    }

    [Fact]
    public void NotReadyAtCommandStartReportsOneFailureWithoutAnyStep()
    {
        var pins = new Pins();
        var control = pins.Control();
        control.Specify(0xf1, 2);
        pins.Ready[0] = false;
        control.Seek(0, 7);
        control.Advance(100);
        Assert.Empty(pins.Steps);
        Assert.True(control.TrySenseInterrupt(out var status, out _));
        Assert.Equal(0x68, status);
        Assert.False(control.TrySenseInterrupt(out _, out _));
        Assert.False(control.Irq);
    }

    [Fact]
    public void ResetCancelsSeeksWithoutMovingThePhysicalDriveOrClearingItsRememberedPosition()
    {
        var pins = new Pins();
        var control = pins.Control();
        control.Specify(0xf1, 2);
        control.Seek(0, 8);
        control.Advance(3);
        control.Reset();
        control.Advance(100);
        Assert.Equal(3, pins.Position[0]);
        Assert.Equal(3, control.PresentCylinder(0));
        Assert.Equal(0, control.BusyMask);
        Assert.False(control.Irq);
        Assert.False(control.NonDma);
    }

    [Theory]
    [InlineData(8_000_000, 8_000_000)]
    [InlineData(8_000_000, 4_000_000)]
    [InlineData(1_000_000, 8_000_000)]
    [InlineData(3_000_001, 8_000_000)]
    public void ReadyChangesAreSampledAtTheControllerClockBoundary(int boardClock, int chipClock)
    {
        var ready = true;
        var control = new Upd765DriveControl(_ => ready ? (byte)0x20 : (byte)0,
            (_, _) => { }, boardClock, chipClock);
        control.Specify(0xf1, 2);
        ready = false;
        var sample = ((long)boardClock * 8192 + chipClock - 1) / chipClock;
        var firstSample = ((long)boardClock * (8192 - 360) + chipClock - 1) / chipClock;
        var start = ((long)boardClock * (8192 - 364) + chipClock - 1) / chipClock;
        Assert.Equal(start, control.NextEventTicks);
        control.Advance((int)firstSample - 1);
        Assert.False(control.Irq);
        control.Advance((int)(sample - firstSample + 1));
        Assert.True(control.Irq);
        for (var unit = 0; unit < 4; unit++)
        {
            Assert.True(control.TrySenseInterrupt(out var status, out var cylinder));
            Assert.Equal(0xc8 | unit, status);
            Assert.Equal(0, cylinder);
        }
        Assert.False(control.Irq);
    }

    [Fact]
    public void AReadyPulseBetweenSamplesDoesNotCreateAPolledInterrupt()
    {
        var ready = true;
        var control = new Upd765DriveControl(_ => ready ? (byte)0x20 : (byte)0,
            (_, _) => { });
        control.Specify(0xf1, 2);
        ready = false;
        control.Advance(4096);
        ready = true;
        control.Advance(4096);
        Assert.False(control.Irq);
    }

    [Theory]
    [InlineData(8_000_000)]
    [InlineData(3_000_001)]
    [InlineData(1000)]
    [InlineData(1)]
    public void PollingKeepsTheFractionAcrossManyPeriods(int boardClock)
    {
        var batchedSamples = new List<byte>();
        var steppedSamples = new List<byte>();
        var batched = new Upd765DriveControl(unit => { batchedSamples.Add((byte)unit); return 0x20; },
            (_, _) => { }, boardClock);
        var stepped = new Upd765DriveControl(unit => { steppedSamples.Add((byte)unit); return 0x20; },
            (_, _) => { }, boardClock);
        batched.Specify(0xf1, 2);
        stepped.Specify(0xf1, 2);
        batchedSamples.Clear();
        steppedSamples.Clear();
        var duration = Math.Max(1, (int)((long)boardClock * 8192 * 12 / 8_000_000));
        batched.Advance(duration);
        for (var index = 0; index < duration; index++) stepped.Advance(1);
        Assert.Equal(steppedSamples, batchedSamples);
        Assert.Equal(stepped.NextEventTicks, batched.NextEventTicks);
        Assert.InRange(batched.NextEventTicks, 1, long.MaxValue);
    }

    [Fact]
    public void ResetReleaseReportsOnlyTheReadyDrivesOnceWithoutSpecify()
    {
        var control = new Upd765DriveControl(unit => unit is 1 or 3 ? (byte)0x20 : (byte)0,
            (_, _) => { });
        control.ReleaseReset();
        control.Advance(7951);
        Assert.False(control.Irq);
        control.Advance(241);
        Assert.True(control.TrySenseInterrupt(out var first, out _));
        Assert.Equal(0xc1, first);
        Assert.True(control.TrySenseInterrupt(out var second, out _));
        Assert.Equal(0xc3, second);
        control.Advance(81920);
        Assert.False(control.Irq);
        Assert.False(control.TrySenseInterrupt(out _, out _));
    }

    [Fact]
    public void ResetCancelsTheDeferredReadyReport()
    {
        var control = new Upd765DriveControl(_ => 0x20, (_, _) => { });
        control.ReleaseReset();
        control.Advance(8191);
        control.Reset();
        control.Advance(81920);
        Assert.False(control.Irq);
    }

    [Fact]
    public void ResetInsideAStepCallbackCannotResumeTheCancelledSeek()
    {
        Upd765DriveControl? control = null;
        var steps = 0;
        control = new(_ => 0x20, (_, _) => { steps++; control!.Reset(); }, boardClockHz: 1000);
        control.Specify(0xf1, 2);
        control.Seek(0, 3);
        control.Advance(100);
        Assert.Equal(1, steps);
        Assert.Equal(0, control.BusyMask);
        Assert.Equal(0, control.PresentCylinder(0));
        Assert.False(control.Irq);
    }

    private sealed class Pins
    {
        internal readonly int[] Position = new int[4];
        internal readonly bool[] Ready = [true, true, true, true];
        internal readonly List<(int Unit, int Direction)> Steps = [];
        internal bool TrackZeroBroken;

        internal Upd765DriveControl Control(int controllerClock = 8000000) => new(Status, Step,
            boardClockHz: 1000, controllerClockHz: controllerClock);

        private byte Status(int unit) => (byte)((Ready[unit] ? 0x20 : 0) | 0x08
            | (!TrackZeroBroken && Position[unit] == 0 ? 0x10 : 0));

        private void Step(int unit, int direction)
        {
            Position[unit] = Math.Max(0, Position[unit] + direction);
            Steps.Add((unit, direction));
        }
    }
}
