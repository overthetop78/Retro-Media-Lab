using System.Text.Json;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class ClockRasterTests
{
    private const long Denominator = 21_477_270;
    private static long Date(long edge) => (long)((Int128)edge * 1_000_000_000 / Denominator);
    private static Tms9918A Create(byte r1 = 0x20, Tms9918CpuTiming? timing = null) => new(
        new(new(0, r1, 0, 0, 0, 0, 0, 3), 0, new(0, false),
            new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true)),
        timing ?? Tms9918CpuTiming.SpecifiedMaximum);

    [Fact]
    public void FractionalTrainsMatchEveryNativePixelAndCpuClockAcrossFrameBoundary()
    {
        const int pixels = 342 * 262;
        const int edges = pixels * 4 + 2;
        var scalar = Create();
        var grouped = Create();
        var expected = new List<Tms9918VideoSignal>();
        var actual = new List<Tms9918VideoSignal>();
        var expectedClock = new List<Tms9918ClockOutput>();
        var actualClock = new List<Tms9918ClockOutput>();
        var expectedGrom = new List<Tms9918ClockOutput>();
        var actualGrom = new List<Tms9918ClockOutput>();
        var frames = new List<long>();
        var irq = new List<Tms9918CpuOutputs>();
        try
        {
            scalar.VideoSignalProduced = expected.Add;
            scalar.CpuClockChanged = expectedClock.Add;
            grouped.VideoSignalProduced = actual.Add;
            grouped.CpuClockChanged = actualClock.Add;
            scalar.GromClockChanged = expectedGrom.Add;
            grouped.GromClockChanged = actualGrom.Add;
            grouped.FrameStarted = frames.Add;
            grouped.CpuOutputsChanged = irq.Add;
            bool high = false;
            for (int i = 0; i < edges; i++) scalar.ReceiveMasterClock(Date(i), high = !high);
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 0, 257));
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 257, 1001));
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 1258, edges - 1258));
            Assert.Equal(expected, actual);
            Assert.Equal(expectedClock, actualClock);
            Assert.Equal(expectedGrom, actualGrom);
            Assert.Equal(edges / 24, actualGrom.Count);
            for (int edge = 0; edge < actualGrom.Count; edge++)
                Assert.Equal(new Tms9918ClockOutput(Date((edge + 1) * 24 - 1), (edge & 1) == 0), actualGrom[edge]);
            Assert.Equal(scalar.ClockState, grouped.ClockState);
            Assert.Equal(scalar.RasterState, grouped.RasterState);
            Assert.Equal(pixels + 1, actual.Count);
            Assert.Equal(2, frames.Count);
            Assert.Equal(Date(pixels * 4 + 1) - Date(1), frames[1] - frames[0]);
            Assert.Equal(pixels * 2 / 3, actualClock.Count(x => x.High));
            Assert.Equal(Date(2), actualClock[0].TimeNanoseconds);
            Assert.Single(irq);
            Assert.False(irq[0].IntHigh);
            Assert.Equal(Date(192 * 342 * 4 + 1), irq[0].TimeNanoseconds);
            var frame = actual.Take(pixels).ToArray();
            Assert.Equal(256 * 192, frame.Count(x => x.Active));
            Assert.Equal(26 * 262, frame.Count(x => x.HorizontalSync));
            Assert.Equal(3 * 342, frame.Count(x => x.VerticalSync));
            Assert.Equal(14 * (262 - 3), frame.Count(x => x.ColorBurst));
            Assert.All(frame, x => Assert.True(x.ColourKnown)); // affichage désactivé
            Assert.Equal(3, frame.First(x => !x.Blanking).Colour);
            Assert.Equal(new Tms9918RasterState(1, 0, 1), grouped.RasterState);
        }
        finally
        {
            scalar.VideoSignalProduced = null;
            scalar.CpuClockChanged = null;
            grouped.VideoSignalProduced = null;
            grouped.CpuClockChanged = null;
            scalar.GromClockChanged = grouped.GromClockChanged = null;
            grouped.FrameStarted = null;
            grouped.CpuOutputsChanged = null;
        }
    }

    [Fact]
    public void GromDividerPreservesAnIndependentSeedAcrossResetAndGroupedBoundaries()
    {
        var scalar = new Tms9918A(new(new(0, 0, 0, 0, 0, 0, 0, 3), 0, default,
            new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true),
            new(false, 0, false, false, 0, 23, true)), Tms9918CpuTiming.SpecifiedMaximum);
        var grouped = new Tms9918A(new(new(0, 0, 0, 0, 0, 0, 0, 3), 0, default,
            new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true),
            new(false, 0, false, false, 0, 23, true)), Tms9918CpuTiming.SpecifiedMaximum);
        var expected = new List<Tms9918ClockOutput>();
        var actual = new List<Tms9918ClockOutput>();
        try
        {
            scalar.GromClockChanged = expected.Add;
            grouped.GromClockChanged = actual.Add;
            scalar.SetReset(0, true); grouped.SetReset(0, true);
            for (int edge = 0; edge < 100; edge++) scalar.ReceiveMasterClock(Date(edge), (edge & 1) == 0);
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 0, 100));
            Assert.Equal(expected, actual);
            Assert.Equal(5, actual.Count);
            Assert.Equal(new Tms9918ClockOutput(0, false), actual[0]);
            Assert.Equal(scalar.ClockState, grouped.ClockState);
        }
        finally { scalar.GromClockChanged = grouped.GromClockChanged = null; }
    }

    [Fact]
    public void CpuDeadlineAtOrBeforeFirstTrainEdgeCanRearmBetweenEdgesWithoutLosingClockState()
    {
        foreach (int firstEdge in new[] { 120, 125, 135 })
        {
            var scalar = Create(timing: new(120, 10));
            var grouped = Create(timing: new(120, 10));
            var expected = new List<(Tms9918CpuOutputs Pins, Tms9918ClockState Clock)>();
            var actual = new List<(Tms9918CpuOutputs Pins, Tms9918ClockState Clock)>();
            void Wire(Tms9918A chip, List<(Tms9918CpuOutputs, Tms9918ClockState)> changes)
            {
                chip.ReceiveCpuPins(0, new(0, true, true, false, true));
                chip.CpuOutputsChanged = pins =>
                {
                    changes.Add((pins, chip.ClockState));
                    if (pins.DataDriven)
                        chip.ReceiveCpuPins(pins.TimeNanoseconds, new(0, true, true, true, true));
                };
            }
            try
            {
                Wire(scalar, expected); Wire(grouped, actual);
                scalar.ReceiveMasterClock(firstEdge, true);
                scalar.ReceiveMasterClock(firstEdge + 50, false);
                grouped.ReceiveMasterClockTrain(new(firstEdge, 50, 1, 0, 2));
                Assert.Equal(expected, actual);
                Assert.Equal(2, actual.Count);
                Assert.Equal(130, actual[1].Pins.TimeNanoseconds);
                Assert.Equal(firstEdge < 130 ? 1 : 0, actual[1].Clock.HalfEdges);
                Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
            }
            finally { scalar.CpuOutputsChanged = grouped.CpuOutputsChanged = null; }
        }
    }

    [Fact]
    public void TrainMaterializesIntermediateMasterStateBeforeCpuOutputCallbackAndPreservesSeeds()
    {
        var scalar = Create(timing: new(120, 65));
        var grouped = Create(timing: new(120, 65));
        var expected = new List<(Tms9918CpuOutputs, Tms9918ClockState)>();
        var actual = new List<(Tms9918CpuOutputs, Tms9918ClockState)>();
        void Wire(Tms9918A chip, List<(Tms9918CpuOutputs, Tms9918ClockState)> output)
        {
            bool started = false;
            chip.CpuClockChanged = edge =>
            {
                if (started) return;
                started = true;
                chip.ReceiveCpuPins(edge.TimeNanoseconds, new(0, true, true, false, true));
            };
            chip.CpuOutputsChanged = pins => output.Add((pins, chip.ClockState));
        }
        try
        {
            Wire(scalar, expected); Wire(grouped, actual);
            bool high = false;
            for (int i = 0; i < 100; i++) scalar.ReceiveMasterClock(Date(i), high = !high);
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 0, 100));
            Assert.Equal(expected, actual);
            Assert.Single(actual);
            Assert.Equal(5, actual[0].Item2.HalfEdges);
            var beforeClock = grouped.ClockState;
            var beforeRaster = grouped.RasterState;
            grouped.ReceiveMasterClock(grouped.TimeNanoseconds, beforeClock.MasterHigh);
            Assert.Equal(beforeClock, grouped.ClockState);
            Assert.Throws<ArgumentOutOfRangeException>(() => grouped.ReceiveMasterClockTrain(
                new(long.MaxValue, 1_000_000_000, Denominator, 1, 2)));
            Assert.Equal(beforeClock, grouped.ClockState);
            Assert.Equal(beforeRaster, grouped.RasterState);
            var resumed = new Tms9918A(new(new(0, 0x20, 0, 0, 0, 0, 0, 3), 0,
                new(0, false), new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true),
                beforeClock, beforeRaster), Tms9918CpuTiming.SpecifiedMaximum);
            resumed.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 100, 100));
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 100, 100));
            Assert.Equal(grouped.ClockState, resumed.ClockState);
            Assert.Equal(grouped.RasterState, resumed.RasterState);
        }
        finally
        {
            scalar.CpuClockChanged = null; scalar.CpuOutputsChanged = null;
            grouped.CpuClockChanged = null; grouped.CpuOutputsChanged = null;
        }
    }

    [Fact]
    public void TextWindowAndResetRetainNativeTimingWithoutClaimingMissingPixelData()
    {
        var chip = Create(0x70); // IE, display, texte
        var samples = new List<Tms9918VideoSignal>();
        try
        {
            chip.VideoSignalProduced = samples.Add;
            chip.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 0, 342 * 4));
            Assert.Equal(240, samples.Count(x => x.Active));
            Assert.Equal(6, samples.First(x => x.Active).Dot);
            Assert.Equal(245, samples.Last(x => x.Active).Dot);
            Assert.All(samples.Where(x => x.Active), x => Assert.False(x.ColourKnown));
            long reset = chip.TimeNanoseconds;
            chip.SetReset(reset, true);
            chip.ReceiveMasterClockTrain(new(0, 1_000_000_000, Denominator, 342 * 4, 100));
            Assert.Equal(default, chip.RasterState);
            Assert.Equal(342, samples.Count);
            Assert.True(chip.CpuOutputs.IntHigh);
            chip.SetReset(chip.TimeNanoseconds, false);
            Assert.Equal((byte)0, chip.RegisterState.R1);
        }
        finally { chip.VideoSignalProduced = null; }
    }
}
