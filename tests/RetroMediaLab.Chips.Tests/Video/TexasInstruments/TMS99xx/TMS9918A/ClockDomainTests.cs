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
