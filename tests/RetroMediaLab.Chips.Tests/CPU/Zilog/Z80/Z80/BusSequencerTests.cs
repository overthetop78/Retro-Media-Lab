using RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

/// <summary>Traces attendues depuis NEC pages 4-10/4-11, avec ses maxima µPD780-1.
/// L'horloge de 4 MHz est fournie par le banc, pas inventée dans le composant.</summary>
public sealed class BusSequencerTests(ITestOutputHelper output)
{
    [Fact]
    public void CompleteBusWaveformsRetainTheFrozenPrecomputedTargetsReference()
    {
        // Empreinte du parcours antérieur à la refonte, pas un oracle silicium.
        // Les oracles constructeur datés ci-dessous restent indépendants.
        string[] expected = [
            "93EAB6EB777DBF77ABADC9A79D6FE5ED8A0A5BAB568026C58E5E08E1117C4097",
            "9B03A73AAF85337A9A834173DC20B74AAF8F72C71DA791CE70B8398FE1C31FCF",
            "9B03A73AAF85337A9A834173DC20B74AAF8F72C71DA791CE70B8398FE1C31FCF"];
        Z80BusTiming[] timings = [Z80BusTiming.ZilogZ80Maximum, Z80BusTiming.ZilogZ80AMaximum,
            Upd780C1Profile.Timing];
        for (int model = 0; model < timings.Length; model++)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var cycle in Enum.GetValues<Z80BusCycle>())
            foreach (int waiting in Enumerable.Range(0, 4))
            foreach (bool held in cycle == Z80BusCycle.MemoryWrite ? new[] { false, true } : new[] { false })
            {
                var bus = new Z80BusSequencer(timings[model], 0x5a);
                try
                {
                    bus.PinsChanged = pins =>
                    {
                        Record((bus.TimeNanoseconds, pins));
                        if (pins.ReadHigh == false) bus.SetData((byte)(bus.TimeNanoseconds ^ 0xa5));
                    };
                    bus.NextEventNanosecondsChanged = n => Record((bus.TimeNanoseconds, n));
                    bus.SetClock(true); bus.BeginCycle(cycle, 0x1234, 0x9abc, 0xa5, held);
                    long half = timings[model].MinimumPeriod / 2;
                    for (int edge = 1; edge <= 40; edge++)
                    {
                        if (edge == 1 + waiting) bus.SetWait(true);
                        if (edge == 10) bus.SetWait(false);
                        bus.AdvanceTo(edge * half); bus.SetClock((edge & 1) == 0);
                        if (edge == 19) bus.SetHalt(true);
                        if (edge == 22) bus.SetBusGranted(true);
                        if (edge == 27) bus.SetBusGranted(false);
                        if (edge == 29) { bus.SetHalt(false); bus.ReleaseWriteData(); }
                        if (edge == 33) bus.EnterReset();
                        Record(bus.CaptureState());
                    }
                    bus.AdvanceTo(44 * half); Record(bus.CaptureState());
                }
                finally { bus.PinsChanged = null; bus.NextEventNanosecondsChanged = null; }
            }
            string actual = Convert.ToHexString(hash.GetHashAndReset());
            output.WriteLine($"BASELINE {model} {actual}");
            Assert.Equal(expected[model], actual);
            void Record<T>(T value) => hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(value,
                new JsonSerializerOptions { IncludeFields = true }));
        }
    }

    [Fact]
    public void M1SamplesBeforeReadReleaseThenRefreshesWithoutRead()
    {
        var bus = Start(Z80BusCycle.OpcodeFetch);
        var trace = Observe(bus);
        bus.PinsChanged += pins => { if (pins.ReadHigh == false) bus.SetData(0x31); };
        Edges(bus, 1, 4);
        Assert.True(bus.HasSample);
        Assert.Equal((byte)0x31, bus.SampledData);
        Assert.False(bus.Pins.ReadHigh); // RD n'est pas encore remonté : délai positif.
        bus.AdvanceTo(501); bus.SetData(0x72);
        Edges(bus, 5, 8); bus.AdvanceTo(1120);
        Assert.Equal(new long[] { 100, 110, 210, 220, 585, 600, 610, 630, 710, 960, 1120 },
            trace.Select(entry => entry.Time));
        Assert.False(trace[0].Pins.MachineCycleOneHigh);
        Assert.Equal((ushort)0x1234, trace[1].Pins.Address);
        Assert.Equal((ushort)0x9abc, trace[6].Pins.Address);
        Assert.False(trace[7].Pins.RefreshHigh);
        Assert.True(trace[8].Pins.ReadHigh);
        Assert.False(trace[8].Pins.MemoryRequestHigh);
        Assert.True(trace[^1].Pins.RefreshHigh);
        Assert.Equal((byte)0x31, bus.SampledData);
        Assert.False(bus.IsActive);
        Assert.Equal(5, bus.RisingClocks); // montant T1 suivant inclus, aucune clock ajoutée.
        Assert.Equal(1, bus.CompletedCycles);
    }

    [Fact]
    public void MemoryReadSamplesOnFallingT3AndKeepsDataUntilThen()
    {
        var bus = Start(Z80BusCycle.MemoryRead);
        var trace = Observe(bus);
        Edges(bus, 1, 4);
        Assert.False(bus.HasSample);
        bus.AdvanceTo(600); bus.SetData(0xe7);
        Edges(bus, 5, 6); bus.AdvanceTo(900);
        Assert.Equal((byte)0xe7, bus.SampledData);
        Assert.Equal(new long[] { 110, 210, 220, 710 }, trace.Select(entry => entry.Time));
        Assert.All(trace, entry => { Assert.True(entry.Pins.MachineCycleOneHigh); Assert.True(entry.Pins.RefreshHigh); });
        Assert.True(trace[^1].Pins.ReadHigh);
        Assert.True(trace[^1].Pins.MemoryRequestHigh);
    }

    [Fact]
    public void MemoryWriteKeepsDataThroughTheEndOfWritePulse()
    {
        var bus = Start(Z80BusCycle.MemoryWrite);
        var trace = Observe(bus);
        Edges(bus, 1, 6); bus.AdvanceTo(900);
        Assert.Equal(new long[] { 110, 210, 275, 455, 705, 710, 840 }, trace.Select(entry => entry.Time));
        Assert.Equal((byte)0xa5, trace[2].Pins.Data);
        Assert.False(trace[3].Pins.WriteHigh);
        Assert.True(trace[4].Pins.WriteHigh);
        Assert.Equal((byte)0xa5, trace[4].Pins.Data);
        Assert.Null(trace[^1].Pins.Data);
        Assert.False(bus.HasSample);
    }

    [Theory]
    [InlineData(Z80BusCycle.IoRead)]
    [InlineData(Z80BusCycle.IoWrite)]
    public void IoIncludesOneAutomaticWaitAndUsesItsOwnCommandEdges(Z80BusCycle cycle)
    {
        var bus = Start(cycle);
        var trace = Observe(bus);
        Edges(bus, 1, 6);
        Assert.True(bus.IsActive);
        Assert.False(bus.HasSample);
        bus.SetData(0xd6);
        Edges(bus, 7, 8); bus.AdvanceTo(1200);
        long[] expected = cycle == Z80BusCycle.IoRead ? [110, 325, 335, 960] : [110, 275, 315, 325, 955, 960, 1090];
        Assert.Equal(expected, trace.Select(entry => entry.Time));
        Assert.All(trace, entry => Assert.True(entry.Pins.MemoryRequestHigh));
        if (cycle == Z80BusCycle.IoRead) Assert.Equal((byte)0xd6, bus.SampledData);
        else Assert.False(bus.HasSample);
        Assert.False(bus.IsActive);
    }

    [Fact]
    public void InterruptAcknowledgeUsesTwoAutomaticWaitsAndNeverAssertsRead()
    {
        var bus = Start(Z80BusCycle.InterruptAcknowledge);
        var trace = Observe(bus);
        bus.PinsChanged += pins => { if (pins.IoRequestHigh == false) bus.SetData(0xcb); };
        Edges(bus, 1, 7);
        Assert.False(bus.HasSample);
        Edges(bus, 8, 8);
        Assert.Equal((byte)0xcb, bus.SampledData);
        Assert.False(bus.Pins.IoRequestHigh);
        bus.SetData(0x99);
        Edges(bus, 9, 12); bus.AdvanceTo(1800);
        Assert.Equal(new long[] { 100, 110, 710, 1085, 1100, 1110, 1130, 1210, 1460, 1620 },
            trace.Select(entry => entry.Time));
        Assert.All(trace, entry => Assert.True(entry.Pins.ReadHigh));
        Assert.False(trace[2].Pins.IoRequestHigh);
        Assert.False(trace[2].Pins.MachineCycleOneHigh);
        Assert.True(trace[2].Pins.MemoryRequestHigh);
        Assert.False(trace[7].Pins.MemoryRequestHigh);
        Assert.Equal((byte)0xcb, bus.SampledData);
        Assert.Equal(1, bus.CompletedCycles);
    }

    [Theory]
    [InlineData(Z80BusCycle.OpcodeFetch)]
    [InlineData(Z80BusCycle.MemoryRead)]
    [InlineData(Z80BusCycle.MemoryWrite)]
    [InlineData(Z80BusCycle.IoRead)]
    [InlineData(Z80BusCycle.IoWrite)]
    [InlineData(Z80BusCycle.InterruptAcknowledge)]
    public void WaitExtendsTheTransactionWithoutRepeatingTransfersAndCanBeRestored(Z80BusCycle cycle)
    {
        var bus = Start(cycle);
        bus.SetWait(true);
        int samplePhase = WaitSamplePhase(cycle);
        Edges(bus, 1, samplePhase + 1); // premier montant de Tw supplémentaire.
        Assert.False(bus.HasSample);
        var clone = new Z80BusSequencer(Upd780C1Profile.Timing, 0);
        clone.RestoreState(bus.CaptureState());
        var originalTrace = Observe(bus);
        var cloneTrace = Observe(clone);
        int end = EndPhase(cycle) + 2;
        foreach (var target in new[] { bus, clone })
        {
            target.AdvanceTo(target.TimeNanoseconds + 1);
            target.SetData(0x77); target.SetWait(false);
            Edges(target, samplePhase + 2, end);
            target.AdvanceTo(end * 125L + 150);
            Assert.False(target.IsActive);
            Assert.Equal(1, target.CompletedCycles);
            if (cycle is not (Z80BusCycle.MemoryWrite or Z80BusCycle.IoWrite))
                Assert.Equal((byte)0x77, target.SampledData);
        }
        Assert.Equal(originalTrace, cloneTrace);
        AssertState(bus.CaptureState(), clone.CaptureState());
    }

    [Theory]
    [InlineData(Z80BusCycle.OpcodeFetch)]
    [InlineData(Z80BusCycle.MemoryRead)]
    [InlineData(Z80BusCycle.MemoryWrite)]
    [InlineData(Z80BusCycle.IoRead)]
    [InlineData(Z80BusCycle.IoWrite)]
    [InlineData(Z80BusCycle.InterruptAcknowledge)]
    public void LateWaitCannotRetroactivelyStretchTheCycle(Z80BusCycle cycle)
    {
        var bus = Start(cycle);
        int samplePhase = WaitSamplePhase(cycle);
        Edges(bus, 1, samplePhase);
        bus.AdvanceTo(samplePhase * 125L + 1);
        bus.SetWait(true);
        Edges(bus, samplePhase + 1, EndPhase(cycle));
        Assert.False(bus.IsActive);
    }

    [Theory]
    [InlineData(Z80BusCycle.OpcodeFetch)]
    [InlineData(Z80BusCycle.MemoryRead)]
    [InlineData(Z80BusCycle.MemoryWrite)]
    [InlineData(Z80BusCycle.IoRead)]
    [InlineData(Z80BusCycle.IoWrite)]
    [InlineData(Z80BusCycle.InterruptAcknowledge)]
    public void EveryCaptureAndPropagationBoundaryResumesTheSameTrace(Z80BusCycle cycle)
    {
        long[] captureTimes = [0, 99, 100, 109, 110, 209, 210, 219, 220, 274, 275,
            314, 315, 324, 325, 334, 335, 374, 375, 499, 500, 624, 625, 709, 710,
            749, 750, 874, 875, 959, 960, 999, 1000, 1084, 1085, 1099, 1100, 1109, 1110,
            1120, 1129, 1130, 1209, 1210, 1459, 1460, 1499, 1500, 1619, 1620];
        foreach (long time in captureTimes)
        {
            var original = Start(cycle);
            int end = EndPhase(cycle);
            int last = (int)Math.Min(time / 125, end);
            Edges(original, 1, last); original.AdvanceTo(time);
            var restored = new Z80BusSequencer(Upd780C1Profile.Timing, 0);
            restored.RestoreState(original.CaptureState());
            var expected = Observe(original); var actual = Observe(restored);
            Edges(original, last + 1, end); original.AdvanceTo(1800);
            Edges(restored, last + 1, end); restored.AdvanceTo(1800);
            Assert.Equal(expected, actual);
            AssertState(original.CaptureState(), restored.CaptureState());
        }
    }

    [Theory]
    [InlineData(Z80BusCycle.OpcodeFetch)]
    [InlineData(Z80BusCycle.MemoryRead)]
    [InlineData(Z80BusCycle.MemoryWrite)]
    [InlineData(Z80BusCycle.IoRead)]
    [InlineData(Z80BusCycle.IoWrite)]
    [InlineData(Z80BusCycle.InterruptAcknowledge)]
    public void DenseAndDeadlineAdvancementPublishIdenticalSignals(Z80BusCycle cycle)
    {
        var dense = Start(cycle); var grouped = Start(cycle);
        var expected = Observe(dense); var actual = Observe(grouped);
        int end = EndPhase(cycle);
        for (long time = 1; time <= 1800; time++)
        {
            dense.AdvanceTo(time);
            if (time % 125 == 0 && time / 125 <= end) dense.SetClock(time % 250 == 0);
        }
        Edges(grouped, 1, end); grouped.AdvanceTo(1800);
        Assert.Equal(expected, actual);
        AssertState(dense.CaptureState(), grouped.CaptureState());
    }

    [Fact]
    public void AdjacentCyclesCanBeginOnTheSameRisingBoundary()
    {
        var bus = Start(Z80BusCycle.MemoryWrite);
        Edges(bus, 1, 6);
        bus.BeginCycle(Z80BusCycle.MemoryRead, 0xabcd, 0);
        Edges(bus, 7, 12); bus.AdvanceTo(1600);
        Assert.Equal(2, bus.CompletedCycles);
        Assert.Equal((ushort)0xabcd, bus.Pins.Address);
        Assert.Null(bus.Pins.Data);
        Assert.Equal((byte)0x5a, bus.SampledData);
    }

    [Fact]
    public void InvalidInputsDoNotPartiallyReplaceClockOrState()
    {
        var bus = Start(Z80BusCycle.OpcodeFetch);
        bus.AdvanceTo(109);
        var before = bus.CaptureState();
        Assert.Throws<ArgumentException>(() => bus.SetClock(false));
        AssertState(before, bus.CaptureState());
        Assert.Throws<ArgumentException>(() => bus.RestoreState(before with { Phase = 9 }));
        Assert.Throws<ArgumentException>(() => bus.RestoreState(before with { Values = [] }));
        Assert.Throws<ArgumentException>(() => bus.RestoreState(before with
            { Timing = Upd780C1Profile.Timing with { Address = 111 } }));
        AssertState(before, bus.CaptureState());
        Assert.Throws<ArgumentOutOfRangeException>(() => bus.AdvanceTo(108));
    }

    [Fact]
    public void NotificationsCanDriveInputsButCannotAdvanceTimeOrClockRecursively()
    {
        var bus = Start(Z80BusCycle.MemoryRead);
        int notifications = 0;
        bus.PinsChanged = pins =>
        {
            notifications++;
            Assert.Throws<InvalidOperationException>(() => bus.AdvanceTo(bus.TimeNanoseconds + 1));
            Assert.Throws<InvalidOperationException>(() => bus.SetClock(!bus.CaptureState().ClockHigh));
            if (pins.ReadHigh == false) bus.SetData(0x81);
        };
        bus.NextEventNanosecondsChanged = next =>
            Assert.Throws<InvalidOperationException>(() => bus.AdvanceTo(bus.TimeNanoseconds + 1));
        Edges(bus, 1, 6); bus.AdvanceTo(1000);
        Assert.Equal((byte)0x81, bus.SampledData);
        Assert.Equal(4, notifications);
    }

    private static Z80BusSequencer Start(Z80BusCycle cycle)
    {
        var bus = new Z80BusSequencer(Upd780C1Profile.Timing, 0x5a);
        bus.SetClock(true);
        bus.BeginCycle(cycle, 0x1234, 0x9abc, 0xa5);
        return bus;
    }

    private static List<(long Time, Z80BusPins Pins)> Observe(Z80BusSequencer bus)
    {
        var trace = new List<(long, Z80BusPins)>();
        bus.PinsChanged = pins => trace.Add((bus.TimeNanoseconds, pins));
        return trace;
    }

    private static int EndPhase(Z80BusCycle cycle) => cycle == Z80BusCycle.InterruptAcknowledge ? 12
        : cycle is Z80BusCycle.MemoryRead or Z80BusCycle.MemoryWrite ? 6 : 8;
    private static int WaitSamplePhase(Z80BusCycle cycle) => cycle == Z80BusCycle.InterruptAcknowledge ? 7
        : cycle is Z80BusCycle.IoRead or Z80BusCycle.IoWrite ? 5 : 3;
    private static void Edges(Z80BusSequencer bus, int first, int last)
    {
        for (int half = first; half <= last; half++) { bus.AdvanceTo(half * 125L); bus.SetClock(half % 2 == 0); }
    }

    private static void AssertState(Z80BusState expected, Z80BusState actual)
    {
        Assert.Equal(expected.Values, actual.Values);
        Assert.Equal(expected.Targets, actual.Targets);
        Assert.Equal(expected.Deadlines, actual.Deadlines);
        Assert.Equal(expected with { Values = actual.Values, Targets = actual.Targets, Deadlines = actual.Deadlines }, actual);
    }
}
