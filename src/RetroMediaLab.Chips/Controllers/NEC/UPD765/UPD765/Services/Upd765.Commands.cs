using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private bool _drivePreparationWaiting;

    private static int CommandLength(Upd765Command command) => command switch
    {
        Upd765Command.Specify or Upd765Command.Seek => C.SpecifyCommandBytes,
        Upd765Command.SenseDriveStatus or Upd765Command.Recalibrate or Upd765Command.ReadId => C.DriveCommandBytes,
        Upd765Command.ReadData or Upd765Command.WriteData or Upd765Command.ReadTrack
            or Upd765Command.ReadDeleted or Upd765Command.WriteDeleted or Upd765Command.ScanEqual
            or Upd765Command.ScanLowEqual or Upd765Command.ScanHighEqual => C.CommandBytes,
        Upd765Command.FormatTrack => C.FormatCommandBytes,
        _ => C.SingleCommandBytes
    };

    private void ExecuteCommand()
    {
        var generation = _generation;
        if (_drives.BusyMask != 0 && _command is not (Upd765Command.Seek or Upd765Command.Recalibrate
            or Upd765Command.SenseInterrupt or Upd765Command.SenseDriveStatus))
        { Result([C.InvalidCommand]); return; }
        switch (_command)
        {
            case Upd765Command.Specify:
                _drives.Specify(_parameters[C.SpecifyRatesParameter], _parameters[C.SpecifyLoadParameter]);
                _phase = Upd765Phase.Idle;
                break;
            case Upd765Command.SenseDriveStatus:
                _phase = Upd765Phase.SenseDrive;
                _drives.SenseDriveStatus(_parameters[C.UnitParameter]);
                break;
            case Upd765Command.SenseInterrupt:
                if (_drives.TrySenseInterrupt(out var status, out var cylinder)) Result([status, cylinder]);
                else Result([C.InvalidCommand]);
                break;
            case Upd765Command.Seek:
                ExecuteSeek(recalibrate: false);
                break;
            case Upd765Command.Recalibrate:
                ExecuteSeek(recalibrate: true);
                break;
            case Upd765Command.Version:
                Result([_model == Upd765Model.Upd765A ? C.InvalidCommand : C.VersionB]);
                break;
            case Upd765Command.ReadId:
            case Upd765Command.ReadTrack:
            case Upd765Command.FormatTrack:
            case Upd765Command.ReadData:
            case Upd765Command.ReadDeleted:
            case Upd765Command.WriteData:
            case Upd765Command.WriteDeleted:
            case Upd765Command.ScanEqual:
            case Upd765Command.ScanLowEqual:
            case Upd765Command.ScanHighEqual:
                BeginDataCommand();
                break;
            default:
                Result([C.InvalidCommand]);
                break;
        }
        if (_generation != generation || _resetAsserted) return;
        if (_phase == Upd765Phase.Idle) _drives.PollingEnabled = true;
        UpdateInterrupt();
    }

    private void ExecuteSeek(bool recalibrate)
    {
        var generation = _generation;
        var accepted = recalibrate ? _drives.Recalibrate(_parameters[C.UnitParameter])
            : _drives.Seek(_parameters[C.UnitParameter], _parameters[C.SeekCylinderParameter]);
        if (_generation != generation || _resetAsserted) return;
        if (!accepted) { Result([C.InvalidCommand]); return; }
        // AN10 p. 7: HDL becomes inactive during Seek. A later data command
        // must reload the heads, even when the previous HUT has not expired.
        Array.Clear(_loaded);
        Array.Clear(_unload);
        _phase = Upd765Phase.Idle;
        DriveSignalsChanged?.Invoke();
    }

    private void BeginDataCommand()
    {
        _mfmOutput = (_parameters[0] & C.Mfm) != 0;
        _unit = _parameters[C.UnitParameter] & C.UnitMask;
        _head = (_parameters[C.UnitParameter] & C.HeadMask) == 0 ? 0 : 1;
        _cylinder = _command == Upd765Command.ReadId || Formatting ? _drives.PresentCylinder(_unit) : _parameters[C.CylinderParameter];
        _logicalHead = _command == Upd765Command.ReadId || Formatting ? (byte)_head : _parameters[C.HeadParameter];
        _record = _command == Upd765Command.ReadId || Formatting ? (byte)0 : _parameters[C.RecordParameter];
        _size = Formatting ? _parameters[C.FormatSizeParameter] : _command == Upd765Command.ReadId ? (byte)0 : _parameters[C.SizeParameter];
        _terminalCount = false;
        _dataError = false;
        _controlMark = false;
        _trackFields = 0;
        _trackStatus1 = _trackStatus2 = 0;
        _offset = _length = 0;
        _hostOffset = 0;
        _hostDataReady = false;
        _stickyDrq = false;
        _drives.PollingEnabled = false;
        if (_command != Upd765Command.ReadId && _size > C.MaximumSizeCode)
        { FinishData(C.NoData); return; }
        if (Formatting && _parameters[C.FormatCountParameter] == 0) { FinishData(C.NoData); return; }
        _unload[_unit] = 0;
        _phase = Upd765Phase.DrivePrepare;
        _drivePreparationWaiting = _drives.SignalsBusy;
        _delay = _drivePreparationWaiting ? _drives.NextSignalTicks : C.FaultResetClocks;
        if (!_drivePreparationWaiting) StartFaultReset();
    }

    private void StartFaultReset()
    {
        var generation = _generation;
        DriveSignalsChanged?.Invoke();
        if (_generation != generation || _resetAsserted) return;
        if (!Ready()) { FinishData(status: C.Abnormal | C.NotReady); return; }
        if (WriteCommand && (_statusPins(_unit) & C.WriteProtectedPin) != 0)
        { FinishData(C.WriteProtected); return; }
        SetFaultReset(true);
    }

    private void CompleteDrivePreparation()
    {
        if (_drivePreparationWaiting)
        {
            _drivePreparationWaiting = _drives.SignalsBusy;
            _delay = _drivePreparationWaiting ? _drives.NextSignalTicks : C.FaultResetClocks;
            if (!_drivePreparationWaiting) StartFaultReset();
            return;
        }
        var generation = _generation;
        SetFaultReset(false);
        if (_generation != generation || _resetAsserted) return;
        if ((_statusPins(_unit) & C.FaultPin) != 0)
        { FinishData(status: C.Abnormal | C.EquipmentCheck); return; }
        if (_loaded[_unit]) SearchSector();
        else
        {
            _loaded[_unit] = true;
            _phase = Upd765Phase.HeadLoad;
            _delay = Math.Max(1, _drives.HeadLoadTicks);
        }
    }

    private void FinishData(byte error = 0, byte detail = 0, byte status = 0)
    {
        var code = (byte)(_unit | (_head == 0 ? 0 : C.HeadMask) | status
            | (error != 0 || (detail & ~C.ScanEqualHit) != 0 ? C.Abnormal : 0));
        var generation = _generation;
        SetFaultReset(false);
        if (generation != _generation || _resetAsserted) return;
        _writer.SetGate(false);
        if (generation != _generation || _resetAsserted) return;
        _writePhase = Upd765WritePhase.Idle;
        _writeGapCells = 0;
        _unload[_unit] = _drives.HeadUnloadTicks;
        _drives.PollingEnabled = true;
        Result([code, error, detail, _cylinder, _logicalHead, _record, _size], interrupt: true);
    }
}
