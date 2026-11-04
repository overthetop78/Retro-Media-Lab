using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    private int Glyph(int data, int row, int raster)
    {
        if (data < C.FirstCharacter || data >= C.FirstCharacter + C.CharacterCount || (uint)row >= C.CharacterRows) return 0;
        return _glyphs[((data - C.FirstCharacter) * C.CharacterRows + row) * C.RasterParities + (raster & 1)];
    }

    private int BuildGlyph(int data, int row, int raster)
    {
        if (data < C.FirstCharacter || data > C.CharacterDataMask || (uint)row >= C.CharacterRows || _characters.Length == 0) return 0;
        var current = Expanded(_characters[(data - C.FirstCharacter) * C.CharacterRows + row]);
        var neighbour = row + ((raster & 1) == 0 ? -1 : 1);
        var adjacent = (uint)neighbour < C.CharacterRows ? Expanded(_characters[(data - C.FirstCharacter) * C.CharacterRows + neighbour]) : 0;
        return current | ((current >> 1) & adjacent & ~(adjacent >> 1)) | ((current << 1) & adjacent & ~(adjacent << 1));
    }

    private static int Expanded(int value)
    {
        var result = 0;
        for (var bit = 0; bit < C.GlyphColumns; bit++) if ((value & (1 << bit)) != 0) result |= 3 << (bit * 2);
        return result;
    }

    private static int Mosaic(int data, int row, bool separated)
    {
        if ((uint)row >= C.CharacterRows || separated && row is C.FirstSeparatorRow or C.SecondSeparatorRow or C.LastSeparatorRow) return 0;
        var block = row < C.FirstMosaicBoundary ? 0 : row < C.SecondMosaicBoundary ? 2 : 4;
        return ((data & (1 << block)) != 0 ? separated ? C.SeparatedMosaicLeft : C.MosaicLeft : 0) | ((data & (1 << (block == 4 ? C.MosaicBottomRightBit : block + 1))) != 0 ? separated ? C.SeparatedMosaicRight : C.MosaicRight : 0);
    }
}
