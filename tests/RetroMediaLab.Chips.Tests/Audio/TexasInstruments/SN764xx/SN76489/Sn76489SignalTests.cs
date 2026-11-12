using System.Text.Json;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Contracts;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class Sn76489SignalTests
{
    [Fact]
    public void ReadyDelayCancellationAndClockedCompletionKeepTheirDates()
    {
        var chip = new Sn76489(150);
        var edges = new List<(long Time, long Clocks, bool Ready)>();
        var deadlines = new List<long?>();
        chip.ReadyChanged = ready =>
        {
            Assert.Equal(ready, chip.Ready);
            edges.Add((chip.TimeNanoseconds, chip.InputClockCount, ready));
        };
        chip.NextEventNanosecondsChanged = deadlines.Add;
        try
        {
            chip.SetBus(100, 0x90, true, true);
            Assert.True(chip.Ready);
            Assert.Equal(250, chip.NextEventNanoseconds);
            chip.AdvanceTo(249);
            Assert.Empty(edges);
            chip.AdvanceTo(250);
            Assert.Equal(new[] { (250L, 0L, false) }, edges);
            for (int clock = 0; clock < 32; clock++)
            {
                long time = 300 + clock * 280;
                chip.ReceiveClock(time, true);
                chip.ReceiveClock(time, true);
                chip.ReceiveClock(time + 140, false);
            }
            Assert.Equal((8980L, 32L, true), edges[1]);
            Assert.Equal(2, edges.Count);
            chip.SetBus(10_000, 0, false, false);
            chip.SetBus(10_000, 0, true, false);
            chip.SetBus(10_100, 0, false, false);
            chip.AdvanceTo(11_000);
            Assert.Equal(2, edges.Count);
            Assert.Null(chip.NextEventNanoseconds);
            chip.SetBus(11_000, 0, true, false);
            chip.AdvanceTo(11_150);
            chip.SetBus(11_160, 0, false, false);
            Assert.Equal((11_160L, 32L, true), edges[^1]);
            Assert.Equal(4, edges.Count);
            Assert.Equal(new long?[] { 250, null, 10_150, null, 11_150, null }, deadlines);
        }
        finally { chip.ReadyChanged = null; chip.NextEventNanosecondsChanged = null; }
    }

    [Fact]
    public void PendingReadyAndClockPhaseRestoreWithoutNotificationsOrPartialMutation()
    {
        var original = new Sn76489(150);
        var restored = new Sn76489(150);
        original.SetBus(100, 0x85, true, true);
        original.ReceiveClock(150, true);
        var state = JsonSerializer.Deserialize<Sn76489State>(Snapshot(original))!;
        var expected = new List<(long, bool)>();
        var actual = new List<(long, bool)>();
        original.ReadyChanged = ready => expected.Add((original.TimeNanoseconds, ready));
        restored.ReadyChanged = ready => actual.Add((restored.TimeNanoseconds, ready));
        try
        {
            restored.RestoreState(state);
            Assert.Empty(actual);
            foreach (var invalid in new[]
            {
                state with { TimeNanoseconds = -1 },
                state with { ReadyDueNanoseconds = 149 },
                state with { ReadyDueNanoseconds = null },
                state with { TransferClocks = 33 },
                state with { Output = -1 },
                state with { ReadyAssertDelayNanoseconds = 0 }
            })
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                Assert.Equal(Snapshot(original), Snapshot(restored));
            }
            Assert.Throws<ArgumentException>(() => new Sn76489().RestoreState(state));
            for (int clock = 1; clock < 160; clock++)
            {
                long time = 150 + clock * 280;
                original.ReceiveClock(time - 140, false);
                restored.ReceiveClock(time - 140, false);
                original.ReceiveClock(time, true);
                restored.ReceiveClock(time, true);
            }
            Assert.Equal(expected, actual);
            Assert.Equal(Snapshot(original), Snapshot(restored));
        }
        finally { original.ReadyChanged = null; restored.ReadyChanged = null; }
    }

    [Fact]
    public void ReadyFeedbackCanReleaseAndRearmAtTheSameDate()
    {
        var chip = new Sn76489(150);
        var trace = new List<(long, bool)>();
        chip.ReadyChanged = ready =>
        {
            trace.Add((chip.TimeNanoseconds, ready));
            if (!ready && chip.TimeNanoseconds == 150)
            {
                chip.SetBus(150, 0, false, false);
                chip.SetBus(150, 0, true, false);
            }
        };
        try
        {
            chip.SetBus(0, 0, true, false);
            chip.AdvanceTo(400);
            Assert.Equal(new[] { (150L, false), (150L, true), (300L, false) }, trace);
            Assert.Equal(400, chip.TimeNanoseconds);
            Assert.False(chip.Ready);
        }
        finally { chip.ReadyChanged = null; }
    }

    [Fact]
    public void GroupedClocksPublishAudioAndReadyFromTheSameResolvedState()
    {
        var scalar = new Sn76489();
        var grouped = new Sn76489();
        var expected = new List<(long, int, bool)>();
        var actual = new List<(long, int, bool)>();
        scalar.OutputChanged = _ => expected.Add((scalar.InputClockCount, scalar.Output, scalar.Ready));
        scalar.ReadyChanged = _ => expected.Add((scalar.InputClockCount, scalar.Output, scalar.Ready));
        grouped.OutputChanged = _ => actual.Add((grouped.InputClockCount, grouped.Output, grouped.Ready));
        grouped.ReadyChanged = _ => actual.Add((grouped.InputClockCount, grouped.Output, grouped.Ready));
        try
        {
            for (int value = 0; value < 256; value++)
            {
                scalar.SetBus((byte)value, true, true);
                grouped.SetBus((byte)value, true, true);
                for (int clock = 0; clock < 79; clock++) scalar.Tick();
                grouped.AdvanceClocks(79);
                scalar.SetBus(0, false, false);
                grouped.SetBus(0, false, false);
            }
            Assert.Equal(expected, actual);
            Assert.Equal(Snapshot(scalar), Snapshot(grouped));
        }
        finally
        {
            scalar.OutputChanged = grouped.OutputChanged = null;
            scalar.ReadyChanged = grouped.ReadyChanged = null;
        }
    }

    private static byte[] Snapshot(Sn76489 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
