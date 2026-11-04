using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.CassetteConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Câblage de carte entre SERPROC et MC6850 emprunté ; aucun fichier ni périphérique Windows.</summary>
internal sealed class Serial(IAcia6850 acia)
{
    internal BbcSerialUla Ula { get; } = new(C.CpuClock);
    private long _overridePhase;
    internal int BaudRateOverride { get => acia.BaudRateOverride; set => acia.BaudRateOverride = value; }
    internal void PowerOn()
    {
        Ula.PowerOn();
        acia.Reset();
        _overridePhase = 0;
    }

    internal void Tick(bool? cassetteInput)
    {
        Ula.SetCassetteInput(cassetteInput);
        Ula.Tick(acia.TransmitBit, acia.RtsHigh);
        acia.SetCts(Ula.ClearToSendHigh);
        acia.SetDcd(Ula.DataCarrierDetectHigh);
        var transmitEdge = Ula.TransmitClockEdge;
        var receiveEdge = Ula.ReceiveClockEdge;
        if (!Ula.Rs423Selected && acia.BaudRateOverride > 0)
        {
            _overridePhase += (long)acia.BaudRateOverride * acia.ClockDivider;
            transmitEdge = receiveEdge = _overridePhase >= C.CpuClock;
            if (transmitEdge) _overridePhase -= C.CpuClock;
        }
        if (transmitEdge) acia.ClockTransmit();
        if (receiveEdge) acia.ClockReceive(Ula.ReceiveData);
    }
}
