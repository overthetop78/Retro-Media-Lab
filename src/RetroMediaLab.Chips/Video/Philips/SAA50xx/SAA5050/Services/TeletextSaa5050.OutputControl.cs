using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    private bool _boxed;
    private int _previousCode;

    private bool _pictureOn;
    private bool _displayEnabled = true;
    private bool _superimposeInputHigh = true;
    public bool PictureOn { get => _pictureOn; set { _pictureOn = value; UpdateSerialOutputs(); } }
    public bool DisplayEnabled { get => _displayEnabled; set { _displayEnabled = value; UpdateSerialOutputs(); } }
    public bool SuperimposeInputHigh { get => _superimposeInputHigh; set { _superimposeInputHigh = value; UpdateSerialOutputs(); } }
    public bool Boxed => _boxed;

    /// <summary>Transistor de sortie SI conducteur ; la carte résout le niveau de la ligne.</summary>
    public bool SuperimposeDriveLow => _superimpose && DisplayEnabled;

    // Table 3, p. 15 Mullard ; le signal monochrome représente le point et non
    // la luminance de la couleur RGB. Hors fenêtre, PO décide seul du blanking.
    internal (int Rgb, bool Monochrome, bool Blanking) ResolveVideo(
        bool dot, int foreground, int background, bool displayWindow)
        => ResolveVideo(dot, foreground, background, displayWindow, _boxed, _row);

    private (int Rgb, bool Monochrome, bool Blanking) ResolveVideo(
        bool dot, int foreground, int background, bool displayWindow, bool boxed, int row)
    {
        if (!displayWindow) return (C.Black, false, !PictureOn);
        if (PictureOn && !TextMode && row != 0) return (C.Black, false, false);
        var blanking = !PictureOn || (DisplayEnabled && boxed);
        var enabled = DisplayEnabled && (!PictureOn || _superimpose || boxed);
        if (!enabled) return (C.Black, false, blanking);
        var colour = dot ? foreground : blanking ? background : C.Black;
        return (colour & C.ColourMask, dot, blanking);
    }
}
