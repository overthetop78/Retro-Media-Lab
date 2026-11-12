using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    public void ReceiveCpuPins(long timeNanoseconds, Tms9918CpuPins pins)
    {
        AdvanceTo(timeNanoseconds);
        var previous = _cpuPins;
        _cpuPins = pins;
        if (_reset) return;
        bool falling = previous.CsrHigh && !pins.CsrHigh;
        bool rising = !previous.CsrHigh && pins.CsrHigh;
        if (falling)
        {
            _readMode = pins.Mode;
            _readQualified = pins.CswHigh;
            _dataTargetDriven = true;
            _dataDue = checked(timeNanoseconds + _cpuTiming.AccessNanoseconds);
        }
        if (!previous.CsrHigh && (pins.Mode != previous.Mode || !pins.CswHigh))
        { _readQualified = false; _qualified = false; }

        // Les deux récepteurs ont leur état actualisé avant leurs notifications.
        _statusReadBus.ReceivePins(timeNanoseconds, pins);
        _writeBus.ReceivePins(timeNanoseconds, pins);
        if (rising)
        {
            // Même convention provisoire que le status : demande suivante
            // à la fin du strobe, après observation de l'octet préchargé.
            if (!_readMode && _readQualified)
            {
                bool pending = _memory.HasPendingRequest;
                if (pending) MarkCpuVramConflict();
                _memory.DataReadAccepted();
                if (!pending) ArmCpuVramRequest();
            }
            if (_dataDue is { } access && access > timeNanoseconds) _qualified = false;
            _dataTargetDriven = false;
            _dataDue = checked(timeNanoseconds + _cpuTiming.ReleaseNanoseconds);
        }
        if (!pins.CsrHigh && !pins.CswHigh) _qualified = false;
        PublishDeadline();
        AdvanceTo(timeNanoseconds); // Résoudre aussi un profil à délai nul.
    }

    private void AcceptCpuWrite(Tms9918CpuWrite write)
    {
        if (!write.Qualified) { _qualified = false; return; }
        if (!write.Mode)
        {
            bool pending = _memory.HasPendingRequest;
            if (pending) MarkCpuVramConflict();
            _memory.DataWriteAccepted(write.Data);
            if (!pending) ArmCpuVramRequest();
            return;
        }
        var command = _protocol.AcceptControlByte(write.Data);
        if (!command.IsDocumentedFormat) { _qualified = false; return; }
        if (command.Kind == Tms9918ControlCommandKind.FirstByteLatched)
        {
            if (_memory.HasPendingRequest) MarkCpuVramConflict();
            _memory.AddressLowAccepted(command.FirstByte);
            return;
        }
        bool requestPending = _memory.HasPendingRequest;
        if (requestPending) MarkCpuVramConflict();
        switch (command.Kind)
        {
            case Tms9918ControlCommandKind.WriteRegister:
                _registers.Write(command.Register, command.FirstByte);
                _memory.RegisterWriteAccepted();
                PublishCpuOutputs();
                break;
            case Tms9918ControlCommandKind.SetReadAddress:
                _memory.AddressHighAccepted(command.Address, true);
                if (!requestPending) ArmCpuVramRequest();
                break;
            case Tms9918ControlCommandKind.SetWriteAddress:
                _memory.AddressHighAccepted(command.Address, false);
                break;
        }
    }

    // Les sources ne caractérisent pas tous les croisements hors protocole.
    // Conserver les captures et le cycle physique sans certifier ses octets.
    private void MarkCpuVramConflict()
    {
        _qualified = false;
        if (!_cpuVramInService) return;
        _vramAddressKnown = false;
        if (_ramLoadActive) _ramDataKnown = false;
    }

    public void SetReset(long timeNanoseconds, bool asserted)
        => SetResetSyncVoltage(timeNanoseconds, asserted ? 0 : 5, true);

    private void ApplyResetLevel(long timeNanoseconds, bool asserted)
    {
        AdvanceTo(timeNanoseconds);
        if (_reset == asserted) return;
        _reset = asserted;
        if (asserted)
        {
            _resetStarted = timeNanoseconds;
            _raster = default;
            _clock = _clock with { HalfEdgePhase = 0, PixelClockHigh = false };
            // Modèle numérique provisoire : effet à l'assertion, pas un
            // prétendu délai interne de 3 us déduit d'une largeur minimale.
            _registers.ApplyReset();
            _status.ClearFlags();
            _protocol.StatusReadAccepted();
            _memory = new(new(null, _memory.Data, Tms9918CpuVramOperation.None, _memory.DataKnown));
            CancelVramOnReset();
            ResetSpritePipeline();
            _dataDue = null;
            PublishCpuOutputs(CpuOutputs.Data, false, false);
        }
        else
        {
            if (timeNanoseconds - _resetStarted < 3000) _qualified = false;
            // Réinitialiser les récepteurs aux niveaux reçus pendant RESET,
            // sans rejouer un ancien front CSW/CSR en sortie de reset.
            _writeBus.WriteCaptured = null;
            _statusReadBus.StatusAcknowledged = null;
            _writeBus = new(_cpuPins, timeNanoseconds) { WriteCaptured = AcceptCpuWrite };
            _statusReadBus = new(new(timeNanoseconds, _cpuPins, false, false), _status, _protocol)
                { StatusAcknowledged = _ => PublishCpuOutputs() };
            _readQualified = false;
        }
        PublishDeadline();
    }
}
