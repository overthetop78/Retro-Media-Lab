using System.Text.Json;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class ClockSourceTests
{
    private const long Numerator = 3_000_000_000, Denominator = 21_477_270;
    private static long Date(int index, int origin = 0) => origin + (long)((Int128)index * Numerator / Denominator);

    [Fact]
    public void LocalClockDomainPreservesSoundBusCaptureReadyAndEveryObservedPhase()
    {
        foreach (int delay in new[] { 90, 150 })
        foreach (int origin in new[] { 0, 17, 137 })
        {
            var scalar = new Sn76489(delay); var grouped = new Sn76489(delay);
            var expected = new List<(long Time, string Kind, int Value, long Clocks)>();
            var actual = new List<(long Time, string Kind, int Value, long Clocks)>();
            int edge = 1;
            try
            {
                Wire(scalar, expected); Wire(grouped, actual);
                grouped.ReceiveClockSource(new(origin, Numerator, Denominator, 1, true));
                byte[] commands = [0x81, 2, 0x92, 0xa3, 0, 0xb4, 0xc5, 1, 0xd6, 0xe0, 0xf2, 0xe4, 0xe7, 0x90, 0xbf, 0xdf];
                for (int command = 0; command < commands.Length; command++)
                {
                    long time = command * 12_013L + 37;
                    AdvanceBoth(time);
                    scalar.SetBus(time, commands[command], true, true);
                    grouped.SetBus(time, commands[command], true, true);
                    EqualState();
                    // Observe both clock polarities and partial READY/setup windows.
                    foreach (long observation in new[] { time + 89, time + 150, time + 4001, time + 11_900 })
                        AdvanceBoth(observation);
                    scalar.SetBus(time + 11_900, 0x55, false, false);
                    grouped.SetBus(time + 11_900, 0x55, false, false);
                    EqualState();
                }
                AdvanceBoth(Date(4000, origin));
                Assert.Equal(expected, actual);
                void AdvanceBoth(long time)
                {
                    while (Date(edge, origin) <= time)
                    {
                        scalar.ReceiveClock(Date(edge, origin), (edge & 1) != 0);
                        edge++;
                    }
                    scalar.AdvanceTo(time); grouped.AdvanceTo(time); EqualState();
                }
                void EqualState() => Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()),
                    JsonSerializer.Serialize(grouped.CaptureState()));
                static void Wire(Sn76489 chip, List<(long, string, int, long)> trace)
                {
                    chip.OutputChanged = value => trace.Add((chip.TimeNanoseconds, "Audio", value, chip.InputClockCount));
                    chip.ReadyChanged = value => trace.Add((chip.TimeNanoseconds, "Ready", value ? 1 : 0, chip.InputClockCount));
                }
            }
            finally
            {
                scalar.OutputChanged = grouped.OutputChanged = null;
                scalar.ReadyChanged = grouped.ReadyChanged = null;
            }
        }
    }

    [Fact]
    public void ReadyDeadlineIncludesTheReceivedTransferCompletionAndSourceRevocationPreservesState()
    {
        var chip = new Sn76489(150);
        var source = new RationalClockSource(0, Numerator, Denominator, 1, true);
        chip.ReceiveClockSource(source);
        chip.Write(0x83);
        Assert.Equal(150, chip.NextEventNanoseconds);
        chip.AdvanceTo(150); Assert.False(chip.Ready);
        Assert.Equal(Date(63), chip.NextEventNanoseconds); // TI transfer: 32 received rising clocks.
        chip.AdvanceTo(Date(63) - 1); Assert.False(chip.Ready);
        chip.AdvanceTo(Date(63)); Assert.True(chip.Ready); Assert.Null(chip.NextEventNanoseconds);
        Assert.Equal(32, chip.InputClockCount);
        var saved = chip.CaptureState(); chip.RestoreState(saved);
        chip.AdvanceTo(Date(100));
        Assert.Equal(32, chip.InputClockCount); // Owner reconnects the restored source.

        var explicitInput = new Sn76489(150);
        var describedInput = new Sn76489(150);
        explicitInput.ReceiveClock(0, true);
        describedInput.ReceiveClockSource(new(0, Numerator, Denominator, 0, true));
        describedInput.AdvanceTo(0);
        explicitInput.ReceiveClock(Date(1), false);
        describedInput.ReceiveClock(Date(1), false);
        Assert.Equal(JsonSerializer.Serialize(explicitInput.CaptureState()), JsonSerializer.Serialize(describedInput.CaptureState()));
    }

    [Fact]
    public void ReadyFeedbackCanReplaceTheBusAtTheSameDateWithoutReplayingSourceEdges()
    {
        var scalar = new Sn76489(150); var grouped = new Sn76489(150);
        var expected = new List<(long, bool, long)>(); var actual = new List<(long, bool, long)>();
        try
        {
            Wire(scalar, expected); Wire(grouped, actual);
            grouped.ReceiveClockSource(new(0, Numerator, Denominator, 1, true));
            scalar.SetBus(0, 0x83, true, true); grouped.SetBus(0, 0x83, true, true);
            for (int edge = 1; edge <= 256; edge++) scalar.ReceiveClock(Date(edge), (edge & 1) != 0);
            scalar.AdvanceTo(Date(256)); grouped.AdvanceTo(Date(256));
            Assert.Equal(expected, actual);
            Assert.Equal(JsonSerializer.Serialize(scalar.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
            static void Wire(Sn76489 chip, List<(long, bool, long)> trace)
            {
                int completed = 0;
                chip.ReadyChanged = ready =>
                {
                    trace.Add((chip.TimeNanoseconds, ready, chip.InputClockCount));
                    if (!ready) chip.SetBus(chip.TimeNanoseconds, 0x91, true, true);
                    else
                    {
                        chip.SetBus(chip.TimeNanoseconds, 0x55, false, false);
                        if (++completed < 3) chip.SetBus(chip.TimeNanoseconds, 0xe4, true, true);
                    }
                };
            }
        }
        finally { scalar.ReadyChanged = grouped.ReadyChanged = null; }
    }
}
