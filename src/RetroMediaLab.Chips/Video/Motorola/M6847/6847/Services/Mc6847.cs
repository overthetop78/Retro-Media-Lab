using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Interfaces;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

/// <summary>MC6847 non entrelacé : broches numériques, latches et sorties analogiques nominales.</summary>
public sealed partial class Mc6847 : IMc6847
{
    private readonly Mc6847Timing _timing = new();
    private readonly Mc6847AddressCounter _address = new();
    private readonly Mc6847DataPath _pixels;
    private Mc6847Mode _mode;
    private Mc6847Mode _displayPins;
    private byte _displayData;
    private int _displayRow;
    private int _cellDot;
    private bool _displayValid;
    private int _generation;

    public Mc6847(ReadOnlySpan<byte> characters)
    {
        _pixels = new(characters);
        Reset();
    }

    public byte Data { get; set; }
    public Action? DataRequested { get; set; }
    public Mc6847Mode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            var changed = _mode ^ value;
            _mode = value;
            _timing.Graphics = (value & Mc6847Mode.Graphics) != 0;
            _address.Mode = value;
            if (_displayValid && (changed & C.LiveModeMask) != 0)
            {
                _displayPins = (_displayPins & C.LatchedModeMask) | (value & C.LiveModeMask);
                _pixels.Load(_displayData, _displayRow, _displayPins);
            }
            if (_fetchCount == 0)
            {
                var start = C.ActiveHalfClockStart - C.AddressPreparationCells * Mc6847DataPath.GetPixelCount(value);
                if (_timing.HalfClock < start) _nextFetchHalfClock = start;
            }
            UpdateOutput();
        }
    }
    public bool MemorySelect { get => _address.MemorySelect; set => _address.MemorySelect = value; }
    public ushort? Address => _address.DrivenAddress;
    public bool Clock => _timing.Clock;
    public bool HorizontalSync => _timing.HorizontalSync;
    public bool FieldSync => _timing.FieldSync;
    public bool RowPreset => _timing.RowPreset;
    public bool Active => _timing.Active;
    public int HalfClock => _timing.HalfClock;
    public int Scanline => _timing.Scanline;
    public int CharacterRow => _timing.CharacterRow;
    public long Field => _timing.Field;

    /// <summary>Initialisation du modèle ; il ne s’agit pas d’une broche ajoutée au boîtier.</summary>
    public void Reset()
    {
        _generation++;
        _timing.Reset();
        _address.Reset();
        _mode = _displayPins = Mc6847Mode.None;
        Data = _displayData = 0;
        _displayRow = _cellDot = 0;
        _displayValid = false;
        ResetMemoryLine();
        UpdateOutput();
    }
}
