using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Contracts;
using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.BbcVideoUlaConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class BbcVideoUla
{
    /// <summary>Copie sans front maître ni modification des entrées.</summary>
    public BbcVideoUlaState CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Palette = (byte[])_palette.Clone(),
        Colours = (byte[])_colours.Clone(),
        PackedColours = (ulong[])_packedColours.Clone(),
        MasterClock = _masterClock,
        Phase = _phase,
        Shift = _shift,
        Logical = _logical,
        CursorSegments = _cursorSegments,
        PaletteRevision = _paletteRevision,
        PixelPeriod = _pixelPeriod,
        Control = _control,
        Data = _data,
        Cursor = _cursor,
        TeletextSelected = _teletextSelected,
        FastCrtc = _fastCrtc,
        CharacterPeriodMask = _characterPeriodMask,
        PackedPatternBase = _packedPatternBase,
        PackedShifts = _packedShifts,
        PackedFill = _packedFill,
        CursorPattern = _cursorPattern,
        DisplayEnabled = _displayEnabled,
        Inverted = _inverted,
        TeletextRgb = _teletextRgb,
        Rgb = _rgb,
    };

    /// <summary>Restaure les pixels partiellement décalés sans recalculer les sorties.</summary>
    public void RestoreState(BbcVideoUlaState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.Palette?.Length != C.PaletteEntries
            || state.Colours?.Length != C.PaletteEntries || state.PackedColours?.Length != C.PackedPatternCount)
            throw new ArgumentException(nameof(state));
        state.Palette.CopyTo(_palette, 0);
        state.Colours.CopyTo(_colours, 0);
        state.PackedColours.CopyTo(_packedColours, 0);
        _masterClock = state.MasterClock;
        _phase = state.Phase;
        _shift = state.Shift;
        _logical = state.Logical;
        _cursorSegments = state.CursorSegments;
        _paletteRevision = state.PaletteRevision;
        _pixelPeriod = state.PixelPeriod;
        _control = state.Control;
        _data = state.Data;
        _cursor = state.Cursor;
        _teletextSelected = state.TeletextSelected;
        _fastCrtc = state.FastCrtc;
        _characterPeriodMask = state.CharacterPeriodMask;
        _packedPatternBase = state.PackedPatternBase;
        _packedShifts = state.PackedShifts;
        _packedFill = state.PackedFill;
        _cursorPattern = state.CursorPattern;
        _displayEnabled = state.DisplayEnabled;
        _inverted = state.Inverted;
        _teletextRgb = state.TeletextRgb;
        _rgb = state.Rgb;
    }
}