using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Dictionaries;

/// <summary>Décode les cinq plans de bits de la ROM interne MC6847, extraite du silicium.</summary>
public static class AtomCharacterGenerator
{
    public static byte[] Decode(ReadOnlySpan<byte> rom)
    {
        if (rom.Length != C.CharacterRomBytes) throw new ArgumentException(nameof(rom));
        var glyphs = new byte[C.CharacterCount * C.CharacterHeight];
        for (var character = 0; character < C.CharacterCount; character++)
        {
            var byteColumn = character / C.ByteBits;
            var bit = C.ByteHighBit - character % C.ByteBits;
            for (var row = 0; row < C.GlyphRows; row++)
                for (var column = 0; column < C.GlyphColumns; column++)
                {
                    var address = (C.RawFontBottomRow + row) * C.RawFontBytesPerRow
                        + byteColumn - column * C.RawFontColumnStride;
                    if ((rom[address] & (1 << bit)) != 0)
                        glyphs[character * C.CharacterHeight + C.GlyphTop + row] |= (byte)(1 << (C.ForegroundGlyphBit - column));
                }
        }
        return glyphs;
    }
}
