using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6845.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Saa5050.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using System.Runtime.InteropServices;
using VideoConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Câblage vidéo BBC : bus RAM emprunté, CRTC, ULA et générateur télétexte.</summary>
/// <remarks>Chaque caractère est dessiné pendant son cycle de lecture. Une modification ultérieure de RAM,
/// de palette ou de registres ne redessine pas les pixels déjà balayés. Les images publiées restent immuables.</remarks>
internal sealed class Video
{
    private readonly byte[] _ram;
    private readonly Crtc6845 _crtc = new();
    private readonly TeletextSaa5050 _teletext;
    private readonly byte[] _palette = new byte[16];
    private readonly uint[] _bitmapPalette = new uint[VideoConstants.PaletteEntries * 2];
    private int _bitmapDecodeOffset;
    private int _bitmapWidth = VideoConstants.WideBitmapCellWidth;
    private bool _monochrome;
    private byte[] _drawing;
    private byte[] _completed;
    internal int Height { get; }
    private byte _control;
    private int _phase, _cursorRemaining;
    internal int ScreenWrap { get; set; } = 0x5800;
    internal bool Monochrome
    {
        get => _monochrome;
        set { _monochrome = value; UpdateBitmapPalette(); }
    }
    internal Func<int, byte>? ReadMemory { get; set; }
    internal bool VSync => _crtc.VSync;
    internal long Frame => _crtc.Frame;

    /// <summary>La RAM reste possédée par la carte ; le générateur conserve sa propre copie de ROM externe.</summary>
    internal Video(byte[] ram, ReadOnlySpan<byte> characters, int height = VideoConstants.Height)
    {
        if (height is not (VideoConstants.Height or VideoConstants.NtscHeight)) throw new ArgumentOutOfRangeException(nameof(height));
        _ram = ram;
        _teletext = new(characters);
        Height = height;
        _drawing = new byte[VideoConstants.Width * Height * 4];
        _completed = new byte[_drawing.Length];
        UpdateBitmapMode();
        UpdateBitmapPalette();
    }
    internal void Reset()
    {
        _crtc.Reset(); _teletext.Reset(); Array.Clear(_palette);
        Array.Clear(_drawing); Array.Clear(_completed); _control = 0; _phase = _cursorRemaining = 0; ScreenWrap = 0x5800;
        UpdateBitmapMode();
        UpdateBitmapPalette();
    }
    internal void Select(byte value) => _crtc.Select(value);
    internal void WriteRegister(byte value) => _crtc.Write(value);
    internal byte ReadRegister() => _crtc.Read();
    internal void WriteUla(ushort address, byte value)
    {
        if ((address & 1) == 0) { _control = value; UpdateBitmapMode(); }
        else _palette[value >> 4] = (byte)((value & 15) ^ 7);
        UpdateBitmapPalette();
    }

    /// <summary>Avance depuis l'horloge CPU de 2 MHz ; l'ULA choisit un caractère à 1 ou 2 MHz.</summary>
    internal void Tick()
    {
        _phase ^= 1;
        if ((_control & 16) == 0 && _phase != 0) return;
        if (_crtc.Column == 0) { _teletext.BeginLine(_crtc.Row); _cursorRemaining = 0; }
        if (_crtc.IsProgrammed && _crtc.DisplayEnabled && _crtc.Scanline < Height)
        {
            if (_crtc.CursorForCell(_crtc.Address)) _cursorRemaining = (_control & 2) != 0 ? 1 : VideoConstants.CursorWidths[_control >> 5];
            if ((_control & 2) != 0) TeletextCharacter();
            else BitmapCharacter();
            if (_cursorRemaining > 0) _cursorRemaining--;
        }
        if (_crtc.Tick() && _crtc.IsProgrammed)
        {
            // Render publie une copie immuable : les deux tampons internes restent
            // possédés par la vidéo et peuvent donc être réutilisés sans modifier une image publiée.
            (_completed, _drawing) = (_drawing, _completed);
            Array.Clear(_drawing);
        }
    }

    /// <summary>Retourne une copie de la dernière image terminée, sans relire les registres après coup.</summary>
    internal byte[] Render() => _completed.ToArray();

    private void UpdateBitmapMode()
    {
        var wide = (_control & 16) == 0;
        _bitmapWidth = wide ? VideoConstants.WideBitmapCellWidth : VideoConstants.BitmapCellWidth;
        var mode = Math.Min(((_control >> 2) & 3) + (wide ? 1 : 0), VideoConstants.BitmapDecodeModes - 1);
        _bitmapDecodeOffset = mode * VideoConstants.BitmapByteValues * VideoConstants.BitmapCellWidth;
    }

    private void UpdateBitmapPalette()
    {
        var colours = _monochrome ? VideoConstants.PackedMonochrome : VideoConstants.PackedColours;
        for (var logical = 0; logical < VideoConstants.PaletteEntries; logical++)
        {
            var physical = _palette[logical];
            var colour = (physical & 8) != 0 && (_control & 1) != 0 ? (physical ^ 7) & 7 : physical & 7;
            _bitmapPalette[logical] = colours[colour];
            _bitmapPalette[logical + VideoConstants.CursorPaletteOffset] = colours[colour ^ 7];
        }
    }

    private void BitmapCharacter()
    {
        if (_crtc.RasterAddress >= 8) return;
        var address = (_crtc.Address << 3) | _crtc.RasterAddress;
        if (address >= 0x8000) address = (address + ScreenWrap) & 0x7fff;
        var offset = address & (_ram.Length - 1);
        var data = ReadMemory?.Invoke(offset) ?? _ram[offset];
        var width = _bitmapWidth;
        var left = _crtc.DisplayColumn * width;
        var y = _crtc.Scanline;
        if ((uint)y >= Height) return;
        var first = Math.Max(0, -left);
        var last = Math.Min(width, VideoConstants.Width - left);
        var source = _bitmapDecodeOffset + data * VideoConstants.BitmapCellWidth;
        var logicalPixels = VideoConstants.BitmapLogicalPixels;
        var pixels = MemoryMarshal.Cast<byte, uint>(_drawing.AsSpan());
        var destination = y * VideoConstants.Width + left;
        var cursor = _cursorRemaining > 0 ? VideoConstants.CursorPaletteOffset : 0;
        var shift = width == VideoConstants.WideBitmapCellWidth ? 1 : 0;
        for (var x = first; x < last; x++)
        {
            pixels[destination + x] = _bitmapPalette[logicalPixels[source + (x >> shift)] + cursor];
        }
    }

    private void TeletextCharacter()
    {
        var offset = (0x7c00 | (_crtc.Address & 0x3ff)) & (_ram.Length - 1);
        var data = ReadMemory?.Invoke(offset) ?? _ram[offset];
        var raster = _crtc.InterlaceVideo ? _crtc.RasterAddress : _crtc.RasterAddress * 2;
        var cell = _teletext.Character(data, raster, _crtc.Frame);
        var left = _crtc.DisplayColumn * VideoConstants.TeletextCellWidth;
        var y = _crtc.Scanline;
        if ((uint)y >= Height) return;
        var first = Math.Max(0, -left);
        var last = Math.Min(VideoConstants.TeletextCellWidth, VideoConstants.Width - left);
        var masks = VideoConstants.TeletextPixelMasks;
        var colours = Monochrome ? VideoConstants.PackedMonochrome : VideoConstants.PackedColours;
        var cursor = _cursorRemaining > 0 ? 7 : 0;
        var foreground = colours[cell.Foreground ^ cursor];
        var background = colours[cell.Background ^ cursor];
        var pixels = MemoryMarshal.Cast<byte, uint>(_drawing.AsSpan());
        var destination = y * VideoConstants.Width + left;
        for (var x = first; x < last; x++)
        {
            pixels[destination + x] = (cell.Mask & masks[x]) != 0 ? foreground : background;
        }
    }

}
