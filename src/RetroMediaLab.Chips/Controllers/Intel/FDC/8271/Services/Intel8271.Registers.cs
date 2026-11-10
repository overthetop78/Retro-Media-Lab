using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    /// <summary>RESET reste actif jusqu’à sa libération. Reset() est la commodité de réinitialisation du propriétaire.</summary>
    public void SetReset(bool asserted)
    {
        if (asserted && !_resetAsserted) Reset();
        _resetAsserted = asserted;
        PublishClockEvent();
    }

    /// <summary>Cycle CPU avec CS, ou cycle de données avec DACK indépendamment de CS ; null signifie haute impédance.</summary>
    public byte? ReadBus(bool selected, bool dmaAcknowledge, int register)
    {
        if (dmaAcknowledge) return Read(4);
        return selected ? Read(register & 3) : null;
    }

    public void WriteBus(bool selected, bool dmaAcknowledge, int register, byte value)
    {
        if (dmaAcknowledge) Write(4, value);
        else if (selected) BufferBusWrite(register & 3, value);
    }

    private void AcknowledgeData()
    {
        if (!_awaitingData) return;
        _dataRequest = _awaitingData = false;
        SetStatus((byte)(_status & ~(C.Interrupt | C.NonDma)));
        PublishClockEvent();
    }

    private void UpdateTiming()
    {
        // Les unités sont des cycles du contrôleur : à 2 MHz elles doublent par rapport à 4 MHz.
        _stepTicks = TimingTicks(_special[C.StepRate]);
        _settleTicks = TimingTicks(_special[C.SettleTime]);
        _loadTicks = TimingTicks((_special[C.LoadUnload] & 15) * C.HeadLoadUnitMilliseconds);
    }

    private int TimingTicks(int standardMilliseconds) =>
        checked((int)((long)standardMilliseconds * C.ChipCyclesPerMillisecond * _clock / _chipClock));

    private void Specify()
    {
        // Ces trois destinations sont les seules documentées par Intel (initialisation et mauvais tracks).
        var first = _parameters[0];
        if (first is not (C.StepRate or C.BadTrack00 or C.BadTrack10)) return;
        for (var index = 1; index < 4; index++) WriteSpecial((byte)(first + index - 1), _parameters[index]);
    }

    private byte ReadSpecial(byte address) => (address & C.SpecialRegisterMask) switch
    {
        C.Track0 => _tracks[0],
        C.Track1 => _tracks[1],
        C.Mode => _mode,
        C.DriveOutput => _driveOutput,
        C.DriveInput => DriveInputs(),
        C.ScanSector => (byte)_sector,
        var register => _special[register]
    };

    private void WriteSpecial(byte address, byte value)
    {
        var register = address & C.SpecialRegisterMask;
        _special[register] = value;
        switch (register)
        {
            case C.Track0: _tracks[0] = value; break;
            case C.Track1: _tracks[1] = value; break;
            case C.Mode:
                _mode = (byte)(value | C.ModeFixed);
                _special[register] = _mode;
                break;
            case C.DriveOutput:
                SetDriveOutput(value);
                break;
            case C.StepRate:
            case C.SettleTime:
            case C.LoadUnload:
                UpdateTiming();
                break;
        }
    }

    private byte DriveInputs()
    {
        SampleReady();
        var value = 0;
        if (Ready(0)) value |= C.InputReady0;
        if (Ready(1)) value |= C.InputReady1;
        if (TrackZero()) value |= C.InputTrackZero;
        if (WriteProtected()) value |= C.InputWriteProtect;
        if (IndexAsserted()) value |= C.InputIndex;
        if (_faultInputs[_drive]) value |= C.InputFault;
        return (byte)value;
    }
}
