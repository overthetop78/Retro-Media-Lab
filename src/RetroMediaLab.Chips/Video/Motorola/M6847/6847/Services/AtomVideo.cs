using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

/// <summary>MC6847 d’Atom : pixels actifs, modes texte/semi-graphiques/graphiques et FS.</summary>
/// <remarks>Le bus fournit la RAM vidéo ; la machine fournit l’horloge VDG.
/// Les tampons et la copie des caractères sont possédés par ce composant géré.</remarks>
public sealed class AtomVideo
:RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Interfaces.IAtomVideo{
    private readonly Func<int, byte> _read;
    private readonly byte[] _characters;
    private byte[] _drawing = new byte[C.FrameBytes];
    private byte[] _completed = new byte[C.FrameBytes];
    private int _clock;
    private int _line;
    private byte _mode;
    private bool _colorSet;
    private static readonly int[] Colors = [C.Green, C.Yellow, C.Blue, C.Red, C.Buff, C.Cyan, C.Magenta, C.Orange];

    public AtomVideo(Func<int, byte> read, ReadOnlySpan<byte> characters)
    {
        ArgumentNullException.ThrowIfNull(read);
        _read = read;
        _characters = AtomCharacterGenerator.Decode(characters);
    }

    public byte[] CompletedFrame => _completed;
    public long Frame { get; private set; }
    public bool FrameSync => _line < C.FrameSyncLine;
    public void SetMode(byte value) => _mode = value;
    public void SetColorSet(bool value) => _colorSet = value;

    public void Reset()
    {
        _clock = _line = 0;
        _mode = 0;
        _colorSet = false;
        Frame = 0;
        Array.Clear(_drawing);
        Array.Clear(_completed);
    }

    public bool Tick()
    {
        var y = _line - C.ActiveLineStart;
        var column = _clock - C.ActiveClockStart;
        if (y is >= 0 and < C.Height && column is >= 0 and < C.ActiveClocks)
        {
            var x = column * C.PixelsPerClock;
            for (var pixel = 0; pixel < C.PixelsPerClock; pixel++) WritePixel(x + pixel, y, ColorAt(x + pixel, y));
        }
        if (++_clock < C.ClocksPerLine) return false;
        _clock = 0;
        if (++_line < C.LinesPerFrame) return false;
        _line = 0;
        Frame++;
        (_drawing, _completed) = (_completed, _drawing);
        return true;
    }

    private int ColorAt(int x, int y)
    {
        if ((_mode & C.GraphicsSelect) != 0)
        {
            var mode = (_mode >> C.GraphicsModeShift) & C.GraphicsModeMask;
            var (samples, rows) = C.GraphicsGeometry[mode];
            var bits = (mode & C.MonochromeModeBit) == 0 ? C.BitsPerColorPixel : 1;
            var width = samples * C.ByteBits / bits;
            var sourceX = x * width / C.Width;
            var value = _read(y * rows / C.Height * samples + sourceX * bits / C.ByteBits);
            var shift = C.ByteBits - bits - sourceX * bits % C.ByteBits;
            var index = (value >> shift) & ((1 << bits) - 1);
            return bits == C.BitsPerColorPixel ? Colors[index + (_colorSet ? C.ColorSetOffset : 0)]
                : index == 0 ? C.Black : _colorSet ? C.Buff : C.Green;
        }
        var code = _read(y / C.CharacterHeight * C.Columns + x / C.CharacterWidth);
        var cellX = x % C.CharacterWidth;
        var cellY = y % C.CharacterHeight;
        if ((code & C.SemiGraphicsSelect) != 0)
        {
            var blockX = cellX * C.SemiGraphicsColumns / C.CharacterWidth;
            var blockY = cellY * C.SemiGraphicsRows / C.CharacterHeight;
            var block = C.SemiGraphicsRows * C.SemiGraphicsColumns - 1 - blockY * C.SemiGraphicsColumns - blockX;
            return (code & (1 << block)) == 0 ? C.Black
                : Colors[((code >> C.SemiGraphicsColorShift) & C.ColorPixelMask) + (_colorSet ? C.ColorSetOffset : 0)];
        }
        var glyph = _characters[(code & C.CharacterMask) * C.CharacterHeight + cellY];
        var lit = ((glyph >> (C.ByteHighBit - cellX)) & 1) != 0;
        if ((code & C.InverseSelect) != 0) lit = !lit;
        return _colorSet ? lit ? C.TextBrightOrange : C.TextDarkOrange : lit ? C.TextBrightGreen : C.TextDarkGreen;
    }

    private void WritePixel(int x, int y, int color)
    {
        var offset = (y * C.Width + x) * C.PixelBytes;
        _drawing[offset] = (byte)(color & C.ColorChannelMask);
        _drawing[offset + 1] = (byte)((color >> C.ColorChannelShift) & C.ColorChannelMask);
        _drawing[offset + 2] = (byte)((color >> C.RedChannelShift) & C.ColorChannelMask);
    }
}
