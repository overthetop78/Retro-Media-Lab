using System.Text.Json;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class ClockDomainTests
{
    [Fact]
    public void VideoRangeResumesAfterDatedInputBatchesWithoutReturningToItsClockDriver()
    {
        foreach (int phase in new[] { 0, 3, 7, 11 })
        {
            var scalar = Create(phase, false);
            var grouped = Create(phase, false);
            var source = new RationalClockSource(17, 1_000_000_000, 21_477_270, 1, true);
            long[] inputs = [source.EdgeNanoseconds(2), source.EdgeNanoseconds(17), 500, 500,
                12345, source.EdgeNanoseconds(2048) + 1];
            Array.Sort(inputs);
            var expected = new List<Tms9918VideoSignal>();
            var actual = new List<Tms9918VideoSignal>();
            var expectedInputs = new List<(long, Tms9918ClockState)>();
            var actualInputs = new List<(long, Tms9918ClockState)>();
            int scalarInput = 0, groupedInput = 0;
            void Receive(Tms9918A chip, int index, List<(long, Tms9918ClockState)> trace)
            {
                chip.SetVramReadData(inputs[index], (byte)(index * 53), (index & 1) != 0);
                trace.Add((inputs[index], chip.ClockState));
            }
            try
            {
                scalar.VideoSignalProduced = expected.Add;
                grouped.VideoSignalProduced = actual.Add;
                grouped.ReceiveMasterClockSource(source);
                grouped.SetClockInputBoundary(inputs[0]);
                grouped.ClockInputBoundaryReached = date =>
                {
                    while (groupedInput < inputs.Length && inputs[groupedInput] == date)
                        Receive(grouped, groupedInput++, actualInputs);
                    grouped.SetClockInputBoundary(groupedInput == inputs.Length ? null : inputs[groupedInput]);
                };
                for (int edge = 1; edge <= 2048; edge++)
                {
                    long date = source.EdgeNanoseconds(edge);
                    while (scalarInput < inputs.Length && inputs[scalarInput] <= date)
                        Receive(scalar, scalarInput++, expectedInputs);
                    scalar.ReceiveMasterClock(date, (edge & 1) != 0);
                }
                while (scalarInput < inputs.Length) Receive(scalar, scalarInput++, expectedInputs);
                Assert.Equal(2048, grouped.AdvanceMasterClockSourceUntil(inputs[^1] + 1));
                Assert.Equal(expected, actual);
                Assert.Equal(expectedInputs, actualInputs);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
                Assert.Throws<ArgumentOutOfRangeException>(() => grouped.SetClockInputBoundary(grouped.TimeNanoseconds - 1));
                grouped.SetClockInputBoundary(grouped.TimeNanoseconds);
                grouped.ClockInputBoundaryReached = _ => { };
                Assert.Throws<InvalidOperationException>(() => grouped.AdvanceMasterClockSourceUntil(grouped.TimeNanoseconds + 1));
            }
            finally
            {
                scalar.VideoSignalProduced = grouped.VideoSignalProduced = null;
                grouped.ClockInputBoundaryReached = null;
            }
        }
    }

    [Fact]
    public void LocalMinimumIsCurrentBeforeCallbacksAndSurvivesRestoreAndMaximumDate()
    {
        var chip = new Tms9918A(new(default, 0, default,
            new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true)), new(120, 10));
        var deadlines = new List<long?>();
        var responses = new List<(long, bool, long?)>();
        try
        {
            chip.NextEventNanosecondsChanged = due =>
            {
                Assert.Equal(due, chip.NextEventNanoseconds);
                deadlines.Add(due);
            };
            chip.CpuOutputsChanged = pins =>
            {
                responses.Add((pins.TimeNanoseconds, pins.DataDriven, chip.NextEventNanoseconds));
                if (pins.DataDriven)
                    chip.ReceiveCpuPins(pins.TimeNanoseconds, new(0, true, true, true, true));
            };
            chip.ReceiveCpuPins(0, new(0, true, true, false, true));
            chip.AdvanceTo(100);
            var saved = chip.CaptureState();
            chip.RestoreState(saved);
            Assert.Equal(120L, chip.NextEventNanoseconds);
            chip.AdvanceTo(130);
            Assert.Equal(new long?[] { 120, 130, null }, deadlines);
            Assert.Equal(new (long, bool, long?)[] { (120, true, null), (130, false, null) }, responses);
            chip.CpuOutputsChanged = null;
            chip.AdvanceTo(long.MaxValue - 120);
            chip.ReceiveCpuPins(long.MaxValue - 120, new(0, true, true, false, true));
            Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
            chip.AdvanceTo(long.MaxValue);
            Assert.Null(chip.NextEventNanoseconds);
            Assert.True(chip.CpuOutputs.DataDriven);
        }
        finally { chip.CpuOutputsChanged = null; chip.NextEventNanosecondsChanged = null; }
    }

    [Fact]
    public void WideRationalTransportAndOrdinaryArithmeticProduceTheSameExactDates()
    {
        var idle = Create(0, false);
        idle.AdvanceTo(long.MaxValue);
        Assert.Equal(long.MaxValue, idle.TimeNanoseconds);
        foreach (long numerator in new[] { 1_000_000_000L, long.MaxValue / 4 })
        {
            var scalar = Create(0, false);
            var grouped = Create(0, false);
            var source = new RationalClockSource(1_000_000_000_000, numerator, numerator / 47, 1, true, 17);
            grouped.ReceiveMasterClockSource(source);
            for (int i = 0; i < 2048; i++)
                scalar.ReceiveMasterClock(source.EdgeNanoseconds(i + 1), (i & 1) == 0);
            Assert.Equal(2048, grouped.AdvanceMasterClockSourceUntil(source.EdgeNanoseconds(2048) + 1));
            Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
        }
    }

    private static Tms9918A Create(int phase, bool pixel) => new(new(
        new(0, 0x20, 0, 0, 0, 0, 0, 3), 0, default,
        new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true),
        new(false, phase % 12, false, pixel, 0, phase % 48, false)),
        Tms9918CpuTiming.SpecifiedMaximum);

    [Fact]
    public void LocalVideoDomainPreservesWholeStateAndDatedSignalsAcrossAllDividerPhases()
    {
        foreach (int phase in Enumerable.Range(0, 48))
        foreach (bool pixel in new[] { false, true })
        {
            var scalar = Create(phase, pixel);
            var grouped = Create(phase, pixel);
            var source = new RationalClockSource(17, 1_000_000_000, 21_477_270, 1, true, 1234567);
            var expected = new List<Tms9918VideoSignal>();
            var actual = new List<Tms9918VideoSignal>();
            var clocks = new List<Tms9918ClockOutput>();
            var pins = new List<Tms9918VramSignal>();
            var gotPins = new List<Tms9918VramSignal>();
            try
            {
                scalar.VideoSignalProduced = expected.Add;
                grouped.VideoSignalProduced = actual.Add;
                scalar.CpuClockChanged = clocks.Add;
                scalar.VramPinsChanged = pins.Add;
                grouped.VramPinsChanged = gotPins.Add;
                grouped.ReceiveMasterClockSource(source);
                var divided = grouped.CpuClockSource!.Value;
                for (int i = 0; i < 2048; i++)
                    scalar.ReceiveMasterClock(source.EdgeNanoseconds(i + 1), (i & 1) == 0);
                int consumed = 0;
                for (int i = 7; i < 2048; i += 37)
                    consumed += grouped.AdvanceMasterClockSourceUntil(source.EdgeNanoseconds(i + 1));
                consumed += grouped.AdvanceMasterClockSourceUntil(source.EdgeNanoseconds(2048) + 1);
                Assert.Equal(2048, consumed);
                Assert.Equal(expected, actual);
                Assert.Equal(pins, gotPins);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
                for (int i = 0; i < clocks.Count; i++)
                {
                    Assert.Equal(clocks[i].TimeNanoseconds, divided.EdgeNanoseconds(i));
                    Assert.Equal(clocks[i].High, (i & 1) == 0 ? divided.FirstEdgeHigh : !divided.FirstEdgeHigh);
                }
            }
            finally
            {
                scalar.VideoSignalProduced = grouped.VideoSignalProduced = null;
                scalar.VramPinsChanged = grouped.VramPinsChanged = null;
                scalar.CpuClockChanged = null;
            }
        }
    }

    [Fact]
    public void InactiveCpuWireTransportMatchesImmediateTransportAfterVideoCatchesUp()
    {
        var scalar = Create(0, false);
        var grouped = Create(0, false);
        var source = new RationalClockSource(0, 1_000_000_000, 21_477_270, 1, true);
        grouped.ReceiveMasterClockSource(source);
        int edge = 1;
        for (int time = 137; time < 10_000; time += 137)
        {
            while (source.EdgeNanoseconds(edge) <= time)
            {
                scalar.ReceiveMasterClock(source.EdgeNanoseconds(edge), (edge & 1) != 0);
                edge++;
            }
            var pins = new Tms9918CpuPins((byte)time, true, (time & 1) != 0, true, true);
            scalar.ReceiveCpuPins(time, pins);
            Assert.True(grouped.TryReceiveInactiveCpuPins(time, pins));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => grouped.TryAdvanceInactiveCpuInterfaceTo(9863));
        Assert.Throws<InvalidOperationException>(() => grouped.CaptureState());
        grouped.AdvanceMasterClockSourceUntil(9865);
        grouped.AdvanceTo(9864);
        Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
        Assert.False(grouped.TryReceiveInactiveCpuPins(9864, new(0, true, true, false, true)));
        var saved = grouped.CaptureState();
        grouped.RestoreState(saved);
        Assert.Null(grouped.CpuClockSource);
    }
}
