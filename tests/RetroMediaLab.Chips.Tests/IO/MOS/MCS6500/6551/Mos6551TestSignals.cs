using System.Numerics;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

internal static class Mos6551TestSignals
{
    internal static Mos6551 Device(byte control = 0, byte command = 9)
    {
        var chip = new Mos6551(1_843_200, 0);
        chip.SetCts(false);
        chip.SetDcd(false);
        chip.SetDsr(false);
        chip.Write(3, control);
        chip.Write(2, command);
        return chip;
    }

    internal static void Tx(Mos6551 chip, int clocks = 1)
    {
        for (var clock = 0; clock < clocks; clock++)
        {
            chip.SetTransmitClock(true);
            chip.SetTransmitClock(false);
        }
    }

    internal static void Rx(Mos6551 chip, bool value, int clocks = 16)
    {
        chip.SetRxd(value);
        for (var clock = 0; clock < clocks; clock++)
        {
            chip.SetReceiveClock(true);
            chip.SetReceiveClock(false);
        }
    }

    internal static bool[] Frame(int bits, int parity, byte data)
    {
        var result = new List<bool> { false };
        for (var bit = 0; bit < bits; bit++) result.Add((data & (1 << bit)) != 0);
        var ones = BitOperations.PopCount((uint)(data & ((1 << bits) - 1)));
        if (parity != 0) result.Add(parity switch { 1 => (ones & 1) == 0, 3 => (ones & 1) != 0, 5 => true, _ => false });
        result.Add(true);
        return result.ToArray();
    }

    internal static void Send(Mos6551 chip, byte data, bool? parity = null, bool stop = true)
    {
        Rx(chip, false);
        for (var bit = 0; bit < 8; bit++) Rx(chip, (data & (1 << bit)) != 0);
        if (parity.HasValue) Rx(chip, parity.Value);
        Rx(chip, stop);
    }
}
