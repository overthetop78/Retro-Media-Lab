using static RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants.Crtc6845Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    /// <summary>Represent a linear MA sequence strictly before the next other
    /// pin transition or internal boundary. The receiver must consume every
    /// address effect, in order, without modifying CRTC inputs or observing
    /// intermediate CRTC state. It returns false without mutation if unable.
    /// Ordinary MA listeners receive the final level after resolution; callers
    /// with additional per-edge observers must retain Tick instead.</summary>
    /// <param name="receiveAddresses">First MA after CLK, and number of clocks.
    /// No wrapping address is offered. All other output pins remain stable.</param>
    public int AdvanceLinearCharacters(int maximum, Func<int, int, bool> receiveAddresses)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximum);
        ArgumentNullException.ThrowIfNull(receiveAddresses);
        if (maximum < 2 || _resetPin || _resetActive || _testModeActive
            || _lightPenPending || _restoreSyncOutput || _rasterOutputChanged
            || _verticalDisplayEndPending || _checkAdjust || _column <= LastRasterLatchColumn
            || _column >= _registers[HorizontalTotal]
            || _column == _registers[HorizontalDisplayed] || _cursor || _cursorHistory != 0)
            return 0;
        var rawDisplay = _horizontalDisplay && _verticalOutputEnabled;
        if (_displayHistory != (rawDisplay ? OutputHistoryMask : 0)
            || _displayEnabled != (rawDisplay && _displaySkew != SuppressedSkew)) return 0;
        // A pending vertical synchronization is resolved at the next ordinary
        // clock, including its half-line qualification in interlace mode.
        if (_verticalSyncEligible || (_verticalSyncCounter >= 0 && _verticalSyncCounted
            && _verticalSyncCounter == (_verticalWidth & SyncCounterMask))) return 0;

        var count = Math.Min(maximum, _registers[HorizontalTotal] - _column - 1);
        count = Math.Min(count, MemoryAddressMask - _memoryAddress);
        if (_column < _registers[HorizontalDisplayed])
            count = Math.Min(count, _registers[HorizontalDisplayed] - _column - 1);
        if (_horizontalSyncCounter >= 0)
        {
            var end = (_horizontalWidth - _horizontalSyncCounter) & SyncCounterMask;
            if (_horizontalWidth == 0) return 0;
            if (end == 0) end = SyncPeriod;
            count = Math.Min(count, end - 1);
        }
        else if (_horizontalWidth != 0 && _column < _registers[HorizontalSyncPosition])
            count = Math.Min(count, _registers[HorizontalSyncPosition] - _column - 1);
        if (rawDisplay && _cursorOnRaster && _cursorAddress > _memoryAddress)
            count = Math.Min(count, _cursorAddress - _memoryAddress - 1);
        if (count < 2 || !receiveAddresses(_memoryAddress + 1, count)) return 0;

        _column += count;
        _memoryAddress += count;
        if (_horizontalSyncCounter >= 0)
            _horizontalSyncCounter = (_horizontalSyncCounter + count) & SyncCounterMask;
        _addressDelayOne = _memoryAddress;
        _addressDelayTwo = _memoryAddress - 1;
        _address = _displaySkew switch
        {
            1 => _memoryAddress - 1,
            2 => _memoryAddress - 2,
            _ => _memoryAddress
        };
        if (_observedPins != 0) PublishPins();
        return count;
    }
}
