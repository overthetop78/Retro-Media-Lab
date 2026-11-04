using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Interfaces;
using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.BbcVideoUlaConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

/// <summary>VIDPROC BBC : horloges, sérialisation, palette et multiplexage RGB.</summary>
/// <remarks>Write représente une écriture de bus déjà sélectionnée par la carte. Reset est
/// l'initialisation déterministe de l'émulateur, pas une broche RESET inexistante de l'ULA.</remarks>
public sealed partial class BbcVideoUla : IBbcVideoUla
{
    private readonly byte[] _palette = new byte[C.PaletteEntries];
    private readonly byte[] _colours = new byte[C.PaletteEntries];
    private int _pixelPeriod = C.FastCharacterPeriod;
    public byte Control { get; private set; }
    public byte Data { get; set; }
    public bool Cursor { get; set; }
    public bool TeletextSelected { get; private set; }
    public bool FastCrtc { get; private set; }

    public BbcVideoUla() => Reset();

    public void Write(ushort address, byte value)
    {
        if ((address & C.RegisterSelectMask) == 0)
        {
            var changed = Control ^ value;
            Control = value;
            TeletextSelected = (value & C.TeletextControlBit) != 0;
            FastCrtc = (value & C.FastCrtcBit) != 0;
            _pixelPeriod = C.FastCharacterPeriod >> ((value >> C.PixelRateShift) & C.PixelRateMask);
            if ((changed & C.FlashControlBit) != 0)
                for (var index = 0; index < C.PaletteEntries; index++) UpdateColour(index);
        }
        else
        {
            var index = value >> C.PaletteIndexShift;
            _palette[index] = (byte)(value & C.PaletteValueMask);
            UpdateColour(index);
        }
        UpdateOutput();
    }

    private void UpdateColour(int index)
    {
        var raw = _palette[index];
        var complement = (raw & C.FlashPaletteBit) == 0 || (Control & C.FlashControlBit) == 0;
        _colours[index] = (byte)((raw ^ (complement ? C.ColourMask : 0)) & C.ColourMask);
    }

    public void Reset()
    {
        Array.Fill(_palette, (byte)C.ColourMask);
        Control = 0;
        TeletextSelected = FastCrtc = false;
        Data = 0;
        Cursor = false;
        _pixelPeriod = C.FastCharacterPeriod;
        _phase = 0;
        _masterClock = false;
        _shift = _logical = _cursorSegments = 0;
        _displayEnabled = _inverted = false;
        _teletextRgb = 0;
        for (var index = 0; index < C.PaletteEntries; index++) UpdateColour(index);
        UpdateOutput();
    }
}
