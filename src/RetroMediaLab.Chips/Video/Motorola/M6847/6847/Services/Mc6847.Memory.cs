using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

public sealed partial class Mc6847
{
    private byte _pendingData;
    private Mc6847Mode _pendingPins;
    private int _pendingRow;
    private bool _pendingValid;
    private bool _addressPresented;
    private int _fetchCount;
    private int _nextFetchHalfClock;

    private void ResetMemoryLine()
    {
        _pendingValid = _addressPresented = _displayValid = false;
        _fetchCount = _cellDot = 0;
        _nextFetchHalfClock = C.ActiveHalfClockStart - C.AddressPreparationCells * Mc6847DataPath.GetPixelCount(_mode);
    }

    private void ClockMemory()
    {
        if (!_timing.ActiveLine || _timing.HalfClock != _nextFetchHalfClock) return;
        var dots = Mc6847DataPath.GetPixelCount(_mode);
        var accesses = dots == C.LongCellDots ? C.LongLineAccesses : C.ShortLineAccesses;
        if (_fetchCount >= accesses) return;

        if (_pendingValid)
        {
            _displayData = _pendingData;
            _displayRow = _pendingRow;
            _displayPins = (_pendingPins & C.LatchedModeMask) | (_mode & C.LiveModeMask);
            _pixels.Load(_displayData, _displayRow, _displayPins);
            _cellDot = 0;
            _displayValid = true;
        }
        if (_addressPresented)
        {
            // Le propriétaire met DD et les entrées liées aux données au niveau du bus.
            // MS ne suspend pas les latches : si le bus est libéré, les niveaux DD fournis restent pris.
            var generation = _generation;
            if (_address.MemorySelect) DataRequested?.Invoke();
            if (_generation != generation) return;
            _pendingData = Data;
            _pendingPins = _mode;
            _pendingRow = _timing.CharacterRow;
            _pendingValid = true;
            _address.AdvanceCell();
        }
        _addressPresented = true;
        _fetchCount++;
        _nextFetchHalfClock += dots;
    }
}
