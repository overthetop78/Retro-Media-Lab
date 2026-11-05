using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.BbcVideoUlaConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class BbcVideoUla
{
    private bool _displayEnabled;
    private bool _inverted;
    private int _teletextRgb;
    public bool DisplayEnabled { get => _displayEnabled; set { if (_displayEnabled == value) return; _displayEnabled = value; UpdateOutput(); } }
    public bool Inverted { get => _inverted; set { if (_inverted == value) return; _inverted = value; UpdateOutput(); } }
    public int TeletextRgb { get => _teletextRgb; set { value &= C.ColourMask; if (_teletextRgb == value) return; _teletextRgb = value; if (_teletextSelected) UpdateOutput(); } }
    private int _rgb;
    public int Rgb => _rgb;
    public bool Red => (_rgb & C.RedMask) != 0;
    public bool Green => (_rgb & C.GreenMask) != 0;
    public bool Blue => (_rgb & C.BlueMask) != 0;

    private void UpdateOutput()
    {
        var colour = _teletextSelected ? _teletextRgb : _displayEnabled ? _colours[_logical] : 0;
        if (_displayEnabled && (_cursorSegments & C.FirstCursorSegment) != 0) colour ^= C.ColourMask;
        if (_inverted) colour ^= C.ColourMask;
        _rgb = colour;
    }
}
