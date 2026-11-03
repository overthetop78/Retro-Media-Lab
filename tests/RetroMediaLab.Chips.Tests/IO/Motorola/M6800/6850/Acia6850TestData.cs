using System.Numerics;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;

internal static class Acia6850TestData
{
    internal static bool[] Frame(int format, byte value)
    {
        var bits = new List<bool> { false };
        var width = format < 4 ? 7 : 8;
        for (var bit = 0; bit < width; bit++) bits.Add((value & 1 << bit) != 0);
        if (format is not (4 or 5))
        {
            var odd = (BitOperations.PopCount((uint)value & ((1u << width) - 1)) & 1) != 0;
            bits.Add(odd != ((format & 1) != 0));
        }
        bits.Add(true);
        if (format is 0 or 1 or 4) bits.Add(true);
        return bits.ToArray();
    }

    internal static void Receive(Acia6850 acia, int format, byte value)
    {
        acia.BeginReceive();
        foreach (var bit in Frame(format, value).Skip(1)) acia.ReceiveBit(bit);
    }

    internal static void ReceiveClocked(Acia6850 acia, int format, byte value, int divider)
    {
        foreach (var bit in Frame(format, value))
            for (var clock = 0; clock < divider; clock++) acia.ClockReceive(bit);
    }

    internal static void Clocks(Acia6850 acia, int count)
    {
        for (var clock = 0; clock < count; clock++) acia.ClockTransmit();
    }
}
