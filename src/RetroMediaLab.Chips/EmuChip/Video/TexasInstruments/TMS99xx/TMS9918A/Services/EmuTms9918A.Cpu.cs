using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class EmuTms9918A
{
    private long _cpuInputTime;
    /// <summary>Only inactive strobes can be transported ahead of the video
    /// domain. They cannot capture data, acknowledge status or drive CPU data.</summary>
    public bool TryReceiveInactiveCpuPins(long timeNanoseconds, Tms9918CpuPins pins)
    {
        if (!_cpuPins.CsrHigh || !_cpuPins.CswHigh || !pins.CsrHigh || !pins.CswHigh || _dataDue >= 0)
            return false;
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _cpuInputTime);
        _cpuInputTime = timeNanoseconds;
        _cpuPins = pins;
        if (!_reset)
        {
            _statusReadBus.ReceivePins(timeNanoseconds, pins);
            _writeBus.ReceivePins(timeNanoseconds, pins);
        }
        return true;
    }

    private const ulong CpuInputMask = 4095, InactiveCpuStrobes = 3072;
    private long _inactiveCpuPrograms, _inactiveCpuProgramWords;
    public long InactiveCpuPrograms => _inactiveCpuPrograms;
    public long InactiveCpuProgramWords => _inactiveCpuProgramWords;

    // Low byte DATA, then knowledge, MODE, CSR and CSW. The source word is
    // prepared once by its actual connections, not once per receiver.
    public static ulong PackCpuInput(Tms9918CpuPins pins)
        => pins.Data | (pins.DataKnown ? 256UL : 0) | (pins.Mode ? 512UL : 0)
            | (pins.CsrHigh ? 1024UL : 0) | (pins.CswHigh ? 2048UL : 0);

    private static Tms9918CpuPins UnpackCpuInput(ulong word)
        => new((byte)word, (word & 256) != 0, (word & 512) != 0, (word & 1024) != 0, (word & 2048) != 0);

    public int TryReceiveInactiveCpuProgram(ReadOnlySpan<EmuSignalWord> inputs)
    {
        if (!_cpuPins.CsrHigh || !_cpuPins.CswHigh || _dataDue >= 0 || inputs.IsEmpty) return 0;
        ulong previous = PackCpuInput(_cpuPins), final = previous;
        long time = Math.Max(_cpuInputTime, _time), firstData = -1, lastData = -1, lastMode = -1;
        int count = 0;
        foreach (ref readonly var input in inputs)
        {
            if ((input.Bits & ~CpuInputMask) != 0) throw new ArgumentOutOfRangeException(nameof(inputs));
            if ((input.Bits & InactiveCpuStrobes) != InactiveCpuStrobes) break;
            ArgumentOutOfRangeException.ThrowIfLessThan(input.Time, time);
            time = input.Time;
            ulong changed = previous ^ input.Bits;
            if ((changed & 511) != 0)
            {
                if (firstData < 0) firstData = time;
                lastData = time;
            }
            if ((changed & 512) != 0) lastMode = time;
            final = previous = input.Bits;
            count++;
        }
        if (count == 0) return 0;
        var pins = UnpackCpuInput(final);
        if (!_reset)
        {
            _statusReadBus.ReceiveInactiveProgram(time, pins);
            _writeBus.ReceiveInactiveProgram(time, pins, firstData, lastData, lastMode);
        }
        _cpuInputTime = time; _cpuPins = pins;
        _inactiveCpuPrograms++; _inactiveCpuProgramWords += count;
        return count;
    }

    public void ReceiveCpuInputWord(long time, ulong word)
    {
        if ((word & ~CpuInputMask) != 0) throw new ArgumentOutOfRangeException(nameof(word));
        ReceiveCpuPins(time, UnpackCpuInput(word));
    }

    public bool TryAdvanceInactiveCpuInterfaceTo(long timeNanoseconds)
    {
        if (!_cpuPins.CsrHigh || !_cpuPins.CswHigh || _dataDue >= 0) return false;
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _cpuInputTime);
        _cpuInputTime = timeNanoseconds;
        if (!_reset)
        {
            _statusReadBus.AdvanceInputTime(timeNanoseconds);
            _writeBus.AdvanceInputTime(timeNanoseconds);
        }
        return true;
    }

    /// <summary>Stable-input time transport. Actual pending VRAM/data responses
    /// still run through AdvanceTo; only unchanged input decoding is avoided.</summary>
    public void AdvanceCpuInterfaceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _cpuInputTime);
        AdvanceTo(timeNanoseconds);
        _cpuInputTime = timeNanoseconds;
        if (_reset) return;
        _statusReadBus.AdvanceInputTime(timeNanoseconds);
        _writeBus.AdvanceInputTime(timeNanoseconds);
    }

    public void ReceiveCpuPins(long timeNanoseconds, Tms9918CpuPins pins)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _cpuInputTime);
        AdvanceTo(timeNanoseconds);
        _cpuInputTime = timeNanoseconds;
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
            SetDataDeadline(checked(timeNanoseconds + _cpuTiming.AccessNanoseconds));
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
            if (_dataDue > timeNanoseconds) _qualified = false;
            _dataTargetDriven = false;
            SetDataDeadline(checked(timeNanoseconds + _cpuTiming.ReleaseNanoseconds));
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
        InvalidateClockPredictions();
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
            SetDataDeadline(-1);
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
        if (_hasMasterSource && asserted) CpuClockSourceChanged?.Invoke(CpuClockSource);
    }
}
