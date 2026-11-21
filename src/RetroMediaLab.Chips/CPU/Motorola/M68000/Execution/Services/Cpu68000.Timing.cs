using System.Collections.Concurrent;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

internal static class Cpu68000Timing
{
    private static readonly ConcurrentDictionary<int, ushort[]> Tables = new();

    internal static ushort[] GetInstructionCycles(int scale, ReadOnlySpan<byte> clocks)
    {
        if (Tables.TryGetValue(scale, out ushort[]? existing)) return existing;
        ushort[] cycles = new ushort[clocks.Length];
        for (int i = 0; i < cycles.Length; i++) cycles[i] = checked((ushort)(clocks[i] * scale));
        return Tables.GetOrAdd(scale, cycles);
    }
}
