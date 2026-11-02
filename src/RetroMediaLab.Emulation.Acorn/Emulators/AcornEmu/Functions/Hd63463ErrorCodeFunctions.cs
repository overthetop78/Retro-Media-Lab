using System.Numerics;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants.Hd63463Constants;
using E = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants.Hd63463ErrorCodeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

/// <summary>Polynômes série du HD63463 ; aucun syndrome n'est déduit d'un indicateur d'intégrité.</summary>
internal static class Hd63463ErrorCodeFunctions
{
    internal static uint Fire(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> data) =>
        AppendFire(AppendFire(0, prefix), data);

    private static uint AppendFire(uint state, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            state ^= (uint)value << E.FireByteShift;
            for (var bit = 0; bit < C.ByteBits; bit++)
                state = (state << 1) ^ ((state & E.FireHighBit) != 0 ? E.FirePolynomial : 0);
        }
        return state;
    }

    internal static ushort Crc(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> data, ushort polynomial, ushort initial) =>
        AppendCrc(AppendCrc(initial, prefix, polynomial), data, polynomial);

    private static ushort AppendCrc(ushort state, ReadOnlySpan<byte> data, ushort polynomial)
    {
        foreach (var value in data)
        {
            state ^= (ushort)(value << C.ByteBits);
            for (var bit = 0; bit < C.ByteBits; bit++)
                state = unchecked((ushort)((state << 1) ^ ((state & E.CrcHighBit) != 0 ? polynomial : 0)));
        }
        return state;
    }

    internal static bool Locate(uint syndrome, int dataBytes, out int address, out uint pattern)
    {
        address = 0;
        pattern = 0;
        if (syndrome == 0 || dataBytes < E.PatternBytes) return false;
        var candidate = ReverseZeros(syndrome, E.FireBits);
        var found = false;
        // Undo the zero suffix following each possible three-byte error window.
        // A Fire burst is at most eleven bits; equivalent overlapping windows
        // are normalized to the same physical bytes before checking uniqueness.
        for (var offset = dataBytes - E.PatternBytes; offset >= 0; offset--)
        {
            if ((candidate & E.PatternOverflowMask) == 0 && candidate != 0
                && E.FireBits - BitOperations.LeadingZeroCount(candidate) - BitOperations.TrailingZeroCount(candidate) <= E.MaximumBurstBits)
            {
                var leadingBytes = (candidate & E.PatternHighByteMask) != 0 ? 0
                    : (candidate & E.PatternMiddleByteMask) != 0 ? 1 : E.PatternBytes - 1;
                var normalizedAddress = Math.Min(offset + leadingBytes, dataBytes - E.PatternBytes);
                var normalizedPattern = (candidate << ((normalizedAddress - offset) * C.ByteBits)) & E.PatternMask;
                if (found && (address != normalizedAddress || pattern != normalizedPattern)) return false;
                found = true;
                address = normalizedAddress;
                pattern = normalizedPattern;
            }
            candidate = ReverseZeros(candidate, C.ByteBits);
        }
        return found;
    }

    private static uint ReverseZeros(uint state, int bits)
    {
        for (var bit = 0; bit < bits; bit++)
            state = (state & 1) != 0 ? ((state ^ E.FirePolynomial) >> 1) | E.FireHighBit : state >> 1;
        return state;
    }
}
