namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    private bool _crtcVSync;
    private int _crtcRaster;
    private int _crtcMemoryAddress;
    private bool _crtcDisplayEnabled;
    private bool _crtcCursor;
    private int _crtcDisplayColumn;
    private int _crtcScanline;
    private bool _crtcProgrammed;

    // La carte possède le CRTC et actualise ces niveaux après chaque action
    // susceptible de les modifier : RESET, écriture MPU ou front caractère.
    private void RefreshCrtcSignals()
    {
        _crtcVSync = _crtc.VSync;
        _crtcRaster = _crtc.RasterAddress;
        _crtcMemoryAddress = _crtc.MemoryAddress;
        _crtcDisplayEnabled = _crtc.DisplayEnabled;
        _crtcCursor = _crtc.Cursor;
        _crtcDisplayColumn = _crtc.DisplayColumn;
        _crtcScanline = _crtc.Scanline;
        _crtcProgrammed = _crtc.IsProgrammed;
    }
}
