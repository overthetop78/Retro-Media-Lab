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
    private readonly ulong[] _packedColours = new ulong[C.PackedPatternCount];
    private uint _paletteRevision = C.InitialPaletteRevision;
    private int _pixelPeriod = C.FastCharacterPeriod;
    private byte _control;
    private byte _data;
    private bool _cursor;
    private bool _teletextSelected;
    private bool _fastCrtc;
    private int _characterPeriodMask = C.SlowCharacterPeriod - 1;
    private int _packedPatternBase;
    private int _packedShifts = C.PackedMasterPeriods / C.FastCharacterPeriod;
    private int _packedFill = C.ShiftDataMask >> (C.ShiftDataBits - C.PackedMasterPeriods / C.FastCharacterPeriod);
    private int _cursorPattern;
    public byte Control => _control;
    public byte Data { get => _data; set => _data = value; }
    public bool Cursor { get => _cursor; set => _cursor = value; }
    public bool TeletextSelected => _teletextSelected;
    public bool FastCrtc => _fastCrtc;

    public BbcVideoUla() => Reset();

    /// <summary>Équivalent aux trois setters successifs DATA, CURSOR, DE ; aucune horloge n'avance.</summary>
    public void SetCharacterInputs(byte data, bool cursor, bool displayEnabled)
    {
        _data = data;
        _cursor = cursor;
        if (_displayEnabled == displayEnabled) return;
        _displayEnabled = displayEnabled;
        UpdateOutput();
    }

    public void Write(ushort address, byte value)
    {
        if ((address & C.RegisterSelectMask) == 0)
        {
            var changed = _control ^ value;
            _control = value;
            _teletextSelected = (value & C.TeletextControlBit) != 0;
            _fastCrtc = (value & C.FastCrtcBit) != 0;
            _pixelPeriod = C.FastCharacterPeriod >> ((value >> C.PixelRateShift) & C.PixelRateMask);
            _characterPeriodMask = (_fastCrtc ? C.FastCharacterPeriod : C.SlowCharacterPeriod) - 1;
            _packedPatternBase = ((value >> C.PixelRateShift) & C.PixelRateMask) * (C.ShiftDataMask + 1);
            _packedShifts = C.PackedMasterPeriods / _pixelPeriod;
            _packedFill = C.ShiftDataMask >> (C.ShiftDataBits - _packedShifts);
            _cursorPattern = ((value & C.FirstCursorBit) != 0 ? C.FirstCursorSegment : 0)
                | ((value & C.SecondCursorBit) != 0 ? C.SecondCursorSegment : 0)
                | ((value & C.LastCursorBit) != 0 ? C.LastCursorSegments : 0);
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
        var complement = (raw & C.FlashPaletteBit) == 0 || (_control & C.FlashControlBit) == 0;
        var colour = (byte)((raw ^ (complement ? C.ColourMask : 0)) & C.ColourMask);
        if (_colours[index] == colour) return;
        _colours[index] = colour;
        if (unchecked(++_paletteRevision) == 0)
        {
            Array.Clear(_packedColours);
            _paletteRevision = C.InitialPaletteRevision;
        }
    }

    public void Reset()
    {
        Array.Fill(_palette, (byte)C.ColourMask);
        _control = _data = 0;
        _teletextSelected = _fastCrtc = _cursor = false;
        _pixelPeriod = C.FastCharacterPeriod;
        _characterPeriodMask = C.SlowCharacterPeriod - 1;
        _packedPatternBase = _cursorPattern = 0;
        _packedShifts = C.PackedMasterPeriods / C.FastCharacterPeriod;
        _packedFill = C.ShiftDataMask >> (C.ShiftDataBits - _packedShifts);
        _phase = 0;
        _masterClock = false;
        _shift = _logical = _cursorSegments = 0;
        _displayEnabled = _inverted = false;
        _teletextRgb = 0;
        for (var index = 0; index < C.PaletteEntries; index++) UpdateColour(index);
        UpdateOutput();
    }
}
