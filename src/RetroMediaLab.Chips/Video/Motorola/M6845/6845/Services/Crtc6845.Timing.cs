using static RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants.Crtc6845Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    private int _adjustCounter;
    private bool _checkAdjust;
    private bool _endMain;
    private bool _endAdjust;
    private bool _adjusting;
    private bool _extraScanline;
    private bool _syncSeenInRow;
    private int _horizontalSyncCounter = InactiveSyncCounter;
    private int _verticalSyncCounter = InactiveSyncCounter;
    private bool _verticalSyncCounted;
    private bool _publishedVSync;
    private bool _publishedHSync;
    private bool _rasterOutputChanged;
    private bool _restoreSyncOutput;
    public Action<bool>? VSyncChanged { get; set; }
    public Action<bool>? HSyncChanged { get; set; }
    public Action<int>? RasterAddressChanged { get; set; }

    private void PublishHorizontalSync()
    {
        var level = HSync;
        if (_publishedHSync == level) return;
        _publishedHSync = level;
        HSyncChanged?.Invoke(level);
    }

    private void PublishRasterAddress()
    {
        _rasterOutputChanged = false;
        RasterAddressChanged?.Invoke(_rasterAddress);
    }

    private void PublishVerticalSync()
    {
        var level = VSync;
        if (_publishedVSync == level) return;
        _publishedVSync = level;
        VSyncChanged?.Invoke(level);
    }

    public bool HSync => !_resetActive && !_testModeActive && _horizontalSyncCounter >= 0;
    public bool VSync => !_resetActive && !_testModeActive && _verticalSyncCounter >= 0;

    private bool LastRaster()
    {
        if (!_interlaceVideo) return _rasterAddress == _registers[MaximumRaster];
        if (_hitachi)
            return _rasterAddress >= _registers[MaximumRaster];
        return (_rasterAddress >> 1) == (_registers[MaximumRaster] >> 1);
    }

    private int FirstRaster()
    {
        if (!_interlaceVideo) return 0;
        var parity = (int)(_blinkFields & InterlaceSyncMask);
        // Hitachi table 9 : avec Nr+2 impair, la parité s'inverse à chaque ligne de caractères.
        if (_hitachi && (_registers[MaximumRaster] & InterlaceSyncMask) != 0)
            parity ^= _row & InterlaceSyncMask;
        return parity;
    }

    private bool AdvanceCharacter()
    {
        if (_column == _registers[HorizontalDisplayed]) _nextLineAddress = _memoryAddress;
        if (_hitachi)
        {
            if (_checkAdjust)
            {
                _checkAdjust = false;
                if (_endMain)
                {
                    if (_adjustCounter == _registers[VerticalAdjust]) _endAdjust = true;
                    _adjustCounter = (_adjustCounter + 1) & RasterMask;
                }
            }
            if (_column == LastRasterLatchColumn)
            {
                if (!_adjusting && !_extraScanline && _row == _registers[VerticalTotal] && LastRaster())
                {
                    _endMain = true;
                    _adjustCounter = 0;
                }
                _checkAdjust = true;
            }
        }
        _memoryAddress = (_memoryAddress + 1) & MemoryAddressMask;
        var frameStarted = false;
        if (_column == _registers[HorizontalTotal])
        {
            var previousRaster = _rasterAddress;
            frameStarted = EndScanline();
            RefreshCursorVisibility();
            RefreshVerticalGates();
            _column = 0;
            _horizontalDisplay = true;
            _memoryAddress = _lineAddress;
            // EndScanline may increment RA and then load its row/field value.
            // Only the final pin value is visible at this character-clock edge.
            if (_rasterAddress != previousRaster) _rasterOutputChanged = true;
        }
        else _column = (_column + 1) & DataMask;

        if (_column == _registers[HorizontalDisplayed] || _column == _registers[HorizontalTotal])
            _horizontalDisplay = false;
        if (_verticalDisplayEndPending)
        {
            _verticalDisplay = false;
            _verticalOutputEnabled = false;
            _verticalDisplayEndPending = false;
            _blinkFields++;
            _syncOnEveryCharacter = !_interlaceSync || (_blinkFields & InterlaceSyncMask) == 0;
            RefreshCursorVisibility();
        }
        return frameStarted;
    }

    private bool EndScanline()
    {
        _scanline++;
        _firstScanline = false;
        if (_verticalSyncCounter >= 0)
        {
            _verticalSyncCounter = (_verticalSyncCounter + 1) & SyncCounterMask;
            _verticalSyncCounted = true;
        }
        var lastRaster = LastRaster();
        if (lastRaster) _lineAddress = _nextLineAddress;
        _rasterAddress = (_rasterAddress + (_interlaceVideo ? 2 : 1)) & RasterMask;

        if (!_hitachi)
        {
            if (_extraScanline) return BeginFrame();
            if (_adjusting)
            {
                _adjustCounter = (_adjustCounter + 1) & RasterMask;
                if (_adjustCounter == _registers[VerticalAdjust]) return FinishMainField();
            }
            else if (lastRaster)
            {
                if (_row == _registers[VerticalTotal])
                {
                    if (_registers[VerticalAdjust] == 0) return FinishMainField();
                    _adjusting = true;
                    _adjustCounter = 0;
                }
                BeginRow();
            }
        }
        else
        {
            if (!_adjusting && lastRaster) BeginRow();
            if (_extraScanline) return BeginFrame();
            if (_endAdjust) return FinishMainField();
            if (_endMain) _adjusting = true;
        }
        return false;
    }

    private bool FinishMainField()
    {
        _adjusting = false;
        _endAdjust = false;
        if (_interlaceSync && (_blinkFields & InterlaceSyncMask) != 0)
        {
            _extraScanline = true;
            return false;
        }
        return BeginFrame();
    }

    private void BeginRow()
    {
        _row = (_row + 1) & RowMask;
        _rasterAddress = FirstRaster();
        _syncSeenInRow = false;
    }

    private bool BeginFrame()
    {
        _row = 0;
        _scanline = 0;
        _rasterAddress = FirstRaster();
        _lineAddress = _registers[StartAddressHigh] << AddressByteShift | _registers[StartAddressLow];
        _nextLineAddress = _lineAddress;
        _firstScanline = true;
        _verticalDisplay = true;
        _suppressDisplay = false;
        _adjustCounter = 0;
        _adjusting = false;
        _extraScanline = false;
        _endMain = false;
        _endAdjust = false;
        _checkAdjust = false;
        _syncSeenInRow = false;
        _frame++;
        return true;
    }

    private bool UpdateSynchronizations()
    {
        var horizontalChanged = _restoreSyncOutput;
        if (_horizontalSyncCounter >= 0)
        {
            _horizontalSyncCounter = (_horizontalSyncCounter + 1) & SyncCounterMask;
            if (_horizontalWidth == 0 || _horizontalSyncCounter == (_horizontalWidth & SyncCounterMask))
            {
                _horizontalSyncCounter = InactiveSyncCounter;
                horizontalChanged = true;
            }
        }
        if (_horizontalSyncCounter < 0 && _horizontalWidth != 0 && _column == _registers[HorizontalSyncPosition])
        {
            _horizontalSyncCounter = 0;
            horizontalChanged = true;
        }
        if (horizontalChanged) PublishHorizontalSync();

        var syncPoint = _syncOnEveryCharacter || _column == _halfLineColumn;
        var ended = false;
        var changed = _restoreSyncOutput;
        _restoreSyncOutput = false;
        if (_verticalSyncCounter >= 0 && _verticalSyncCounted
            && _verticalSyncCounter == (_verticalWidth & SyncCounterMask) && syncPoint)
        {
            _verticalSyncCounter = InactiveSyncCounter;
            ended = true;
            changed = true;
        }
        if (!ended && _verticalSyncCounter < 0 && _verticalSyncEligible && syncPoint)
        {
            _verticalSyncCounter = 0;
            _verticalSyncCounted = false;
            _syncSeenInRow = true;
            _verticalSyncEligible = false;
            changed = true;
        }
        return changed;
    }
}
