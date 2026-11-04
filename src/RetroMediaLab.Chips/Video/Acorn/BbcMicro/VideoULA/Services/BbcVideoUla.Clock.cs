using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.BbcVideoUlaConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class BbcVideoUla
{
    private static readonly byte[] CpuLogicalPixels = CreateCpuLogicalPixels();
    private bool _masterClock;
    private int _phase;
    private int _shift;
    private int _logical;
    private int _cursorSegments;
    public bool Clock8 => (_phase & C.Clock8Mask) != 0;
    public bool Clock4 => (_phase & C.Clock4Mask) != 0;
    public bool Clock2 => (_phase & C.Clock2Mask) != 0;
    public bool Clock1 => (_phase & C.Clock1Mask) != 0;
    public bool CrtcClock => FastCrtc ? Clock2 : Clock1;
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
        var period = FastCrtc ? C.FastCharacterPeriod : C.SlowCharacterPeriod;
        if ((_phase & (period - 1)) == 0)
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
        _shift = Data;
        _cursorSegments >>= 1;
        if (Cursor)
        {
            _cursorSegments = ((Control & C.FirstCursorBit) != 0 ? C.FirstCursorSegment : 0)
                | ((Control & C.SecondCursorBit) != 0 ? C.SecondCursorSegment : 0)
                | ((Control & C.LastCursorBit) != 0 ? C.LastCursorSegments : 0);
        }
    }

    private void ShiftPixel()
    {
        _logical = C.GetColourTap(_shift);
        _shift = ((_shift << 1) | C.ShiftInputBit) & C.ShiftDataMask;
    }

    /// <summary>Périodes complètes connues de la carte : toutes les entrées RGB et sorties des huit points sont conservées.</summary>
    internal uint EmitCpuPixels(uint teletext)
    {
        uint result = 0;
        if ((_phase & (C.CpuMasterClocks - 1)) != 0)
        {
            for (var dot = 0; dot < C.CpuMasterClocks; dot++)
            {
                TeletextRgb = (int)(teletext >> (dot * C.ColourBits)) & C.ColourMask;
                AdvanceMasterClock();
                result |= (uint)Rgb << (dot * C.ColourBits);
            }
            return result;
        }
        var periodMask = (FastCrtc ? C.FastCharacterPeriod : C.SlowCharacterPeriod) - 1;
        if ((_phase & periodMask) == 0) LoadCharacter();
        var xor = _inverted ? C.ColourMask : 0;
        if (_displayEnabled && (_cursorSegments & C.FirstCursorSegment) != 0) xor ^= C.ColourMask;
        var selected = TeletextSelected;
        var pattern = ((((Control >> C.PixelRateShift) & C.PixelRateMask) * (C.ShiftDataMask + 1)) + _shift) * C.CpuMasterClocks;
        for (var dot = 0; dot < C.CpuMasterClocks; dot++)
        {
            var logical = CpuLogicalPixels[pattern + dot];
            var colour = (selected ? (int)(teletext >> (dot * C.ColourBits)) & C.ColourMask : _displayEnabled ? _colours[logical] : 0) ^ xor;
            result |= (uint)colour << (dot * C.ColourBits);
        }
        _logical = CpuLogicalPixels[pattern + C.CpuMasterClocks - 1];
        var shifts = C.CpuMasterClocks / _pixelPeriod;
        _shift = ((_shift << shifts) | (C.ShiftDataMask >> (C.ShiftDataBits - shifts))) & C.ShiftDataMask;
        _phase = (_phase + C.CpuMasterClocks) & C.ClockPhaseMask;
        _teletextRgb = (int)(teletext >> C.LastCpuPixelShift) & C.ColourMask;
        Rgb = (int)(result >> C.LastCpuPixelShift) & C.ColourMask;
        return result;
    }

    private static byte[] CreateCpuLogicalPixels()
    {
        var values = new byte[(C.PixelRateMask + 1) * (C.ShiftDataMask + 1) * C.CpuMasterClocks];
        for (var mode = 0; mode <= C.PixelRateMask; mode++)
        for (var data = 0; data <= C.ShiftDataMask; data++)
        {
            var hold = C.FastCharacterPeriod >> mode;
            var shift = data;
            var logical = 0;
            var start = (mode * (C.ShiftDataMask + 1) + data) * C.CpuMasterClocks;
            for (var dot = 0; dot < C.CpuMasterClocks; dot++)
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
