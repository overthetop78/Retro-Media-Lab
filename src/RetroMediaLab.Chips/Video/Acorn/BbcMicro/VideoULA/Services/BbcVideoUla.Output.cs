using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.BbcVideoUlaConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class BbcVideoUla
{
    private bool _displayEnabled;
    private bool _inverted;
    private int _teletextRgb;
    public bool DisplayEnabled { get => _displayEnabled; set { if (_displayEnabled == value) return; _displayEnabled = value; UpdateOutput(); } }
    public bool Inverted { get => _inverted; set { if (_inverted == value) return; _inverted = value; UpdateOutput(); } }
    public int TeletextRgb { get => _teletextRgb; set { value &= C.ColourMask; if (_teletextRgb == value) return; _teletextRgb = value; if (TeletextSelected) UpdateOutput(); } }
    public int Rgb { get; private set; }
    public bool Red => (Rgb & C.RedMask) != 0;
    public bool Green => (Rgb & C.GreenMask) != 0;
    public bool Blue => (Rgb & C.BlueMask) != 0;

    private void UpdateOutput()
    {
        var colour = TeletextSelected ? _teletextRgb : _displayEnabled ? _colours[_logical] : 0;
        if (_displayEnabled && CursorActive) colour ^= C.ColourMask;
        if (_inverted) colour ^= C.ColourMask;
        Rgb = colour;
    }
}
