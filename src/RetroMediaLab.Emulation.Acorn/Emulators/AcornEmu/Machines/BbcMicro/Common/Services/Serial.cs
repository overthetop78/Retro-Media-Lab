using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.CassetteConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Câblage de carte entre SERPROC et MC6850 emprunté ; aucun fichier ni périphérique Windows.</summary>
internal sealed partial class Serial
{
    private readonly IAcia6850 _acia;
    private readonly BbcSerialUla _ula = new(C.CpuClock);
    internal BbcSerialUla Ula => _ula;
    private long _overridePhase;
    private int _baudRateOverride;
    private bool _modemPinsKnown;
    private bool _ctsHigh;
    private bool _dcdHigh;
    private bool _receiveData;
    private bool _rs423Selected;
    private int _clockDivider;
    private long _overrideClockStep;

    internal Serial(IAcia6850 acia)
    {
        _acia = acia;
        _baudRateOverride = acia.BaudRateOverride;
        acia.TransmitPinsChanged = _ula.SetTransmitPins;
        acia.ClockDividerChanged = divider =>
        {
            _clockDivider = divider;
            _overrideClockStep = (long)_baudRateOverride * divider;
        };
        _ula.SerialPinsChanged = ReceiveSerialPins;
        _ula.SerialClocksRaised = ReceiveSerialClocks;
        _ula.SetTransmitPins(acia.TransmitBit, acia.RtsHigh);
        _clockDivider = acia.ClockDivider;
        _overrideClockStep = (long)_baudRateOverride * _clockDivider;
        ReceiveSerialPins(_ula.ClearToSendHigh, _ula.DataCarrierDetectHigh, _ula.ReceiveData, _ula.Rs423Selected);
    }
    internal int BaudRateOverride
    {
        get => _baudRateOverride;
        set { _acia.BaudRateOverride = value; _baudRateOverride = value; _overrideClockStep = (long)value * _clockDivider; }
    }
    internal void PowerOn()
    {
        _modemPinsKnown = false;
        _ula.PowerOn();
        _acia.Reset();
        _ula.SetTransmitPins(_acia.TransmitBit, _acia.RtsHigh);
        _clockDivider = _acia.ClockDivider;
        _overrideClockStep = (long)_baudRateOverride * _clockDivider;
        _overridePhase = 0;
        ReceiveSerialPins(_ula.ClearToSendHigh, _ula.DataCarrierDetectHigh, _ula.ReceiveData, _ula.Rs423Selected);
    }

    internal void Tick(bool? cassetteInput)
    {
        _ula.SetCassetteInput(cassetteInput);
        Tick();
    }

    internal void Tick()
    {
        _ula.Tick();
        if (_baudRateOverride > 0 && !_rs423Selected)
        {
            _overridePhase += _overrideClockStep;
            if (_overridePhase >= C.CpuClock)
            {
                _overridePhase -= C.CpuClock;
                _acia.ClockTransmit();
                _acia.ClockReceive(_receiveData);
            }
        }
    }

    private void ReceiveSerialPins(bool cts, bool dcd, bool data, bool selected)
    {
        if (!_modemPinsKnown || _ctsHigh != cts)
        {
            _acia.SetCts(cts);
            _ctsHigh = cts;
        }
        if (!_modemPinsKnown || _dcdHigh != dcd)
        {
            _acia.SetDcd(dcd);
            _dcdHigh = dcd;
        }
        _modemPinsKnown = true;
        _receiveData = data;
        _rs423Selected = selected;
    }

    private void ReceiveSerialClocks(bool transmitEdge, bool receiveEdge, bool data)
    {
        if (_baudRateOverride > 0 && !_rs423Selected) return;
        if (transmitEdge) _acia.ClockTransmit();
        if (receiveEdge) _acia.ClockReceive(data);
    }
}
