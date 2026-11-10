using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS165;

public sealed class Sn74Ls165RepresentedTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SamplesStatesAndOrdinaryResumptionMatchBothPhases(bool phase)
    {
        foreach (var data in new byte[] { 0, 1, 0x55, 0x96, 0xfe, 255 })
        foreach (var known in new byte[] { 0, 0x5a, 255 })
        foreach (var serial in new bool?[] { false, true, null })
        foreach (var mode in new[] { 0, 1, 2 })
        for (var count = 0; count <= 8; count++)
        {
            var actual = Create(); var reference = Create();
            actual.RepresentSampledPeriods(count, out var levels, out var knowledge);
            for (var n = 0; n < count; n++)
            {
                if (phase) reference.SetClock(false);
                var sample = reference.SerialOutput;
                Assert.Equal(sample.HasValue, (knowledge & (128 >> n)) != 0);
                if (sample.HasValue) Assert.Equal(sample.Value, (levels & (128 >> n)) != 0);
                reference.SetClock(true); if (!phase) reference.SetClock(false);
            }
            Compare();
            var a = new List<bool?>(); var b = new List<bool?>();
            actual.OutputsChanged = (value, _) => a.Add(value);
            reference.OutputsChanged = (value, _) => b.Add(value);
            foreach (var chip in new[] { actual, reference })
            {
                chip.SetShiftLoad(true); chip.SetClockInhibit(false);
                for (var n = 0; n < 10; n++) { chip.SetClock(!phase); chip.SetClock(phase); }
            }
            Compare(); Assert.Equal(b, a);
            Sn74Ls165 Create()
            {
                var chip = new Sn74Ls165(12_000_000);
                chip.SetSerialInput(serial); chip.SetParallelData(data, known); chip.SetShiftLoad(false);
                chip.SetClock(phase); if (mode != 1) chip.SetShiftLoad(true);
                if (mode == 2) chip.SetClockInhibit(true);
                return chip;
            }
            void Compare()
            {
                Assert.Equal(reference.StoredData, actual.StoredData);
                Assert.Equal(reference.KnownBits, actual.KnownBits);
                Assert.Equal(reference.InputClockCount, actual.InputClockCount);
                Assert.Equal(reference.ShiftClockCount, actual.ShiftClockCount);
                Assert.Equal(reference.GatedClockHigh, actual.GatedClockHigh);
                Assert.Equal(reference.ClockHigh, actual.ClockHigh);
                Assert.Equal(reference.StableClockPeriods, actual.StableClockPeriods);
            }
        }
    }
}
