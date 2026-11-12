using System.Text.Json;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS257;

public sealed class Sn74Ls257Tests
{
    [Fact]
    public void AllDataPairsSelectAndDisableMatchTheFunctionTable()
    {
        var chip = new Sn74Ls257(Sn74Ls257Timing.CharacterizedMaximum);
        for (int a = 0; a < 16; a++)
        for (int b = 0; b < 16; b++)
        for (int control = 0; control < 4; control++)
        {
            chip.SetInputs((byte)a, 15, (byte)b, 15, (control & 1) != 0, (control & 2) != 0);
            chip.AdvanceTo(chip.TimeNanoseconds + 30);
            int selected = (control & 1) != 0 ? b : a;
            for (int output = 0; output < 4; output++)
                Assert.Equal((control & 2) != 0 ? LogicPinState.HighImpedance
                    : (selected & (1 << output)) != 0 ? LogicPinState.High : LogicPinState.Low, chip.GetOutput(output));
        }
    }

    [Fact]
    public void UnknownInputsAreResolvedOnlyWhenEveryPossibleOutputAgrees()
    {
        var chip = new Sn74Ls257(Sn74Ls257Timing.Typical);
        foreach (bool? a in new bool?[] { false, true, null })
        foreach (bool? b in new bool?[] { false, true, null })
        foreach (bool? select in new bool?[] { false, true, null })
        foreach (bool? disable in new bool?[] { false, true, null })
        {
            chip.SetInputs((byte)(a == true ? 15 : 0), (byte)(a.HasValue ? 15 : 0),
                (byte)(b == true ? 15 : 0), (byte)(b.HasValue ? 15 : 0), select, disable);
            chip.AdvanceTo(chip.TimeNanoseconds + 30);
            var possibilities = new HashSet<LogicPinState>();
            foreach (bool av in Expand(a))
            foreach (bool bv in Expand(b))
            foreach (bool sv in Expand(select))
            foreach (bool gv in Expand(disable))
                possibilities.Add(gv ? LogicPinState.HighImpedance : (sv ? bv : av) ? LogicPinState.High : LogicPinState.Low);
            var expected = possibilities.Count == 1 ? possibilities.Single() : LogicPinState.Unknown;
            for (int output = 0; output < 4; output++) Assert.Equal(expected, chip.GetOutput(output));
        }
    }

    [Fact]
    public void DataSelectionAndDriverTransitionsKeepTheirSpecifiedDelays()
    {
        var chip = new Sn74Ls257(Sn74Ls257Timing.Typical);
        chip.SetInputs(0, 15, 15, 15, false, true);
        chip.AdvanceTo(100);
        var trace = new List<(long, int, LogicPinState)>();
        chip.OutputChanged = (output, value) =>
        {
            trace.Add((chip.TimeNanoseconds, output, value));
            for (int pin = 0; pin < 4; pin++) Assert.Equal(value, chip.GetOutput(pin));
        };
        try
        {
            chip.SetInputs(0, 15, 15, 15, false, false);
            chip.AdvanceTo(119);
            Assert.Equal((119L, 0, LogicPinState.Low), trace[0]);
            chip.SetInputs(15, 15, 0, 15, false, false);
            chip.AdvanceTo(127);
            Assert.Equal((127L, 0, LogicPinState.High), trace[4]);
            chip.SetInputs(15, 15, 0, 15, true, false);
            chip.AdvanceTo(144);
            Assert.Equal((144L, 0, LogicPinState.Low), trace[8]);
            chip.SetInputs(15, 15, 0, 15, true, true);
            chip.AdvanceTo(160);
            Assert.Equal((160L, 0, LogicPinState.HighImpedance), trace[12]);
            chip.SetInputs(15, 15, 0, 15, false, true);
            chip.AdvanceTo(200);
            Assert.Equal(16, trace.Count); // changing select while disabled never drives the bus
            chip.SetInputs(15, 15, 0, 15, false, false);
            chip.AdvanceTo(215);
            Assert.Equal((215L, 0, LogicPinState.High), trace[16]);
            chip.SetInputs(15, 15, 0, 15, false, true);
            chip.AdvanceTo(233);
            Assert.Equal((233L, 0, LogicPinState.HighImpedance), trace[20]);
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void DriverPropagationSurvivesJsonRestoreWithoutFakeEdges()
    {
        var chip = new Sn74Ls257(Sn74Ls257Timing.CharacterizedMaximum);
        chip.SetInputs(15, 15, 0, 15, false, false);
        chip.AdvanceTo(40);
        chip.SetInputs(15, 15, 0, 15, false, true);
        chip.AdvanceTo(50);
        var state = JsonSerializer.Deserialize<CombinationalState>(Snapshot(chip))!;
        var restored = new Sn74Ls257(Sn74Ls257Timing.CharacterizedMaximum);
        int changes = 0;
        restored.OutputChanged = (_, value) => { Assert.Equal(LogicPinState.HighImpedance, value); changes++; };
        try
        {
            restored.RestoreState(state);
            Assert.Equal(0, changes);
            Assert.Throws<ArgumentException>(() => restored.RestoreState(state with { Targets = 0 }));
            Assert.Equal(Snapshot(chip), Snapshot(restored));
            Assert.Throws<ArgumentException>(() => new Sn74Ls257(Sn74Ls257Timing.Typical).RestoreState(state));
            chip.AdvanceTo(70); restored.AdvanceTo(70);
            Assert.Equal(4, changes);
            Assert.Equal(Snapshot(chip), Snapshot(restored));
        }
        finally { restored.OutputChanged = null; }
    }
    private static IEnumerable<bool> Expand(bool? value) => value.HasValue ? [value.Value] : [false, true];
    private static byte[] Snapshot(Sn74Ls257 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
