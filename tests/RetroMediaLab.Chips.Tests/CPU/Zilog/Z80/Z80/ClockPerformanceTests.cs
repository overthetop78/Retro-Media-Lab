using System.Diagnostics;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;
using Xunit;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

public sealed class ClockPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void CompleteClockBusExecutionMeasuresThroughputAndDenseAdvanceEquivalence()
    {
        foreach (byte[] program in new byte[][]
            { [0x00, 0xc3, 0, 0], [0x34, 0x7e, 0xd3, 0x20, 0xc3, 0, 0], [0xdd, 0xcb, 1, 0x06, 0xc3, 0, 0] })
        {
            var dense = Run(program, 100, true);
            var grouped = Run(program, 100, false);
            Assert.Equal(dense.Hash, grouped.Hash);
            Assert.Equal(dense.Registers, grouped.Registers);
            Assert.Equal(dense.Clocks, grouped.Clocks);
            Run(program, 5000, false); // chauffe JIT, hors mesure.
            var milliseconds = new double[7]; long clocks = 0;
            for (int repeat = 0; repeat < milliseconds.Length; repeat++)
            {
                var result = Run(program, 50000, false);
                milliseconds[repeat] = result.ElapsedMilliseconds; clocks = result.Clocks;
            }
            Array.Sort(milliseconds);
            double median = milliseconds[milliseconds.Length / 2];
            output.WriteLine($"Programme {Convert.ToHexString(program)} : médiane {median:F3} ms ; " +
                $"{clocks / median / 1000:F3} MHz émulés ; {clocks} clocks ; 50000 instructions ; " +
                $"7 passages, horloge extérieure 4 MHz, propagations Zilog Z80A maximales, RAM et E/S raccordées.");
        }
    }

    [Fact]
    public void PairedScalarAndClockTrainExecutionPreserveAllPinsAndMeasureWholeBusPath()
    {
        foreach (byte[] program in new byte[][]
            { [0x00, 0xc3, 0, 0], [0x34, 0x7e, 0xd3, 0x20, 0xc3, 0, 0],
              [0xdd, 0xcb, 1, 0x06, 0xc3, 0, 0], [0x09, 0x18, 0xfd] })
        {
            const int edges = 800000;
            var explicitSignals = Run(program, 0, false, edgeCount: 10000);
            var trainSignals = Run(program, 0, false, edgeCount: 10000, train: true);
            Assert.Equal(explicitSignals.Hash, trainSignals.Hash);
            Assert.Equal(explicitSignals.Memory, trainSignals.Memory);
            // Mesurer le CPU et le raccordement RAM/E/S, sans attribuer au cœur
            // le coût de l'empreinte de diagnostic des dix sorties.
            var scalar = new double[7]; var trains = new double[7];
            for (int pair = 0; pair < scalar.Length; pair++)
            {
                var a = Run(program, 0, false, edges, train: (pair & 1) != 0, captureSignals: false);
                var b = Run(program, 0, false, edges, train: (pair & 1) == 0, captureSignals: false);
                Assert.Equal(a.Registers, b.Registers); Assert.Equal(a.Clocks, b.Clocks);
                Assert.Equal(a.Memory, b.Memory);
                Assert.Equal(a.State.Bus.Values, b.State.Bus.Values);
                Assert.Equal(a.State.Bus.Targets, b.State.Bus.Targets);
                Assert.Equal(a.State.Bus.Deadlines, b.State.Bus.Deadlines);
                Assert.Equal(a.State with { Bus = b.State.Bus }, b.State);
                scalar[pair] = (pair & 1) == 0 ? a.ElapsedMilliseconds : b.ElapsedMilliseconds;
                trains[pair] = (pair & 1) == 0 ? b.ElapsedMilliseconds : a.ElapsedMilliseconds;
            }
            double[] reductions = scalar.Zip(trains, (a, b) => 100 * (a - b) / a).Order().ToArray();
            output.WriteLine("Paires ms scalaire/train : " + string.Join(" ; ", scalar.Zip(trains, (a, b) => $"{a:F3}/{b:F3}")));
            Array.Sort(scalar); Array.Sort(trains);
            output.WriteLine($"{Convert.ToHexString(program)} : scalaire {scalar[3]:F3} ms ; train {trains[3]:F3} ms ; " +
                $"réduction appariée {reductions[3]:F2} % ; {400000 / trains[3] / 1000:F3} MHz émulés ; " +
                "7 paires alternées, 800000 fronts à 4 MHz, RAM/E/S par broches, empreinte hors mesure.");
        }
    }

    private static (Z80Registers Registers, long Clocks, ulong Hash, double ElapsedMilliseconds, byte[] Memory, CpuZ80State State)
        Run(byte[] program, int instructions, bool dense, int? edgeCount = null, bool train = false, bool captureSignals = true)
    {
        byte[] memory = new byte[65536]; program.CopyTo(memory, 0);
        var cpu = new CpuZ80(Z80BusTiming.ZilogZ80AMaximum,
            new Z80Registers { SP = 0xf000, HL = 0x4000, IX = 0x5000 }, default, 0xff);
        Z80BusPins previous = cpu.Pins;
        ulong hash = 14695981039346656037;
        cpu.PinsChanged = pins =>
        {
            if (pins.IsMemoryRead()) cpu.SetData(memory[pins.GetAddressOrZero()]);
            else if (pins.IsIoRead()) cpu.SetData(0x5a);
            if (previous.IsMemoryWrite() && !pins.IsMemoryWrite())
                memory[previous.GetAddressOrZero()] = previous.GetDataOrZero();
            if (captureSignals)
            {
                hash = unchecked((hash ^ (ulong)cpu.TimeNanoseconds) * 1099511628211);
                ulong value = (ulong)(pins.Address ?? 0) | ((ulong)(pins.Data ?? 0) << 16)
                | (pins.Address.HasValue ? 1UL << 24 : 0) | (pins.Data.HasValue ? 1UL << 25 : 0)
                | (Level(pins.MemoryRequestHigh) << 26) | (Level(pins.IoRequestHigh) << 28)
                | (Level(pins.ReadHigh) << 30) | (Level(pins.WriteHigh) << 32)
                | (pins.MachineCycleOneHigh ? 1UL << 34 : 0) | (pins.RefreshHigh ? 1UL << 35 : 0)
                | (pins.HaltHigh ? 1UL << 36 : 0) | (pins.BusAcknowledgeHigh ? 1UL << 37 : 0);
                hash = unchecked((hash ^ value) * 1099511628211);
            }
            previous = pins;
        };
        try
        {
            long start = Stopwatch.GetTimestamp();
            long edge = 0; bool high = true;
            if (edgeCount is { } count)
            {
                if (train)
                    for (int offset = 0; offset < count; offset += 256)
                        cpu.ReceiveClockEdges(offset * 125L, 125, Math.Min(256, count - offset));
                else
                    for (int i = 0; i < count; i++)
                    {
                        cpu.AdvanceTo(i * 125L); cpu.SetClock((i & 1) == 0);
                    }
            }
            else while (cpu.InstructionsCompleted < instructions)
            {
                if (dense)
                    for (long t = cpu.TimeNanoseconds + 1; t < edge; t++) cpu.AdvanceTo(t);
                cpu.AdvanceTo(edge); cpu.SetClock(high);
                high = !high; edge += 125;
            }
            double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            return (cpu.Registers, cpu.InputClockCount, hash, elapsed, memory, cpu.CaptureState());
        }
        finally { cpu.PinsChanged = null; cpu.NextEventNanosecondsChanged = null; }
    }

    private static ulong Level(bool? level) => level.HasValue ? level.Value ? 2UL : 1UL : 0;
}
