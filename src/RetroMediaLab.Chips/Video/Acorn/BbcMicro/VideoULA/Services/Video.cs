using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using System.Runtime.InteropServices;
using VideoConstants = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.VideoConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

/// <summary>Carte vidéo BBC : le CRTC, VIDPROC, SAA5050 et leurs connexions restent distincts.</summary>
public sealed partial class Video : Interfaces.IVideo
{
    private readonly byte[] _ram;
    private readonly bool _teletextHighBankOnly;
    private readonly Crtc6845 _crtc = new(Crtc6845Model.Hd6845S);
    private readonly TeletextSaa5050 _teletext;
    private readonly BbcVideoUla _ula = new();
    private static readonly uint[] ColourPixels = VideoConstants.PackedColours.ToArray();
    private static readonly uint[] MonochromePixels = VideoConstants.PackedMonochrome.ToArray();
    private uint[] _outputColours = ColourPixels;
    private uint[] _drawing;
    private uint[] _completed;
    private bool _monochrome;
    private int _phase;
    private bool _characterPending;
    private int _scanoutX;
    private int _scanoutLine;
    private bool _scanoutVisible;
    private int _lineMasterDot;
    private int _teletextClockPhase;

    public int Height { get; }
    public int ScreenWrap { get; set; } = VideoConstants.DefaultScreenWrap;
    public bool Monochrome
    {
        get => _monochrome;
        set { _monochrome = value; _outputColours = value ? MonochromePixels : ColourPixels; }
    }
    public Func<int, byte>? ReadMemory { get; set; }
    public bool VSync => _crtc.VSync;
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
        _drawing = new uint[VideoConstants.Width * Height];
        _completed = new uint[_drawing.Length];
        Reset();
    }

    public void Reset()
    {
        _crtc.Reset();
        _teletext.Reset();
        _ula.Reset();
        ResetTeletextWiring();
        Array.Clear(_drawing);
        Array.Clear(_completed);
        _phase = VideoConstants.CpuPhaseMask;
        _scanoutX = _scanoutLine = _lineMasterDot = _teletextClockPhase = 0;
        _characterPending = _scanoutVisible = false;
        ScreenWrap = VideoConstants.DefaultScreenWrap;
    }

    public void Select(byte value) => _crtc.Select(value);
    public void WriteRegister(byte value) => _crtc.Write(value);
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
            _ula.Data = ReadVideoMemory(_crtc.MemoryAddress, _crtc.RasterAddress);
            _ula.Cursor = _crtc.Cursor;
            _ula.DisplayEnabled = _crtc.DisplayEnabled && (_crtc.RasterAddress & VideoConstants.BitmapRasterBlankBit) == 0;
            var width = fast ? VideoConstants.BitmapCellWidth : VideoConstants.WideBitmapCellWidth;
            _scanoutX = _crtc.DisplayColumn * width;
            _scanoutLine = _crtc.Scanline;
            _scanoutVisible = _crtc.IsProgrammed;
        }
        uint teletext = 0;
        for (var dot = 0; dot < VideoConstants.CpuMasterDots; dot++)
        {
            _teletextClockPhase += VideoConstants.TeletextClockNumerator;
            if (_teletextClockPhase >= VideoConstants.TeletextClockDenominator)
            {
                _teletextClockPhase -= VideoConstants.TeletextClockDenominator;
                _teletextDotLevel = !_teletextDotLevel;
                _teletext.SetDotClock(_teletextDotLevel);
            }
            teletext |= (uint)_teletext.Rgb << (dot * VideoConstants.ColourBits);
        }
        var output = _ula.EmitCpuPixels(teletext);
        var selected = _ula.TeletextSelected;
        var left = selected ? _lineMasterDot + 1 - VideoConstants.TeletextViewportMasterDots : _scanoutX;
        _lineMasterDot += VideoConstants.CpuMasterDots;
        _scanoutX += VideoConstants.CpuMasterDots;
        if (_scanoutVisible && (uint)_scanoutLine < Height)
        {
            var first = Math.Max(0, -left);
            var last = Math.Min(VideoConstants.CpuMasterDots, VideoConstants.Width - left);
            if (first < last)
            {
                var destination = _scanoutLine * VideoConstants.Width + left;
                for (var dot = first; dot < last; dot++) _drawing[destination + dot] = _outputColours[(int)(output >> (dot * VideoConstants.ColourBits)) & VideoConstants.ColourMask];
            }
        }
        if (!_characterPending || (!fast && _phase == 0)) return;
        _characterPending = false;
        var frameStarted = _crtc.Tick();
        if (_crtc.Column == 0) _lineMasterDot = 0;
        if (frameStarted && _crtc.IsProgrammed)
        {
            (_completed, _drawing) = (_drawing, _completed);
            Array.Clear(_drawing);
        }
    }

    /// <summary>Copie immuable de la dernière image terminée.</summary>
    public byte[] Render() => MemoryMarshal.AsBytes(_completed.AsSpan()).ToArray();
}
