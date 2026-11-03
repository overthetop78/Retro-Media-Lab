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
            parity ^= Row & InterlaceSyncMask;
        return parity;
    }

    private bool AdvanceCharacter()
    {
        if (Column == _registers[HorizontalDisplayed]) _nextLineAddress = _memoryAddress;
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
            if (Column == LastRasterLatchColumn)
            {
                if (!_adjusting && !_extraScanline && Row == _registers[VerticalTotal] && LastRaster())
                {
                    _endMain = true;
                    _adjustCounter = 0;
                }
                _checkAdjust = true;
            }
        }
        _memoryAddress = (_memoryAddress + 1) & MemoryAddressMask;
        var frameStarted = false;
        if (Column == _registers[HorizontalTotal])
        {
            frameStarted = EndScanline();
            RefreshCursorVisibility();
            Column = 0;
            _horizontalDisplay = true;
            _memoryAddress = _lineAddress;
        }
        else Column = (Column + 1) & DataMask;

        if (Column == _registers[HorizontalDisplayed] || Column == _registers[HorizontalTotal])
            _horizontalDisplay = false;
        if (_verticalDisplay && Row == _registers[VerticalDisplayed]
            && (!_hitachi || !_firstScanline))
        {
            _verticalDisplay = false;
            _blinkFields++;
            RefreshCursorVisibility();
        }
        return frameStarted;
    }

    private bool EndScanline()
    {
        Scanline++;
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
                if (Row == _registers[VerticalTotal])
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
        Row = (Row + 1) & RowMask;
        _rasterAddress = FirstRaster();
        _syncSeenInRow = false;
    }

    private bool BeginFrame()
    {
        Row = 0;
        Scanline = 0;
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
        Frame++;
        return true;
    }

    private void UpdateSynchronizations()
    {
        if (_horizontalSyncCounter >= 0)
        {
            _horizontalSyncCounter = (_horizontalSyncCounter + 1) & SyncCounterMask;
            if (_horizontalWidth == 0 || _horizontalSyncCounter == (_horizontalWidth & SyncCounterMask))
                _horizontalSyncCounter = InactiveSyncCounter;
        }
        if (_horizontalSyncCounter < 0 && _horizontalWidth != 0 && Column == _registers[HorizontalSyncPosition])
            _horizontalSyncCounter = 0;

        var syncPoint = !_interlaceSync || (_blinkFields & InterlaceSyncMask) == 0
            || Column == ((_registers[HorizontalTotal] + 1) >> 1);
        var ended = false;
        if (_verticalSyncCounter >= 0 && _verticalSyncCounted
            && _verticalSyncCounter == (_verticalWidth & SyncCounterMask) && syncPoint)
        {
            _verticalSyncCounter = InactiveSyncCounter;
            ended = true;
        }
        if (!ended && _verticalSyncCounter < 0 && !_syncSeenInRow
            && Row == _registers[VerticalSyncPosition] && !_adjusting && !_extraScanline && syncPoint)
        {
            _verticalSyncCounter = 0;
            _verticalSyncCounted = false;
            _syncSeenInRow = true;
        }
    }
}
