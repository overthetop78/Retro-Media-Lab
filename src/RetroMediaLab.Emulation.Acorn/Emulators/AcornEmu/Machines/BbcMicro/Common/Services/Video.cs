using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using VideoConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Carte vidéo BBC : le CRTC, VIDPROC, SAA5050 et leurs connexions restent distincts.</summary>
internal sealed partial class Video
{
    private readonly byte[] _ram;
    private readonly bool _teletextHighBankOnly;
    private readonly Crtc6845 _crtc = new(Crtc6845Model.Hd6845S);
    private readonly TeletextSaa5050 _teletext;
    private readonly BbcVideoUla _ula = new();
    private uint[] _drawing;
    private uint[] _completed;
    private bool _monochrome;
    private int _phase;
    private bool _characterPending;
    private int _scanoutX;
    private int _scanoutLine;
    private bool _scanoutVisible;
    private int _lineMasterDot;

    public int Height { get; }
    public int ScreenWrap { get; set; } = VideoConstants.DefaultScreenWrap;
    public bool Monochrome
    {
        get => _monochrome;
        set
        {
            _monochrome = value;
            _scanoutLookup = value ? MonochromeScanoutLookup : ColourScanoutLookup;
        }
    }
    public Func<int, byte>? ReadMemory { get; set; }
    public bool VSync => _crtcVSync;
    public long Frame => _crtc.Frame;

    /// <summary>La carte emprunte la RAM ; ses circuits et tampons lui appartiennent.</summary>
    public Video(byte[] ram, ReadOnlySpan<byte> characters, int height = VideoConstants.Height)
        : this(ram, characters, height, false)
    {
    }

    public Video(byte[] ram, ReadOnlySpan<byte> characters, int height, bool teletextHighBankOnly)
    {
        if (height is not (VideoConstants.Height or VideoConstants.NtscHeight)) throw new ArgumentOutOfRangeException(nameof(height));
        _ram = ram;
        _teletextHighBankOnly = teletextHighBankOnly;
        _teletext = new(characters);
        Height = height;
        _drawing = new uint[VideoConstants.ScanoutWordsPerLine * Height];
        _completed = new uint[_drawing.Length];
        _previousField = new uint[_drawing.Length];
        Reset();
    }

    public void Reset()
    {
        _crtc.Reset();
        RefreshCrtcSignals();
        _teletext.Reset();
        _ula.Reset();
        ResetTeletextWiring();
        Array.Clear(_drawing);
        Array.Clear(_completed);
        ResetFields();
        _phase = VideoConstants.CpuPhaseMask;
        _scanoutX = _scanoutLine = _lineMasterDot = 0;
        _characterPending = _scanoutVisible = false;
        ScreenWrap = VideoConstants.DefaultScreenWrap;
    }

    public void Select(byte value) => _crtc.Select(value);
    public void WriteRegister(byte value)
    {
        _crtc.Write(value);
        RefreshCrtcSignals();
    }
    public byte ReadRegister() => _crtc.Read();
    public void WriteUla(ushort address, byte value) => _ula.Write(address, value);

    /// <summary>Un cycle CPU 2 MHz : huit périodes 16 MHz, sans dessiner à l'avance la demi-cellule suivante.</summary>
    public void Tick()
    {
        _phase ^= VideoConstants.CpuPhaseMask;
        PrepareTeletext();
        var fast = _ula.FastCrtc;
        if (fast || _phase == 0)
        {
            _characterPending = true;
            _ula.SetCharacterInputs(ReadVideoMemory(_crtcMemoryAddress, _crtcRaster), _crtcCursor,
                _crtcDisplayEnabled && (_crtcRaster & VideoConstants.BitmapRasterBlankBit) == 0);
            var width = fast ? VideoConstants.BitmapCellWidth : VideoConstants.WideBitmapCellWidth;
            _scanoutX = _crtcDisplayColumn * width;
            _scanoutLine = _crtcScanline;
            _scanoutVisible = _crtcProgrammed;
        }
        // Huit points 16 MHz contiennent exactement six transitions TR6 :
        // RGB initial, puis échantillons 0,1,2,2,3,4,5. Aucun front n'est supprimé.
        var initialRgb = (uint)_teletext.Rgb;
        var samples = (uint)_teletext.ClockHalfDots(VideoConstants.TeletextHalfDotsPerCpuCycle);
        var teletext = initialRgb |
            ((samples & VideoConstants.TeletextFirstSampleGroupMask) << VideoConstants.ColourBits) |
            (((samples >> VideoConstants.TeletextRepeatedSampleShift) & VideoConstants.ColourMask) << VideoConstants.TeletextRepeatedOutputShift) |
            ((samples >> VideoConstants.TeletextFirstSampleGroupBits) << VideoConstants.TeletextLastOutputGroupShift);
        var output = _ula.ClockMasterPeriods(teletext);
        var selected = _ula.TeletextSelected;
        var left = selected ? _lineMasterDot + 1 - VideoConstants.TeletextViewportMasterDots : _scanoutX;
        _lineMasterDot += VideoConstants.CpuMasterDots;
        _scanoutX += VideoConstants.CpuMasterDots;
        if (_scanoutVisible && (uint)_scanoutLine < Height) WriteScanout(left, _scanoutLine, output);
        if (!_characterPending || (!fast && _phase == 0)) return;
        _characterPending = false;
        var frameStarted = _crtc.Tick();
        RefreshCrtcSignals();
        if (_crtc.Column == 0) _lineMasterDot = 0;
        if (frameStarted && _crtcProgrammed)
        {
            CompleteField();
        }
    }

    /// <summary>Copie immuable de la dernière image terminée.</summary>
    public byte[] Render() => _completedInterlaced ? ExpandInterlacedScanout() : ExpandScanout(_completed);

}
