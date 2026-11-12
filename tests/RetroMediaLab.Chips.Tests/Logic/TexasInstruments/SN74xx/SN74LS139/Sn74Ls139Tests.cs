using System.Text.Json;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS139;

public sealed class Sn74Ls139Tests
{
    [Fact]
    public void BothHalvesMatchEveryKnownAndUnknownInputCombination()
    {
        var chip = new Sn74Ls139(Sn74Ls139Timing.CharacterizedMaximum);
        foreach (bool? a in new bool?[] { false, true, null })
        foreach (bool? b in new bool?[] { false, true, null })
        foreach (bool? g in new bool?[] { false, true, null })
        {
            chip.SetInputs(0, a, b, g);
            chip.SetInputs(1, b, a, g);
            chip.AdvanceTo(chip.TimeNanoseconds + 38);
            for (int output = 0; output < 8; output++)
            {
                var possible = new HashSet<bool>();
                foreach (bool actualA in Expand(output < 4 ? a : b))
                foreach (bool actualB in Expand(output < 4 ? b : a))
                foreach (bool actualG in Expand(g))
                    possible.Add(actualG || (output & 3) != ((actualA ? 1 : 0) | (actualB ? 2 : 0)));
                var expected = possible.Count == 2 ? LogicPinState.Unknown : possible.Single() ? LogicPinState.High : LogicPinState.Low;
                Assert.Equal(expected, chip.GetOutput(output));
            }
        }
        // Independent simultaneously applied A/B/G levels on both halves.
        for (int inputs = 0; inputs < 64; inputs++)
        {
            chip.SetInputs((byte)inputs, 63);
            chip.AdvanceTo(chip.TimeNanoseconds + 38);
            for (int output = 0; output < 8; output++)
            {
                int local = inputs >> ((output / 4) * 3);
                bool high = (local & 4) != 0 || (output & 3) != (local & 3);
                Assert.Equal(high ? LogicPinState.High : LogicPinState.Low, chip.GetOutput(output));
            }
        }
    }

    [Fact]
    public void SelectAndEnableFollowTheirDistinctPhysicalPaths()
    {
        var chip = new Sn74Ls139(Sn74Ls139Timing.Typical);
        chip.SetInputs(0, false, false, true);
        chip.AdvanceTo(100);
        var trace = new List<(long, int, LogicPinState)>();
        chip.OutputChanged = (output, value) => trace.Add((chip.TimeNanoseconds, output, value));
        try
        {
            chip.SetInputs(0, false, false, false);
            chip.AdvanceTo(121);
            Assert.Equal(new[] { (121L, 0, LogicPinState.Low) }, trace);
            chip.SetInputs(0, true, false, false);
            chip.AdvanceTo(134);
            Assert.Equal((134L, 0, LogicPinState.High), trace[^1]);
            chip.AdvanceTo(146);
            Assert.Equal((146L, 1, LogicPinState.Low), trace[^1]);
            chip.SetInputs(0, false, false, false);
            chip.AdvanceTo(164);
            Assert.Equal((164L, 1, LogicPinState.High), trace[^1]);
            chip.AdvanceTo(168);
            Assert.Equal((168L, 0, LogicPinState.Low), trace[^1]);
            chip.SetInputs(0, false, false, true);
            chip.AdvanceTo(184);
            Assert.Equal((184L, 0, LogicPinState.High), trace[^1]);
            Assert.Equal(6, trace.Count);
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void PendingSelectionRestoresWithoutEdgesAndRejectsAnotherProfile()
    {
        var chip = new Sn74Ls139(Sn74Ls139Timing.CharacterizedMaximum);
        chip.SetInputs(0, 63);
        chip.AdvanceTo(40);
        chip.SetInputs(3, 63);
        chip.AdvanceTo(50);
        var state = JsonSerializer.Deserialize<CombinationalState>(Snapshot(chip))!;
        var restored = new Sn74Ls139(Sn74Ls139Timing.CharacterizedMaximum);
        var expected = new List<(long, int, LogicPinState)>();
        var actual = new List<(long, int, LogicPinState)>();
        chip.OutputChanged = (pin, level) => expected.Add((chip.TimeNanoseconds, pin, level));
        restored.OutputChanged = (pin, level) => actual.Add((restored.TimeNanoseconds, pin, level));
        try
        {
            restored.RestoreState(state);
            Assert.Empty(actual);
            foreach (var invalid in new[] { state with { InputKnown = 128 }, state with { Targets = 0 }, state with { Outputs = ushort.MaxValue } })
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                Assert.Equal(Snapshot(chip), Snapshot(restored));
            }
            Assert.Throws<ArgumentException>(() => new Sn74Ls139(Sn74Ls139Timing.Typical).RestoreState(state));
            chip.AdvanceTo(100); restored.AdvanceTo(100);
            Assert.Equal(expected, actual);
            Assert.Equal(Snapshot(chip), Snapshot(restored));
        }
        finally { chip.OutputChanged = null; restored.OutputChanged = null; }
    }
    private static IEnumerable<bool> Expand(bool? value) => value.HasValue ? [value.Value] : [false, true];
    private static byte[] Snapshot(Sn74Ls139 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
