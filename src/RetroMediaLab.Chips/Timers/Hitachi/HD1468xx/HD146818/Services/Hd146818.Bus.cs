using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Enums;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;

public sealed partial class Hd146818
{
    private int _busAddress;
    private bool _busAddressStrobe;
    private bool _busDataStrobe;
    private bool _busIntelMode;
    private bool _busReading;
    private bool _busWriting;
    private bool _standby;
    private bool _standbyPin;
    private bool _standbySample;
    private bool _motorolaMode = true;
    private byte _busReadData;

    public bool DrivingDataBus => _busReading;
    public byte DataBus => _busAddress switch
    {
        R.StatusC => _busReadData,
        R.StatusA => (byte)(_registers[R.StatusA] | (_updateInProgress ? R.UpdateInProgress : 0)),
        _ => _registers[_busAddress]
    };

    /// <summary>CE sélectionné, AS/ALE, DS/RD et R/W/WR à leurs niveaux physiques.
    /// Sans suffixe A, DS à la chute AS sélectionne le mode ; la révision A utilise MOT.</summary>
    public void SetBus(byte data, bool chipEnabled, bool addressStrobe, bool dataStrobe, bool readWriteHigh)
    {
        var addressFell = _busAddressStrobe && !addressStrobe;
        var dataEnded = _busIntelMode ? !_busDataStrobe && dataStrobe : _busDataStrobe && !dataStrobe;
        if (Variant == Hd146818Variant.Hd146818A && addressFell) _standbySample = _standbyPin;
        var selected = chipEnabled && !_standby;
        if (selected && addressFell)
        {
            _busAddress = data & R.AddressMask;
            _busIntelMode = Variant == Hd146818Variant.Hd146818A ? !_motorolaMode : dataStrobe;
        }
        var reading = selected && !addressStrobe && (_busIntelMode ? !dataStrobe : dataStrobe && readWriteHigh);
        var writing = selected && !addressStrobe && (_busIntelMode ? !readWriteHigh : dataStrobe && !readWriteHigh);
        if (_busWriting && !writing && selected) WriteRegister(_busAddress, data);
        if (reading && !_busReading) _busReadData = ReadRegister(_busAddress);
        _busAddressStrobe = addressStrobe;
        _busDataStrobe = dataStrobe;
        _busReading = reading;
        _busWriting = writing;
        // STBY gates AD and CS only after the AS/DS latch sequence. These strobes remain live on battery.
        if (Variant == Hd146818Variant.Hd146818A && dataEnded)
        {
            _standby = _standbySample;
            if (_standby) _busReading = _busWriting = false;
        }
    }

    public void SetStandby(bool asserted)
    {
        _standbyPin = asserted;
    }

    public void SetMotorolaMode(bool high) => _motorolaMode = high;
}
