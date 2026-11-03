using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

public sealed partial class Crtc6845
{
    private bool _busSelected;
    private bool _busRegister;
    private bool _busRead;
    private bool _busEnable;
    private bool _busAccessValid;

    public bool DataBusDriven => _busSelected && _busEnable && _busRead;
    public byte? DataBusValue => DataBusDriven
        ? _busRegister ? Read() : Crtc6845Constants.UnreadableRegisterValue
        : null;

    /// <summary>CS est fourni logiquement actif (broche physique basse), RS haut sélectionne la donnée,
    /// R/W haut lit ; E est le niveau physique. L'écriture échantillonne la donnée au descendant de E.
    /// Aucun front E ne fait avancer l'horloge caractère CLK.</summary>
    public void SetBus(bool selected, bool registerSelect, bool read, bool enable, byte data)
    {
        var sameControl = selected == _busSelected && registerSelect == _busRegister && read == _busRead;
        if (_busEnable && !sameControl) _busAccessValid = false;
        if (_busEnable && !enable && _busAccessValid && selected && !read)
        {
            if (registerSelect) Write(data);
            else Select(data);
        }
        if (!_busEnable && enable) _busAccessValid = selected;
        _busSelected = selected;
        _busRegister = registerSelect;
        _busRead = read;
        _busEnable = enable;
    }

    private void ResetBus()
    {
        _busSelected = false;
        _busRegister = false;
        _busRead = false;
        _busEnable = false;
        _busAccessValid = false;
    }
}
