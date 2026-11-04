using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

/// <summary>Séquence numérique non entrelacée ; origine au FS montant, fronts électriques idéaux.</summary>
internal sealed class Mc6847Timing
{
    private bool _clock;
    private bool _graphics;
    private bool _presetLine;

    internal int HalfClock { get; private set; }
    internal int Scanline { get; private set; }
    internal long Field { get; private set; }
    internal int CharacterRow { get; private set; }
    internal bool Clock => _clock;
    internal bool HorizontalSync => HalfClock >= C.HorizontalSyncEnd;
    internal bool FieldSync => Scanline < C.FieldSyncStart;
    internal bool ActiveLine => Scanline >= C.ActiveLineStart && Scanline < C.ActiveLineEnd;
    internal bool Active => ActiveLine && HalfClock >= C.ActiveHalfClockStart && HalfClock < C.ActiveHalfClockEnd;
    internal bool RowPreset => _graphics || !_presetLine || HalfClock < C.RowPresetStart || HalfClock >= C.RowPresetEnd;
    internal bool Graphics
    {
        get => _graphics;
        set
        {
            if (_graphics && !value)
            {
                CharacterRow = 0;
                _presetLine = false;
            }
            _graphics = value;
        }
    }

    /// <summary>Initialisation logicielle déterministe ; le circuit ne possède pas de broche RESET.</summary>
    internal void Reset()
    {
        _clock = _graphics = _presetLine = false;
        HalfClock = Scanline = CharacterRow = 0;
        Field = 0;
    }

    internal void SetClock(bool level)
    {
        if (_clock == level) return;
        _clock = level;
        AdvanceHalfClock();
    }

    private void AdvanceHalfClock()
    {
        if (++HalfClock < C.HalfClocksPerLine) return;
        HalfClock = 0;
        var previousActive = ActiveLine;
        if (++Scanline == C.LinesPerField)
        {
            Scanline = 0;
            Field++;
        }
        _presetLine = false;
        if (Scanline == C.ActiveLineStart)
        {
            CharacterRow = 0;
        }
        else if (previousActive)
        {
            if (++CharacterRow == C.CharacterHeight)
            {
                CharacterRow = 0;
                _presetLine = true;
            }
        }
    }
}
