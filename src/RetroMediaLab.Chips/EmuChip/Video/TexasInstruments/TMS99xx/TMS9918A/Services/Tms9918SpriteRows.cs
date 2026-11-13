namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Transformations de coordonnées des sprites8–31 sur TI en M3.
/// Résultats matériels publiés par hap (MRC,18mai2012), y compris R3 et
/// troncatures. Les masques sont dérivés des registres, jamais sauvegardés
/// comme un second état matériel. Aucun sprite ni accès VRAM supplémentaire.</summary>
internal sealed class Tms9918SpriteRows
{
    private readonly Transform[] _blocks = new Transform[4];
    private readonly record struct Transform(bool Enabled, int LineMask = 255,
        int FirstXor = 0, int FirstOr = 0, int SecondLineMask = 255,
        int SecondXor = 0, int SecondOr = 0);

    internal void Configure(byte r0, byte r3, byte r4)
    {
        Array.Clear(_blocks);
        if ((r0 & 2) == 0 || (r4 & 3) == 3) return;
        if ((r4 & 1) == 0)
            _blocks[1] = new(true, 63, SecondOr: (~r3 << 1) & 64);
        if ((r4 & 2) == 0)
            _blocks[2] = new(true, FirstXor: 128,
                SecondXor: 128, SecondOr: (r3 << 1) & 128);
        int mask = (~r4 << 6) & 192;
        int lineMask = 255;
        if ((r3 & 64) == 0 && (mask & 128) != 0)
        { lineMask = 127; mask &= 127; }
        _blocks[3] = new(true, lineMask, FirstOr: mask,
            SecondLineMask: lineMask, SecondOr: mask);
    }

    internal int Offset(int sprite, int displayLine, byte y, int height)
    {
        int line = (displayLine - 1) & 255;
        var transform = _blocks[line >> 6];
        if (sprite < 8 || !transform.Enabled)
            return displayLine - ((y >= 0xe1 ? y - 256 : y) + 1);
        int offset = unchecked((byte)((line & transform.LineMask)
            - ((y ^ transform.FirstXor) | transform.FirstOr)));
        if (offset < height) return offset;
        return unchecked((byte)((line & transform.SecondLineMask)
            - ((y ^ transform.SecondXor) | transform.SecondOr)));
    }
}
