using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Banque interne du VDP, TI MP010A §2.2 : décodage aux mutations.
/// Les transactions, l'horloge et les notifications physiques appartiennent au VDP.
/// Cette banque seule ne constitue pas une puce vidéo fonctionnelle.</summary>
internal sealed class Tms9918Registers
{
    private readonly byte[] _values = new byte[8];
    internal Tms9918DisplayMode Mode;
    internal int ModeBits, NameBase, ColorBase, PatternBase, SpriteAttributeBase, SpritePatternBase;
    internal int GraphicsIIPatternBase, GraphicsIIColorBase, GraphicsIIPatternMask, GraphicsIIColorMask;
    internal byte TextColor, BackdropColor;
    internal bool DisplayEnabled, InterruptEnabled, SixteenKilobyteVram, ExternalVideoEnabled;
    internal bool LargeSprites, MagnifiedSprites;
    internal bool UsesTextTiming;
    internal readonly Tms9918SpriteRows SpriteRows = new();

    internal Tms9918Registers(Tms9918RegisterState initial)
    {
        _values[0] = initial.R0; _values[1] = initial.R1;
        _values[2] = initial.R2; _values[3] = initial.R3;
        _values[4] = initial.R4; _values[5] = initial.R5;
        _values[6] = initial.R6; _values[7] = initial.R7;
        for (int register = 0; register < _values.Length; register++) Decode(register);
    }

    internal Tms9918RegisterState Capture() => new(_values[0], _values[1], _values[2], _values[3],
        _values[4], _values[5], _values[6], _values[7]);

    internal bool Write(int register, byte value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(register);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(register, _values.Length);
        if (_values[register] == value) return false;
        _values[register] = value;
        Decode(register);
        return true;
    }

    // §2.1.7 : RESET efface R0 et R1 ; il ne promet pas R2–R7 = 0.
    // La qualification temporelle du RESET appartient au cœur, pas à la banque.
    internal void ApplyReset() { Write(0, 0); Write(1, 0); }

    private void Decode(int register)
    {
        byte value = _values[register];
        switch (register)
        {
            case 0:
            case 1:
                byte r0 = _values[0], r1 = _values[1];
                ModeBits = ((r1 >> 2) & 4) | ((r1 >> 2) & 2) | ((r0 >> 1) & 1);
                Mode = ModeBits switch
                {
                    0 => Tms9918DisplayMode.GraphicsI,
                    1 => Tms9918DisplayMode.GraphicsII,
                    2 => Tms9918DisplayMode.Multicolor,
                    3 => Tms9918DisplayMode.GraphicsIIMulticolor,
                    4 => Tms9918DisplayMode.Text,
                    5 => Tms9918DisplayMode.GraphicsIIText,
                    6 => Tms9918DisplayMode.TextMulticolor,
                    7 => Tms9918DisplayMode.GraphicsIITextMulticolor,
                    _ => Tms9918DisplayMode.UndocumentedCombination,
                };
                UsesTextTiming = (r1 & 0x10) != 0;
                ExternalVideoEnabled = (r0 & 1) != 0;
                DisplayEnabled = (r1 & 0x40) != 0;
                InterruptEnabled = (r1 & 0x20) != 0;
                SixteenKilobyteVram = (r1 & 0x80) != 0;
                LargeSprites = (r1 & 2) != 0;
                MagnifiedSprites = (r1 & 1) != 0;
                UpdateSpriteRows();
                break;
            case 2: NameBase = (value & 15) << 10; break;
            case 3:
                ColorBase = value << 6;
                GraphicsIIColorBase = (value & 0x80) << 6;
                GraphicsIIColorMask = ((value & 0x7f) << 6) | 0x3f;
                UpdateGraphicsIIPatternMask();
                UpdateSpriteRows();
                break;
            case 4:
                PatternBase = (value & 7) << 11;
                GraphicsIIPatternBase = (value & 4) << 11;
                UpdateGraphicsIIPatternMask();
                UpdateSpriteRows();
                break;
            case 5: SpriteAttributeBase = (value & 0x7f) << 7; break;
            case 6: SpritePatternBase = (value & 7) << 11; break;
            case 7: TextColor = (byte)(value >> 4); BackdropColor = (byte)(value & 15); break;
        }
    }

    // TI guide 1984 §8.4.2 décrit les tables réduites (R3=9F,R4=00).
    // Analyse Nouspikel : R3 fournit aussi les bits bas du masque pattern.
    // Les masques restent dérivés des registres, jamais un deuxième état sauvé.
    private void UpdateGraphicsIIPatternMask() =>
        GraphicsIIPatternMask = ((_values[4] & 3) << 11) | (GraphicsIIColorMask & 0x7ff);

    private void UpdateSpriteRows() => SpriteRows.Configure(_values[0], _values[3], _values[4]);
}
