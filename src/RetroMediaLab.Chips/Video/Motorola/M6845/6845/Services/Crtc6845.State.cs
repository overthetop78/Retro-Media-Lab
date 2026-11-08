using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Contracts;
using C = RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants.Crtc6845Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    /// <summary>Capture sans avancer CLK, E, les sorties retardées ou le stylo optique.</summary>
    public Crtc6845State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Model = Model,
        Registers = (byte[])_registers.Clone(),
        BusSelected = _busSelected,
        BusRegister = _busRegister,
        BusRead = _busRead,
        BusEnable = _busEnable,
        BusAccessValid = _busAccessValid,
        Selected = _selected,
        MemoryAddress = _memoryAddress,
        LineAddress = _lineAddress,
        NextLineAddress = _nextLineAddress,
        RasterAddress = _rasterAddress,
        BlinkFields = _blinkFields,
        HorizontalDisplay = _horizontalDisplay,
        VerticalDisplay = _verticalDisplay,
        FirstScanline = _firstScanline,
        SuppressDisplay = _suppressDisplay,
        InterlaceSync = _interlaceSync,
        InterlaceVideo = _interlaceVideo,
        Programmed = _programmed,
        DisplaySkew = _displaySkew,
        CursorSkew = _cursorSkew,
        CursorAddress = _cursorAddress,
        CursorStart = _cursorStart,
        CursorEnd = _cursorEnd,
        CursorMode = _cursorMode,
        HorizontalWidth = _horizontalWidth,
        VerticalWidth = _verticalWidth,
        Column = _column,
        Row = _row,
        Scanline = _scanline,
        Frame = _frame,
        LightPenLevel = _lightPenLevel,
        LightPenPending = _lightPenPending,
        CursorOnRaster = _cursorOnRaster,
        DisplayHistory = _displayHistory,
        CursorHistory = _cursorHistory,
        AddressDelayOne = _addressDelayOne,
        AddressDelayTwo = _addressDelayTwo,
        DisplayEnabled = _displayEnabled,
        Cursor = _cursor,
        VerticalOutputEnabled = _verticalOutputEnabled,
        VerticalDisplayEndPending = _verticalDisplayEndPending,
        VerticalSyncEligible = _verticalSyncEligible,
        SyncOnEveryCharacter = _syncOnEveryCharacter,
        HalfLineColumn = _halfLineColumn,
        Address = _address,
        ResetPin = _resetPin,
        ResetActive = _resetActive,
        TestModeActive = _testModeActive,
        TestLowCounter = _testLowCounter,
        TestHighCounter = _testHighCounter,
        AdjustCounter = _adjustCounter,
        CheckAdjust = _checkAdjust,
        EndMain = _endMain,
        EndAdjust = _endAdjust,
        Adjusting = _adjusting,
        ExtraScanline = _extraScanline,
        SyncSeenInRow = _syncSeenInRow,
        HorizontalSyncCounter = _horizontalSyncCounter,
        VerticalSyncCounter = _verticalSyncCounter,
        VerticalSyncCounted = _verticalSyncCounted,
        PublishedVSync = _publishedVSync,
        RestoreSyncOutput = _restoreSyncOutput,
    };

    /// <summary>Restaure sans notification de broche ; conserve le raccordement existant.</summary>
    public void RestoreState(Crtc6845State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.Model != Model || state.Registers?.Length != C.RegisterCount)
            throw new ArgumentException(nameof(state));
        state.Registers.CopyTo(_registers, 0);
        _busSelected = state.BusSelected;
        _busRegister = state.BusRegister;
        _busRead = state.BusRead;
        _busEnable = state.BusEnable;
        _busAccessValid = state.BusAccessValid;
        _selected = state.Selected;
        _memoryAddress = state.MemoryAddress;
        _lineAddress = state.LineAddress;
        _nextLineAddress = state.NextLineAddress;
        _rasterAddress = state.RasterAddress;
        _blinkFields = state.BlinkFields;
        _horizontalDisplay = state.HorizontalDisplay;
        _verticalDisplay = state.VerticalDisplay;
        _firstScanline = state.FirstScanline;
        _suppressDisplay = state.SuppressDisplay;
        _interlaceSync = state.InterlaceSync;
        _interlaceVideo = state.InterlaceVideo;
        _programmed = state.Programmed;
        _displaySkew = state.DisplaySkew;
        _cursorSkew = state.CursorSkew;
        _cursorAddress = state.CursorAddress;
        _cursorStart = state.CursorStart;
        _cursorEnd = state.CursorEnd;
        _cursorMode = state.CursorMode;
        _horizontalWidth = state.HorizontalWidth;
        _verticalWidth = state.VerticalWidth;
        _column = state.Column;
        _row = state.Row;
        _scanline = state.Scanline;
        _frame = state.Frame;
        _lightPenLevel = state.LightPenLevel;
        _lightPenPending = state.LightPenPending;
        _cursorOnRaster = state.CursorOnRaster;
        _displayHistory = state.DisplayHistory;
        _cursorHistory = state.CursorHistory;
        _addressDelayOne = state.AddressDelayOne;
        _addressDelayTwo = state.AddressDelayTwo;
        _displayEnabled = state.DisplayEnabled;
        _cursor = state.Cursor;
        _verticalOutputEnabled = state.VerticalOutputEnabled;
        _verticalDisplayEndPending = state.VerticalDisplayEndPending;
        _verticalSyncEligible = state.VerticalSyncEligible;
        _syncOnEveryCharacter = state.SyncOnEveryCharacter;
        _halfLineColumn = state.HalfLineColumn;
        _address = state.Address;
        _resetPin = state.ResetPin;
        _resetActive = state.ResetActive;
        _testModeActive = state.TestModeActive;
        _testLowCounter = state.TestLowCounter;
        _testHighCounter = state.TestHighCounter;
        _adjustCounter = state.AdjustCounter;
        _checkAdjust = state.CheckAdjust;
        _endMain = state.EndMain;
        _endAdjust = state.EndAdjust;
        _adjusting = state.Adjusting;
        _extraScanline = state.ExtraScanline;
        _syncSeenInRow = state.SyncSeenInRow;
        _horizontalSyncCounter = state.HorizontalSyncCounter;
        _verticalSyncCounter = state.VerticalSyncCounter;
        _verticalSyncCounted = state.VerticalSyncCounted;
        _publishedVSync = state.PublishedVSync;
        _restoreSyncOutput = state.RestoreSyncOutput;
        _publishedHSync = HSync;
        _rasterOutputChanged = false;
    }
}
