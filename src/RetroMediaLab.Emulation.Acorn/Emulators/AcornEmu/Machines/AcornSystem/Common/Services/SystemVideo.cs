using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Cartes VDU System 40 colonnes SAA5050 et 80 colonnes à ROM de caractères.</summary>
/// <remarks>L’assemblage fournit l’horloge des caractères. Les adresses proviennent
/// du CRTC existant ; aucune position de curseur n’est inventée à partir du texte.</remarks>
internal sealed class SystemVideo
{
    private readonly Crtc6845 _crtc = new();
    private readonly bool _eightyColumns;
    private readonly bool _inverted;
    private readonly byte[] _ram;
    private readonly byte[] _characters;
    private readonly TeletextSaa5050? _teletext;
    private readonly int _ramStart;
    private readonly int _crtcStart;
    private byte[] _drawing = new byte[C.FrameBytes];
    private byte[] _completed = new byte[C.FrameBytes];

    internal SystemVideo(bool eightyColumns, ReadOnlySpan<byte> characters, bool relocated = false, bool inverted = false)
    {
        _eightyColumns = eightyColumns;
        _inverted = inverted;
        _characters = characters.ToArray();
        if (eightyColumns && characters.Length != C.CharacterRomBytes) throw new ArgumentException(nameof(characters));
        _ram = new byte[eightyColumns ? C.MonochromeRamBytes : C.TeletextRamBytes];
        _ramStart = eightyColumns ? relocated ? C.RelocatedMonochromeRamStart : C.MonochromeRamStart : C.TeletextRamStart;
        _crtcStart = eightyColumns ? relocated ? C.RelocatedMonochromeCrtcStart : C.MonochromeCrtcStart : C.TeletextCrtcStart;
        if (!eightyColumns) _teletext = new TeletextSaa5050(characters);
    }

    internal long Frame => _crtc.Frame;
    internal byte[] CompletedFrame => _completed;

    internal bool Handles(int address) => InRam(address) || CrtcPort(address) >= 0;

    internal byte Read(int address)
    {
        if (InRam(address)) return _ram[address - _ramStart];
        return CrtcPort(address) switch
        {
            0 => _crtc.VSync ? C.VerticalBlankStatus : (byte)0,
            1 => _crtc.Read(),
            _ => byte.MaxValue
        };
    }

    internal void Write(int address, byte value)
    {
        if (InRam(address)) _ram[address - _ramStart] = value;
        else if (CrtcPort(address) == 0) _crtc.Select(value);
        else if (CrtcPort(address) == 1) _crtc.Write(value);
    }

    internal void Reset()
    {
        _crtc.Reset();
        _teletext?.Reset();
        Array.Clear(_drawing);
        Array.Clear(_completed);
    }

    internal bool TickCharacter()
    {
        if (_crtc.DisplayEnabled) DrawCell();
        if (!_crtc.Tick()) return false;
        (_drawing, _completed) = (_completed, _drawing);
        Array.Clear(_drawing);
        return true;
    }

    private bool InRam(int address) => address >= _ramStart && address < _ramStart + _ram.Length;

    private int CrtcPort(int address)
    {
        var decoded = _eightyColumns ? address & ~C.MonochromeCrtcMirror : address;
        return decoded == _crtcStart ? 0 : decoded == _crtcStart + 1 ? 1 : -1;
    }

    private void DrawCell()
    {
        var cellPixels = _eightyColumns ? C.MonochromeCellPixels : C.TeletextCellPixels;
        var x = _crtc.DisplayColumn * cellPixels;
        var fieldStep = _crtc.InterlaceVideo ? C.InterlaceFieldStep : 1;
        var rowHeight = (_crtc[C.CrtcRasterHeight] + fieldStep) / fieldStep;
        var y = _crtc.Row * rowHeight + _crtc.Raster / fieldStep;
        if (x < 0 || x + cellPixels > C.Width || y < 0 || y >= C.Height) return;
        var code = _ram[_crtc.Address & (_ram.Length - 1)];
        int mask;
        int foreground;
        int background;
        if (_eightyColumns)
        {
            var glyphAddress = code * C.MonochromeGlyphBytes + (_crtc.RasterAddress & C.MonochromeRasterMask);
            var glyph = _crtc.Cursor ? byte.MaxValue : _characters[glyphAddress & (C.CharacterRomBytes - 1)];
            mask = glyph >> C.MonochromeGlyphPadding;
            foreground = _inverted ? 0 : C.White;
            background = _inverted ? C.White : 0;
        }
        else
        {
            if (_crtc.DisplayColumn == 0) _teletext!.BeginLine(_crtc.Row);
            var raster = _crtc.InterlaceVideo ? _crtc.RasterAddress : _crtc.RasterAddress * C.TeletextRasterMultiplier;
            (mask, foreground, background) = _teletext!.Character(code, raster, _crtc.Frame);
            if (_crtc.Cursor) mask ^= (1 << cellPixels) - 1;
        }
        for (var pixel = 0; pixel < cellPixels; pixel++)
        {
            var color = (mask & (1 << (cellPixels - pixel - 1))) != 0 ? foreground : background;
            var offset = (y * C.Width + x + pixel) * C.PixelBytes;
            _drawing[offset] = (color & C.BlueMask) != 0 ? (byte)C.PixelLevel : (byte)0;
            _drawing[offset + 1] = (color & C.GreenMask) != 0 ? (byte)C.PixelLevel : (byte)0;
            _drawing[offset + 2] = (color & C.RedMask) != 0 ? (byte)C.PixelLevel : (byte)0;
        }
    }
}
