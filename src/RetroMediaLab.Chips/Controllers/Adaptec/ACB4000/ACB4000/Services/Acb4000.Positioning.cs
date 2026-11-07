using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PositioningConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private readonly bool[] _driveReady = new bool[C.DriveCount];
    private readonly bool[] _driveSeekComplete = new bool[C.DriveCount];
    private readonly bool[] _driveTrackZero = new bool[C.DriveCount];
    private readonly bool[] _driveMoving = new bool[C.DriveCount];
    private readonly int[] _driveCylinder = new int[C.DriveCount];
    private long _positioningOffset;
    private IScsiStorage? _positioningDrive;
    private Acb4000PositioningPurpose _positioningPurpose;
    private bool _positioningExpired;

    public event Action<int, bool>? DriveStepped;

    public void SetDriveSignals(int unit, bool ready, bool seekComplete, bool trackZero)
    {
        ValidateDriveUnit(unit);
        var changed = _driveReady[unit] != ready;
        _driveReady[unit] = ready;
        _driveSeekComplete[unit] = seekComplete;
        _driveTrackZero[unit] = trackZero;
        if (changed && unit == Unit) UpdateDriveGates();
    }

    public bool DriveStepIssued(int unit) { ValidateDriveUnit(unit); return _positioners[unit].StepIssued; }
    public bool DriveDirectionIn(int unit) { ValidateDriveUnit(unit); return _positioners[unit].DirectionIn; }
    public void ExpireDriveSeek(int unit)
    {
        ValidateDriveUnit(unit);
        if (_stage == Acb4000Stage.Positioning && Unit == unit) _positioningExpired = true;
        _positioners[unit].ExpireSeekComplete();
    }

    private static void ValidateDriveUnit(int unit) => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)unit, (uint)C.DriveCount);

    private void TickPositioners()
    {
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var positioning = _positioners[unit];
            var wasBusy = positioning.Busy;
            positioning.Tick(_driveReady[unit], _driveSeekComplete[unit], _driveTrackZero[unit]);
            if (positioning.StepIssued)
            {
                _driveCylinder[unit] += positioning.DirectionIn ? 1 : -1;
                _stepCount[unit]++;
                DriveStepped?.Invoke(unit, positioning.DirectionIn);
            }
            // The owner must have an opportunity to react to the final STEP before sampling completion.
            if (!wasBusy && _driveMoving[unit] && _driveSeekComplete[unit]) _driveMoving[unit] = false;
        }
    }

    private void BeginRezeroCommand()
    {
        if (!ReservedShort()) { Complete(C.BadArgument); return; }
        _positioningOffset = 0;
        _positioningPurpose = Acb4000PositioningPurpose.Command;
        _positioningDrive = Drive;
        _positioningExpired = false;
        _positioners[Unit].BeginRezero(_modeParameters[Unit].StepRate);
        _driveMoving[Unit] = true;
        SetStage(Acb4000Stage.Positioning);
    }

    private void BeginSeekCommand()
    {
        if (_command[C.ShortCountOffset] != 0) { Complete(C.BadArgument); return; }
        if (!_hasFormat[Unit]) { Complete(C.BlownFormat); return; }
        if (!AddressAvailable()) { Complete(C.IllegalAddress, true); return; }
        var drive = Drive!;
        if (drive.SectorsPerTrack <= 0 || drive.Heads <= 0 || drive.SectorSize <= 0)
        { Complete(C.BlownFormat); return; }
        BeginPositioning((long)_address * BlockSize, Acb4000PositioningPurpose.Command);
    }

    private void BeginParkingCommand()
    {
        if (!_hasFormat[Unit]) { Complete(C.BlownFormat); return; }
        _positioningDrive = Drive;
        _positioningPurpose = Acb4000PositioningPurpose.Parking;
        _positioningExpired = false;
        SelectedHead = 0;
        // The landing cylinder may lie outside the data geometry; it is not a sector address.
        _positioners[Unit].BeginSeek(_modeParameters[Unit].LandingCylinder - _driveCylinder[Unit],
            _modeParameters[Unit].StepRate);
        _driveMoving[Unit] = true;
        SetStage(Acb4000Stage.Positioning);
    }

    private void BeginPositioning(long byteOffset, Acb4000PositioningPurpose purpose)
    {
        if (Drive is not { } drive || !_driveReady[Unit]) { Complete(C.NotReady); return; }
        if (purpose != Acb4000PositioningPurpose.Format
            && (drive.SectorsPerTrack <= 0 || drive.Heads <= 0 || drive.SectorSize <= 0))
        { Complete(C.BlownFormat); return; }
        _positioningOffset = byteOffset;
        _positioningPurpose = purpose;
        _positioningDrive = drive;
        _positioningExpired = false;
        var cylinder = purpose == Acb4000PositioningPurpose.Format ? FormatCylinder
            : checked((int)(byteOffset / drive.SectorSize / drive.SectorsPerTrack / drive.Heads));
        SelectedHead = purpose == Acb4000PositioningPurpose.Format ? FormatHead
            : (int)(byteOffset / drive.SectorSize / drive.SectorsPerTrack % drive.Heads);
        _positioners[Unit].BeginSeek(cylinder - _driveCylinder[Unit], _modeParameters[Unit].StepRate);
        _driveMoving[Unit] = true;
        SetStage(Acb4000Stage.Positioning);
    }

    private void TickPositioningCommand()
    {
        if (Drive is not { } drive || !ReferenceEquals(drive, _positioningDrive) || !_driveReady[Unit])
        {
            _positioners[Unit].Reset();
            _driveMoving[Unit] = false;
            Complete(C.NotReady);
            return;
        }
        var positioning = _positioners[Unit];
        if (_positioningExpired)
        {
            positioning.Reset();
            _driveMoving[Unit] = false;
            Complete(M.NoSeekComplete);
            return;
        }
        if (_driveWriteFault && (_positioningPurpose is Acb4000PositioningPurpose.Write or Acb4000PositioningPurpose.Format))
        {
            positioning.Reset();
            _driveMoving[Unit] = false;
            Complete(C.WriteFault, true);
            return;
        }
        if (!positioning.Complete) return;
        if (positioning.Error != C.NoSense)
        {
            _driveMoving[Unit] = false;
            Complete(positioning.Error);
            return;
        }
        if (_code == Acb4000CommandCode.Rezero || _positioningPurpose == Acb4000PositioningPurpose.Parameters)
            _driveCylinder[Unit] = 0;
        // The final STEP has just changed the mechanics; the current input may still be the old level.
        // Explicit SEEK releases after its pulse train, but data operations must let the owner react.
        if (_positioningPurpose != Acb4000PositioningPurpose.Command
            && (positioning.StepIssued || !_driveSeekComplete[Unit])) return;
        try
        {
            if (_positioningPurpose == Acb4000PositioningPurpose.Parameters)
            {
                StartParameterAcquisition();
                return;
            }
            if (_positioningPurpose == Acb4000PositioningPurpose.Parking)
            {
                _driveMoving[Unit] = false;
                Complete();
                return;
            }
            drive.Seek(_positioningOffset);
            switch (_positioningPurpose)
            {
                case Acb4000PositioningPurpose.Read:
                    StartReadChannel(verify: false);
                    break;
                case Acb4000PositioningPurpose.Verify:
                    StartReadChannel(verify: true);
                    break;
                case Acb4000PositioningPurpose.Write:
                    WritePositionedBlock();
                    break;
                case Acb4000PositioningPurpose.Format:
                    StartFormatTrack();
                    break;
                default:
                    Complete();
                    break;
            }
        }
        catch (Exception error) when (StorageFailure(error))
        {
            _driveMoving[Unit] = false;
            Complete(M.NoSeekComplete);
        }
    }

    private void ResetPositioners()
    {
        foreach (var positioning in _positioners) positioning.Reset();
        Array.Clear(_driveMoving);
        _positioningOffset = 0;
        _positioningDrive = null;
        _positioningPurpose = Acb4000PositioningPurpose.Command;
        _positioningExpired = false;
        // Neither the physical head position nor the externally supplied levels are reset by the controller.
    }
}
