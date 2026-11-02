using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
:RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Interfaces.IUpd765{
    private static int CommandLength(Upd765Command command) => command switch
    {
        Upd765Command.Specify or Upd765Command.Seek => 3,
        Upd765Command.SenseDriveStatus or Upd765Command.Recalibrate or Upd765Command.ReadId => 2,
        Upd765Command.ReadData or Upd765Command.WriteData or Upd765Command.ReadTrack
            or Upd765Command.ReadDeleted or Upd765Command.WriteDeleted or Upd765Command.ScanEqual
            or Upd765Command.ScanLowEqual or Upd765Command.ScanHighEqual => C.CommandBytes,
        Upd765Command.FormatTrack => 6,
        _ => 1
    };

    private void ExecuteCommand()
    {
        if (_drives.BusyMask != 0 && _command is not (Upd765Command.Seek or Upd765Command.Recalibrate
            or Upd765Command.SenseInterrupt or Upd765Command.SenseDriveStatus))
        { Result([C.InvalidCommand]); return; }
        switch (_command)
        {
            case Upd765Command.Specify:
                _drives.Specify(_parameters[1], _parameters[2]);
                _phase = Upd765Phase.Idle;
                break;
            case Upd765Command.SenseDriveStatus:
                Result([_drives.SenseDriveStatus(_parameters[C.UnitParameter])]);
                break;
            case Upd765Command.SenseInterrupt:
                if (_drives.TrySenseInterrupt(out var status, out var cylinder)) Result([status, cylinder]);
                else Result([C.InvalidCommand]);
                break;
            case Upd765Command.Seek:
                if (_drives.Seek(_parameters[1], _parameters[2])) _phase = Upd765Phase.Idle;
                else Result([C.InvalidCommand]);
                break;
            case Upd765Command.Recalibrate:
                if (_drives.Recalibrate(_parameters[C.UnitParameter])) _phase = Upd765Phase.Idle;
                else Result([C.InvalidCommand]);
                break;
            case Upd765Command.Version:
                Result([_model == Upd765Model.Upd72065B ? C.Version72065B : C.InvalidCommand]);
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
        UpdateInterrupt();
    }

    private void BeginDataCommand()
    {
        _unit = _parameters[C.UnitParameter] & C.UnitMask;
        _head = (_parameters[C.UnitParameter] & C.HeadMask) == 0 ? 0 : 1;
        _cylinder = _command == Upd765Command.ReadId || Formatting ? _drives.PresentCylinder(_unit) : _parameters[C.CylinderParameter];
        _logicalHead = _command == Upd765Command.ReadId || Formatting ? (byte)_head : _parameters[C.HeadParameter];
        _record = _command == Upd765Command.ReadId || Formatting ? (byte)0 : _parameters[C.RecordParameter];
        _size = Formatting ? _parameters[C.FormatSizeParameter] : _command == Upd765Command.ReadId ? (byte)0 : _parameters[C.SizeParameter];
        _image = _disk(_unit);
        _terminalCount = false;
        _dataError = false;
        _controlMark = false;
        _trackFields = 0;
        _trackStatus1 = _trackStatus2 = 0;
        _offset = _length = 0;
        _trackCylinder = _trackHead = -1;
        _trackRecords = [];
        _trackSectors = [];
        _formatSectors.Clear();
        _drives.PollingEnabled = false;
        if (_image is null || !Ready()) { FinishData(status: C.Abnormal | C.NotReady); return; }
        if ((_statusPins(_unit) & C.FaultPin) != 0)
        { FinishData(status: C.Abnormal | C.EquipmentCheck); return; }
        if (WriteCommand
            && (_image.IsReadOnly || (_statusPins(_unit) & C.WriteProtectedPin) != 0))
        { FinishData(C.WriteProtected); return; }
        if (_command != Upd765Command.ReadId && _size > C.MaximumSizeCode)
        { FinishData(C.NoData); return; }
        if (Formatting && _parameters[C.FormatCountParameter] == 0) { FinishData(C.NoData); return; }
        _image.Mechanics.SetActivity(!WriteCommand, WriteCommand);
        _unload[_unit] = 0;
        if (_loaded[_unit]) SearchSector();
        else
        {
            _phase = Upd765Phase.HeadLoad;
            _delay = Math.Max(1, _drives.HeadLoadTicks);
        }
    }

    private void FinishData(byte error = 0, byte detail = 0, byte status = 0)
    {
        var code = (byte)(_unit | (_head == 0 ? 0 : C.HeadMask) | status
            | (error != 0 || (detail & ~C.ScanEqualHit) != 0 ? C.Abnormal : 0));
        _image?.Mechanics.SetActivity(false, false);
        _image = null;
        _trackRecords = [];
        _trackSectors = [];
        _formatSectors.Clear();
        _unload[_unit] = _drives.HeadUnloadTicks;
        _drives.PollingEnabled = true;
        Result([code, error, detail, _cylinder, _logicalHead, _record, _size], interrupt: true);
    }
}
