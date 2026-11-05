using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Enums;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

/// <summary>Générateur télétexte SAA505x : ROM externe, attributs de ligne et mosaïques.</summary>
/// <remarks>La copie de ROM appartient au composant. Les commandes sont traitées à chaque passage de ligne,
/// avec leurs effets « à cette cellule » ou « à la cellule suivante », sans modifier la RAM écran.</remarks>
public sealed partial class TeletextSaa5050
    : RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Interfaces.ITeletextSaa5050
{
    public const int CharacterRomSize = C.CharacterRomSize;
    private readonly byte[] _characters;
    private readonly int[] _glyphs;
    private int _foreground, _background, _held = C.FirstCharacter, _row = -1;
    private bool _graphics, _flash, _conceal, _separated, _hold, _double, _bottom, _rowDouble, _heldSeparated;
    public TeletextSaa5050(ReadOnlySpan<byte> characters)
    {
        if (characters.Length != 0 && characters.Length != CharacterRomSize) throw new ArgumentException(nameof(characters));
        _characters = characters.ToArray();
        _glyphs = new int[C.CharacterCount * C.CharacterRows * C.RasterParities];
        for (var glyph = 0; glyph < C.CharacterCount; glyph++)
            for (var row = 0; row < C.CharacterRows; row++)
                for (var parity = 0; parity < C.RasterParities; parity++)
                    _glyphs[(glyph * C.CharacterRows + row) * C.RasterParities + parity] = BuildGlyph(glyph + C.FirstCharacter, row, parity);
        Reset();
    }

    public void Reset()
    {
        _row = -1;
        _bottom = _rowDouble = false;
        ResetRemoteControl();
        BeginLine(0);
        ResetSignals();
        _conceal = true;
    }
    public void BeginLine(int row)
    {
        if (row != _row)
        {
            _bottom = row != 0 && !_bottom && _rowDouble;
            _rowDouble = false; _row = row;
        }
        _boxed = false;
        _previousCode = 0;
        if (!_bigCharacterSelectHigh) _bottom = false;
        _foreground = C.White; _background = C.Black; _held = C.FirstCharacter;
        _graphics = _flash = _conceal = _separated = _hold = _double = _heldSeparated = false;
    }

    /// <summary>Décode une cellule ; les douze bits du masque représentent une ligne de pixels télétexte.</summary>
    public (int Mask, int Foreground, int Background) Character(int data, int raster, long frame)
    {
        data &= C.CharacterDataMask;
        if (data == (int)Saa5050Control.StartBox && _previousCode == data) _boxed = true;
        if (data == (int)Saa5050Control.EndBox && _previousCode == data) _boxed = false;
        _previousCode = data;
        var previousGraphics = _graphics;
        var previousDouble = _double;
        var previousHold = _hold;
        var foreground = _foreground;
        var separated = _separated;
        if (data < C.FirstCharacter)
        {
            // Attributs prenant effet sur la cellule qui porte la commande.
            switch (data)
            {
                case (int)Saa5050Control.Steady: _flash = false; break;
                case (int)Saa5050Control.NormalHeight: _double = false; break;
                case (int)Saa5050Control.Conceal: _conceal = true; break;
                case (int)Saa5050Control.ContiguousGraphics: _separated = false; break;
                case (int)Saa5050Control.SeparatedGraphics: _separated = true; break;
                case (int)Saa5050Control.BlackBackground: _background = 0; break;
                case (int)Saa5050Control.NewBackground: _background = _foreground; break;
                case (int)Saa5050Control.HoldGraphics: _hold = true; break;
            }
        }
        var glyph = data;
        if (data < C.FirstCharacter)
        {
            glyph = previousGraphics && (previousHold || _hold) && _double == previousDouble ? _held : C.FirstCharacter;
            separated = _heldSeparated;
        }
        else if (_graphics && (data & C.GraphicsCharacterBit) != 0) { _held = data; _heldSeparated = _separated; }
        _rowDouble |= _double;
        var mask = 0;
        var large = _double || !_bigCharacterSelectHigh;
        var fontRow = !_bigCharacterSelectHigh ? raster / C.DoubleHeightRasterDivisor : _double ? (_bottom ? C.HalfCharacterRows : 0) + raster / C.DoubleHeightRasterDivisor : raster / 2;
        var isMosaic = _graphics && (glyph & C.GraphicsCharacterBit) != 0;
        var rasterLimit = C.CharacterRows * C.RasterParities * (_bigCharacterSelectHigh ? 1 : C.RasterParities);
        if ((uint)raster < rasterLimit && (!_bottom || large) && (!_conceal || _reveal) && (!_flash || frame % C.FlashFields < C.FlashVisibleFields))
            mask = isMosaic ? Mosaic(glyph, fontRow, separated) : Glyph(glyph, fontRow, large ? raster / C.RasterParities : raster);
        var result = (mask, foreground, _background);
        if (data is >= (int)Saa5050Control.AlphaRed and <= (int)Saa5050Control.AlphaWhite) { _foreground = data; _graphics = _conceal = false; }
        if (data is >= (int)Saa5050Control.GraphicsRed and <= (int)Saa5050Control.GraphicsWhite) { _foreground = data & C.ColourMask; _graphics = true; _conceal = false; }
        if (data == (int)Saa5050Control.Flash) _flash = true;
        if (data == (int)Saa5050Control.DoubleHeight) { _double = true; _rowDouble = true; }
        if (data == (int)Saa5050Control.ReleaseGraphics) _hold = false;
        if (!_graphics || previousDouble != _double) _held = C.FirstCharacter;
        return result;
    }

}
