using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    internal VideoState CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Height = Height,
        TeletextHighBankOnly = _teletextHighBankOnly,
        ScreenWrap = ScreenWrap,
        Crtc = _crtc.CaptureState(),
        Ula = _ula.CaptureState(),
        Teletext = _teletext.CaptureState(),
        Drawing = (uint[])_drawing.Clone(),
        Completed = (uint[])_completed.Clone(),
        PreviousField = (uint[])_previousField.Clone(),
        CrtcVSync = _crtcVSync,
        CrtcRaster = _crtcRaster,
        CrtcMemoryAddress = _crtcMemoryAddress,
        CrtcDisplayEnabled = _crtcDisplayEnabled,
        CrtcCursor = _crtcCursor,
        CrtcDisplayColumn = _crtcDisplayColumn,
        CrtcScanline = _crtcScanline,
        CrtcProgrammed = _crtcProgrammed,
        Monochrome = _monochrome,
        Phase = _phase,
        CharacterPending = _characterPending,
        ScanoutX = _scanoutX,
        ScanoutLine = _scanoutLine,
        ScanoutVisible = _scanoutVisible,
        LineMasterDot = _lineMasterDot,
        DrawingInterlaced = _drawingInterlaced,
        CompletedInterlaced = _completedInterlaced,
        PreviousFieldValid = _previousFieldValid,
        DrawingParity = _drawingParity,
        CompletedParity = _completedParity,
        TeletextLatchedData = _teletextLatchedData,
        TeletextWindow = _teletextWindow,
        TeletextRasterParity = _teletextRasterParity,
    };

    internal void RestoreState(VideoState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var length = C.ScanoutWordsPerLine * Height;
        if (state.FormatVersion != C.StateFormatVersion || state.Height != Height
            || state.TeletextHighBankOnly != _teletextHighBankOnly || state.Drawing?.Length != length
            || state.Completed?.Length != length || state.PreviousField?.Length != length)
            throw new ArgumentException(nameof(state));
        // Les contrôles propres aux circuits sont effectués avant de remplacer les tampons.
        _crtc.RestoreState(state.Crtc);
        _ula.RestoreState(state.Ula);
        _teletext.RestoreState(state.Teletext);
        _drawing = (uint[])state.Drawing.Clone();
        _completed = (uint[])state.Completed.Clone();
        _previousField = (uint[])state.PreviousField.Clone();
        ScreenWrap = state.ScreenWrap;
        _crtcVSync = state.CrtcVSync;
        _crtcRaster = state.CrtcRaster;
        _crtcMemoryAddress = state.CrtcMemoryAddress;
        _crtcDisplayEnabled = state.CrtcDisplayEnabled;
        _crtcCursor = state.CrtcCursor;
        _crtcDisplayColumn = state.CrtcDisplayColumn;
        _crtcScanline = state.CrtcScanline;
        _crtcProgrammed = state.CrtcProgrammed;
        _monochrome = state.Monochrome;
        _phase = state.Phase;
        _characterPending = state.CharacterPending;
        _scanoutX = state.ScanoutX;
        _scanoutLine = state.ScanoutLine;
        _scanoutVisible = state.ScanoutVisible;
        _lineMasterDot = state.LineMasterDot;
        _drawingInterlaced = state.DrawingInterlaced;
        _completedInterlaced = state.CompletedInterlaced;
        _previousFieldValid = state.PreviousFieldValid;
        _drawingParity = state.DrawingParity;
        _completedParity = state.CompletedParity;
        _teletextLatchedData = state.TeletextLatchedData;
        _teletextWindow = state.TeletextWindow;
        _teletextRasterParity = state.TeletextRasterParity;
        _scanoutLookup = _monochrome ? MonochromeScanoutLookup : ColourScanoutLookup;
        // Le cache de présentation est dérivé ; le prochain Render le reconstruit.
        _renderedWords = [];
        _renderedPixels = [];
    }
}