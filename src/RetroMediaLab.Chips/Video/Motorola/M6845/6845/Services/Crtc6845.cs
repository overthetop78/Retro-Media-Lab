using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Interfaces;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

/// <summary>CRTC cadencé par caractères ; aucune RAM, palette ni ressource hôte n'est possédée.</summary>
public sealed partial class Crtc6845 : ICrtc6845
{
    private readonly byte[] _registers = new byte[Crtc6845Constants.RegisterCount];
    private byte _selected;
    private readonly bool _hitachi;
    private int _memoryAddress;
    private int _lineAddress;
    private int _nextLineAddress;
    private int _rasterAddress;
    private long _blinkFields;
    private bool _horizontalDisplay;
    private bool _verticalDisplay;
    private bool _firstScanline;
    private bool _suppressDisplay;
    private bool _interlaceSync;
    private bool _interlaceVideo;
    private bool _programmed;
    private int _displaySkew;
    private int _cursorSkew;
    private int _cursorAddress;
    private int _cursorStart;
    private int _cursorEnd;
    private int _cursorMode;
    private int _horizontalWidth;
    private int _verticalWidth;

    private int _column;
    private int _row;
    private int _scanline;
    private long _frame;
    public int Column => _column;
    public int Row => _row;
    /// <summary>Position de balayage pour les propriétaires de rendu ; RA est exposé par RasterAddress.</summary>
    public int Raster => _interlaceVideo ? _rasterAddress & Crtc6845Constants.EvenRasterMask : _rasterAddress;
    public int RasterAddress => _rasterAddress;
    public int Scanline => _scanline;
    public long Frame => _frame;
    public int this[int index] => _registers[index];
    public bool InterlaceVideo => _interlaceVideo;
    public bool IsProgrammed => _programmed;

    /// <summary>Un descendant CLK. E reste une entrée indépendante du bus MPU.</summary>
    public bool Tick()
    {
        if (ClockReset()) return false;
        var frameStarted = AdvanceCharacter();
        var syncChanged = UpdateSynchronizations();
        PublishOutputs();
        CaptureLightPen();
        if (_rasterOutputChanged) PublishRasterAddress();
        if (syncChanged) PublishVerticalSync();
        return frameStarted;
    }
}
