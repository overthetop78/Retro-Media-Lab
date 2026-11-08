using static RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants.Crtc6845Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    private bool _resetPin;
    private bool _resetActive;
    private bool _testModeActive;
    private int _testLowCounter;
    private int _testHighCounter;

    public bool TestMode => _testModeActive;

    /// <summary>Entrée RESET logique active (broche physique basse), appliquée au descendant CLK.
    /// Les registres et le bus MPU ne sont pas réinitialisés par cette broche.</summary>
    public void SetReset(bool asserted) => _resetPin = asserted;

    /// <summary>Initialisation complète par le propriétaire de carte, distincte de la broche RESET.</summary>
    public void Reset()
    {
        Array.Clear(_registers);
        _selected = 0;
        _resetPin = false;
        _resetActive = false;
        _testModeActive = false;
        ResetBus();
        ResetLightPen();
        ClearTiming(false);
        RefreshParameters();
        _restoreSyncOutput = false;
        PublishHorizontalSync();
        PublishVerticalSync();
        if (_rasterOutputChanged) PublishRasterAddress();
    }

    private bool ClockReset()
    {
        if (_resetPin)
        {
            _lightPenPending = false;
            if (!_lightPenLevel)
            {
                ClearTiming(!_hitachi);
                _resetActive = true;
                _testModeActive = false;
            }
            else
            {
                // Structure documentée par Motorola ; l'horloge interne du test n'est pas spécifiée.
                // Modèle numérique nominal : les deux compteurs 7 bits avancent à CLK sans retenue commune.
                _testLowCounter = (_testLowCounter + 1) & TestCounterMask;
                _testHighCounter = (_testHighCounter + 1) & TestCounterMask;
                _memoryAddress = _testHighCounter << TestCounterShift | _testLowCounter;
                _testModeActive = true;
                _displayEnabled = false;
                _cursor = false;
            }
            if (_publishedHSync) PublishHorizontalSync();
            if (_publishedVSync) PublishVerticalSync();
            if (_rasterOutputChanged) PublishRasterAddress();
            return true;
        }
        _restoreSyncOutput = _resetActive || _testModeActive;
        _resetActive = false;
        _testModeActive = false;
        return false;
    }

    private void ClearTiming(bool suppressDisplay)
    {
        var previousRaster = _rasterAddress;
        _column = 0;
        _row = 0;
        _scanline = 0;
        _frame = 0;
        _rasterAddress = 0;
        _memoryAddress = 0;
        _lineAddress = 0;
        _nextLineAddress = 0;
        _blinkFields = 0;
        _adjustCounter = 0;
        _checkAdjust = false;
        _endMain = false;
        _endAdjust = false;
        _adjusting = false;
        _extraScanline = false;
        _syncSeenInRow = false;
        _horizontalSyncCounter = InactiveSyncCounter;
        _verticalSyncCounter = InactiveSyncCounter;
        _verticalSyncCounted = false;
        _horizontalDisplay = true;
        _verticalDisplay = true;
        _firstScanline = true;
        _suppressDisplay = suppressDisplay;
        _displayHistory = 0;
        _cursorHistory = 0;
        _addressDelayOne = 0;
        _addressDelayTwo = 0;
        _testLowCounter = 0;
        _testHighCounter = 0;
        _address = 0;
        _displayEnabled = false;
        _cursor = false;
        RefreshCursorVisibility();
        RefreshVerticalGates();
        if (previousRaster != 0) _rasterOutputChanged = true;
    }
}
