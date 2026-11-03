using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    private bool _busSelected;
    private bool _busRead;
    private int _busRegister;
    private byte _busWriteData;

    /// <summary>DAL piloté pendant CS actif et R/W haut ; null hors lecture sélectionnée.</summary>
    public byte? BusData { get; private set; }

    /// <summary>
    /// CS est normalisé actif ; read représente le niveau de R/W.
    /// Les données écrites sont retenues au dernier appel du cycle, puis échantillonnées
    /// au relâchement de CS ou au changement de R/W. Une lecture est acquittée à sa fin.
    /// Les temps de propagation électriques ne sont pas ajoutés par cet accès logique.
    /// </summary>
    public void SetBus(bool selected, bool read, int register, byte value = 0)
    {
        register &= C.RegisterMask;
        var continuing = _busSelected && selected && _busRead == read && _busRegister == register;
        if (_busSelected && !continuing)
        {
            if (_busRead) AcknowledgeRead(_busRegister);
            else Write(_busRegister, _busWriteData);
        }

        if (selected && read)
        {
            if (!continuing) BusData = PeekRegister(register);
        }
        else BusData = null;

        _busSelected = selected;
        _busRead = read;
        _busRegister = register;
        if (selected && !read) _busWriteData = value;
    }

    private void ResetBus()
    {
        _busSelected = false;
        _busRead = false;
        _busRegister = C.StatusCommandRegister;
        _busWriteData = 0;
        BusData = null;
    }
}
