using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;
using V = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

/// <summary>Connexions vidéo Atom et récepteur RGB du MC6847 ; aucun second décodeur de pixels.</summary>
public sealed class AtomVideo : Interfaces.IAtomVideo
{
    private readonly Func<int, byte> _read;
    private readonly Mc6847 _chip;
    private byte[] _drawing = new byte[C.FrameBytes];
    private byte[] _completed = new byte[C.FrameBytes];
    private Mc6847Mode _controls;
    private static readonly int[] Colours =
    [
        C.Black, C.Green, C.Yellow, C.Blue, C.Red, C.Buff, C.Cyan, C.Magenta, C.Orange,
        C.TextDarkGreen, C.TextDarkOrange, C.TextBrightOrange
    ];

    public AtomVideo(Func<int, byte> read, ReadOnlySpan<byte> characters)
    {
        ArgumentNullException.ThrowIfNull(read);
        _read = read;
        _chip = new(characters);
        _chip.DataRequested = ReadData;
    }

    public byte[] CompletedFrame => _completed;
    public long Frame => _chip.Field;
    public bool FrameSync => _chip.FieldSync;

    public void SetMode(byte value)
    {
        var graphics = (value & C.GraphicsSelect) != 0 ? Mc6847Mode.Graphics : Mc6847Mode.None;
        var modes = (Mc6847Mode)(((value >> C.GraphicsModeShift) & C.GraphicsModeMask) << V.GraphicsModeShift);
        _controls = (_controls & Mc6847Mode.ColourSet) | graphics | modes;
        ApplyControls();
    }

    public void SetColorSet(bool value)
    {
        _controls = value ? _controls | Mc6847Mode.ColourSet : _controls & ~Mc6847Mode.ColourSet;
        ApplyControls();
    }

    private void ApplyControls() => _chip.Mode = (_chip.Mode & C.DataModeMask) | _controls;

    private void ReadData()
    {
        var address = _chip.Address;
        if (address is null) return;
        var data = _read(address.Value);
        _chip.Data = data;
        var alpha = (data & C.SemiGraphicsSelect) != 0 ? Mc6847Mode.Semigraphics | Mc6847Mode.External : Mc6847Mode.None;
        if ((data & C.InverseSelect) != 0) alpha |= Mc6847Mode.Inverse;
        _chip.Mode = _controls | alpha;
    }

    public void Reset()
    {
        _controls = Mc6847Mode.None;
        _chip.Reset();
        Array.Clear(_drawing);
        Array.Clear(_completed);
    }

    /// <summary>Une période CLK du VDG, soit deux points ; les demi-cellules ne sont pas dessinées à l’avance.</summary>
    public bool Tick()
    {
        var frame = Frame;
        for (var half = 0; half < V.HalfClocksPerClock; half++)
        {
            _chip.SetClock(!_chip.Clock);
            if (_chip.Active)
            {
                var x = _chip.HalfClock - V.ActiveHalfClockStart;
                var y = _chip.Scanline - V.ActiveLineStart;
                WritePixel(x, y, Colours[(int)_chip.Colour]);
            }
            if (Frame != frame) (_drawing, _completed) = (_completed, _drawing);
        }
        return Frame != frame;
    }

    private void WritePixel(int x, int y, int colour)
    {
        var offset = (y * C.Width + x) * C.PixelBytes;
        _drawing[offset] = (byte)(colour & C.ColorChannelMask);
        _drawing[offset + 1] = (byte)((colour >> C.ColorChannelShift) & C.ColorChannelMask);
        _drawing[offset + 2] = (byte)((colour >> C.RedChannelShift) & C.ColorChannelMask);
    }
}
