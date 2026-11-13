using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using K = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918VramSlotKind;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Transcription des sélections du diagramme TI Maggi 1978 confrontée
/// à sa reconstruction Vermaelen. Ne représente pas une mesure de RAS/CAS du 9918A.
/// Les incertitudes de révision, bordure Multicolor et sprites absents sont consignées.</summary>
internal static class Tms9918VramSchedule
{
    private const int Slots = 171;
    private const int CoordinateOrigin = 63;
    private static readonly Tms9918VramSlot[] Graphics = CreateGraphics();
    private static readonly Tms9918VramSlot[] Text = CreateText();
    private static readonly Tms9918VramSlot[] Multicolor = CreateMulticolor();
    private static readonly Tms9918VramSlot[] Refresh = CreateRefresh();

    internal static Tms9918VramSlot At(int dot, Tms9918DisplayMode mode, bool refresh)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dot, 342);
        if ((dot & 1) != 0) throw new ArgumentException("VRAM selections start on even raster dots.");
        var table = refresh ? Refresh : mode switch
        {
            Tms9918DisplayMode.Text or Tms9918DisplayMode.GraphicsIIText
                or Tms9918DisplayMode.TextMulticolor or Tms9918DisplayMode.GraphicsIITextMulticolor => Text,
            Tms9918DisplayMode.Multicolor or Tms9918DisplayMode.GraphicsIIMulticolor => Multicolor,
            _ => Graphics,
        };
        return table[dot >> 1];
    }

    private static Tms9918VramSlot[] CreateGraphics()
    {
        var slots = new Tms9918VramSlot[Slots]; // zéro = CPU
        for (int column = 0, sprite = 8; column < 32; column++)
        {
            Put(slots, 53 + 8 * column, K.BackgroundName, column);
            Put(slots, 57 + 8 * column, K.BackgroundColour, column);
            Put(slots, 59 + 8 * column, K.BackgroundPattern, column);
            if ((column & 3) != 0) Put(slots, 55 + 8 * column, K.SpriteVisibility, sprite++);
        }
        for (int sprite = 0; sprite < 8; sprite++) Put(slots, 37 + 2 * sprite, K.SpriteVisibility, sprite);
        ReadOnlySpan<int> spriteCoordinates = [313, 325, 337, 17];
        ReadOnlySpan<K> kinds = [K.SpriteY, K.SpriteX, K.SpriteName, K.SpriteColour,
            K.SpritePatternLeft, K.SpritePatternRight];
        for (int sprite = 0; sprite < 4; sprite++)
            for (int part = 0; part < kinds.Length; part++)
            {
                int coordinate = spriteCoordinates[sprite] + 2 * part;
                if (coordinate >= 341) coordinate = coordinate - 342 + 10; // CPU 341,1,3,5,7
                Put(slots, coordinate, kinds[part], sprite);
            }
        return slots;
    }

    private static Tms9918VramSlot[] CreateText()
    {
        var slots = new Tms9918VramSlot[Slots];
        for (int column = 0; column < 40; column++)
        {
            Put(slots, 59 + 6 * column, K.BackgroundName, column);
            Put(slots, 63 + 6 * column, K.BackgroundPattern, column);
        }
        return slots;
    }

    private static Tms9918VramSlot[] CreateMulticolor()
    {
        var slots = (Tms9918VramSlot[])Graphics.Clone();
        for (int index = 0; index < slots.Length; index++)
            if (slots[index].Kind == K.BackgroundColour) slots[index] = default;
        return slots;
    }

    private static Tms9918VramSlot[] CreateRefresh()
    {
        var slots = new Tms9918VramSlot[Slots];
        for (int coordinate = 53; coordinate <= 305; coordinate += 4)
            Put(slots, coordinate, K.Refresh, (coordinate - 53) / 4);
        return slots;
    }

    private static void Put(Tms9918VramSlot[] slots, int coordinate, K kind, int index)
    {
        int dot = (coordinate + 342 - CoordinateOrigin) % 342;
        if ((dot & 1) != 0) throw new InvalidOperationException("Invalid VRAM selection phase.");
        int slot = dot >> 1;
        if (slots[slot].Kind != K.Cpu) throw new InvalidOperationException("Overlapping VRAM selections.");
        slots[slot] = new(kind, index);
    }
}
