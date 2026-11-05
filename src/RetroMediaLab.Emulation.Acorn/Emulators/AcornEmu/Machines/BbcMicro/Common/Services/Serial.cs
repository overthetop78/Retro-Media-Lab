using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.CassetteConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Câblage de carte entre SERPROC et MC6850 emprunté ; aucun fichier ni périphérique Windows.</summary>
internal sealed class Serial(IAcia6850 acia)
{
    private readonly BbcSerialUla _ula = new(C.CpuClock);
    internal BbcSerialUla Ula => _ula;
    private long _overridePhase;
    private int _baudRateOverride = acia.BaudRateOverride;
    private bool _modemPinsKnown;
    private bool _ctsHigh;
    private bool _dcdHigh;
    internal int BaudRateOverride
    {
        get => _baudRateOverride;
        set { acia.BaudRateOverride = value; _baudRateOverride = value; }
    }
    internal void PowerOn()
    {
        _ula.PowerOn();
        acia.Reset();
        _overridePhase = 0;
        _modemPinsKnown = false;
    }

    internal void Tick(bool? cassetteInput)
    {
        _ula.SetCassetteInput(cassetteInput);
        _ula.Tick(acia.TransmitBit, acia.RtsHigh);
        var cts = _ula.ClearToSendHigh;
        if (!_modemPinsKnown || _ctsHigh != cts)
        {
            acia.SetCts(cts);
            _ctsHigh = cts;
        }
        var dcd = _ula.DataCarrierDetectHigh;
        if (!_modemPinsKnown || _dcdHigh != dcd)
        {
            acia.SetDcd(dcd);
            _dcdHigh = dcd;
        }
        _modemPinsKnown = true;
        var transmitEdge = _ula.TransmitClockEdge;
        var receiveEdge = _ula.ReceiveClockEdge;
        if (_baudRateOverride > 0 && !_ula.Rs423Selected)
        {
            _overridePhase += (long)_baudRateOverride * acia.ClockDivider;
            transmitEdge = receiveEdge = _overridePhase >= C.CpuClock;
            if (transmitEdge) _overridePhase -= C.CpuClock;
        }
        if (transmitEdge) acia.ClockTransmit();
        if (receiveEdge) acia.ClockReceive(_ula.ReceiveData);
    }
}
