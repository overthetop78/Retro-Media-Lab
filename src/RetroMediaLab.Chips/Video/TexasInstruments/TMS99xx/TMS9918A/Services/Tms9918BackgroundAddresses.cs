using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>TI MP010A §2.4 et §3.2 : formation combinatoire des adresses.
/// Le séquenceur choisit la phase de lecture ; ce bloc n'accède pas à la RAM.</summary>
internal static class Tms9918BackgroundAddresses
{
    internal static Tms9918FetchAddress Name(Tms9918Registers registers, int line, int column)
    {
        ValidateLine(line);
        int columns = registers.UsesTextTiming ? 40 : 32;
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, columns);
        int offset = (line >> 3) * columns + column;
        return new((ushort)(registers.NameBase | offset),
            registers.Mode != Tms9918DisplayMode.UndocumentedCombination);
    }

    internal static Tms9918FetchAddress Pattern(Tms9918Registers registers, int line, byte name, bool nameKnown)
    {
        ValidateLine(line);
        int address = registers.PatternBase | name << 3 | (line & 7);
        bool qualified = nameKnown;
        switch (registers.Mode)
        {
            case Tms9918DisplayMode.GraphicsII:
                address = registers.GraphicsIIPatternBase
                    | ((((line & 0xc0) << 5) | name << 3 | (line & 7)) & registers.GraphicsIIPatternMask);
                break;
            case Tms9918DisplayMode.GraphicsIIText:
                address = registers.GraphicsIIPatternBase
                    | ((((line & 0xc0) << 5) | name << 3 | (line & 7))
                        & ((registers.GraphicsIIPatternMask & 0x1800) | 0x7ff));
                break;
            case Tms9918DisplayMode.GraphicsIIMulticolor:
                address = registers.GraphicsIIPatternBase
                    | ((((line & 0xc0) << 5) | name << 3 | ((line >> 2) & 7))
                        & ((registers.GraphicsIIPatternMask & 0x1800) | 0x7ff));
                break;
            case Tms9918DisplayMode.Multicolor:
                address = registers.PatternBase | name << 3 | ((line >> 2) & 7);
                break;
            case Tms9918DisplayMode.UndocumentedCombination:
                qualified = false;
                break;
        }
        return new((ushort)address, qualified);
    }

    internal static Tms9918FetchAddress Colour(Tms9918Registers registers, int line, byte name, bool nameKnown)
    {
        ValidateLine(line);
        return registers.Mode switch
        {
            Tms9918DisplayMode.GraphicsI => new((ushort)(registers.ColorBase | name >> 3), nameKnown),
            Tms9918DisplayMode.GraphicsII => new((ushort)(registers.GraphicsIIColorBase
                | ((((line & 0xc0) << 5) | name << 3 | (line & 7)) & registers.GraphicsIIColorMask)), nameKnown),
            _ => new(0, false), // aucune table couleur dans ces modes
        };
    }

    private static void ValidateLine(int line)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(line);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(line, 192);
    }
}
