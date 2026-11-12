using System.Text.Json;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using Xunit;
using Circuit = RetroMediaLab.Chips.Tests.CPU.Common.Z80.Z80TestCircuit;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

public sealed class ClockSourceTests
{
    private const long Numerator = 3_000_000_000, Denominator = 21_477_270;
    private static long Date(int index, int origin) => origin + (long)((Int128)index * Numerator / Denominator);

    [Fact]
    public void PeriodicInputPreservesWholeCpuAndDatedBusAcrossExternalResponsesAndControlChanges()
    {
        byte[][] programs =
        [
            [0x21, 0, 0x80, 0x36, 0x55, 0x7e, 0x32, 1, 0x80, 0xdb, 0xdc, 0xd3, 0x7f, 0x18, 0xfe],
            [0xdd, 0x21, 0, 0x90, 0xdd, 0x36, 2, 0xa5, 0xdd, 0xcb, 2, 6, 0xfd, 0x21, 0, 0x91, 0xed, 0x44, 0xed, 0x4a, 0x18, 0xfe],
            [0x21, 0, 0x90, 0x11, 0, 0x91, 1, 4, 0, 0xed, 0xb0, 0xed, 0xb1, 0x76],
            [0x21, 0x34, 0x12, 0x11, 0x45, 0x23, 0x19, 0xed, 0x5a, 0xfb, 0x18, 0xfe]
        ];
        foreach (var silicon in new[] { Z80SiliconVariant.ZilogNmos, Z80SiliconVariant.NecNmos })
        foreach (byte[] program in programs)
        foreach (int origin in new[] { 0, 1, 17, 137 })
        {
            Circuit? scalar = null, grouped = null;
            try
            {
                scalar = new(program, silicon: silicon); grouped = new(program, silicon: silicon);
                long boundary = long.MaxValue;
                var connected = grouped.Cpu.PinsChanged!;
                grouped.Cpu.PinsChanged = pins =>
                {
                    connected(pins);
                    // A connected receiver notifies a response between clocks.
                    // This test changes the asynchronous control inputs there.
                    long due = grouped.Cpu.TimeNanoseconds + 19;
                    boundary = Math.Min(boundary, due);
                    grouped.Cpu.LimitClockSourceBefore(due);
                };
                grouped.Cpu.ReceiveClockSource(new( origin, Numerator, Denominator, 1, false));
                int index = 1, iteration = 0;
                while (index < 801)
                {
                    boundary = Date(Math.Min(801, index + 16), origin);
                    int count = grouped.Cpu.AdvanceClockSourceUntil(boundary);
                    Assert.InRange(count, 0, 801 - index);
                    for (int edge = index; edge < index + count; edge++)
                    {
                        scalar.Cpu.AdvanceTo(Date(edge, origin));
                        scalar.Cpu.SetClock((edge & 1) == 0);
                    }
                    index += count;
                    scalar.Cpu.AdvanceTo(grouped.Cpu.TimeNanoseconds);
                    EqualState();
                    scalar.Cpu.AdvanceTo(boundary); grouped.Cpu.AdvanceTo(boundary);
                    foreach (var cpu in new[] { scalar.Cpu, grouped.Cpu })
                    {
                        cpu.SetWait(iteration is >= 3 and < 8);
                        cpu.SetNmi(iteration == 12);
                        cpu.SetInt(iteration is >= 10 and < 20);
                        cpu.SetBusRequest(iteration is >= 24 and < 30);
                        cpu.SetReset(iteration is >= 40 and < 48);
                    }
                    EqualState();
                    Assert.True(++iteration < 4000);
                }
                Assert.Equal(scalar.Pins, grouped.Pins);
                Assert.Equal(scalar.Reads, grouped.Reads);
                Assert.Equal(scalar.MemoryWrites, grouped.MemoryWrites);
                Assert.Equal(scalar.PortWrites, grouped.PortWrites);
                Assert.Equal(scalar.Memory, grouped.Memory);
                void EqualState() => Assert.Equal(JsonSerializer.Serialize(scalar.Cpu.CaptureState()),
                    JsonSerializer.Serialize(grouped.Cpu.CaptureState()));
            }
            finally { grouped?.Dispose(); scalar?.Dispose(); }
        }
    }

    [Fact]
    public void InvalidSourcesDoNotMutateCpuAndExplicitTransportOrRestoreRequiresOwnerRebind()
    {
        Circuit? circuit = null;
        try
        {
            circuit = new([0]); var cpu = circuit.Cpu;
            var source = new RationalClockSource(0, Numerator, Denominator, 1, false);
            cpu.ReceiveClockSource(source);
            string before = JsonSerializer.Serialize(cpu.CaptureState());
            Assert.Throws<ArgumentOutOfRangeException>(() => cpu.ReceiveClockSource(source with { HalfPeriodDenominator = 0 }));
            Assert.Throws<ArgumentException>(() => cpu.ReceiveClockSource(source with { HalfPeriodNumerator = Denominator }));
            Assert.Throws<ArgumentException>(() => cpu.ReceiveClockSource(source with { FirstEdgeHigh = true }));
            Assert.Equal(before, JsonSerializer.Serialize(cpu.CaptureState()));
            Assert.Equal(40, cpu.AdvanceClockSourceUntil(Date(41, 0)));
            var saved = cpu.CaptureState();
            cpu.RestoreState(saved);
            Assert.Throws<InvalidOperationException>(() => cpu.AdvanceClockSourceUntil(Date(42, 0)));
            cpu.ReceiveClockSource(source with { FirstEdgeIndex = 41 });
            cpu.AdvanceTo(cpu.TimeNanoseconds + 1000); cpu.SetClock(false);
            Assert.Throws<InvalidOperationException>(() => cpu.AdvanceClockSourceUntil(cpu.TimeNanoseconds + 1000));
        }
        finally { circuit?.Dispose(); }
    }
}
