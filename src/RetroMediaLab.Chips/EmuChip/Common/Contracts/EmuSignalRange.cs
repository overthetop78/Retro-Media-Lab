namespace RetroMediaLab.Chips.EmuChip.Common.Contracts;

/// <summary>Borrowed electrical words. Payload encoding belongs to its receiver;
/// this transport owns no clock, state, queue or physical component.</summary>
public readonly struct EmuSignalWord(long time, ulong bits)
{
    public readonly long Time = time;
    public readonly ulong Bits = bits;
}

public readonly ref struct EmuSignalRange(long start, long end, ulong initial, ReadOnlySpan<EmuSignalWord> words)
{
    public readonly long Start = start, End = end;
    public readonly ulong Initial = initial;
    public readonly ReadOnlySpan<EmuSignalWord> Words = words;

    public void Validate(long current, int maximum)
    {
        if (Start < current || End < Start || Words.Length > maximum)
            throw new ArgumentOutOfRangeException(nameof(End));
        long previous = current;
        foreach (ref readonly var word in Words)
        {
            if (word.Time < previous || word.Time > End || word.Time < Start)
                throw new ArgumentOutOfRangeException(nameof(Words));
            previous = word.Time;
        }
    }
}

public delegate void EmuSignalRangeHandler(in EmuSignalRange range);

public readonly struct EmuMemoryResponse(uint final, int responses, long next)
{
    public readonly uint Final = final;
    public readonly int Responses = responses;
    public readonly long Next = next;
}

public readonly ref struct EmuLogicResponse(ushort initial, ReadOnlySpan<EmuSignalWord> words, long? next)
{
    public readonly ushort Initial = initial;
    // Bits0..15 = resolved outputs; Bits16..23 = changed-pin mask.
    public readonly ReadOnlySpan<EmuSignalWord> Words = words;
    public readonly long? Next = next;
}

/// <summary>Native parallel-memory wire representation, not hardware state.
/// Pins/captures retain their existing public contracts.</summary>
public static class EmuMemoryPins
{
    public const ulong AddressMask = 0xffff, AddressKnownMask = 1UL << 16;
    public const ulong CsMask = 1UL << 17, OeMask = 1UL << 18, WeMask = 1UL << 19, Cs2Mask = 1UL << 20;
    public const ulong ControlsMask = CsMask | OeMask | WeMask, DataBitsMask = 0xffffUL << 24;
    public const ulong AllMask = AddressMask | AddressKnownMask | ControlsMask | Cs2Mask | DataBitsMask;
    public const int DataShift = 24, DataKnownShift = 32, OutputKnownShift = 8;
    public const uint OutputDrivenMask = 0x10000;
    internal const int StateVersion = 3, DefaultSupplyMillivolts = 5000;

    public static ulong Encode(ushort address, bool known, bool cs, bool oe, bool we, bool cs2, byte data = 0, byte dataKnown = 0)
        => address | (known ? AddressKnownMask : 0) | (cs ? CsMask : 0) | (oe ? OeMask : 0)
            | (we ? WeMask : 0) | (cs2 ? Cs2Mask : 0)
            | ((ulong)(data & dataKnown) << DataShift) | ((ulong)dataKnown << DataKnownShift);
}
