using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    private static readonly byte[] ReversedBytes = CreateReversedBytes();

    internal static int ReverseSerialMask(int mask) =>
        ((ReversedBytes[mask & C.ByteMask] << C.ByteBits) | ReversedBytes[mask >> C.ByteBits]) >> C.ReversePaddingBits;

    /// <summary>Le bit de poids faible décrit le prochain point ; chaque point occupe un triplet RGB.</summary>
    internal static ulong PackSerialColours(int mask, int count, int foreground, int background)
    {
        ulong points = (uint)mask;
        points = (points | (points << C.SpreadFirstShift)) & C.SpreadFirstMask;
        points = (points | (points << C.SpreadSecondShift)) & C.SpreadSecondMask;
        points = (points | (points << C.SpreadThirdShift)) & C.SpreadThirdMask;
        points = (points | (points << C.SpreadLastShift)) & C.SpreadLastMask;
        var off = C.RepeatedColour * (uint)background;
        var difference = C.RepeatedColour * (uint)(foreground ^ background);
        return (off ^ (difference & (points * C.ColourMask))) & ((1UL << (count * C.ColourBits)) - 1);
    }

    private static byte[] CreateReversedBytes()
    {
        var values = new byte[C.ByteMask + 1];
        for (var value = 0; value < values.Length; value++)
        {
            var reversed = 0;
            for (var bit = 0; bit < C.ByteBits; bit++) reversed = (reversed << 1) | ((value >> bit) & 1);
            values[value] = (byte)reversed;
        }
        return values;
    }
}
