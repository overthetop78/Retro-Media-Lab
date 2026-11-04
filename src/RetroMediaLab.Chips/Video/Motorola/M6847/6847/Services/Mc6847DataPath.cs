using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

/// <summary>Octet et sélections chargés dans le chemin vidéo ; aucun accès mémoire par point.</summary>
internal sealed class Mc6847DataPath
{
    private readonly byte[] _characters;
    internal ulong Pixels { get; private set; }
    internal int PixelCount { get; private set; } = C.ShortCellDots;
    internal int BytesPerLine { get; private set; } = C.ShortBytesPerLine;
    internal int RowsPerMemoryLine { get; private set; } = C.CharacterHeight;

    internal Mc6847DataPath(ReadOnlySpan<byte> characters)
    {
        _characters = AtomCharacterGenerator.Decode(characters);
    }

    internal void Load(byte data, int row, Mc6847Mode pins)
    {
        if ((uint)row >= C.CharacterHeight) throw new ArgumentOutOfRangeException(nameof(row));
        var graphics = (pins & Mc6847Mode.Graphics) != 0;
        var mode = ((int)pins >> C.GraphicsModeShift) & C.GraphicsModeMask;
        PixelCount = GetPixelCount(pins);
        BytesPerLine = GetBytesPerLine(pins);
        RowsPerMemoryLine = GetRowsPerMemoryLine(pins);
        var colourSet = (pins & Mc6847Mode.ColourSet) != 0;
        ulong pixels = 0;
        if (graphics)
        {
            var bits = (mode & C.ResolutionModeBit) == 0 ? C.ColourPixelBits : 1;
            var width = PixelCount / (C.DataBits / bits);
            for (var dot = 0; dot < PixelCount; dot++)
            {
                var value = (data >> (C.DataBits - bits - dot / width * bits)) & ((1 << bits) - 1);
                var colour = bits == C.ColourPixelBits
                    ? (Mc6847Colour)((int)Mc6847Colour.Green + value + (colourSet ? C.ColourBankSize : 0))
                    : value == 0 ? Mc6847Colour.Black : colourSet ? Mc6847Colour.Buff : Mc6847Colour.Green;
                pixels |= (ulong)colour << (dot * C.PackedColourBits);
            }
        }
        else if ((pins & Mc6847Mode.Semigraphics) != 0)
        {
            var six = (pins & Mc6847Mode.External) != 0;
            var blockHeight = six ? C.SemigraphicsSixRows : C.SemigraphicsFourRows;
            var colourIndex = six ? (data >> C.SemigraphicsSixColourShift) + (colourSet ? C.ColourBankSize : 0)
                : (data >> C.SemigraphicsFourColourShift) & C.SemigraphicsFourColourMask;
            var colour = (Mc6847Colour)((int)Mc6847Colour.Green + colourIndex);
            var shift = ((C.CharacterHeight - 1 - row) / blockHeight) * C.BlocksPerRow;
            for (var dot = 0; dot < PixelCount; dot++)
            {
                var lit = (data & (1 << (shift + 1 - dot / C.SemigraphicsBlockWidth))) != 0;
                pixels |= (ulong)(lit ? colour : Mc6847Colour.Black) << (dot * C.PackedColourBits);
            }
        }
        else
        {
            var pattern = (pins & Mc6847Mode.External) != 0 ? data : _characters[(data & C.CharacterMask) * C.CharacterHeight + row];
            if ((pins & Mc6847Mode.Inverse) != 0) pattern ^= byte.MaxValue;
            var foreground = colourSet ? Mc6847Colour.BrightOrange : Mc6847Colour.Green;
            var background = colourSet ? Mc6847Colour.DarkOrange : Mc6847Colour.DarkGreen;
            for (var dot = 0; dot < PixelCount; dot++)
                pixels |= (ulong)((pattern & (1 << (C.DataHighBit - dot))) != 0 ? foreground : background) << (dot * C.PackedColourBits);
        }
        Pixels = pixels;
    }

    internal static int GetPixelCount(Mc6847Mode pins)
    {
        var mode = ((int)pins >> C.GraphicsModeShift) & C.GraphicsModeMask;
        var longCell = (pins & Mc6847Mode.Graphics) != 0 && (mode == 0 || ((mode & C.ResolutionModeBit) != 0 && mode != C.GraphicsModeMask));
        return longCell ? C.LongCellDots : C.ShortCellDots;
    }

    internal static int GetBytesPerLine(Mc6847Mode pins) => GetPixelCount(pins) == C.LongCellDots ? C.LongBytesPerLine : C.ShortBytesPerLine;

    internal static int GetRowsPerMemoryLine(Mc6847Mode pins) => (pins & Mc6847Mode.Graphics) != 0
        ? C.GraphicsRowRepeats[((int)pins >> C.GraphicsModeShift) & C.GraphicsModeMask] : C.CharacterHeight;

    internal Mc6847Colour GetPixel(int dot) => (Mc6847Colour)((Pixels >> (dot * C.PackedColourBits)) & C.PackedColourMask);
}
