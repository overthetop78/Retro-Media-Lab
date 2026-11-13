using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Verrous du chemin pattern TI MP010A §2.4. Les octets sont livrés
/// à la fin des vrais services mémoire ; le balayage déclenche BeginCell/ShiftPixel.
/// Le composant ne lit ni table externe ni tableau de VRAM caché.</summary>
internal sealed class Tms9918BackgroundPipeline
{
    private Tms9918BackgroundState _state;

    internal Tms9918BackgroundPipeline(Tms9918BackgroundState initial)
    {
        if (initial.PixelPosition is < 0 or > 8)
            throw new ArgumentException("Invalid TMS9918 background shifter position.");
        _state = initial;
    }

    internal Tms9918BackgroundState Capture() => _state;
    internal byte Name => _state.Name;
    internal bool NameKnown => _state.NameKnown;
    internal void LoadName(byte value, bool known) => _state = _state with { Name = value, NameKnown = known };
    internal void LoadPattern(byte value, bool known) => _state = _state with { Pattern = value, PatternKnown = known };
    internal void LoadColour(byte value, bool known) => _state = _state with { Colour = value, ColourKnown = known };

    internal void BeginCell() => _state = _state with
    {
        ActivePattern = _state.Pattern, ActivePatternKnown = _state.PatternKnown,
        ActiveColour = _state.Colour, ActiveColourKnown = _state.ColourKnown, PixelPosition = 0,
    };

    private static readonly ulong[] PatternBytes = CreatePatternBytes();
    private static ulong[] CreatePatternBytes()
    {
        var table = new ulong[256];
        for (int pattern = 0; pattern < table.Length; pattern++)
            for (int pixel = 0; pixel < 8; pixel++)
                if ((pattern & (0x80 >> pixel)) != 0) table[pattern] |= 255UL << (pixel * 8);
        return table;
    }

    // Decode a physical shifter portion once. The active latches stay in this
    // owner; no direct VRAM lookup, replacement framebuffer or saved cache.
    internal (ulong Colours, byte Known) ShiftPixels(Tms9918Registers registers, int count)
    {
        int position = _state.PixelPosition;
        int width = registers.UsesTextTiming ? 6 : 8;
        if (count <= 0 || count > 8) throw new ArgumentOutOfRangeException(nameof(count));
        // A mode change can leave a shifter beyond the new cell width until
        // the next BeginCell. The scalar mechanism then emits unknown zero
        // without advancing it; a portion must preserve that exact state too.
        int valid = Math.Min(count, Math.Max(0, width - position));
        if (valid == 0) return (0, 0);
        byte foreground, background;
        ulong pattern;
        bool known = _state.ActivePatternKnown;
        switch (registers.Mode)
        {
            case Tms9918DisplayMode.GraphicsI:
            case Tms9918DisplayMode.GraphicsII:
                foreground = (byte)(_state.ActiveColour >> 4); background = (byte)(_state.ActiveColour & 15);
                pattern = PatternBytes[_state.ActivePattern]; known &= _state.ActiveColourKnown;
                break;
            case Tms9918DisplayMode.Multicolor:
            case Tms9918DisplayMode.GraphicsIIMulticolor:
                foreground = (byte)(_state.ActivePattern >> 4); background = (byte)(_state.ActivePattern & 15);
                pattern = 0x00000000ffffffffUL;
                break;
            case Tms9918DisplayMode.Text:
            case Tms9918DisplayMode.GraphicsIIText:
                foreground = registers.TextColor; background = registers.BackdropColor;
                pattern = PatternBytes[_state.ActivePattern];
                break;
            case Tms9918DisplayMode.TextMulticolor:
            case Tms9918DisplayMode.GraphicsIITextMulticolor:
                foreground = registers.TextColor; background = registers.BackdropColor;
                pattern = 0x00000000ffffffffUL; known = true;
                break;
            default:
                foreground = background = 0; pattern = 0; known = false;
                break;
        }
        if (foreground == 0) foreground = registers.BackdropColor;
        if (background == 0) background = registers.BackdropColor;
        const ulong repeat = 0x0101010101010101UL;
        ulong pixels = (pattern & (repeat * foreground)) | (~pattern & (repeat * background));
        _state = _state with { PixelPosition = position + valid };
        ulong used = valid == 8 ? ulong.MaxValue : (1UL << (valid * 8)) - 1;
        return ((pixels >> (position * 8)) & used, known ? (byte)((1 << valid) - 1) : (byte)0);
    }

    internal Tms9918Pixel ShiftPixel(Tms9918Registers registers)
    {
        int position = _state.PixelPosition;
        int width = registers.UsesTextTiming ? 6 : 8;
        if (position >= width) return new(0, false);
        byte colour;
        bool known = _state.ActivePatternKnown;
        switch (registers.Mode)
        {
            case Tms9918DisplayMode.GraphicsI:
            case Tms9918DisplayMode.GraphicsII:
                colour = (byte)((_state.ActivePattern & (0x80 >> position)) != 0
                    ? _state.ActiveColour >> 4 : _state.ActiveColour & 15);
                known &= _state.ActiveColourKnown;
                break;
            case Tms9918DisplayMode.Multicolor:
            case Tms9918DisplayMode.GraphicsIIMulticolor:
                colour = (byte)(position < 4 ? _state.ActivePattern >> 4 : _state.ActivePattern & 15);
                break;
            case Tms9918DisplayMode.Text:
            case Tms9918DisplayMode.GraphicsIIText:
                colour = (_state.ActivePattern & (0x80 >> position)) != 0
                    ? registers.TextColor : registers.BackdropColor;
                break;
            case Tms9918DisplayMode.TextMulticolor:
            case Tms9918DisplayMode.GraphicsIITextMulticolor:
                // Description des combinaisons M1/M3 : les tables n'affectent
                // plus les pixels ; quatre points texte puis deux backdrop.
                colour = position < 4 ? registers.TextColor : registers.BackdropColor;
                known = true;
                break;
            default:
                colour = 0; known = false;
                break;
        }
        _state = _state with { PixelPosition = position + 1 };
        // Une couleur transparente du plan pattern laisse voir le backdrop.
        if (colour == 0) colour = registers.BackdropColor;
        return new(colour, known);
    }
}
