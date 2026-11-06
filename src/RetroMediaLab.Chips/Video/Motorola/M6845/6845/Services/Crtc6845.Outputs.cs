using static RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants.Crtc6845Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    private bool _cursorOnRaster;
    private int _displayHistory;
    private int _cursorHistory;
    private int _addressDelayOne;
    private int _addressDelayTwo;

    private bool _displayEnabled;
    private bool _cursor;
    private bool _verticalOutputEnabled;
    private bool _verticalDisplayEndPending;
    private bool _verticalSyncEligible;
    private bool _syncOnEveryCharacter;
    private int _halfLineColumn;

    private int _address;
    public bool DisplayEnabled => _displayEnabled;
    public bool Cursor => _cursor;
    public int DisplayColumn => _column - _displaySkew;
    /// <summary>Adresse associée à DE retardé pour les propriétaires de rendu ; la broche MA est MemoryAddress.</summary>
    public int Address => _address;

    public bool CursorForCell(int address)
    {
        return _cursorOnRaster && !_resetActive && !_testModeActive && !_suppressDisplay && address == _cursorAddress;
    }

    private void RefreshCursorVisibility()
    {
        _cursorOnRaster = _cursorSkew != SuppressedSkew && _cursorMode != CursorOffMode
            && (_cursorMode != CursorBlink16Mode || (_blinkFields & CursorBlink16HalfPeriod) == 0)
            && (_cursorMode != CursorBlink32Mode || (_blinkFields & CursorBlink32HalfPeriod) == 0)
            && _rasterAddress >= _cursorStart && _rasterAddress <= _cursorEnd;
    }

    private void PublishOutputs()
    {
        var rawDisplay = _horizontalDisplay && _verticalOutputEnabled;
        _displayHistory = ((_displayHistory << 1) | (rawDisplay ? 1 : 0)) & OutputHistoryMask;
        _cursorHistory = ((_cursorHistory << 1) | (rawDisplay && _cursorOnRaster && _memoryAddress == _cursorAddress ? 1 : 0)) & OutputHistoryMask;
        _displayEnabled = _displaySkew != SuppressedSkew && ((_displayHistory >> _displaySkew) & 1) != 0;
        _cursor = _displayEnabled && _cursorSkew != SuppressedSkew && ((_cursorHistory >> _cursorSkew) & 1) != 0;
        _address = _displaySkew switch { 1 => _addressDelayOne, 2 => _addressDelayTwo, _ => _memoryAddress };
        _addressDelayTwo = _addressDelayOne;
        _addressDelayOne = _memoryAddress;
    }

    private void RefreshParameters()
    {
        var mode = _registers[Mode];
        _interlaceSync = (mode & InterlaceSyncMask) != 0;
        _interlaceVideo = (mode & InterlaceVideoMask) == InterlaceVideoMask;
        _displaySkew = (mode >> DisplaySkewShift) & SkewMask;
        _cursorSkew = (mode >> CursorSkewShift) & SkewMask;
        _cursorAddress = _registers[CursorAddressHigh] << AddressByteShift | _registers[CursorAddressLow];
        _cursorStart = _registers[CursorStart] & RasterMask;
        _cursorEnd = _registers[CursorEnd];
        _cursorMode = (_registers[CursorStart] >> CursorModeShift) & CursorModeMask;
        RefreshCursorVisibility();
        _horizontalWidth = _registers[SyncWidth] & SyncCounterMask;
        _verticalWidth = SyncPeriod;
        if (_hitachi)
        {
            if (_horizontalWidth == 0) _horizontalWidth = SyncPeriod;
            var width = _registers[SyncWidth] >> DisplaySkewShift;
            if (width != 0) _verticalWidth = width;
        }
        if (_displaySkew == SuppressedSkew)
        {
            _displayEnabled = false;
            _cursor = false;
        }
        if (_cursorSkew == SuppressedSkew) _cursor = false;
        _programmed = _registers[HorizontalTotal] != 0 && _registers[HorizontalDisplayed] != 0
            && _registers[VerticalTotal] != 0 && _registers[VerticalDisplayed] != 0;
        _halfLineColumn = (_registers[HorizontalTotal] + 1) >> 1;
        RefreshVerticalGates();
    }

    // Ces entrées ne changent qu'aux écritures MPU, aux transitions verticales
    // et à la fin de ligne. Leur résolution ne dépend pas du caractère courant.
    private void RefreshVerticalGates()
    {
        _verticalOutputEnabled = _verticalDisplay && !_adjusting && !_extraScanline && !_suppressDisplay;
        _verticalDisplayEndPending = _verticalDisplay && _row == _registers[VerticalDisplayed]
            && (!_hitachi || !_firstScanline);
        _verticalSyncEligible = !_syncSeenInRow && _row == _registers[VerticalSyncPosition]
            && !_adjusting && !_extraScanline;
        _syncOnEveryCharacter = !_interlaceSync || (_blinkFields & InterlaceSyncMask) == 0;
    }
}
