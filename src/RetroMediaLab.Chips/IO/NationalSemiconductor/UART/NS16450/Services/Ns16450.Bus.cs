using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;
using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Enums;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

public sealed partial class Ns16450
{
    private bool _addressStrobeHigh;
    private bool _selected;
    private int _busRegister;
    private bool _busWriteActive;
    private int _busWriteRegister;
    private byte _busWriteData;
    private bool _busReadActive;
    private byte _busReadData;

    public bool ChipSelected => _selected;
    public bool DriverDisableHigh => !_busReadActive;

    public byte? SetBus(bool cs0High, bool cs1High, bool cs2High, bool addressStrobeHigh,
        bool readHigh, bool readLowHigh, bool writeHigh, bool writeLowHigh, int register, byte input)
    {
        // National section 6: ADS is transparent while low and closes on its positive edge.
        if (!addressStrobeHigh || !_addressStrobeHigh)
        {
            _selected = cs0High && cs1High && !cs2High;
            _busRegister = register & C.RegisterMask;
        }
        _addressStrobeHigh = addressStrobeHigh;
        var reading = _selected && !_reset && (readHigh || !readLowHigh);
        var writing = _selected && !_reset && (writeHigh || !writeLowHigh);
        if (reading && writing)
        {
            // Simultaneous read/write is not a defined transfer. Never drive the bus in that case.
            _busWriteActive = _busReadActive = false;
            return null;
        }
        if (_busWriteActive && !writing)
        {
            _busWriteActive = false;
            if (_selected && !_reset) Write(_busWriteRegister, _busWriteData);
        }
        if (writing)
        {
            if (!_busWriteActive && _busRegister == (int)Ns16450Register.DataDivisorLow && !DivisorAccess)
            {
                // WRTHR clears both empty flags at its leading edge, before data is latched.
                _lineStatus &= unchecked((byte)~(C.HoldingEmpty | C.TransmitterEmpty));
                _transmitInterrupt = false;
                _transmitInterruptTicks = _transmitEmptyTicks = 0;
                UpdateInterrupt();
            }
            _busWriteRegister = _busRegister;
            _busWriteData = input;
            _busWriteActive = !_reset;
        }
        if (!reading)
        {
            _busReadActive = false;
            return null;
        }
        if (!_busReadActive)
        {
            // A stretched RD is one access: IIR and the data bus remain frozen throughout it.
            _busReadActive = true;
            _busReadData = Read(_busRegister);
        }
        return _reset ? null : _busReadData;
    }
}
