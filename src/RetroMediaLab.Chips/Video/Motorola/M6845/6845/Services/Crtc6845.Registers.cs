using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    /// <summary>Le chemin existant Acorn utilise les extensions du HD6845S.</summary>
    public Crtc6845(Crtc6845Model model = Crtc6845Model.Hd6845S)
    {
        if (!Enum.IsDefined(model)) throw new ArgumentOutOfRangeException(nameof(model));
        Model = model;
        _hitachi = model == Crtc6845Model.Hd6845S;
        Reset();
    }

    public Crtc6845Model Model { get; }

    public void Select(byte value) => _selected = (byte)(value & Crtc6845Constants.RegisterAddressMask);

    /// <summary>Lecture logique ; les adresses non lisibles retournent zéro comme les raccordements existants.</summary>
    public byte Read()
    {
        var firstReadable = Model == Crtc6845Model.Hd6845S
            ? Crtc6845Constants.StartAddressHigh : Crtc6845Constants.CursorAddressHigh;
        return _selected >= firstReadable && _selected < Crtc6845Constants.RegisterCount
            ? _registers[_selected] : Crtc6845Constants.UnreadableRegisterValue;
    }

    public void Write(byte value)
    {
        if (_selected >= Crtc6845Constants.WritableRegisterCount) return;
        var mask = Crtc6845Constants.RegisterMasks[_selected];
        if (Model == Crtc6845Model.Mc6845)
        {
            if (_selected == Crtc6845Constants.SyncWidth) mask = Crtc6845Constants.MotorolaSyncWidthMask;
            else if (_selected == Crtc6845Constants.Mode) mask = Crtc6845Constants.MotorolaModeMask;
        }
        _registers[_selected] = (byte)(value & mask);
        RefreshParameters();
        if (_observedPins != 0) PublishPins();
    }
}
