using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.BbcVideoUlaConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class BbcVideoUla
{
    private static readonly byte[] PackedLogicalPixels = CreatePackedLogicalPixels();
    private bool _masterClock;
    private int _phase;
    private int _shift;
    private int _logical;
    private int _cursorSegments;
    public bool Clock8 => (_phase & C.Clock8Mask) != 0;
    public bool Clock4 => (_phase & C.Clock4Mask) != 0;
    public bool Clock2 => (_phase & C.Clock2Mask) != 0;
    public bool Clock1 => (_phase & C.Clock1Mask) != 0;
    public bool CrtcClock => (_phase & (_fastCrtc ? C.Clock2Mask : C.Clock1Mask)) != 0;
    public bool CursorActive => (_cursorSegments & C.FirstCursorSegment) != 0;

    /// <summary>Entrée 16 MHz : seuls ses fronts montants avancent les diviseurs.
    /// La phase zéro est l'origine numérique commune du chargement et des pixels.</summary>
    public void SetMasterClock(bool level)
    {
        if (_masterClock == level) return;
        _masterClock = level;
        if (!level) return;
        AdvanceMasterClock();
    }

    // La carte utilise ce chemin lorsque son horloge est déjà une suite de périodes complètes.
    internal void AdvanceMasterClock()
    {
        var previousCursor = _cursorSegments & C.FirstCursorSegment;
        var previousLogical = _logical;
        if ((_phase & _characterPeriodMask) == 0)
        {
            LoadCharacter();
        }
        if ((_phase & (_pixelPeriod - 1)) == 0)
        {
            // Les quatre prises physiques restent actives dans tous les modes.
            // L'insertion de 1 explique les couleurs supplémentaires des modes atypiques.
            ShiftPixel();
        }
        _phase = (_phase + 1) & C.ClockPhaseMask;
        if (previousLogical != _logical || previousCursor != (_cursorSegments & C.FirstCursorSegment)) UpdateOutput();
    }

    private void LoadCharacter()
    {
        _shift = _data;
        _cursorSegments >>= 1;
        if (_cursor) _cursorSegments = _cursorPattern;
    }

    private void ShiftPixel()
    {
        _logical = C.GetColourTap(_shift);
        _shift = ((_shift << 1) | C.ShiftInputBit) & C.ShiftDataMask;
    }

    /// <summary>Périodes maître complètes, sans dépendance à la cadence ou aux autres circuits de la carte.</summary>
    public uint ClockMasterPeriods(uint inputRgb)
    {
        uint result = 0;
        if ((_phase & (C.PackedMasterPeriods - 1)) != 0)
        {
            for (var dot = 0; dot < C.PackedMasterPeriods; dot++)
            {
                TeletextRgb = (int)(inputRgb >> (dot * C.ColourBits)) & C.ColourMask;
                AdvanceMasterClock();
                result |= (uint)_rgb << (dot * C.ColourBits);
            }
            return result;
        }
        if ((_phase & _characterPeriodMask) == 0) LoadCharacter();
        var xor = _inverted ? C.ColourMask : 0;
        if (_displayEnabled && (_cursorSegments & C.FirstCursorSegment) != 0) xor ^= C.ColourMask;
        var entry = _packedPatternBase + _shift;
        var pattern = entry * C.PackedMasterPeriods;
        if (_teletextSelected) result = inputRgb & C.PackedPixelMask;
        else if (_displayEnabled) result = ResolvePackedColours(entry, pattern);
        result ^= (uint)xor * C.RepeatedPackedColour;
        _logical = PackedLogicalPixels[pattern + C.PackedMasterPeriods - 1];
        _shift = ((_shift << _packedShifts) | _packedFill) & C.ShiftDataMask;
        _phase = (_phase + C.PackedMasterPeriods) & C.ClockPhaseMask;
        _teletextRgb = (int)(inputRgb >> C.LastPackedPixelShift) & C.ColourMask;
        _rgb = (int)(result >> C.LastPackedPixelShift) & C.ColourMask;
        return result;
    }

    private uint ResolvePackedColours(int entry, int pattern)
    {
        var cached = _packedColours[entry];
        if ((uint)(cached >> C.PaletteRevisionShift) == _paletteRevision) return (uint)cached;
        uint colours = 0;
        for (var dot = 0; dot < C.PackedMasterPeriods; dot++)
            colours |= (uint)_colours[PackedLogicalPixels[pattern + dot]] << (dot * C.ColourBits);
        _packedColours[entry] = ((ulong)_paletteRevision << C.PaletteRevisionShift) | colours;
        return colours;
    }

    private static byte[] CreatePackedLogicalPixels()
    {
        var values = new byte[(C.PixelRateMask + 1) * (C.ShiftDataMask + 1) * C.PackedMasterPeriods];
        for (var mode = 0; mode <= C.PixelRateMask; mode++)
        for (var data = 0; data <= C.ShiftDataMask; data++)
        {
            var hold = C.FastCharacterPeriod >> mode;
            var shift = data;
            var logical = 0;
            var start = (mode * (C.ShiftDataMask + 1) + data) * C.PackedMasterPeriods;
            for (var dot = 0; dot < C.PackedMasterPeriods; dot++)
            {
                if ((dot & (hold - 1)) == 0)
                {
                    logical = C.GetColourTap(shift);
                    shift = ((shift << 1) | C.ShiftInputBit) & C.ShiftDataMask;
                }
                values[start + dot] = (byte)logical;
            }
        }
        return values;
    }
}
