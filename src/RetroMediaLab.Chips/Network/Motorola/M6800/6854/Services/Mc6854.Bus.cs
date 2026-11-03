using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private bool _busEnable;
    private bool _busSelected;
    private bool _busRead;
    private byte? _busData;
    private int _busRegister;
    private bool _receiveDmaSuppressed;
    private bool _transmitDmaSuppressed;

    /// <summary>Broches E, sélection CS active et R/W. Null représente le bus de données non piloté.</summary>
    /// <remarks>Le propriétaire fournit un cycle respectant setup/hold ; la puce ne crée pas de temps hôte.</remarks>
    public byte? SetBus(bool enable, bool selected, bool read, int register, byte data = 0)
    {
        if (enable && !_busEnable)
        {
            _transmitDmaSuppressed = false;
            if (selected && read)
            {
                _busData = Read(register);
                _receiveDmaSuppressed = (register & C.RegisterMask) > C.AddressControl;
            }
        }
        if (!enable && _busEnable && selected && _busSelected && !read && !_busRead)
        {
            Write(register, data);
            _transmitDmaSuppressed = (register & C.RegisterMask) > C.AddressControl &&
                !((register & C.RegisterMask) == C.RegisterMask && (_control1 & C.AddressControl) != 0);
        }
        if (!enable)
            _receiveDmaSuppressed = false;
        _busRegister = register;
        _busEnable = enable;
        _busSelected = selected;
        _busRead = read;
        if (!enable || !selected || !read)
            _busData = null;
        return _busData;
    }
}
