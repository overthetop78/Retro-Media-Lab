using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private bool[]? _packetBits;
    private int _packetOffset;
    private int _packetLength;
    private bool _packetLineLevel = true;
    private int _fillBit;

    /// <summary>Convertit une trame délimitée en bits de ligne ; FIFO, FCS et temps passent par le récepteur réel.</summary>
    public bool ReceiveFrame(ReadOnlySpan<byte> frame, bool validFcs = true)
    {
        if (frame.Length is 0 or > C.MaximumFrameBytes || !_clockPresent || !_carrierPresent || _dcdRaised ||
            (_control1 & C.ReceiveReset) != 0 || Receiving || (_receiveStatus & C.ReceiveErrors) != 0)
            return false;
        var maximumData = frame.Length * C.BitsPerOctet + C.FcsBits;
        var bits = new bool[maximumData + maximumData / C.StuffingOnes + C.FcsBits + 1];
        var length = 0;
        AppendRaw(bits, ref length, C.Flag, C.BitsPerOctet);
        var crc = C.CrcInitial;
        var ones = 0;
        var field = AdlcFrameField.Address;
        foreach (var value in frame)
        {
            var width = FieldLength(field, C.ReceiveWordLengthShift);
            crc = CrcWord(crc, value, width);
            for (var bit = 0; bit < width; bit++)
            {
                var level = ((value >> bit) & 1) != 0;
                AppendStuffed(bits, ref length, level, ref ones);
            }
            field = NextField(field, value);
        }
        var fcs = (ushort)~crc;
        if (!validFcs)
            fcs ^= 1;
        for (var bit = 0; bit < C.FcsBits; bit++)
            AppendStuffed(bits, ref length, ((fcs >> bit) & 1) != 0, ref ones);
        AppendRaw(bits, ref length, C.Flag, C.BitsPerOctet);
        if ((_control4 & C.Nrzi) != 0)
        {
            var level = _rxLastLevel;
            for (var index = 0; index < length; index++)
            {
                if (!bits[index])
                    level = !level;
                bits[index] = level;
            }
        }
        _stableIdle = false;
        _packetBits = bits;
        _packetLength = length;
        _packetOffset = 0;
        return true;
    }

    private bool NextPacketBit()
    {
        if (_packetBits is not null)
        {
            _packetLineLevel = _packetBits[_packetOffset++];
            if (_packetOffset == _packetLength)
                _packetBits = null;
            return _packetLineLevel;
        }
        var bit = _lineIdle || ((C.Flag >> _fillBit) & 1) != 0;
        _fillBit = (_fillBit + 1) & (C.BitsPerOctet - 1);
        if ((_control4 & C.Nrzi) == 0)
            return bit;
        if (!bit)
            _packetLineLevel = !_packetLineLevel;
        return _packetLineLevel;
    }

    private static void AppendRaw(bool[] bits, ref int length, uint value, int count)
    {
        for (var bit = 0; bit < count; bit++)
            bits[length++] = ((value >> bit) & 1) != 0;
    }

    private static void AppendStuffed(bool[] bits, ref int length, bool bit, ref int ones)
    {
        bits[length++] = bit;
        ones = bit ? ones + 1 : 0;
        if (ones == C.StuffingOnes)
        {
            bits[length++] = false;
            ones = 0;
        }
    }
}
