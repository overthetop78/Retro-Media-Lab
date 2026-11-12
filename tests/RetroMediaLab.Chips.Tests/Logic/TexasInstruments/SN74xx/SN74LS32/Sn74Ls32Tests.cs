using System.Text.Json;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS32;

public sealed class Sn74Ls32Tests
{
    [Fact]
    public void EveryGateImplementsTheCompleteThreeValuedTruthTable()
    {
        var chip = new Sn74Ls32(new(7, 11));
        var trace = new List<(long Time, int Gate, LogicPinState Level)>();
        chip.OutputChanged = (gate, level) =>
        {
            Assert.Equal(level, chip.GetOutput(gate));
            trace.Add((chip.TimeNanoseconds, gate, level));
        };
        try
        {
            foreach (bool? a in new bool?[] { false, true, null })
            foreach (bool? b in new bool?[] { false, true, null })
            {
                var expected = a == true || b == true ? LogicPinState.High
                    : a == false && b == false ? LogicPinState.Low : LogicPinState.Unknown;
                for (int gate = 0; gate < 4; gate++) chip.SetInputs(gate, a, b);
                chip.AdvanceTo(chip.TimeNanoseconds + 11);
                for (int gate = 0; gate < 4; gate++) Assert.Equal(expected, chip.GetOutput(gate));
                Assert.Null(chip.NextEventNanoseconds);
            }
            Assert.NotEmpty(trace);
            for (int a = 0; a < 16; a++)
            for (int b = 0; b < 16; b++)
            {
                chip.SetInputs((byte)a, 15, (byte)b, 15);
                chip.AdvanceTo(chip.TimeNanoseconds + 11);
                for (int gate = 0; gate < 4; gate++)
                    Assert.Equal(((a | b) & (1 << gate)) == 0 ? LogicPinState.Low : LogicPinState.High, chip.GetOutput(gate));
            }
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void DatesCancellationFeedbackAndSharedFrontArePreserved()
    {
        var chip = new Sn74Ls32(new(7, 11));
        chip.SetInputs(0, 15, 0, 15);
        chip.AdvanceTo(11);
        var trace = new List<(long, int, LogicPinState)>();
        chip.OutputChanged = (gate, level) =>
        {
            trace.Add((chip.TimeNanoseconds, gate, level));
            if (chip.TimeNanoseconds == 18)
            {
                // All four pins from this shared front are resolved before the first callback.
                for (int pin = 0; pin < 4; pin++) Assert.Equal(LogicPinState.High, chip.GetOutput(pin));
                if (gate == 0) chip.SetInputs(0, false, false);
            }
        };
        try
        {
            chip.SetInputs(15, 15, 0, 15);
            chip.AdvanceTo(17);
            Assert.Empty(trace);
            chip.SetInputs(15, 15, 0, 15); // unchanged input cannot postpone the response
            chip.AdvanceTo(18);
            Assert.Equal(4, trace.Count);
            Assert.Equal(29, chip.NextEventNanoseconds);
            chip.AdvanceTo(29);
            Assert.Equal((29L, 0, LogicPinState.Low), trace[^1]);
            chip.SetInputs(0, true, false);
            chip.AdvanceTo(32);
            chip.SetInputs(0, false, false);
            chip.AdvanceTo(100);
            Assert.Equal(5, trace.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(99));
            Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInputs(4, true, false));
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void SnapshotRestoresPendingPropagationAndRejectsMalformedStatesAtomically()
    {
        var chip = new Sn74Ls32(Sn74Ls32Timing.CharacterizedMaximum);
        chip.SetInputs(15, 15, 0, 15);
        chip.AdvanceTo(9);
        var state = JsonSerializer.Deserialize<CombinationalState>(Snapshot(chip))!;
        var restored = new Sn74Ls32(Sn74Ls32Timing.CharacterizedMaximum);
        int notifications = 0;
        restored.OutputChanged = (_, _) => notifications++;
        restored.NextEventNanosecondsChanged = _ => notifications++;
        try
        {
            restored.RestoreState(state);
            Assert.Equal(0, notifications);
            foreach (var invalid in new[]
            {
                state with { Version = 2 }, state with { Part = "other" },
                state with { TimeNanoseconds = 22 }, state with { Targets = 0 },
                state with { InputKnown = 0 }, state with { DueNanoseconds = [] },
                state with { Outputs = ushort.MaxValue }
            })
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                Assert.Equal(Snapshot(chip), Snapshot(restored));
            }
            Assert.Throws<ArgumentException>(() => new Sn74Ls32(Sn74Ls32Timing.Typical).RestoreState(state));
            chip.AdvanceTo(22);
            restored.AdvanceTo(22);
            Assert.Equal(Snapshot(chip), Snapshot(restored));
            Assert.Equal(5, notifications); // four pins, then no next deadline
            state.DueNanoseconds[0] = 99;
            Assert.Null(restored.NextEventNanoseconds);
            chip.AdvanceTo(long.MaxValue);
            var before = Snapshot(chip);
            Assert.Throws<OverflowException>(() => chip.SetInputs(0, false, false));
            Assert.Equal(before, Snapshot(chip));
        }
        finally { restored.OutputChanged = null; restored.NextEventNanosecondsChanged = null; }
    }

    private static byte[] Snapshot(Sn74Ls32 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
