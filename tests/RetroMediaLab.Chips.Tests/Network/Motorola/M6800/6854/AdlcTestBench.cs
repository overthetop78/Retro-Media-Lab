using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;

internal static class AdlcTestBench
{
    internal static Mc6854 Device(int width = 8, byte control1 = 0, byte control3 = 0, byte control4 = 0)
    {
        var chip = new Mc6854(C.BitsPerOctet);
        chip.SetLine(true);
        chip.Write(0, C.AddressControl);
        chip.Write(1, control3);
        var length = width - C.MinimumInformationBits;
        chip.Write(3, (byte)(control4 | (length << C.TransmitWordLengthShift) | (length << C.ReceiveWordLengthShift)));
        chip.Write(0, control1);
        return chip;
    }

    internal static bool ClockTransmit(Mc6854 chip)
    {
        chip.SetTransmitClock(true);
        chip.SetTransmitClock(false);
        return chip.TransmitData;
    }

    internal static void ClockReceive(Mc6854 chip, bool bit)
    {
        chip.SetReceiveClock(false);
        chip.SetReceiveData(bit);
        chip.SetReceiveClock(true);
    }

    internal static (byte[] Frame, bool[] Bits) Transmit(Mc6854 chip, byte[] data, bool nrzi = false)
    {
        byte[]? frame = null;
        var wire = new List<bool>();
        var index = 0;
        chip.FrameTransmitted = value => frame = value.ToArray();
        try
        {
            for (var clock = 0; clock < (data.Length + 16) * 32 && frame is null; clock++)
            {
                if (index < data.Length && (chip.Read(0) & C.TransmitAvailable) != 0)
                    chip.Write(index == data.Length - 1 ? 3 : 2, data[index++]);
                wire.Add(ClockTransmit(chip));
            }
            Assert.NotNull(frame);
            if (nrzi)
                wire.Add(ClockTransmit(chip));
            return (frame, wire.ToArray());
        }
        finally
        {
            chip.FrameTransmitted = null;
        }
    }

    internal static byte[] ReceivePacket(Mc6854 chip, byte[] data, bool valid = true)
    {
        Assert.True(chip.ReceiveFrame(data, valid));
        var result = new List<byte>();
        for (var cycle = 0; cycle < (data.Length + 16) * 32 && result.Count < data.Length; cycle++)
        {
            chip.Tick();
            var status = chip.Read(1);
            if ((status & C.ReceiveAvailable) != 0)
                result.Add(chip.Read(2));
        }
        Assert.Equal(data.Length, result.Count);
        return result.ToArray();
    }

    internal static byte[] ReceiveBits(Mc6854 chip, IEnumerable<bool> bits)
    {
        var bytes = new List<byte>();
        foreach (var bit in bits)
        {
            ClockReceive(chip, bit);
            if ((chip.Read(1) & C.ReceiveAvailable) != 0)
                bytes.Add(chip.Read(2));
        }
        while ((chip.Read(1) & C.ReceiveAvailable) != 0)
            bytes.Add(chip.Read(2));
        return bytes.ToArray();
    }

    internal static List<bool> Octets(params byte[] bytes) => bytes.SelectMany(value =>
        Enumerable.Range(0, 8).Select(bit => (value & (1 << bit)) != 0)).ToList();

    internal static bool[] Encode(IEnumerable<bool> data, bool badFcs = false)
    {
        var payload = data.ToList();
        ushort remainder = 0xffff;
        foreach (var bit in payload)
        {
            var low = ((remainder & 1) != 0) != bit;
            remainder >>= 1;
            if (low)
                remainder ^= 0x8408;
        }
        remainder = (ushort)~remainder;
        if (badFcs)
            remainder ^= 1;
        payload.AddRange(Octets((byte)remainder, (byte)(remainder >> 8)));
        var result = Octets(0x7e);
        var ones = 0;
        foreach (var bit in payload)
        {
            result.Add(bit);
            ones = bit ? ones + 1 : 0;
            if (ones == 5)
            {
                result.Add(false);
                ones = 0;
            }
        }
        result.AddRange(Octets(0x7e));
        return result.ToArray();
    }

    internal static byte[] Decode(bool[] wire)
    {
        Assert.Equal(Octets(0x7e), wire.Take(8));
        Assert.Equal(Octets(0x7e), wire.TakeLast(8));
        var payload = new List<bool>();
        var ones = 0;
        foreach (var bit in wire.Skip(8).SkipLast(8))
        {
            if (ones == 5)
            {
                Assert.False(bit);
                ones = 0;
                continue;
            }
            payload.Add(bit);
            ones = bit ? ones + 1 : 0;
        }
        Assert.Equal(0, payload.Count % 8);
        return payload.Chunk(8).Select(bits => (byte)bits.Select((bit, index) => bit ? 1 << index : 0).Sum()).ToArray();
    }
}
