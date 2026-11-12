using System.Diagnostics;
using System.Text.Json;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Memory.Toshiba.TMM23xx.TMM23256;

public sealed class Tmm23256Tests(ITestOutputHelper output)
{
    private static byte Pattern(int address) => (byte)(address ^ (address >> 8));
    private static byte[] Contents() => Enumerable.Range(0, 32768).Select(Pattern).ToArray();
    private static Tmm23256 Chip() => new(Contents(), Tmm23256Timing.SpecifiedMaximum);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteBusThroughputExcludesAssertionsAndIncludesObservedOutputs(bool observed)
    {
        const int count = 65536;
        uint expected = 0;
        for (int access = 0; access < count; access++) expected = unchecked(expected * 16777619 ^ Pattern(access & 32767));
        var durations = new List<double>();
        for (int repetition = 0; repetition < 3; repetition++)
        {
            var chip = Chip();
            int edges = 0;
            if (observed) chip.OutputChanged = _ => edges++;
            try
            {
                Run(chip, 100_000, 8192); // JIT and working-set warm-up outside the measurement
                edges = 0;
                var watch = Stopwatch.StartNew();
                uint checksum = Run(chip, chip.TimeNanoseconds + 100, count);
                watch.Stop();
                Assert.Equal(expected, checksum);
                Assert.Equal(observed ? count * 2 : 0, edges);
                Assert.True(chip.IsTimingQualified);
                Assert.False(chip.Output.Driven);
                durations.Add(watch.Elapsed.TotalMilliseconds);
            }
            finally { chip.OutputChanged = null; }
        }
        output.WriteLine("Bus65536CE/OE transactions, observed={0}; host ms=[{1}]; median={2:F3}ms; {3:F0}accesses/s; no machine speed inferred.",
            observed, string.Join(",", durations.Select(value => value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))),
            durations.Order().ElementAt(1), count * 1000 / durations.Order().ElementAt(1));

        static uint Run(Tmm23256 chip, long time, int accesses)
        {
            uint checksum = 0;
            for (int access = 0; access < accesses; access++)
            {
                var pins = new Tmm23256Pins((ushort)(access & 32767), true, false, false);
                chip.ReceivePins(time, pins);
                chip.AdvanceTo(time + 150);
                var data = chip.Output;
                checksum = unchecked(checksum * 16777619 ^ data.Data);
                if (!data.Known || !data.Driven) throw new InvalidOperationException("Unqualified ROM transfer.");
                chip.ReceivePins(time + 160, pins with { CeHigh = true, OeHigh = true });
                chip.AdvanceTo(time + 300);
                time += 300;
            }
            return checksum;
        }
    }

    [Fact]
    public void EveryMaskAddressHasDatedAccessAndReleaseWithoutMutableBackingStorage()
    {
        var contents = Contents();
        var chip = new Tmm23256(contents, Tmm23256Timing.SpecifiedMaximum);
        Array.Fill(contents, (byte)0xa7);
        long time = 100_000;
        int changes = 0;
        chip.OutputChanged = value => { Assert.Equal(value, chip.Output); changes++; };
        try
        {
            var watch = Stopwatch.StartNew();
            for (int address = 0; address < 32768; address++)
            {
                var pins = new Tmm23256Pins((ushort)address, true, false, false);
                chip.ReceivePins(time, pins);
                chip.ReceivePins(time, pins);
                Assert.Equal(time + 150, chip.NextEventNanoseconds);
                chip.AdvanceTo(time + 149);
                Assert.False(chip.Output.Driven);
                chip.AdvanceTo(time + 150);
                Assert.Equal(new(Pattern(address), true, true), chip.Output);
                chip.ReceivePins(time + 160, pins with { CeHigh = true, OeHigh = true });
                chip.AdvanceTo(time + 229);
                Assert.True(chip.Output.Driven);
                chip.AdvanceTo(time + 230);
                Assert.Equal(default, chip.Output);
                time += 300;
            }
            watch.Stop();
            Assert.True(chip.IsInitialized);
            Assert.True(chip.IsTimingQualified);
            Assert.Equal(65536, changes);
            output.WriteLine("32768 complete bus accesses including assertions and output listener: {0:F3}ms host; {1:F0}accesses/s. No machine throughput inferred.",
                watch.Elapsed.TotalMilliseconds, 32768 / watch.Elapsed.TotalSeconds);
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void AddressChangesAfterHoldAndOutputEnableCannotRecaptureTheMaskAddress()
    {
        var chip = Chip();
        var pins = new Tmm23256Pins(1, true, false, false);
        chip.ReceivePins(100_000, pins);
        chip.ReceivePins(100_030, pins with { Address = 2 });
        chip.AdvanceTo(100_150);
        Assert.Equal(new(1, true, true), chip.Output);
        chip.ReceivePins(100_200, pins with { Address = 2, OeHigh = true });
        chip.AdvanceTo(100_270);
        Assert.False(chip.Output.Driven);
        chip.ReceivePins(100_300, pins with { Address = 2 });
        chip.AdvanceTo(100_369);
        Assert.False(chip.Output.Driven);
        chip.AdvanceTo(100_370);
        Assert.Equal(new(1, true, true), chip.Output);
        chip.ReceivePins(100_450, pins with { Address = 2, CeHigh = true });
        chip.AdvanceTo(100_520);
        chip.ReceivePins(100_530, pins with { Address = 2 });
        chip.AdvanceTo(100_680);
        Assert.Equal(new(2, true, true), chip.Output);
        Assert.True(chip.IsTimingQualified);
    }

    [Fact]
    public void BothInitializationMethodsAndUnqualifiedCyclesRemainExplicit()
    {
        var high = Chip();
        high.AdvanceTo(99_999); Assert.False(high.IsInitialized);
        high.AdvanceTo(100_000); Assert.True(high.IsInitialized);
        var cycled = Chip();
        var pins = new Tmm23256Pins(37, true, false, false);
        cycled.ReceivePins(0, pins);
        cycled.AdvanceTo(150);
        Assert.Equal(new(0, false, true), cycled.Output);
        cycled.ReceivePins(100_000, pins with { CeHigh = true });
        cycled.ReceivePins(100_100, pins);
        Assert.False(cycled.IsInitialized);
        cycled.ReceivePins(100_260, pins with { CeHigh = true });
        cycled.ReceivePins(100_400, pins);
        Assert.True(cycled.IsInitialized);
        cycled.AdvanceTo(100_550);
        Assert.Equal(new(37, true, true), cycled.Output);
        Assert.True(cycled.IsTimingQualified);
        var violated = Chip();
        violated.ReceivePins(100_000, pins);
        violated.ReceivePins(100_029, pins with { Address = 38 });
        violated.AdvanceTo(100_150);
        Assert.False(violated.IsTimingQualified);
        Assert.Equal(new(0, false, true), violated.Output);
        violated.ReceivePins(100_160, pins with { CeHigh = true });
        violated.ReceivePins(100_200, pins); // both tCC and tCYC violated
        violated.AdvanceTo(100_350);
        Assert.False(violated.Output.Known);
    }

    [Fact]
    public void OutputFeedbackCanDisableAtTheReadDateWithoutRepeatingTheOutput()
    {
        var chip = Chip();
        var pins = new Tmm23256Pins(3, true, false, false);
        var trace = new List<(long, Tmm23256Output)>();
        chip.OutputChanged = value =>
        {
            trace.Add((chip.TimeNanoseconds, value));
            if (value.Driven) chip.ReceivePins(chip.TimeNanoseconds, pins with { OeHigh = true });
        };
        try
        {
            chip.ReceivePins(100_000, pins);
            chip.AdvanceTo(101_000);
            Assert.Equal(new[] { (100_150L, new Tmm23256Output(3, true, true)), (100_220L, default(Tmm23256Output)) }, trace);
            Assert.Null(chip.NextEventNanoseconds);
            Assert.False(chip.Output.Driven);
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void JsonSnapshotDuringAccessRestoresWithoutEdgesAndRejectsOtherMasksAndProfiles()
    {
        var original = Chip();
        original.ReceivePins(100_000, new(0x4321, true, false, false));
        original.AdvanceTo(100_050);
        var state = JsonSerializer.Deserialize<Tmm23256State>(Snapshot(original))!;
        var restored = Chip();
        int changes = 0;
        restored.OutputChanged = _ => changes++;
        restored.NextEventNanosecondsChanged = _ => changes++;
        try
        {
            restored.RestoreState(state);
            Assert.Equal(0, changes);
            foreach (var invalid in new[]
            {
                state with { Version = 1 },state with { Version = 2 }, state with { RomHash = "other" }, state with { Time = -1 },
                state with { LatchedAddress = 32768 }, state with { OutputDue = state.Time },
                state with { AccessReady = 100_149 }, state with { InitialCeHigh = true },
                state with { Target = new(7, true, true) }
            })
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                Assert.Equal(Snapshot(original), Snapshot(restored));
            }
            var mask = Contents(); mask[0] ^= 1;
            Assert.Throws<ArgumentException>(() => new Tmm23256(mask, Tmm23256Timing.SpecifiedMaximum).RestoreState(state));
            Assert.Throws<ArgumentException>(() => new Tmm23256(Contents(), new(149, 70, 70)).RestoreState(state));
            original.AdvanceTo(100_150); restored.AdvanceTo(100_150);
            Assert.Equal(2, changes); // output and consumed deadline
            Assert.Equal(Snapshot(original), Snapshot(restored));
            Assert.Equal(new(Pattern(0x4321), true, true), restored.Output);
        }
        finally { restored.OutputChanged = null; restored.NextEventNanosecondsChanged = null; }
    }
    private static byte[] Snapshot(Tmm23256 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
