using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class DriveSignalsTests
{
    [Theory]
    [InlineData(4_000_000)] [InlineData(8_000_000)]
    public void SeekSignalsFollowTheNativeSetupPulseAndHoldClocks(int frequency)
    {
        var control = new Upd765DriveControl(_ => 0x20, (_, _) => { }, frequency, frequency);
        control.Specify(0xf1, 2);
        var samples = new List<(long Clock, bool Seek, bool Step, bool Direction)>();
        control.OutputsChanged = () => samples.Add((control.ControllerClocks, control.SeekMode, control.Step, control.Direction));
        control.Seek(6, 1); // Unit 2, head 1.
        var until = 9000;
        control.Advance(until);
        Assert.Equal(3, control.SelectedUnit); Assert.Equal(0, control.SelectedHead);
        Assert.Contains((7840L, false, false, false), samples);
        Assert.Contains((7936L, true, false, false), samples);
        Assert.Contains((7992L, true, false, true), samples);
        Assert.Contains((8000L, true, true, true), samples);
        Assert.Contains((8056L, true, false, true), samples);
        Assert.Contains((8248L, false, false, false), samples);
        Assert.False(control.SignalsBusy);
    }

    [Fact]
    public void FourConcurrentSeeksShareOnePulseBusAndKeepEachUnitsStepRate()
    {
        var pulses = new List<(long Clock, int Unit, int Direction)>();
        Upd765DriveControl? control = null;
        control = new(_ => 0x20, (unit, direction) => pulses.Add((control!.ControllerClocks, unit, direction)));
        control.Specify(0xf1, 2);
        for (byte unit = 0; unit < 4; unit++) control.Seek(unit, 3);
        control.Advance(26000);
        Assert.Equal(12, pulses.Count);
        for (var unit = 0; unit < 4; unit++)
        {
            var own = pulses.Where(pulse => pulse.Unit == unit).ToArray();
            Assert.Equal(new long[] { 8000 + unit * 528, 16000 + unit * 528, 24000 + unit * 528 }, own.Select(pulse => pulse.Clock));
            Assert.All(own, pulse => Assert.Equal(1, pulse.Direction));
            Assert.Equal(3, control.PresentCylinder(unit));
        }
        for (var unit = 0; unit < 4; unit++)
        {
            Assert.True(control.TrySenseInterrupt(out var status, out var cylinder));
            Assert.Equal(0x20 | unit, status); Assert.Equal(3, cylinder);
        }
        Assert.False(control.Irq);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)]
    public void ResetAtEachSignalEdgeCancelsTheRemainingPulseSequence(int edge)
    {
        var control = new Upd765DriveControl(_ => 0x20, (_, _) => { });
        control.Specify(0xf1, 2);
        var count = 0;
        control.OutputsChanged = () => { if (count++ == edge) control.Reset(); };
        control.Seek(0, 3); control.Advance(32000);
        Assert.False(control.SignalsBusy); Assert.False(control.SeekMode); Assert.False(control.Step);
        Assert.False(control.Direction); Assert.False(control.Irq); Assert.Equal(0, control.BusyMask);
    }

    [Fact]
    public void BatchedAndSingleClockExecutionProduceIdenticalPinEdges()
    {
        static (Upd765DriveControl Control, List<(long, int, bool, bool, bool)> Edges) Make()
        {
            var control = new Upd765DriveControl(_ => 0x20, (_, _) => { });
            var edges = new List<(long, int, bool, bool, bool)>();
            control.OutputsChanged = () => edges.Add((control.ControllerClocks, control.SelectedUnit, control.SeekMode, control.Step, control.Direction));
            control.Specify(0xf1, 2); control.Seek(0, 3); control.Seek(1, 2);
            return (control, edges);
        }
        var first = Make(); var second = Make();
        first.Control.Advance(26000);
        for (var clock = 0; clock < 26000; clock++) second.Control.Advance(1);
        Assert.Equal(second.Edges, first.Edges);
    }

    [Fact]
    public void ADataCommandUsesTheReleasedSeekBusForFaultReset()
    {
        var rig = new PinFixture(); Send(rig.Chip, 0x0f, 0, 1);
        Wait(rig.Chip, () => rig.Chip.Irq);
        Assert.True(rig.Chip.Step); Assert.True(rig.Chip.SeekMode);
        Send(rig.Chip, 8); Result(rig.Chip, 2);
        Send(rig.Chip, 0x4a, 0);
        Assert.True(rig.Chip.FaultReset); Assert.False(rig.Chip.SeekMode); Assert.False(rig.Chip.Step);
    }

    [Fact]
    public void SenseStatusSamplesReadPinsThenTheTwoSeekModePins()
    {
        Upd765DriveControl? control = null;
        var samples = new List<(long Clock, int Unit, bool Seek)>();
        control = new(unit =>
        {
            if (control is null) return 0;
            samples.Add((control.ControllerClocks, unit, control.SeekMode));
            return control.SeekMode ? (byte)0x18 : (byte)0xe0;
        }, (_, _) => { });
        samples.Clear(); byte result = 0;
        control.DriveStatusCompleted = value => result = value;
        control.SenseDriveStatus(6);
        Assert.Equal(2, control.SelectedUnit); Assert.Equal(1, control.SelectedHead);
        Assert.False(control.SeekMode); control.Advance(29); Assert.Equal(0, result);
        control.Advance(1); Assert.Equal(0xfe, result);
        Assert.Equal(new[] { (10L, 2, false), (20L, 2, true), (30L, 2, true) }, samples);
        Assert.False(control.SeekMode); Assert.False(control.SignalsBusy);
    }

    [Fact]
    public void SenseStatusWaitsForTheCurrentSeekPulseAndDoesNotProduceAnExtraStep()
    {
        var steps = 0;
        var control = new Upd765DriveControl(_ => 0x38, (_, _) => steps++);
        control.Specify(0xf1, 2); control.Seek(0, 1); control.Advance(8000);
        Assert.True(control.Step); byte result = 0;
        control.DriveStatusCompleted = value => result = value;
        control.SenseDriveStatus(2); control.Advance(367); Assert.Equal(0, result);
        control.Advance(31); Assert.Equal(0x3a, result); Assert.Equal(1, steps);
        Assert.False(control.Step); Assert.False(control.SignalsBusy);
    }

    [Fact]
    public void ResetDuringSenseStatusCancelsItsResultAndMuxChanges()
    {
        var control = new Upd765DriveControl(_ => 0xf8, (_, _) => { });
        var results = new List<byte>(); control.DriveStatusCompleted = results.Add;
        control.SenseDriveStatus(3); control.Advance(10); Assert.True(control.SeekMode);
        control.Reset(); control.Advance(1000);
        Assert.Empty(results); Assert.False(control.SeekMode); Assert.False(control.SignalsBusy);
    }
    [Theory]
    [InlineData(4)] [InlineData(20)]
    public void ReadyScanSelectsEachPhysicalUnitBeforeSamplingItsInput(int responseClocks)
    {
        Upd765DriveControl? control = null;
        var samples = new List<(long Clock, int Unit, int Selected, bool Seek)>();
        control = new(unit =>
        {
            if (control is not null) samples.Add((control.ControllerClocks, unit, control.SelectedUnit, control.SeekMode));
            return 0x20;
        }, (_, _) => { }, unitResponseClocks: responseClocks);
        control.Specify(0xf1, 2); samples.Clear();
        control.Advance(8192);
        Assert.Equal(4, samples.Count);
        for (var unit = 0; unit < 4; unit++)
            Assert.Equal((8192L - (3 - unit) * 120, unit, unit, false), samples[unit]);
        control.Advance(8192);
        Assert.Equal(8, samples.Count);
        for (var unit = 0; unit < 4; unit++)
            Assert.Equal(samples[unit].Clock + 8192, samples[unit + 4].Clock);
    }

    [Fact]
    public void DisablingPollingCancelsAnInFlightScanBeforeAnyFurtherSample()
    {
        var samples = 0;
        var control = new Upd765DriveControl(_ => { samples++; return 0x20; }, (_, _) => { });
        control.Specify(0xf1, 2); control.Advance(7830);
        Assert.True(control.SignalsBusy); samples = 0;
        control.PollingEnabled = false; control.Advance(20000);
        Assert.Equal(0, samples); Assert.False(control.SignalsBusy);
        Assert.Equal(long.MaxValue, control.NextEventTicks);
    }

    [Fact]
    public void ResetFromAReadyScanSelectionCancelsAllSamplesAndInterrupts()
    {
        var samples = 0;
        var control = new Upd765DriveControl(_ => { samples++; return 0x20; }, (_, _) => { });
        control.Specify(0xf1, 2);
        control.OutputsChanged = () => { if (control.SignalsBusy) control.Reset(); };
        var before = samples; control.Advance(10000);
        // RESET snapshots four input pins but does not complete the cancelled scan.
        Assert.Equal(before + 4, samples); Assert.False(control.SignalsBusy); Assert.False(control.Irq);
    }

    [Fact]
    public void CommandParametersSuspendPollingUntilTheCompleteCommandAndResultHaveBeenConsumed()
    {
        var chip = new Upd765(_ => 0x20, (_, _) => { });
        Specify(chip, false); chip.Advance(7828); Assert.Equal(0, chip.Unit);
        chip.WriteFifo(4); chip.Advance(20000);
        Assert.False(chip.Irq); Assert.Equal(0, chip.Unit);
        chip.WriteFifo(3); chip.Advance(96 + 30);
        Assert.Equal(0x23, chip.ReadFifo());
        chip.Advance(20000); Assert.False(chip.Irq);
    }

    [Fact]
    public void CmosSenseStatusWaitsForTheLongerUnitResponseTime()
    {
        var control = new Upd765DriveControl(_ => 0x38, (_, _) => { }, unitResponseClocks: 20);
        byte result = 0; control.DriveStatusCompleted = value => result = value;
        control.SenseDriveStatus(3); control.Advance(59); Assert.Equal(0, result);
        control.Advance(1); Assert.Equal(0x3b, result);
    }

    [Fact]
    public void SenseInterruptDuringResetPollingPreservesTheRemainingReadyReportsWithoutDuplicates()
    {
        var chip = new Upd765(_ => 0x20, (_, _) => { });
        chip.SetReset(true); chip.SetReset(false);
        chip.Advance(7832); Assert.True(chip.Irq);
        Send(chip, 8); Assert.Equal(new byte[] { 0xc0, 0 }, Result(chip, 2));
        chip.Advance(8192);
        for (var unit = 1; unit < 4; unit++)
        {
            Send(chip, 8); Assert.Equal(new byte[] { (byte)(0xc0 | unit), 0 }, Result(chip, 2));
        }
        Assert.False(chip.Irq);
        Send(chip, 8); Assert.Equal(0x80, chip.ReadFifo());
    }

    [Theory]
    [InlineData(4_000_000)] [InlineData(8_000_000)]
    public void PollingSelectsUnitsForOneHundredTwentyClocksWithoutSteppingIdleDrives(int clock)
    {
        var steps = 0;
        var control = new Upd765DriveControl(_ => 0x20, (_, _) => steps++, clock, clock);
        var selections = new List<(long Clock, int Unit)>();
        control.OutputsChanged = () =>
        {
            if (control.SignalsBusy) selections.Add((control.ControllerClocks, control.SelectedUnit));
        };
        control.Specify(0xf1, 2); control.Advance(8307);
        Assert.Equal(new[] { (7828L, 0), (7948L, 1), (8068L, 2), (8188L, 3) }, selections);
        Assert.True(control.SignalsBusy); control.Advance(1); Assert.False(control.SignalsBusy);
        Assert.Equal(0, steps); Assert.False(control.SeekMode); Assert.False(control.Step);
    }

    [Fact]
    public void SeekPreparationPreemptsAnIdlePollingSlotWithoutDelayingTheStep()
    {
        var pulses = new List<long>();
        Upd765DriveControl? control = null;
        control = new(_ => 0x20, (_, _) => pulses.Add(control!.ControllerClocks));
        control.Specify(0xf1, 2); control.Seek(2, 1);
        control.Advance(7832); Assert.False(control.SeekMode);
        control.Advance(8); Assert.Equal(2, control.SelectedUnit);
        control.Advance(160); Assert.Equal(new[] { 8000L }, pulses);
    }

    [Fact]
    public void IdleClockShortcutPreservesElapsedNativeTimeWithoutSamplingOrEmittingPins()
    {
        var samples = 0; var edges = 0;
        var control = new Upd765DriveControl(_ => { samples++; return 0x20; }, (_, _) => { });
        control.OutputsChanged = () => edges++;
        var before = samples; control.Advance(12345);
        Assert.Equal(12345, control.ControllerClocks); Assert.Equal(before, samples); Assert.Equal(0, edges);
        control.Specify(0xf1, 2); control.Seek(0, 1); control.Advance(8000);
        Assert.True(control.Step); Assert.Equal(20345, control.ControllerClocks);
    }

    [Fact]
    public void ResetDuringTheSeekInputSampleCancelsItsPendingState()
    {
        Upd765DriveControl? control = null; var armed = false; var steps = 0;
        control = new(_ => { if (armed) { armed = false; control!.Reset(); } return 0x20; }, (_, _) => steps++);
        armed = true; Assert.False(control.Seek(0, 3)); armed = false;
        control.Advance(40000); Assert.Equal(0, steps); Assert.Equal(0, control.BusyMask); Assert.False(control.Irq);
    }

    [Fact]
    public void ResetDuringTheTrackZeroSampleAfterAStepDoesNotRecreateAnInterrupt()
    {
        Upd765DriveControl? control = null; var steps = 0; var reset = false;
        control = new(_ =>
        {
            if (steps == 0) return 0x20;
            if (!reset) { reset = true; control!.Reset(); }
            return 0x30;
        }, (_, _) => steps++);
        control.Specify(0xf1, 2); control.Recalibrate(0); control.Advance(8000);
        Assert.Equal(1, steps); Assert.False(control.Irq); Assert.Equal(0, control.BusyMask);
        Assert.False(control.SignalsBusy); Assert.False(control.Step);
    }

}
