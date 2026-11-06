using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    // null raccorde la broche au lecteur sectoriel emprunté ; une valeur fournit un signal externe explicite.
    private readonly bool?[] _readyInputs = new bool?[C.DriveCount];
    private readonly bool?[] _protectInputs = new bool?[C.DriveCount];
    private readonly bool?[] _trackZeroInputs = new bool?[C.DriveCount];
    private readonly bool?[] _indexInputs = new bool?[C.DriveCount];
    private readonly bool[] _faultInputs = new bool[C.DriveCount];
    private readonly bool[] _notReadyLatched = new bool[C.DriveCount];
    private readonly int[] _physicalTracks = new int[C.DriveCount];
    private bool _externalCount;
    private bool _recalibrating;
    private bool _didStep;
    private int _seekRemaining;
    private int _seekTarget;
    private int _seekDirection;

    public void SetReady(int drive, bool? ready)
    {
        ValidateDrive(drive);
        _readyInputs[drive] = ready;
        if (!Ready(drive))
        {
            _notReadyLatched[drive] = true;
            if (Active && drive == _drive) Complete(C.NotReady);
        }
        PublishClockEvent();
    }

    public void SetWriteProtected(int drive, bool? writeProtected)
    {
        ValidateDrive(drive);
        _protectInputs[drive] = writeProtected;
        if (Active && drive == _drive && IsWriteCommand && WriteProtected()) Complete(C.WriteProtected);
        PublishClockEvent();
    }

    public void SetWriteFault(int drive, bool fault)
    {
        ValidateDrive(drive);
        _faultInputs[drive] = fault;
        if (Active && drive == _drive && fault) Faulted();
        PublishClockEvent();
    }

    public void SetTrackZero(int drive, bool? atTrackZero)
    {
        ValidateDrive(drive);
        _trackZeroInputs[drive] = atTrackZero;
    }

    public void SetIndex(int drive, bool? asserted)
    {
        ValidateDrive(drive);
        var previous = _indexInputs[drive];
        _indexInputs[drive] = asserted;
        if (asserted == true && previous != true && drive == _drive) OnIndexPulse();
        PublishClockEvent();
    }

    /// <summary>Un front Count fait avancer le positionnement lorsque Step Rate vaut zéro.</summary>
    public void SetCount(bool asserted)
    {
        var rising = asserted && !_externalCount;
        _externalCount = asserted;
        if (!rising || _phase != Phase.ExternalSeek || _resetAsserted) return;
        StepDrive();
        if (SeekFinished())
        {
            PublishDriveControl((byte)(_driveOutput & ~C.OutputStep));
            FinishSeek();
        }
        PublishClockEvent();
    }

    private static void ValidateDrive(int drive) =>
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)drive, (uint)C.DriveCount);

    private bool Ready(int drive) => _readyInputs[drive] ?? false;
    private bool WriteProtected() => _protectInputs[_drive] ?? false;
    private bool TrackZero() => _trackZeroInputs[_drive] ?? false;
    private bool IndexAsserted() => _indexInputs[_drive] ?? false;
    private bool IsWriteCommand => _command is C.WriteSingle or C.WriteMultiple or C.WriteDeletedSingle or C.WriteDeletedMultiple or C.Format;

    private void SampleReady()
    {
        for (var drive = 0; drive < C.DriveCount; drive++)
            if (!Ready(drive)) _notReadyLatched[drive] = true;
    }

    private bool CheckDrive()
    {
        SampleReady();
        if (_notReadyLatched[_drive]) { Complete(C.NotReady); return false; }
        if (_faultInputs[_drive]) { Faulted(); return false; }
        if (IsWriteCommand && WriteProtected()) { Complete(C.WriteProtected); return false; }
        return true;
    }

    private void Faulted()
    {
        PublishDriveControl((byte)(_driveOutput & ~(C.OutputWriteEnable | C.OutputStep | C.OutputDirection | C.OutputLowCurrent)));
        Complete(C.WriteFault);
    }

    private void SelectCommandDrive()
    {
        var select = _drive == 0 ? C.Drive0Select : C.Drive1Select;
        if ((_driveOutput & C.DriveSelectMask) != select)
            PublishDriveControl((byte)((_driveOutput & C.OptionalOutput) | select));
    }

    private void SetDriveOutput(byte value)
    {
        var old = _driveOutput;
        PublishDriveControl(value);
        if ((value & C.Drive1Select) != 0) SetActiveDrive(1);
        else if ((value & C.Drive0Select) != 0) SetActiveDrive(0);
        // Les pas manuels par Write Special sont de vraies sorties du composant, même hors commande Seek.
        if ((old & C.OutputStep) == 0 && (value & C.OutputStep) != 0 && (value & C.DriveSelectMask) != 0)
            MovePhysicalTrack((value & C.OutputDirection) != 0 ? 1 : -1);
    }

    private void OnIndexPulse()
    {
        SerialIndexPulse();
        if (_unloadCount == 0 || _unloadCount == C.UnloadForever || _phase != Phase.Idle) return;
        if (--_unloadCount == 0) UnloadHead();
    }

    private void UnloadHead() =>
        PublishDriveControl((byte)(_driveOutput & ~(C.OutputLoadHead | C.DriveSelectMask | C.OutputWriteEnable | C.OutputStep | C.OutputDirection | C.OutputLowCurrent)));

    private int MechanicalDelay(int ticks) => MechanicalSpeedPercent == 0 ? 1 :
        Math.Max(1, checked((int)((long)ticks * C.PercentageScale / MechanicalSpeedPercent)));

    private void BeginPositioning()
    {
        _seekTarget = (_parameters[0] + _seekRetryCount) & byte.MaxValue;
        _recalibrating = _seekTarget == 0;
        if (!_recalibrating)
        {
            var first = _drive == 0 ? C.BadTrack00 : C.BadTrack10;
            for (var bad = 0; bad < C.BadTracksPerDrive; bad++)
                if (_special[first + bad] <= _seekTarget) _seekTarget = (_seekTarget + 1) & byte.MaxValue;
        }
        _seekDirection = _recalibrating || _seekTarget < _tracks[_drive] ? -1 : 1;
        _seekRemaining = _recalibrating ? C.MaximumRecalibrateSteps : Math.Abs(_seekTarget - _tracks[_drive]);
        _didStep = false;
        PublishDriveControl((byte)((_driveOutput & ~(C.OutputDirection | C.OutputLowCurrent | C.OutputStep)) |
            (_seekDirection > 0 ? C.OutputDirection : 0) | (_seekTarget >= C.LowCurrentTrack ? C.OutputLowCurrent : 0)));
        _tracks[_drive] = (byte)_seekTarget;
        if ((_mode & C.SingleActuator) != 0) _tracks[0] = _tracks[1] = (byte)_seekTarget;
        if (SeekFinished()) { FinishSeek(); return; }
        SetPhase(Phase.SeekSetup);
        _delay = MechanicalDelay(checked(_clock * C.DirectionSetupMicroseconds / C.MicrosecondsPerSecond));
    }

    private bool SeekFinished() => _seekRemaining == 0 || _recalibrating && TrackZero();

    private void MovePhysicalTrack(int direction)
    {
        _physicalTracks[_drive] = Math.Clamp(_physicalTracks[_drive] + direction, 0, byte.MaxValue);
        _seekSteps[_drive]++;
        if ((_mode & C.SingleActuator) != 0) _physicalTracks[1 - _drive] = _physicalTracks[_drive];
    }

    private void StepDrive()
    {
        MovePhysicalTrack(_seekDirection);
        _seekRemaining--;
        _didStep = true;
    }

    private void RaiseStep()
    {
        PublishDriveControl((byte)(_driveOutput | C.OutputStep));
        StepDrive();
        SetPhase(Phase.StepHigh);
        _delay = MechanicalDelay(checked(_clock * C.StepPulseMicroseconds / C.MicrosecondsPerSecond));
    }

    private void FinishSeek()
    {
        if (_recalibrating && !TrackZero()) { Complete(C.TrackZeroNotFound); return; }
        if (_recalibrating)
        {
            _physicalTracks[_drive] = 0;
            if ((_mode & C.SingleActuator) != 0) _physicalTracks[1 - _drive] = 0;
        }
        var delay = !Motor ? _loadTicks : _didStep ? _settleTicks : 0;
        PublishDriveControl((byte)(_driveOutput | C.OutputLoadHead));
        SetPhase(Phase.HeadLoad);
        _delay = delay == 0 ? 0 : MechanicalDelay(delay);
    }

    private bool AdvancePositioning()
    {
        switch (_phase)
        {
            case Phase.SeekSetup:
                if (_stepTicks == 0) { PublishDriveControl((byte)(_driveOutput | C.OutputStep)); SetPhase(Phase.ExternalSeek); }
                else RaiseStep();
                return true;
            case Phase.StepHigh:
                PublishDriveControl((byte)(_driveOutput & ~C.OutputStep));
                SetPhase(Phase.StepWait);
                _delay = Math.Max(1, MechanicalDelay(_stepTicks) - MechanicalDelay(checked(_clock * C.StepPulseMicroseconds / C.MicrosecondsPerSecond)));
                return true;
            case Phase.StepWait:
                if (SeekFinished()) FinishSeek(); else RaiseStep();
                return true;
            case Phase.ExternalSeek:
                return true;
            case Phase.HeadLoad:
            case Phase.Positioning:
                Positioned();
                return true;
            default:
                return false;
        }
    }
}
